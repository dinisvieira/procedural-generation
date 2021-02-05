using Godot;
using System;
using System.Collections;
using System.Collections.Generic;

public class Room : RigidBody2D, IEnumerator<Vector2>, IEnumerable<Vector2>, IEnumerable
{
	//Used to test that the body didn't move over consecutive frames.
	public const int ConsecutiveMaxEqualities = 10;

	[Signal]
	public delegate void SleepingStateChanged();

	[Export(PropertyHint.Length, "Radius of the circle in which we randomly place the room")]
	public int Radius = 600;

	[Export(PropertyHint.Range, "Interval to generate a random width and height for the room, in tiles.")]
	public Vector2 RoomSize = new Vector2(2,9);

	public Vector2 Size = Vector2.Zero;
	public CollisionShape2D CollisionShape;

	private TileMap _level = null;
	private RandomNumberGenerator _rng = null;
	private Transform2D _previousXForm = new Transform2D();
	private int _consecutiveEqualities = 0;

	private float _area = 0;
	private int _iterIndex = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		CollisionShape = GetNode("CollisionShape2D") as CollisionShape2D;

		Position = Utils.GetRngPointInCircle(_rng, Radius);

		var w = _rng.RandiRange((int)RoomSize.x, (int)RoomSize.y);
		var h = _rng.RandiRange((int)RoomSize.x, (int)RoomSize.y);

		_area = w * h;

		Size = new Vector2(w, h);

		var rectangleShape2D = CollisionShape.Shape as RectangleShape2D;
		rectangleShape2D.Extents = _level.MapToWorld(Size) / 2;
	}

	public void Setup(RandomNumberGenerator rng, TileMap level)
	{
		_rng = rng;
		_level = level;
	}

	public override void _IntegrateForces(Physics2DDirectBodyState state)
	{
		if (Mode == ModeEnum.Static)
		{
			GD.Print("Static");
			return;
		}

		if (Utils.IsApproxEqual(_previousXForm.origin, state.Transform.origin))
		{
			GD.Print("_consecutiveEqualities");
			_consecutiveEqualities+=1;
		}

		if (_consecutiveEqualities > ConsecutiveMaxEqualities)
		{
			GD.Print("CallDeferred");
			SetDeferred("mode", ModeEnum.Static);
			CallDeferred("emit_signal", nameof(SleepingStateChanged));
		}

		_previousXForm = state.Transform;
	}

	public IEnumerator<Vector2> GetEnumerator()
	{
		return (IEnumerator<Vector2>)this;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return (IEnumerator)this;
	}

	// Returns the coordinates of a tile in the `_level` tilemap that our room overlaps.
	// Running over the entire loop yields all the tiles we should fill
	// to draw the complete room.
	public Vector2 Current 
	{
		get
		{
			var width = (int)Size.x;
			var offset = Utils.IndexToXY(width, _iterIndex);
			return _level.WorldToMap(Position) - Size / 2 + offset;
		}
	}

	object IEnumerator.Current
	{
		get
		{
			var width = (int)Size.x;
			var offset = Utils.IndexToXY(width, _iterIndex);
			return _level.WorldToMap(Position) - Size / 2 + offset;
		}
	}
	
	public bool MoveNext()
	{
		_iterIndex++;
		return _iterIndex < _area;
	}

	public void Reset()
	{
		_iterIndex = 0;
	}
}
