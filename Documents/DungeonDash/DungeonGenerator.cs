// ------------------------------------------------------------------
// DungeonGenerator — BSP-based procedural dungeon generation
//
// Algorithm:
// 1. Recursively split the map into sub-regions (BSP tree)
// 2. Place a random-sized room inside each leaf region
// 3. Connect sibling rooms with L-shaped corridors
// 4. Place doors at corridor-room transitions
// 5. Place stairs in the farthest room from the spawn
//
// This creates organic, interconnected layouts every time.
// ------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace DungeonDash;

public class DungeonGenerator
{
    // --- Generation parameters ---
    private const int MinRoomSize = 5;   // Smallest room dimension (including walls)
    private const int MaxRoomSize = 12;  // Largest room dimension
    private const int MinLeafSize = 8;   // Smallest BSP leaf before stopping split
    private const int MaxLeafSize = 20;  // Largest leaf that MUST be split
    private const int CorridorWidth = 1; // Corridor is 1 tile wide

    private readonly Random _rng;
    private readonly TileMap _map;
    private readonly List<Room> _rooms = new();

    /// <summary>
    /// All rooms placed during generation. Used for spawning entities.
    /// </summary>
    public IReadOnlyList<Room> Rooms => _rooms;

    /// <summary>
    /// The room where the player spawns.
    /// </summary>
    public Room SpawnRoom { get; private set; }

    /// <summary>
    /// The room containing the stairs down to the next level.
    /// </summary>
    public Room StairsRoom { get; private set; }

    public DungeonGenerator(int width, int height, int? seed = null)
    {
        _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        _map = new TileMap(width, height);
    }

    /// <summary>
    /// Generate a complete dungeon and return the resulting TileMap.
    /// </summary>
    public TileMap Generate()
    {
        // Start BSP: the root region covers the entire map (with 1-tile border)
        var rootRegion = new BspNode(1, 1, _map.Width - 2, _map.Height - 2);

        // Step 1: Recursively split the space
        SplitNode(rootRegion);

        // Step 2: Place rooms in leaf nodes
        PlaceRooms(rootRegion);

        // Step 3: Connect sibling rooms with corridors
        ConnectRooms(rootRegion);

        // Step 4: Choose spawn and stairs rooms
        ChooseSpecialRooms();

        // Step 5: Place doors at corridor-room transitions
        PlaceDoors();

        return _map;
    }

    // ==================== BSP SPLITTING ====================

    /// <summary>
    /// BSP node representing a rectangular region of the map.
    /// Leaf nodes contain rooms; internal nodes have two children.
    /// </summary>
    private class BspNode
    {
        public int X, Y, Width, Height;
        public BspNode Left, Right;
        public Room Room;

        public bool IsLeaf => Left == null && Right == null;

        public BspNode(int x, int y, int w, int h)
        {
            X = x; Y = y; Width = w; Height = h;
        }

        /// <summary>
        /// Get all rooms from this node's subtree.
        /// </summary>
        public void CollectRooms(List<Room> rooms)
        {
            if (Room != null)
                rooms.Add(Room);
            Left?.CollectRooms(rooms);
            Right?.CollectRooms(rooms);
        }

        /// <summary>
        /// Find any room in this subtree (for corridor connections).
        /// </summary>
        public Room FindRoom()
        {
            if (Room != null) return Room;
            var leftRoom = Left?.FindRoom();
            if (leftRoom != null) return leftRoom;
            return Right?.FindRoom();
        }
    }

    /// <summary>
    /// Recursively split a node into two children.
    /// Stops when the region is too small to split further.
    /// </summary>
    private void SplitNode(BspNode node)
    {
        // Don't split if already small enough
        if (node.Width <= MaxLeafSize && node.Height <= MaxLeafSize)
        {
            // Random chance to stop splitting early (creates variety)
            if (node.Width <= MinLeafSize * 2 && node.Height <= MinLeafSize * 2)
                return;
            if (_rng.NextDouble() < 0.25)
                return;
        }

        // Choose split direction
        bool splitH; // true = horizontal split (top/bottom), false = vertical (left/right)
        if (node.Width > node.Height * 1.25)
            splitH = false; // Too wide → split vertically
        else if (node.Height > node.Width * 1.25)
            splitH = true;  // Too tall → split horizontally
        else
            splitH = _rng.NextDouble() < 0.5; // Square-ish → random

        if (splitH)
        {
            // Horizontal split: choose Y position
            if (node.Height < MinLeafSize * 2) return; // Can't split
            int splitY = _rng.Next(MinLeafSize, node.Height - MinLeafSize + 1);
            node.Left = new BspNode(node.X, node.Y, node.Width, splitY);
            node.Right = new BspNode(node.X, node.Y + splitY, node.Width, node.Height - splitY);
        }
        else
        {
            // Vertical split: choose X position
            if (node.Width < MinLeafSize * 2) return; // Can't split
            int splitX = _rng.Next(MinLeafSize, node.Width - MinLeafSize + 1);
            node.Left = new BspNode(node.X, node.Y, splitX, node.Height);
            node.Right = new BspNode(node.X + splitX, node.Y, node.Width - splitX, node.Height);
        }

        // Recurse into children
        SplitNode(node.Left);
        SplitNode(node.Right);
    }

    // ==================== ROOM PLACEMENT ====================

