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

	//Names kept like this so that they are the same as tileset
	public enum Cell
	{
		GROUND = 0,
		VEGETATION,
		SPIKES,
		MAYBE_GROUND,
		MAYBE_BUSH,
		MAYBE_TREE,
		MAYBE_SPIKES
	}

	//const CELL_MAP = {
	//    Cell.GROUND: {"chance": 1.0, "cell": [[Cell.GROUND]], "size": Vector2.ONE},
	//    Cell.VEGETATION: {"chance": 1.0, "cell": [[Cell.VEGETATION]], "size": Vector2.ONE},
	//    Cell.SPIKES: {"chance": 1.0, "cell": [[Cell.SPIKES]], "size": Vector2.ONE},
	//    Cell.MAYBE_GROUND: {"chance": 0.7, "cell": [[Cell.GROUND]], "size": Vector2.ONE},
	//    Cell.MAYBE_BUSH: {"chance": 0.3, "cell": [[Cell.VEGETATION]], "size": Vector2.ONE},
	//    Cell.MAYBE_TREE:
	//    {
	//        "chance": 0.8,
	//        "cell": [[Cell.VEGETATION, Cell.VEGETATION], [Cell.VEGETATION, Cell.VEGETATION]],
	//        "size": 2 * Vector2.ONE
	//    },
	//    Cell.MAYBE_SPIKES: {"chance": 0.5, "cell": [[Cell.SPIKES]], "size": Vector2.ONE}
	//}

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

	//var data := {"objects": [], "tilemap": []}
 //   for object in room.get_children():
 //       data.objects.push_back(object)

 //   for v in room.get_used_cells():
 //       var mapping: Dictionary = CELL_MAP[room.get_cellv(v)]
 //       if _rng.randf() > mapping.chance:
 //           continue

 //       for x in range(mapping.size.x):
 //           for y in range(mapping.size.y):
 //               data.tilemap.push_back({"offset": v + Vector2(x, y), "cell": mapping.cell[x][y]})
 //   return data

	}
}

public class RoomData
{
	public Vector2 Offset { get; set; }
	public int Cell { get; set; }
}
