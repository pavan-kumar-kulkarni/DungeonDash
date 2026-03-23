// ------------------------------------------------------------------
// FogOfWar — Visibility and exploration tracking
//
// Three tile visibility states:
//   Unseen   — never seen, rendered as pure black
//   Explored — previously seen but not currently visible, dimmed
//   Visible  — currently in player's line of sight, full brightness
//
// Uses recursive Shadowcasting (Björn Bergström's algorithm) to
// compute line-of-sight in 8 octants. This is the gold standard
// for roguelike FOV — fast, accurate, no blind spots.
// ------------------------------------------------------------------
using System;

namespace DungeonDash;

public enum Visibility
{
    Unseen,
    Explored,
    Visible
}

public class FogOfWar
{
    public int Width { get; }
    public int Height { get; }

    private Visibility[,] _visibility;
    private readonly TileMap _map;

    /// <summary>
    /// Player's sight radius in tiles.
    /// </summary>
    public int ViewRadius { get; set; } = 8;

    public FogOfWar(TileMap map)
    {
        _map = map;
        Width = map.Width;
        Height = map.Height;
        _visibility = new Visibility[Width, Height];
    }

    /// <summary>
    /// Get the visibility state of a tile.
    /// </summary>
    public Visibility GetVisibility(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return Visibility.Unseen;
        return _visibility[x, y];
    }

    /// <summary>
    /// Reset all Visible tiles to Explored, then recompute visibility
    /// from the player's current position using shadowcasting.
    /// </summary>
    public void Update(int playerX, int playerY)
    {
        // Demote all currently Visible tiles to Explored
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                if (_visibility[x, y] == Visibility.Visible)
                    _visibility[x, y] = Visibility.Explored;

        // The player's tile is always visible
        SetVisible(playerX, playerY);

        // Cast shadows in all 8 octants
        for (int octant = 0; octant < 8; octant++)
        {
            CastShadow(playerX, playerY, 1, 1.0f, 0.0f, octant);
        }
    }

    /// <summary>
    /// Mark a tile as Visible (and implicitly Explored).
    /// </summary>
    private void SetVisible(int x, int y)
    {
        if (x >= 0 && y >= 0 && x < Width && y < Height)
            _visibility[x, y] = Visibility.Visible;
    }

    /// <summary>
    /// Check if a tile blocks line of sight.
    /// Walls block; everything else (floor, door, stairs) is transparent.
    /// </summary>
    private bool BlocksLight(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
            return true;
        return _map.GetTile(x, y) == TileType.Wall;
    }

    // ==================== SHADOWCASTING ====================
    //
    // Recursive shadowcasting works by scanning outward from the
    // player in each of 8 octants. As it encounters walls, it
    // narrows the visible arc (startSlope/endSlope). This naturally
    // creates realistic shadows behind pillars and corners.
    //
    // Octant transformations map (row, col) offsets to (dx, dy):
    //   0: ( col,  row)   1: ( row,  col)
    //   2: ( row, -col)   3: (-col,  row)
    //   4: (-col, -row)   5: (-row, -col)
    //   6: (-row,  col)   7: ( col, -row)

    private void CastShadow(int cx, int cy, int row, float startSlope, float endSlope, int octant)
    {
        if (startSlope < endSlope)
            return;

        float nextStartSlope = startSlope;

        for (int r = row; r <= ViewRadius; r++)
        {
            bool blocked = false;

            for (int col = -r; col <= 0; col++)
            {
                // Map column to slopes
                float leftSlope = (col - 0.5f) / (r + 0.5f);
                float rightSlope = (col + 0.5f) / (r - 0.5f);

                if (startSlope < rightSlope)
                    continue;
                if (endSlope > leftSlope)
                    break;

                // Transform octant-relative (row, col) to map coordinates
                int dx, dy;
                TransformOctant(r, col, octant, out dx, out dy);
                int mapX = cx + dx;
                int mapY = cy + dy;

                // Check distance (circular FOV)
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist <= ViewRadius)
                {
                    SetVisible(mapX, mapY);
                }

                if (blocked)
                {
                    // Previous cell was a wall
                    if (BlocksLight(mapX, mapY))
                    {
                        // Still blocked — update start slope
                        nextStartSlope = rightSlope;
                    }
                    else
                    {
                        // Wall ended — start a new scan
                        blocked = false;
                        startSlope = nextStartSlope;
                    }
                }
                else
                {
                    if (BlocksLight(mapX, mapY) && r < ViewRadius)
                    {
                        // Hit a wall — recurse with narrowed arc, then mark blocked
                        blocked = true;
                        CastShadow(cx, cy, r + 1, startSlope, leftSlope, octant);
                        nextStartSlope = rightSlope;
                    }
                }
            }

            if (blocked)
                break;
        }
    }

    /// <summary>
    /// Transform (row, col) from octant-relative coordinates to (dx, dy) map offsets.
    /// </summary>
    private static void TransformOctant(int row, int col, int octant, out int dx, out int dy)
    {
        switch (octant)
        {
            case 0: dx = col; dy = -row; break;
            case 1: dx = -row; dy = col; break;
            case 2: dx = -row; dy = -col; break;
            case 3: dx = -col; dy = -row; break;
            case 4: dx = col; dy = row; break;  // note: col is negative
            case 5: dx = row; dy = col; break;
            case 6: dx = row; dy = -col; break;
            case 7: dx = -col; dy = row; break;
            default: dx = 0; dy = 0; break;
        }
    }
}
