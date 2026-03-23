// ------------------------------------------------------------------
// Enemy — Dungeon creature with AI behavior
//
// Each enemy has a state machine:
//   Idle    — stands still, hasn't detected the player yet
//   Patrol  — wanders randomly between nearby tiles
//   Chase   — actively pursues the player using pathfinding
//   Attack  — adjacent to the player, strikes each turn
//
// Enemies move one tile per turn (same as the player).
// They use Manhattan distance for detection and simple
// greedy pathfinding to chase the player.
// ------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonDash;

public enum EnemyType
{
    Skeleton,  // Basic melee — low HP, low damage
    Goblin,    // Fast — moves toward player aggressively
    Demon      // Tank — high HP, high damage, slower detection
}

public enum EnemyState
{
    Idle,
    Patrol,
    Chase
}

public class Enemy
{
    // --- Grid position ---
    public int TileX { get; private set; }
    public int TileY { get; private set; }

    // --- Smooth animation (mirrors Player) ---
    private Vector2 _visualPos;
    private Vector2 _moveFrom;
    private Vector2 _moveTo;
    private float _moveTimer = 1f;
    private bool _isMoving;
    private const float MoveDuration = 0.15f;

    // --- Stats ---
    public EnemyType Type { get; }
    public int MaxHP { get; private set; }
    public int HP { get; set; }
    public int Attack { get; private set; }
    public int Defense { get; private set; }
    public int XPReward { get; private set; }

    // --- AI ---
    public EnemyState State { get; private set; } = EnemyState.Idle;
    public int DetectRadius { get; private set; }
    private int _patrolCooldown;
    private readonly Random _rng;

    // --- Damage flash ---
    public float DamageFlash { get; set; }

    public bool IsAlive => HP > 0;
    public bool IsMoving => _isMoving;
    public Vector2 WorldPosition => _visualPos;

    public Enemy(EnemyType type, int tileX, int tileY, Random rng)
    {
        Type = type;
        TileX = tileX;
        TileY = tileY;
        _rng = rng;
        _visualPos = TileToWorld(tileX, tileY);
        _moveFrom = _visualPos;
        _moveTo = _visualPos;

        // Set stats based on type
        switch (type)
        {
            case EnemyType.Skeleton:
                MaxHP = 8; HP = 8;
                Attack = 3; Defense = 0;
                DetectRadius = 6;
                XPReward = 10;
                break;
            case EnemyType.Goblin:
                MaxHP = 6; HP = 6;
                Attack = 4; Defense = 1;
                DetectRadius = 8;
                XPReward = 15;
                break;
            case EnemyType.Demon:
                MaxHP = 15; HP = 15;
                Attack = 6; Defense = 2;
                DetectRadius = 5;
                XPReward = 25;
                break;
        }
    }

    /// <summary>
    /// Scale enemy stats for deeper floors.
    /// </summary>
    public void ScaleForFloor(int floor)
    {
        float mult = 1f + (floor - 1) * 0.25f;
        MaxHP = (int)(MaxHP * mult);
        HP = MaxHP;
        Attack = (int)(Attack * mult);
        Defense = (int)(Defense * (1f + (floor - 1) * 0.15f));
        XPReward = (int)(XPReward * mult);
    }

    /// <summary>
    /// Execute one AI turn. Called after the player moves.
    /// Returns the tile the enemy tries to move to (for collision).
    /// </summary>
    public void TakeTurn(Player player, TileMap map, List<Enemy> allEnemies)
    {
        if (!IsAlive) return;

        int distToPlayer = Math.Abs(TileX - player.TileX) + Math.Abs(TileY - player.TileY);

        // State transitions
        if (distToPlayer <= DetectRadius)
        {
            State = EnemyState.Chase;
        }
        else if (State == EnemyState.Chase && distToPlayer > DetectRadius + 3)
        {
            // Lost sight — go back to patrol
            State = EnemyState.Patrol;
            _patrolCooldown = _rng.Next(1, 4);
        }

        switch (State)
        {
            case EnemyState.Idle:
                // Small chance to start patrolling
                if (_rng.NextDouble() < 0.1)
                    State = EnemyState.Patrol;
                break;

            case EnemyState.Patrol:
                DoPatrol(map, allEnemies, player);
                break;

            case EnemyState.Chase:
                DoChase(player, map, allEnemies);
                break;
        }
    }

