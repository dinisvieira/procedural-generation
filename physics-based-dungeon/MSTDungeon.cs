using Godot;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Threading.Tasks;

public class MSTDungeon : Node2D
{

	//Emitted when all the rooms stabilized.
	[Signal]
	delegate void RoomsPlaced();

	private PackedScene RoomResource = (PackedScene)GD.Load("res://Room.tscn");

	//Maximum number of generated rooms.
	[Export]
	public int MaxRooms = 60;

	//Controls the number of paths we add to the dungeon after generating it, limiting player backtracking.
	[Export]
	public float ReconnectionFactor = 0.025f;

	private RandomNumberGenerator _rng = new RandomNumberGenerator();
	private Dictionary<Vector2, object> _data; //TODO: Don't know type yet
	private AStar2D _path = null;
	private int _sleepingRooms = 0;
	private Vector2 _meanRoomSize = Vector2.Zero;
	private Node2D Rooms;
	private TileMap Level;

	// Called when the node enters the scene tree for the first time.
	public override async void _Ready()
	{
		Rooms = GetNode("Rooms") as Node2D;
		Level = GetNode("Level") as TileMap;

		_rng.Randomize();
		await Generate();
	}

	//This is for visual feedback. We just re-render the rooms every frame.
	public override void _Process(float delta)
	{
		Level.Clear();

		foreach (object roomObj in Rooms.GetChildren())
		{
			if (roomObj is Room room)
			{
				foreach (Vector2 offset in room)
				{
					Level.SetCellv(offset, 0);
				}
			}
			else if (roomObj is RigidBody2D body)
			{
				//var roomCast = body as Room;
				//foreach (Vector2 offset in roomCast)
				//{
				//	Level.SetCellv(offset, 0);
				//}
				//GD.Print("RoomObj is RigidBody2D");
			}
			else
			{
				GD.Print("RoomObj is not recognized. Type is " + roomObj.GetType());
			}
		}
	}

	//Places the rooms and starts the physics simulation. Once the simulation is done
	//("rooms_placed" gets emitted), it continues by assigning tiles in the Level node.
	private async Task Generate()
	{
		for (int i = 1; i <= MaxRooms; i++)
		{
			var room = RoomResource.Instance() as Room;
			room.Connect(nameof(Room.SleepingStateChanged), this, nameof(_on_Room_sleeping_state_changed), room);
			room.Setup(_rng, Level);
			Rooms.AddChild(room);

			_meanRoomSize += room.Size;
		}
		_meanRoomSize /= Rooms.GetChildCount();

		//Wait for all rooms to be positioned in the game world.
		await ToSignal(this, nameof(RoomsPlaced));

		Rooms.QueueFree();

		//Draws the tiles on the `level` tilemap.
		Level.Clear();

		foreach (var entry in _data)
		{
			Level.SetCellv(entry.Key, 0);
		}
	}

	// Called every time stabilizes (mode changes to RigidBody2D.MODE_STATIC).
	//   
	// Once all rooms have stabilized it calculates a playable dungeon `_path` using the MST
	// algorithm. Based on the calculated `_path`, it populates `_data` with room and corridor tile
	// positions.
	//   
	// It emits the "rooms_placed" signal when it finishes so we can begin the tileset placement.
	private void _on_Room_sleeping_state_changed()
	{

		GD.Print("_on_Room_sleeping_state_changed");

		_sleepingRooms++;
		if (_sleepingRooms < MaxRooms)
		{
			return;
		}

		var mainRooms = new List<Room>();
		var mainRoomsPositions = new List<Vector2>();

		foreach (Room room in Rooms.GetChildren())
		{
			if (IsMainRoom(room))
			{
				mainRooms.Add(room);
				mainRoomsPositions.Add(room.Position);
			}
		}

		_path = Utils.MST(mainRoomsPositions);

		foreach (int point1Id in _path.GetPoints())
		{
			foreach (int point2Id in _path.GetPoints())
			{
				if(    point1Id != point2Id
					&& !_path.ArePointsConnected(point1Id, point2Id)
					&& _rng.Randf() < ReconnectionFactor)
				{
					_path.ConnectPoints(point1Id, point2Id);
				}
			}
		}

		foreach (Room room in mainRooms)
		{
			AddRoom(room);
		}
		AddCorridors();

		SetProcess(false);
		EmitSignal(nameof(RoomsPlaced));
	}

	private bool IsMainRoom(Room room)
	{
		return room.Size.x > _meanRoomSize.x && room.Size.y > _meanRoomSize.y;
	}

	//Adds room tile positions to `_data`.
	private void AddRoom(Room room)
	{
		foreach (var offset in room)
		{
			_data[offset] = null;
		}
	}

	// Adds both secondary room and corridor tile positions to `_data`. Secondary rooms are the ones
	// intersecting the corridors.
	private void AddCorridors()
	{
		//Stores existing connections in its keys.
		var connected = new Dictionary<Vector2, object>();

		//Checks if points are connected by a corridor. If not, adds a corridor.

		foreach (int point1Id in _path.GetPoints())
		{
			foreach (var point2Id in _path.GetPointConnections(point1Id))
			{
				var point1 = _path.GetPointPosition(point1Id);
				var point2 = _path.GetPointPosition(point2Id);

				if (connected.ContainsKey(new Vector2(point1Id, point2Id)))
				{
					continue;;
				}

				point1 = Level.WorldToMap(point1);
				point2 = Level.WorldToMap(point2);

				AddCorridor((int)point1.x, (int)point2.x, (int)point1.y, Vector2.Axis.X);
				AddCorridor((int)point1.x, (int)point2.x, (int)point2.y, Vector2.Axis.Y);

				//Stores the connection between point 1 and 2.
				connected[new Vector2(point1Id, point2Id)] = null;
				connected[new Vector2(point2Id, point1Id)] = null;
			}
		}
	}

	// Adds a specific corridor (defined by the input parameters) to `_data`. It also adds all
	// secondary rooms intersecting the corridor path.
	private void AddCorridor(int start, int end, int constant, Vector2.Axis axis)
	{
		var t = Math.Min(start, end);
		while (t <= Math.Max(start, end))
		{
			var point = Vector2.Zero;
			if (axis == Vector2.Axis.X)
			{
				point = new Vector2(t, constant);
			}
			else if (axis == Vector2.Axis.Y)
			{
				point = new Vector2(constant, t);
			}

			t++;

			foreach (Room room in Rooms.GetChildren())
			{
				if(IsMainRoom(room)) { continue; }

				var topLeft = Level.WorldToMap(room.Position - room.Size / 2);
				var bottomright = Level.WorldToMap(room.Position + room.Size / 2);

				if (   topLeft.x <= point.x 
					&& point.x < bottomright.x
					&& topLeft.y <= point.y
					&& point.y < bottomright.y)
				{
					AddRoom(room);

					t = axis == Vector2.Axis.X ? (int)bottomright.x : (int)bottomright.y;
				}

				_data[point] = null;
			}
		}
	}
}
