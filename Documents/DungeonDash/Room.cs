// ------------------------------------------------------------------
// Room — Represents a rectangular room in the dungeon
// Stores position & size, provides helper methods for centers,
// overlap checks, and distance calculations.
// ------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;

namespace DungeonDash;

public class Room
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    // The inner area (1-tile border offset for walls)
    public int InnerX => X + 1;
    public int InnerY => Y + 1;
    public int InnerWidth => Width - 2;
    public int InnerHeight => Height - 2;

    public int CenterX => X + Width / 2;
    public int CenterY => Y + Height / 2;
    public Point Center => new Point(CenterX, CenterY);

    public Room(int x, int y, int width, int height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Check if this room overlaps another (with 1-tile padding).
    /// </summary>
    public bool Overlaps(Room other, int padding = 1)
    {
        return X - padding < other.X + other.Width &&
               X + Width + padding > other.X &&
               Y - padding < other.Y + other.Height &&
               Y + Height + padding > other.Y;
    }

    /// <summary>
    /// Manhattan distance between room centers.
    /// </summary>
    public int DistanceTo(Room other)
    {
        return Math.Abs(CenterX - other.CenterX) + Math.Abs(CenterY - other.CenterY);
    }

    /// <summary>
    /// Check if a tile coordinate is inside the room's inner floor area.
    /// </summary>
    public bool ContainsTile(int tx, int ty)
    {
        return tx >= InnerX && tx < InnerX + InnerWidth &&
               ty >= InnerY && ty < InnerY + InnerHeight;
    }
}
