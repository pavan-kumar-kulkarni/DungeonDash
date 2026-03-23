// ------------------------------------------------------------------
// Player — The hero exploring the dungeon
//
// Turn-based grid movement with smooth pixel interpolation.
// Each key press moves exactly one tile. The player's visual
// position smoothly slides from the old tile to the new tile
// over a short duration, giving a polished feel.
//
// Stats (HP, attack, defense) are set up here for combat in Step 6.
// ------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonDash;

public class Player
{
    // --- Grid position (tile coordinates) ---
    public int TileX { get; private set; }
    public int TileY { get; private set; }

    // --- Smooth movement animation ---
    private Vector2 _visualPos;       // Current pixel position (interpolated)
    private Vector2 _moveFrom;        // Start position of current move
    private Vector2 _moveTo;          // End position of current move
    private float _moveTimer;         // 0..1 progress of current move
    private bool _isMoving;
    private const float MoveDuration = 0.1f; // Seconds per tile move

    // --- Stats ---
    public int MaxHP { get; set; } = 20;
    public int HP { get; set; } = 20;
    public int Attack { get; set; } = 5;
    public int Defense { get; set; } = 2;
    public int Level { get; set; } = 1;
    public int XP { get; set; } = 0;
    public int Gold { get; set; } = 0;

    // --- Direction facing (for sprite orientation) ---
    public int FacingX { get; private set; } = 0;
    public int FacingY { get; private set; } = 1;

    public bool IsAlive => HP > 0;
    public bool IsMoving => _isMoving;

    /// <summary>
    /// World-space pixel position for rendering (smoothly interpolated).
    /// </summary>
    public Vector2 WorldPosition => _visualPos;

    public Player()
    {
    }

    /// <summary>
    /// Place the player at a specific tile and snap visual position.
    /// </summary>
    public void SetPosition(int tileX, int tileY)
    {
        TileX = tileX;
        TileY = tileY;
        _visualPos = TileToWorld(tileX, tileY);
        _moveFrom = _visualPos;
        _moveTo = _visualPos;
        _moveTimer = 1f;
        _isMoving = false;
    }

    /// <summary>
    /// Try to move the player one tile in the given direction.
    /// Returns true if the move was valid (tile is walkable).
    /// </summary>
    public bool TryMove(int dx, int dy, TileMap map)
    {
        if (_isMoving) return false; // Don't allow moves during animation

        int newX = TileX + dx;
        int newY = TileY + dy;

        // Check if walkable
        if (!map.IsWalkable(newX, newY))
            return false;

        // Update facing direction
        if (dx != 0 || dy != 0)
        {
            FacingX = dx;
            FacingY = dy;
        }

        // Start smooth move
        _moveFrom = TileToWorld(TileX, TileY);
        TileX = newX;
        TileY = newY;
        _moveTo = TileToWorld(TileX, TileY);
        _moveTimer = 0f;
        _isMoving = true;

        return true;
    }

    /// <summary>
    /// Update the smooth movement interpolation.
    /// </summary>
    public void Update(float dt)
    {
        if (_isMoving)
        {
            _moveTimer += dt / MoveDuration;
            if (_moveTimer >= 1f)
            {
                _moveTimer = 1f;
                _isMoving = false;
            }

            // Smooth ease-out interpolation
            float t = EaseOut(_moveTimer);
            _visualPos = Vector2.Lerp(_moveFrom, _moveTo, t);
        }
    }

    /// <summary>
    /// Draw the player sprite at its visual position.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Vector2 cameraOffset)
    {
        var tex = TextureFactory.Player;
        var pos = new Vector2(
            _visualPos.X + cameraOffset.X,
            _visualPos.Y + cameraOffset.Y
        );

        spriteBatch.Draw(tex, pos, Color.White);
    }

    /// <summary>
    /// Take damage, clamped to 0.
    /// </summary>
    public void TakeDamage(int amount)
    {
        int actual = Math.Max(0, amount - Defense);
        HP = Math.Max(0, HP - actual);
    }

    /// <summary>
    /// Heal HP, clamped to MaxHP.
    /// </summary>
    public void Heal(int amount)
    {
        HP = Math.Min(MaxHP, HP + amount);
    }

    // --- Helpers ---

    private static Vector2 TileToWorld(int tx, int ty)
    {
        return new Vector2(tx * TextureFactory.TileSize, ty * TextureFactory.TileSize);
    }

    /// <summary>
    /// Quadratic ease-out for smooth deceleration.
    /// </summary>
    private static float EaseOut(float t)
    {
        return 1f - (1f - t) * (1f - t);
    }
}
