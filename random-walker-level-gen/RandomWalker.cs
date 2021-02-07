using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Threading.Tasks;

public class RandomWalker : Node2D
{

	[Signal]
	public delegate void PathCompleted();

	[Export]
	private PackedScene RoomsScene = (PackedScene)GD.Load("res://Rooms.tscn");

	[Export]
	public Vector2 GridSize = new Vector2(8, 6);

	public static List<Vector2> Step = new List<Vector2>() { Vector2.Left, Vector2.Left, Vector2.Right, Vector2.Right, Vector2.Down }; //Left and Right are repeated so that there is a bigger change of that one being chosen randomly from this list

	private Rooms _rooms = null;
	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private WalkerState _state = null;
	private float _horizontalChance = 0;

	public Camera2D Camera;
	public Timer Timer;
	public TileMap Level;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Camera = GetNode("Camera2D") as Camera2D;
		Timer = GetNode("Timer") as Timer;
		Level = GetNode("Level") as TileMap;

		_rng.Randomize();

		_rooms = RoomsScene.Instance() as Rooms;
		_horizontalChance = 1.0f - (float)Step.Count(s => s == Vector2.Down) / Step.Count;

		SetupCamera();
		_ = GenerateLevel();

		GD.Print("_Ready finished");
	}

	private async Task GenerateLevel()
	{
		try
		{
			Reset();
			UpdateStartPosition();

			while (_state.Offset.y < GridSize.y)
			{
				UpdateRoomType();
				UpdateNextPosition();
				UpdateDownCounter();
			}

			PlaceWalls();
			await PlacePathRooms();
			PlaceSideRooms();
		}
		catch (Exception ex)
		{
			GD.PrintErr(ex.Message);
			GD.Print(ex);
		}
	}

	private void PlaceSideRooms()
	{
		//await ToSignal(this, nameof(PathCompleted));
		var roomsMaxIndex = Enum.GetNames(typeof(Rooms.Type)).Length - 1;

		GD.Print("PlaceSideRooms.roomsMaxIndex: " + roomsMaxIndex);

		foreach (var key in _state.EmptyCells.Keys)
		{
			var type = _rng.RandiRange(0, roomsMaxIndex);
			CopyRoom(key, (Rooms.Type)type);
		}
	}

	private async Task PlacePathRooms()
	{
		foreach (var path in _state.Path)
		{
			await ToSignal(Timer, "timeout");
			CopyRoom(path.Offset, path.Type);
		}

		GD.Print("Emit PathCompleted");
		EmitSignal(nameof(PathCompleted));
	}

	private void CopyRoom(Vector2 offset, Rooms.Type type)
	{
		var mapOffset = GridToMap(offset);
		var data = _rooms.GetRoomData(type); //var data: Array = _rooms.get_room_data(type)

		foreach (var roomData in data)
		{
			Level.SetCellv(mapOffset + roomData.Offset, roomData.Cell);
		}
	}

	private void PlaceWalls(int type = 0)
	{
		var cellGridSize = GridToMap(GridSize);

		for (int y = -1; y <= cellGridSize.y; y++)
		{
			Level.SetCell(-1, y, type);
			Level.SetCell((int)cellGridSize.x, y, type);
		}

		for (int x = -1; x <= cellGridSize.x; x++)
		{
			Level.SetCell(x, -1, type);
			Level.SetCell(x, (int)cellGridSize.y, type);
		}
	}

	private void UpdateDownCounter()
	{
		_state.DownCounter = _state.Delta.IsEqualApprox(Vector2.Down) ? _state.DownCounter + 1 : 0;
	}

	private void UpdateNextPosition()
	{
		if (_state.RandomIndex < 0)
		{
			_state.RandomIndex = _rng.RandiRange(0, Step.Count - 1);
		}

		_state.Delta = Step[_state.RandomIndex];

		var horizontalChance = _rng.Randf();
		if (_state.Delta.IsEqualApprox(Vector2.Left))
		{
			_state.RandomIndex = _state.Offset.x > 1 && horizontalChance < _horizontalChance ? 0 : 4;
		}
		else if (_state.Delta.IsEqualApprox(Vector2.Right))
		{
			_state.RandomIndex = _state.Offset.x < GridSize.x - 1 && horizontalChance < _horizontalChance ? 2 : 4;
		}
		else
		{
			if (_state.Offset.x > 0 && _state.Offset.x < (GridSize.x - 1))
			{
				_state.RandomIndex = _rng.RandiRange(0, 4);
			}
			else if (_state.Offset.x == 0)
			{
				_state.RandomIndex = horizontalChance < _horizontalChance ? 2 : 4;
			}
			else if (_state.Offset.x == (GridSize.x - 1))
			{
				_state.RandomIndex = horizontalChance < _horizontalChance ? 0 : 4;
			}
		}

		_state.Delta = Step[_state.RandomIndex];
		_state.Offset += _state.Delta;
	}

	private void UpdateRoomType()
	{
		if (_state.Path != null && _state.Path.Any())
		{
			var last = _state.Path.LastOrDefault();
			if (Rooms.BottomClosed.Contains(last.Type) && _state.Delta.IsEqualApprox(Vector2.Down))
			{
				var index = _rng.RandiRange(0, Rooms.BottomOpened.Count - 1);

				var typeB = Rooms.Type.LRTB;
				if (_state.DownCounter < 2)
				{
					typeB = Rooms.BottomOpened[index];
				}
				
				last.Type = typeB;
			}
		}

		var typesCount = Enum.GetNames(typeof(Rooms.Type)).Length;
		var type = (Rooms.Type)(_rng.RandiRange(1, typesCount - 1));
		if (_state.Delta.IsEqualApprox(Vector2.Down))
		{
			type = Rooms.Type.LRT;
		}

		_state.EmptyCells.Remove(_state.Offset);
		_state.Path.Add(new WalkerPath()
		{
			Offset = _state.Offset,
			Type = type
		});
	}

	private void Reset()
	{
		_state = new WalkerState()
		{
			RandomIndex = -1, //Random index to pick a direction from the STEP constant array from last lesson
			Offset = Vector2.Zero, //Current position on the grid 
			Delta = Vector2.Zero, //Direction to increment the offset key above
			DownCounter = 0, //The number of times the walker moved down without interruption
			Path = new List<WalkerPath>(), //The level's unobstructed path
			EmptyCells = new Dictionary<Vector2, int>() //Coordinates of the cells we haven't populated yet
		};

		for (int x = 0; x < GridSize.x; x++)
		{
			for (int y = 0; y < GridSize.y; y++)
			{
				_state.EmptyCells[new Vector2(x, y)] = 0;
			}
		}
	}

	private void UpdateStartPosition()
	{
		var x = _rng.RandiRange(0, (int)(GridSize.x - 1));
		_state.Offset = new Vector2(x, 0);
	}

	private void SetupCamera()
	{
		Vector2 worldSize = GridToWorld(GridSize);
		Camera.Position = worldSize / 2;

		var ratio = worldSize / OS.WindowSize;
		var zoomMax = Math.Max(ratio.x, ratio.y) + 1;
		Camera.Zoom = new Vector2(zoomMax, zoomMax);
	}

	private Vector2 GridToMap(Vector2 vector)
	{
		return Rooms.RoomSize * vector;
	}

	private Vector2 GridToWorld(Vector2 vector)
	{
		return Rooms.CellSize * Rooms.RoomSize * vector;
	}
}

public class WalkerState
{
	public int RandomIndex { get; set; }
	public Vector2 Offset { get; set; }
	public Vector2 Delta { get; set; }
	public int DownCounter { get; set; }
	public List<WalkerPath> Path { get; set; }
	public Dictionary<Vector2, int> EmptyCells { get; set; }
}

public class WalkerPath
{
	public Vector2 Offset { get; set; }
	public Rooms.Type Type { get; set; }
}