    /// <summary>
    /// Place a randomly-sized room inside each leaf node.
    /// </summary>
    private void PlaceRooms(BspNode node)
    {
        if (node.IsLeaf)
        {
            // Random room size within the leaf bounds
            int roomW = _rng.Next(MinRoomSize, Math.Min(MaxRoomSize, node.Width) + 1);
            int roomH = _rng.Next(MinRoomSize, Math.Min(MaxRoomSize, node.Height) + 1);

            // Random position within the leaf
            int roomX = node.X + _rng.Next(0, node.Width - roomW + 1);
            int roomY = node.Y + _rng.Next(0, node.Height - roomH + 1);

            var room = new Room(roomX, roomY, roomW, roomH);
            node.Room = room;
            _rooms.Add(room);

            // Carve the room's floor into the tile map
            CarveRoom(room);
        }
        else
        {
            if (node.Left != null) PlaceRooms(node.Left);
            if (node.Right != null) PlaceRooms(node.Right);
        }
    }

    /// <summary>
    /// Carve a room's inner area as Floor tiles.
    /// </summary>
    private void CarveRoom(Room room)
    {
        for (int x = room.InnerX; x < room.InnerX + room.InnerWidth; x++)
        {
            for (int y = room.InnerY; y < room.InnerY + room.InnerHeight; y++)
            {
                _map.SetTile(x, y, TileType.Floor);
            }
        }
    }

    // ==================== CORRIDOR CONNECTION ====================

    /// <summary>
    /// Connect rooms in sibling BSP nodes with L-shaped corridors.
    /// </summary>
    private void ConnectRooms(BspNode node)
    {
        if (node.IsLeaf) return;

        // First connect children's internal rooms
        if (node.Left != null) ConnectRooms(node.Left);
        if (node.Right != null) ConnectRooms(node.Right);

        // Then connect a room from each child to each other
        if (node.Left != null && node.Right != null)
        {
            var roomA = node.Left.FindRoom();
            var roomB = node.Right.FindRoom();

            if (roomA != null && roomB != null)
            {
                CarveCorridor(roomA.Center, roomB.Center);
            }
        }
    }

    /// <summary>
    /// Carve an L-shaped corridor between two points.
    /// Randomly chooses horizontal-first or vertical-first.
    /// </summary>
    private void CarveCorridor(Microsoft.Xna.Framework.Point from, Microsoft.Xna.Framework.Point to)
    {
        int x = from.X;
        int y = from.Y;

        if (_rng.NextDouble() < 0.5)
        {
            // Horizontal first, then vertical
            CarveHLine(x, to.X, y);
            CarveVLine(y, to.Y, to.X);
        }
        else
        {
            // Vertical first, then horizontal
            CarveVLine(y, to.Y, x);
            CarveHLine(x, to.X, to.Y);
        }
    }

    /// <summary>
    /// Carve a horizontal line of floor tiles.
    /// </summary>
    private void CarveHLine(int x1, int x2, int y)
    {
        int minX = Math.Min(x1, x2);
        int maxX = Math.Max(x1, x2);
        for (int x = minX; x <= maxX; x++)
        {
            if (_map.InBounds(x, y))
                _map.SetTile(x, y, TileType.Floor);
        }
    }

    /// <summary>
    /// Carve a vertical line of floor tiles.
    /// </summary>
    private void CarveVLine(int y1, int y2, int x)
    {
        int minY = Math.Min(y1, y2);
        int maxY = Math.Max(y1, y2);
        for (int y = minY; y <= maxY; y++)
        {
            if (_map.InBounds(x, y))
                _map.SetTile(x, y, TileType.Floor);
        }
    }

    // ==================== SPECIAL ROOMS ====================

    /// <summary>
    /// Pick spawn room (first room) and stairs room (farthest from spawn).
    /// </summary>
    private void ChooseSpecialRooms()
    {
        if (_rooms.Count == 0) return;

        // Spawn in first room
        SpawnRoom = _rooms[0];

        // Stairs in farthest room from spawn
        int maxDist = 0;
        StairsRoom = _rooms[^1]; // default to last

        foreach (var room in _rooms)
        {
            int dist = SpawnRoom.DistanceTo(room);
            if (dist > maxDist)
            {
                maxDist = dist;
                StairsRoom = room;
            }
        }

        // Place stairs tile
        _map.SetTile(StairsRoom.CenterX, StairsRoom.CenterY, TileType.StairsDown);
    }

    // ==================== DOOR PLACEMENT ====================

    /// <summary>
    /// Scan for corridor-room transition points and place doors.
    /// A door candidate is a floor tile that has walls on two opposite
    /// sides (like a doorway) along a corridor entering a room.
    /// </summary>
    private void PlaceDoors()
    {
        for (int x = 1; x < _map.Width - 1; x++)
        {
            for (int y = 1; y < _map.Height - 1; y++)
            {
                if (_map.GetTile(x, y) != TileType.Floor) continue;
                if (_map.GetTile(x, y) == TileType.StairsDown) continue;

                // Check horizontal doorway pattern: Wall-Floor-Wall (vertically)
                // with floor on both sides (horizontally) — a corridor pinch point
                bool vertWalls = _map.GetTile(x, y - 1) == TileType.Wall &&
                                 _map.GetTile(x, y + 1) == TileType.Wall;
                bool horzFloor = _map.GetTile(x - 1, y) == TileType.Floor &&
                                 _map.GetTile(x + 1, y) == TileType.Floor;

                // Check vertical doorway pattern: Wall-Floor-Wall (horizontally)
                bool horzWalls = _map.GetTile(x - 1, y) == TileType.Wall &&
                                 _map.GetTile(x + 1, y) == TileType.Wall;
                bool vertFloor = _map.GetTile(x, y - 1) == TileType.Floor &&
                                 _map.GetTile(x, y + 1) == TileType.Floor;

                if ((vertWalls && horzFloor) || (horzWalls && vertFloor))
                {
                    // Only place some doors (not every pinch point)
                    if (_rng.NextDouble() < 0.4)
                    {
                        _map.SetTile(x, y, TileType.Door);
                    }
                }
            }
        }
    }
}
