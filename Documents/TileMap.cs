// ------------------------------------------------------------------
// TileMap — 2D grid of tiles with rendering support
// Stores tile data in a 2D array, draws visible tiles to screen.
// In Step 2, the dungeon generator will populate this.
// For now, we create a simple test map.
// ------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonDash;

public class TileMap
{
    public int Width { get; private set; }
    public int Height { get; private set; }
    public TileType[,] Tiles { get; private set; }

    public TileMap(int width, int height)
    {
        Width = width;
        Height = height;
        Tiles = new TileType[width, height];
        Fill(TileType.Wall); // Default everything to wall
    }

    /// <summary>
    /// Fill entire map with a single tile type.
    /// </summary>
    public void Fill(TileType type)
    {
        for (int x = 0; x < Width; x++)
            for (int y = 0; y < Height; y++)
                Tiles[x, y] = type;
    }

    /// <summary>
    /// Safe bounds check before accessing a tile.
    /// </summary>
    public bool InBounds(int x, int y)
        => x >= 0 && y >= 0 && x < Width && y < Height;

    /// <summary>
    /// Get the tile at a position, returns Wall if out of bounds.
    /// </summary>
    public TileType GetTile(int x, int y)
        => InBounds(x, y) ? Tiles[x, y] : TileType.Wall;

    /// <summary>
    /// Set the tile at a position if in bounds.
    /// </summary>
    public void SetTile(int x, int y, TileType type)
    {
        if (InBounds(x, y))
            Tiles[x, y] = type;
    }

    /// <summary>
    /// Returns true if the tile at (x,y) is walkable (not a wall).
    /// </summary>
    public bool IsWalkable(int x, int y)
        => GetTile(x, y) != TileType.Wall;

    /// <summary>
    /// Draw only the tiles visible within the camera viewport.
    /// Applies fog of war tinting when a FogOfWar instance is provided.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Vector2 cameraOffset, int screenW, int screenH, FogOfWar fog = null)
    {
        int tileSize = TextureFactory.TileSize;

        // Calculate visible tile range
        int startX = (int)MathF.Max(0, -cameraOffset.X / tileSize);
        int startY = (int)MathF.Max(0, -cameraOffset.Y / tileSize);
        int endX = (int)MathF.Min(Width, (-cameraOffset.X + screenW) / tileSize + 1);
        int endY = (int)MathF.Min(Height, (-cameraOffset.Y + screenH) / tileSize + 1);

        for (int x = startX; x < endX; x++)
        {
            for (int y = startY; y < endY; y++)
            {
                // Determine visibility tint
                Color tint;
                if (fog != null)
                {
                    var vis = fog.GetVisibility(x, y);
                    if (vis == Visibility.Unseen)
                        continue; // Don't draw unseen tiles at all
                    else if (vis == Visibility.Explored)
                        tint = new Color(90, 85, 110); // Dim blue-gray, still shows bricks/doors
                    else
                        tint = Color.White; // Fully visible
                }
                else
                {
                    tint = Color.White;
                }

                var tex = TextureFactory.GetTexture(Tiles[x, y]);
                var pos = new Vector2(x * tileSize + cameraOffset.X, y * tileSize + cameraOffset.Y);
                spriteBatch.Draw(tex, pos, tint);
            }
        }
    }

}
