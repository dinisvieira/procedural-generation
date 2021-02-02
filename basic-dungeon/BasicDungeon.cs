using Godot;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

public class BasicDungeon : Node2D
{
	
	[Export]
	public Vector2 LevelSize = new Vector2(100,80);
	
	[Export]
	public Vector2 RoomsSize = new Vector2(10,14); //x represents the minimum number of tiles and y the maximum number of tiles
	
	[Export]
	public int RoomsMax = 15;
	
	public TileMap Level;
	public Camera2D Camera;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Level = GetNode("Level") as TileMap;
		Camera = GetNode("Camera2D") as Camera2D;

		SetupCamera();
		Generate();
	}

	private void SetupCamera()
	{
		Camera.Position = Level.MapToWorld(LevelSize / 2);
		var zoom = Math.Max(LevelSize.x, LevelSize.y) / 8;
		Camera.Zoom = new Vector2(zoom, zoom);
	}

	private void Generate()
	{
		Level.Clear();
		var rooms = GenerateData();
		foreach (var room in rooms)
		{
			Level.SetCellv(room, 0);
		}
	}

	private IList<Vector2> GenerateData()
	{
		var rng = new RandomNumberGenerator();
		rng.Randomize();

		var dataDict = new Dictionary<Vector2, object>();
		var rooms = new List<Rect2>();

		Rect2? previousRoom = null;
		for (int i = 0; i < RoomsMax; i++)
		{
			var room = GetRandomRoom(rng);
			if (Intersects(rooms, room))
			{
				continue;
			}

			AddRoom(dataDict, rooms, room);

			if (rooms.Count > 1 && previousRoom.HasValue)
			{
				AddConnection(rng, dataDict, previousRoom.Value, room);
			}
			previousRoom = room;
		}

		return dataDict.Keys.ToList();
	}

	private Rect2 GetRandomRoom(RandomNumberGenerator rng)
	{
		var width = rng.RandiRange((int)RoomsSize.x, (int)RoomsSize.y);
		var height = rng.RandiRange((int)RoomsSize.x, (int)RoomsSize.y);

		var x = rng.RandiRange(0, (int)LevelSize.x - width - 1);
		var y = rng.RandiRange(0, (int)LevelSize.y - height - 1);

		return new Rect2(x, y, width, height);
	}

	private bool Intersects(List<Rect2> rooms, Rect2 room)
	{
		foreach (var r in rooms)
		{
			if (r.Intersects(room))
			{
				return true;
			}
		}

		return false;
	}

	private void AddRoom(Dictionary<Vector2, object> dataDict, List<Rect2> rooms, Rect2 room)
	{
		rooms.Add(room);
		var xStart = (int)room.Position.x;
		var xEnd = (int)room.End.x;
		var yStart = (int)room.Position.y;
		var yEnd = (int)room.End.y;

		for (int x = xStart; x <= xEnd; x++)
		{
			for (int y = yStart; y <= yEnd; y++)
			{
				var vector = new Vector2(x, y);
				dataDict[vector] = null;
			}
		}
	}

	private void AddConnection(RandomNumberGenerator rng, Dictionary<Vector2, object> dataDict, Rect2 room1, Rect2 room2)
	{
		var roomCenter1 = (room1.Position + room1.End) / 2;
		var roomCenter2 = (room2.Position + room2.End) / 2;

		if (rng.RandiRange(0, 1) == 0)
		{
			AddCorridor(dataDict, roomCenter1.x, roomCenter2.x, roomCenter1.y, Vector2.Axis.X);
			AddCorridor(dataDict, roomCenter1.y, roomCenter2.y, roomCenter2.x, Vector2.Axis.Y);
		}
		else
		{
			AddCorridor(dataDict, roomCenter1.y, roomCenter2.y, roomCenter1.x, Vector2.Axis.Y);
			AddCorridor(dataDict, roomCenter1.x, roomCenter2.x, roomCenter2.y, Vector2.Axis.X);
		}
	}

	private void AddCorridor(Dictionary<Vector2, object> dataDict, float start, float end, float constant, Vector2.Axis axis)
	{
		var minVal = (int)Math.Min(start, end);
		var maxVal = (int)Math.Max(start, end);

		for (int t = minVal; t <= maxVal; t++)
		{
			var point = Vector2.Zero;
			if (axis == Vector2.Axis.X)
			{
				point = new Vector2(t, constant);
			}
			else
			{
				point = new Vector2(constant, t);
			}

			dataDict[point] = null;
		}
	}
}
