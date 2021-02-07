using Godot;
using System;
using System.Collections.Generic;

public class Rooms : Node2D
{
	public enum Type
	{
		SIDE = 0,
		LR,
		LRB,
		LRT,
		LRTB
	}

	public static List<Type> BottomOpened = new List<Type>() { Type.LRB, Type.LRTB };
	public static List<Type> BottomClosed = new List<Type>() { Type.LR, Type.LRT };

	public static Vector2 RoomSize = Vector2.Zero;
	public static Vector2 CellSize = Vector2.Zero;

	private RandomNumberGenerator _rng = new RandomNumberGenerator();

	public override void _Notification(int what)
	{
		if (what == Node.NotificationInstanced)
		{
			_rng.Randomize();
			var side = GetNode("Side") as Node2D;

			var room = side.GetChild(0) as TileMap;
			RoomSize = room.GetUsedRect().Size;
			CellSize = room.CellSize;
		}
	}

	public List<RoomData> GetRoomData(Type type)
	{
		var group = GetChild((int)type);
		var index = _rng.RandiRange(0, group.GetChildCount() - 1);
		var room = group.GetChild(index) as TileMap;

		var data = new List<RoomData>();
		foreach (Vector2 usedCell in room.GetUsedCells())
		{
			var cell = room.GetCellv(usedCell);
			var offset = usedCell;
			data.Add(new RoomData()
			{
				Offset = offset,
				Cell = cell
			});
		}

		return data;
	}
}

public class RoomData
{
	public Vector2 Offset { get; set; }
	public int Cell { get; set; }
}