    private void DoPatrol(TileMap map, List<Enemy> allEnemies, Player player)
    {
        _patrolCooldown--;
        if (_patrolCooldown > 0) return;

        // Pick a random adjacent tile
        int[] dirs = { 0, 1, 2, 3 };
        Shuffle(dirs);

        foreach (int d in dirs)
        {
            int dx = d == 0 ? -1 : d == 1 ? 1 : 0;
            int dy = d == 2 ? -1 : d == 3 ? 1 : 0;
            int nx = TileX + dx;
            int ny = TileY + dy;

            if (CanMoveTo(nx, ny, map, allEnemies, player))
            {
                MoveTo(nx, ny);
                break;
            }
        }

        _patrolCooldown = _rng.Next(1, 4);
    }

    private void DoChase(Player player, TileMap map, List<Enemy> allEnemies)
    {
        int distToPlayer = Math.Abs(TileX - player.TileX) + Math.Abs(TileY - player.TileY);

        // If adjacent — don't move (attack is handled by combat system)
        if (distToPlayer <= 1) return;

        // Greedy pathfinding: try moving toward player
        int bestDx = 0, bestDy = 0;
        int bestDist = distToPlayer;

        // Try all 4 directions, pick the one closest to player
        int[] dxs = { -1, 1, 0, 0 };
        int[] dys = { 0, 0, -1, 1 };

        // Shuffle to break ties randomly
        int[] order = { 0, 1, 2, 3 };
        Shuffle(order);

        foreach (int i in order)
        {
            int nx = TileX + dxs[i];
            int ny = TileY + dys[i];

            if (!CanMoveTo(nx, ny, map, allEnemies, player))
                continue;

            int dist = Math.Abs(nx - player.TileX) + Math.Abs(ny - player.TileY);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestDx = dxs[i];
                bestDy = dys[i];
            }
        }

        if (bestDx != 0 || bestDy != 0)
        {
            MoveTo(TileX + bestDx, TileY + bestDy);
        }
    }

    /// <summary>
    /// Check if the enemy can move to a tile (walkable, no other enemy, not player tile).
    /// </summary>
    private bool CanMoveTo(int x, int y, TileMap map, List<Enemy> allEnemies, Player player)
    {
        if (!map.IsWalkable(x, y)) return false;

        // Don't walk onto player
        if (x == player.TileX && y == player.TileY) return false;

        // Don't stack with other enemies
        foreach (var other in allEnemies)
        {
            if (other != this && other.IsAlive && other.TileX == x && other.TileY == y)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Move to a new tile with smooth animation.
    /// </summary>
    private void MoveTo(int newX, int newY)
    {
        _moveFrom = TileToWorld(TileX, TileY);
        TileX = newX;
        TileY = newY;
        _moveTo = TileToWorld(TileX, TileY);
        _moveTimer = 0f;
        _isMoving = true;
    }

    /// <summary>
    /// Update smooth movement animation.
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
            float t = 1f - (1f - _moveTimer) * (1f - _moveTimer);
            _visualPos = Vector2.Lerp(_moveFrom, _moveTo, t);
        }

        // Decay damage flash
        if (DamageFlash > 0f)
            DamageFlash = MathF.Max(0f, DamageFlash - dt * 4f);
    }

    /// <summary>
    /// Draw the enemy sprite at its visual position.
    /// Only draws if the tile is currently visible to the player.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Vector2 cameraOffset, FogOfWar fog)
    {
        // Only draw enemies the player can see
        if (fog != null && fog.GetVisibility(TileX, TileY) != Visibility.Visible)
            return;

        var tex = TextureFactory.GetEnemyTexture(Type);
        var pos = new Vector2(
            _visualPos.X + cameraOffset.X,
            _visualPos.Y + cameraOffset.Y
        );

        // Flash white on damage
        Color tint = DamageFlash > 0f ? Color.Lerp(Color.White, Color.Red, DamageFlash) : Color.White;

        spriteBatch.Draw(tex, pos, tint);
    }

    /// <summary>
    /// Take damage, reduced by defense.
    /// </summary>
    public int TakeDamage(int rawDamage)
    {
        int actual = Math.Max(1, rawDamage - Defense);
        HP = Math.Max(0, HP - actual);
        DamageFlash = 1f;
        return actual;
    }

    // --- Helpers ---

    private static Vector2 TileToWorld(int tx, int ty)
        => new Vector2(tx * TextureFactory.TileSize, ty * TextureFactory.TileSize);

    private void Shuffle(int[] arr)
    {
        for (int i = arr.Length - 1; i > 0; i--)
        {
            int j = _rng.Next(i + 1);
            (arr[i], arr[j]) = (arr[j], arr[i]);
        }
    }
}
