// ------------------------------------------------------------------
// Game1 — Main game class
// Manages game state, camera, and the core update/draw loop.
// Step 1: Framework + tile rendering with test map.
// ------------------------------------------------------------------
using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using DungeonDash;

namespace DungeonDash;

public class Game1 : Game
{
    // --- Audio ---
    private SoundEffectInstance _moveSfx, _attackSfx, _pickupSfx, _levelupSfx, _deathSfx, _stairsSfx;
    // --- Constants ---
    public const int ScreenWidth = 1024;
    public const int ScreenHeight = 768;

    // --- Core ---
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;

    // --- State ---
    private GameState _state = GameState.Menu;
    private TileMap _tileMap;
    private DungeonGenerator _dungeon;
    private Player _player;
    private FogOfWar _fog;
    private List<Enemy> _enemies = new();
    private List<Item> _items = new();
    private System.Random _rng = new();

    // --- Camera ---
    private Vector2 _cameraPos;       // World position the camera centers on
    private Vector2 _cameraOffset;    // Computed screen offset for rendering
    private const float CameraSmooth = 8f; // Camera lerp speed

    // --- Input ---
    private KeyboardState _prevKeyboard;
    private float _moveRepeatTimer;           // For key-repeat on held keys
    private const float MoveRepeatDelay = 0.18f;  // Initial delay before repeat
    private const float MoveRepeatRate = 0.08f;   // Repeat interval
    private bool _firstMoveRepeat;

    // --- Stats ---
    private int _currentFloor = 1;
    private const int MapWidth = 60;
    private const int MapHeight = 45;
    private const int MaxFloors = 5;

    // --- Combat ---
    private List<CombatMessage> _combatMessages = new();
    private float _playerDamageFlash;
    private const int XPPerLevel = 30;

    // --- Minimap ---
    private bool _showMinimap = true;
    private bool _minimapToggleHeld = false;

    /// <summary>
    /// A floating combat message that fades and drifts upward.
    /// </summary>
    private class CombatMessage
    {
        public string Text;
        public Vector2 WorldPos;  // World position (tile-based)
        public float Timer;       // Counts up
        public float Duration;
        public Color BaseColor;

        public CombatMessage(string text, int tileX, int tileY, Color color, float duration = 1.5f)
        {
            Text = text;
            WorldPos = new Vector2(tileX * TextureFactory.TileSize, tileY * TextureFactory.TileSize);
            Timer = 0f;
            Duration = duration;
            BaseColor = color;
        }

        public bool IsExpired => Timer >= Duration;
    }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = ScreenWidth;
        _graphics.PreferredBackBufferHeight = ScreenHeight;
    }

    protected override void Initialize()
    {
        Window.Title = "DungeonDash — Roguelike Dungeon Crawler";
        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // Generate all runtime textures (no image files needed)
        TextureFactory.Initialize(GraphicsDevice);

        // Preload procedural SFX
        _moveSfx = SoundFactory.Get("move")?.CreateInstance();
        _attackSfx = SoundFactory.Get("attack")?.CreateInstance();
        _pickupSfx = SoundFactory.Get("pickup")?.CreateInstance();
        _levelupSfx = SoundFactory.Get("levelup")?.CreateInstance();
        _deathSfx = SoundFactory.Get("death")?.CreateInstance();
        _stairsSfx = SoundFactory.Get("stairs")?.CreateInstance();

        // Create test map (replaced by procedural generation in Step 2)
        StartNewGame();
    }

    /// <summary>
    /// Resets the game to a fresh state.
    /// </summary>
    private void StartNewGame()
    {
        _currentFloor = 1;
        _player = new Player();
        GenerateFloor();
        _state = GameState.Playing;
    }

    /// <summary>
    /// Generate a new dungeon floor and position player + camera at spawn.
    /// </summary>
    private void GenerateFloor()
    {
        _dungeon = new DungeonGenerator(MapWidth, MapHeight);
        _tileMap = _dungeon.Generate();

        // Place player at spawn room center
        var spawn = _dungeon.SpawnRoom;
        _player.SetPosition(spawn.CenterX, spawn.CenterY);

        // Initialize fog of war and compute initial visibility
        _fog = new FogOfWar(_tileMap);
        _fog.Update(_player.TileX, _player.TileY);

        // Spawn enemies in rooms (not in spawn room)
        SpawnEnemies();

        // Spawn items in rooms
        SpawnItems();

        // Snap camera to player
        _cameraPos = _player.WorldPosition;
    }

    /// <summary>
    /// Spawn enemies across dungeon rooms, avoiding the spawn room.
    /// </summary>
    private void SpawnEnemies()
    {
        _enemies.Clear();
        var rooms = _dungeon.Rooms;

        // Cap total enemies based on floor: 5 on floor 1 up to ~12 on floor 5
        int maxTotal = 3 + _currentFloor * 2;

        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            // Skip spawn room
            if (room == _dungeon.SpawnRoom) continue;

            // Stop if we've hit the cap
            if (_enemies.Count >= maxTotal) break;

            // 1-2 enemies per room
            int count = _rng.Next(1, 3);
            count = Math.Min(count, maxTotal - _enemies.Count);
            count = Math.Min(count, room.InnerWidth * room.InnerHeight / 6); // Don't overcrowd

            for (int e = 0; e < count; e++)
            {
                // Choose enemy type based on floor depth
                EnemyType type;
                double roll = _rng.NextDouble();
                if (_currentFloor >= 4 && roll < 0.3)
                    type = EnemyType.Demon;
                else if (_currentFloor >= 2 && roll < 0.5)
                    type = EnemyType.Goblin;
                else
                    type = EnemyType.Skeleton;

                // Find an empty floor tile in the room
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    int ex = room.InnerX + _rng.Next(room.InnerWidth);
                    int ey = room.InnerY + _rng.Next(room.InnerHeight);

                    if (!_tileMap.IsWalkable(ex, ey)) continue;
                    if (ex == _player.TileX && ey == _player.TileY) continue;

                    // Check no other enemy on this tile
                    bool occupied = false;
                    foreach (var other in _enemies)
                        if (other.TileX == ex && other.TileY == ey) { occupied = true; break; }
                    if (occupied) continue;

                    var enemy = new Enemy(type, ex, ey, _rng);
                    enemy.ScaleForFloor(_currentFloor);
                    _enemies.Add(enemy);
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Spawn items across dungeon rooms.
    /// </summary>
    private void SpawnItems()
    {
        _items.Clear();
        var rooms = _dungeon.Rooms;

        for (int i = 0; i < rooms.Count; i++)
        {
            var room = rooms[i];
            if (room == _dungeon.SpawnRoom) continue;

            // 30% chance per room to have an item
            if (_rng.NextDouble() > 0.35) continue;

            // Choose item type
            ItemType type;
            double roll = _rng.NextDouble();
            if (roll < 0.35)
                type = ItemType.HealthPotion;
            else if (roll < 0.55)
                type = ItemType.AttackGem;
            else if (roll < 0.70)
                type = ItemType.DefenseGem;
            else
                type = ItemType.Gold;

            // Place on a random floor tile in the room
            for (int attempt = 0; attempt < 20; attempt++)
            {
                int ix = room.InnerX + _rng.Next(room.InnerWidth);
                int iy = room.InnerY + _rng.Next(room.InnerHeight);

                if (!_tileMap.IsWalkable(ix, iy)) continue;

                // Don't place on player, enemies, or other items
                if (ix == _player.TileX && iy == _player.TileY) continue;
                bool taken = false;
                foreach (var e in _enemies)
                    if (e.TileX == ix && e.TileY == iy) { taken = true; break; }
                if (taken) continue;
                foreach (var it in _items)
                    if (it.TileX == ix && it.TileY == iy) { taken = true; break; }
                if (taken) continue;

                _items.Add(new Item(type, ix, iy, _rng));
                break;
            }
        }
    }

    /// <summary>
    /// Check if the player is standing on an item and pick it up.
    /// </summary>
    private void CheckItemPickup()
    {
        foreach (var item in _items)
        {
            if (item.Collected) continue;
            if (item.TileX == _player.TileX && item.TileY == _player.TileY)
            {
                item.Collected = true;
                string msg = item.Apply(_player);
                _combatMessages.Add(new CombatMessage(
                    msg, _player.TileX, _player.TileY, item.MessageColor, 2f));
                _pickupSfx?.Play();
            }
        }
    }

    /// <summary>
    /// Called after any player action that consumes a turn.
    /// Updates fog and lets all enemies take their turn.
    /// </summary>
    private void PlayerTurnTaken()
    {
        // Update fog of war
        _fog.Update(_player.TileX, _player.TileY);

        // All living enemies take a turn
        foreach (var enemy in _enemies)
        {
            if (!enemy.IsAlive) continue;

            enemy.TakeTurn(_player, _tileMap, _enemies);

            // After moving, check if enemy is adjacent to player → attack
            int dist = Math.Abs(enemy.TileX - _player.TileX) + Math.Abs(enemy.TileY - _player.TileY);
            if (dist <= 1)
            {
                int dmg = Math.Max(1, enemy.Attack - _player.Defense);
                _player.TakeDamage(enemy.Attack); // TakeDamage applies defense internally
                _playerDamageFlash = 1f;
                _combatMessages.Add(new CombatMessage(
                    $"-{dmg}", _player.TileX, _player.TileY, new Color(255, 80, 80)));

                if (!_player.IsAlive)
                {
                    _deathSfx?.Play();
                    _state = GameState.GameOver;
                    return;
                }
            }
        }
    }

    /// <summary>
    /// Check if the player has enough XP to level up.
    /// </summary>
    private void CheckLevelUp()
    {
        int xpNeeded = _player.Level * XPPerLevel;
        while (_player.XP >= xpNeeded)
        {
            _player.XP -= xpNeeded;
            _player.Level++;
            _player.MaxHP += 5;
            _player.HP = _player.MaxHP; // Full heal on level up
            _player.Attack += 2;
            _player.Defense += 1;

            _combatMessages.Add(new CombatMessage(
                $"LEVEL {_player.Level}!", _player.TileX, _player.TileY - 2,
                new Color(255, 215, 0), 2.5f));
            _levelupSfx?.Play();
            xpNeeded = _player.Level * XPPerLevel;
        }
    }

    /// <summary>
    /// Count enemies still alive on this floor.
    /// </summary>
    private int CountAliveEnemies()
    {
        int count = 0;
        foreach (var e in _enemies)
            if (e.IsAlive) count++;
        return count;
    }

    /// <summary>
    /// Advance to the next dungeon floor when stairs are reached.
    /// </summary>
    private void DescendStairs()
    {
        _stairsSfx?.Play();
        _currentFloor++;
        if (_currentFloor > MaxFloors)
        {
            _state = GameState.Victory;
            return;
        }
        GenerateFloor();
    }

    // ==================== UPDATE ====================

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_state)
        {
            case GameState.Menu:
                UpdateMenu(kb);
                break;
            case GameState.Playing:
                UpdatePlaying(kb, dt);
                break;
            case GameState.GameOver:
            case GameState.Victory:
                UpdateEndScreen(kb);
                break;
        }

        _prevKeyboard = kb;
        base.Update(gameTime);
    }

    private bool JustPressed(KeyboardState kb, Keys key)
        => kb.IsKeyDown(key) && _prevKeyboard.IsKeyUp(key);

    private void UpdateMenu(KeyboardState kb)
    {
        if (JustPressed(kb, Keys.Enter) || JustPressed(kb, Keys.Space))
            StartNewGame();

        if (JustPressed(kb, Keys.Escape))
            Exit();
    }

    private void UpdatePlaying(KeyboardState kb, float dt)
    {
        // --- Minimap toggle ---
        if ((kb.IsKeyDown(Keys.Tab) && !_minimapToggleHeld))
        {
            _showMinimap = !_showMinimap;
            _minimapToggleHeld = true;
        }
        else if (!kb.IsKeyDown(Keys.Tab))
        {
            _minimapToggleHeld = false;
        }

        // --- Player grid movement ---
        if (!_player.IsMoving)
        {
            int dx = 0, dy = 0;

            // Determine move direction
            if (kb.IsKeyDown(Keys.W) || kb.IsKeyDown(Keys.Up)) dy = -1;
            else if (kb.IsKeyDown(Keys.S) || kb.IsKeyDown(Keys.Down)) dy = 1;
            else if (kb.IsKeyDown(Keys.A) || kb.IsKeyDown(Keys.Left)) dx = -1;
            else if (kb.IsKeyDown(Keys.D) || kb.IsKeyDown(Keys.Right)) dx = 1;

            if (dx != 0 || dy != 0)
            {
                // Key-repeat logic: first press is instant, then delay, then repeat
                bool doMove = false;

                if (JustPressed(kb, Keys.W) || JustPressed(kb, Keys.Up) ||
                    JustPressed(kb, Keys.S) || JustPressed(kb, Keys.Down) ||
                    JustPressed(kb, Keys.A) || JustPressed(kb, Keys.Left) ||
                    JustPressed(kb, Keys.D) || JustPressed(kb, Keys.Right))
                {
                    doMove = true;
                    _moveRepeatTimer = 0f;
                    _firstMoveRepeat = true;
                }
                else
                {
                    _moveRepeatTimer += dt;
                    float threshold = _firstMoveRepeat ? MoveRepeatDelay : MoveRepeatRate;
                    if (_moveRepeatTimer >= threshold)
                    {
                        doMove = true;
                        _moveRepeatTimer = 0f;
                        _firstMoveRepeat = false;
                    }
                }

                if (doMove)
                {
                    // Check if moving into an enemy (bump attack — handled in Step 6)
                    int targetX = _player.TileX + dx;
                    int targetY = _player.TileY + dy;
                    Enemy bumped = null;
                    foreach (var e in _enemies)
                        if (e.IsAlive && e.TileX == targetX && e.TileY == targetY)
                        { bumped = e; break; }

                    if (bumped != null)
                    {
                        // Bump attack — deal damage to enemy
                        int dmg = bumped.TakeDamage(_player.Attack);
                        _combatMessages.Add(new CombatMessage(
                            $"-{dmg}", bumped.TileX, bumped.TileY, new Color(255, 220, 80)));
                        _attackSfx?.Play();

                        if (!bumped.IsAlive)
                        {
                            // Enemy killed — award XP
                            _player.XP += bumped.XPReward;
                            _combatMessages.Add(new CombatMessage(
                                $"+{bumped.XPReward} XP", bumped.TileX, bumped.TileY - 1,
                                new Color(100, 255, 100), 2f));

                            // Check level up
                            CheckLevelUp();
                        }

                        // Player's turn is used, enemies then move
                        PlayerTurnTaken();
                    }
                    else if (_player.TryMove(dx, dy, _tileMap))
                    {
                        _moveSfx?.Play();
                        // Check for item pickup at new position
                        CheckItemPickup();

                        // Successful move — run enemy turns
                        PlayerTurnTaken();
                    }
                }
            }
            else
            {
                _moveRepeatTimer = 0f;
                _firstMoveRepeat = true;
            }

            // Check stairs
            if (_tileMap.GetTile(_player.TileX, _player.TileY) == TileType.StairsDown)
            {
                DescendStairs();
                return;
            }
        }

        // Update player animation
        _player.Update(dt);

        // Update enemy animations
        foreach (var enemy in _enemies)
            if (enemy.IsAlive)
                enemy.Update(dt);

        // Update item animations
        foreach (var item in _items)
            if (!item.Collected)
                item.Update(dt);

        // Update combat messages
        for (int i = _combatMessages.Count - 1; i >= 0; i--)
        {
            _combatMessages[i].Timer += dt;
            if (_combatMessages[i].IsExpired)
                _combatMessages.RemoveAt(i);
        }

        // Decay player damage flash
        if (_playerDamageFlash > 0f)
            _playerDamageFlash = MathF.Max(0f, _playerDamageFlash - dt * 4f);

        // Smooth camera follow
        _cameraPos = Vector2.Lerp(_cameraPos, _player.WorldPosition, CameraSmooth * dt);

        // Regenerate dungeon (debug / demo key)
        if (JustPressed(kb, Keys.N))
            GenerateFloor();

        // Restart game from floor 1
        if (JustPressed(kb, Keys.R))
            StartNewGame();

        if (JustPressed(kb, Keys.Escape))
            _state = GameState.Menu;
    }

    private void UpdateEndScreen(KeyboardState kb)
    {
        if (JustPressed(kb, Keys.Enter) || JustPressed(kb, Keys.Space))
            _state = GameState.Menu;

        if (JustPressed(kb, Keys.Escape))
            Exit();
    }

    // ==================== DRAW ====================

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(12, 10, 18));

        switch (_state)
        {
            case GameState.Menu:
                DrawMenu();
                break;
            case GameState.Playing:
                DrawPlaying();
                break;
            case GameState.GameOver:
                DrawEndScreen("GAME OVER", new Color(200, 50, 50));
                break;
            case GameState.Victory:
                DrawEndScreen("VICTORY!", new Color(80, 200, 80));
                break;
        }

        base.Draw(gameTime);
    }

    /// <summary>
    /// Compute camera offset so _cameraPos is centered on screen.
    /// </summary>
    private void UpdateCameraOffset()
    {
        _cameraOffset = new Vector2(
            -_cameraPos.X + ScreenWidth / 2f,
            -_cameraPos.Y + ScreenHeight / 2f
        );
    }

    private void DrawMenu()
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        var pixel = TextureFactory.Pixel;

        // Title
        DrawTextCentered("DUNGEON DASH", ScreenHeight / 2 - 80, 4f, new Color(200, 160, 255));

        // Subtitle
        DrawTextCentered("A Roguelike Dungeon Crawler", ScreenHeight / 2 - 20, 1.5f, new Color(150, 140, 170));

        // Prompt
        DrawTextCentered("Press ENTER to Start", ScreenHeight / 2 + 60, 1.5f, new Color(120, 200, 160));

        // Controls info
        DrawTextCentered("WASD / Arrows = Move    ESC = Menu", ScreenHeight / 2 + 120, 1f, new Color(100, 95, 110));

        // Floor info
        DrawTextCentered($"Descend {MaxFloors} floors to escape the dungeon", ScreenHeight / 2 + 150, 1f, new Color(100, 95, 110));

        // Version
        DrawTextCentered("MonoGame / C# — Procedural Dungeons", ScreenHeight - 40, 1f, new Color(70, 65, 80));

        _spriteBatch.End();
    }

    private void DrawPlaying()
    {
        UpdateCameraOffset();

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        // Draw tile map (fully lit — fog of war disabled for now)
        _tileMap.Draw(_spriteBatch, _cameraOffset, ScreenWidth, ScreenHeight);

        // Draw items (before player so they appear on the floor)
        foreach (var item in _items)
            if (!item.Collected)
                item.Draw(_spriteBatch, _cameraOffset);

        // Draw player (flash red when hit)
        if (_playerDamageFlash > 0f)
        {
            var pTex = TextureFactory.Player;
            var pPos = new Vector2(
                _player.WorldPosition.X + _cameraOffset.X,
                _player.WorldPosition.Y + _cameraOffset.Y);
            Color pTint = Color.Lerp(Color.White, Color.Red, _playerDamageFlash);
            _spriteBatch.Draw(pTex, pPos, pTint);
        }
        else
        {
            _player.Draw(_spriteBatch, _cameraOffset);
        }

        // Draw enemies (all visible — fog disabled for now)
        foreach (var enemy in _enemies)
            if (enemy.IsAlive)
                enemy.Draw(_spriteBatch, _cameraOffset, null);

        // --- Draw minimap ---
        if (_showMinimap)
            DrawMinimap();

        // --- Draw floating combat messages ---
        foreach (var msg in _combatMessages)
        {
            float alpha = 1f - (msg.Timer / msg.Duration);
            float yOff = -msg.Timer * 30f; // Drift upward
            var msgPos = new Vector2(
                msg.WorldPos.X + _cameraOffset.X,
                msg.WorldPos.Y + _cameraOffset.Y + yOff);
            Color msgColor = msg.BaseColor * alpha;
            DrawText(msg.Text, msgPos, 1.2f, msgColor);
        }

        // --- HUD ---
        // Floor indicator
        DrawText($"Floor: {_currentFloor}/{MaxFloors}", new Vector2(10, 10), 1.5f, new Color(200, 180, 255));

        // HP bar
        float hpPct = (float)_player.HP / _player.MaxHP;
        Color hpColor = hpPct > 0.5f ? new Color(80, 220, 80) : hpPct > 0.25f ? new Color(220, 180, 50) : new Color(220, 50, 50);
        DrawText($"HP: {_player.HP}/{_player.MaxHP}", new Vector2(10, 30), 1.2f, hpColor);

        // Level & XP
        int xpNeeded = _player.Level * XPPerLevel;
        DrawText($"Lv:{_player.Level}  XP:{_player.XP}/{xpNeeded}", new Vector2(10, 48), 1f, new Color(255, 215, 100));

        // Atk/Def stats
        DrawText($"Atk:{_player.Attack}  Def:{_player.Defense}", new Vector2(10, 62), 1f, new Color(180, 160, 200));

        // Enemy count
        DrawText($"Enemies: {CountAliveEnemies()}", new Vector2(10, 76), 1f, new Color(140, 130, 160));

        // Gold
        DrawText($"Gold: {_player.Gold}", new Vector2(10, 90), 1f, new Color(255, 215, 0));

        // Controls hint
        DrawText("WASD/Arrows: Move   R: Restart   N: New Dungeon   ESC: Menu", new Vector2(10, ScreenHeight - 30), 1f, new Color(80, 75, 90));

        _spriteBatch.End();
    }

    /// <summary>
    /// Draws a minimap in the top-right corner showing explored, visible, and unseen tiles, player, stairs, and enemies.
    /// </summary>
    private void DrawMinimap()
    {
        int mapW = _tileMap.Width, mapH = _tileMap.Height;
        int mmTile = 4; // minimap tile size in pixels
        int mmPad = 8; // padding from screen edge
        int mmW = mapW * mmTile, mmH = mapH * mmTile;
        int mmX = ScreenWidth - mmW - mmPad;
        int mmY = mmPad;
        var pixel = TextureFactory.Pixel;

        // Draw background
        _spriteBatch.Draw(pixel, new Rectangle(mmX - 2, mmY - 2, mmW + 4, mmH + 4), new Color(20, 18, 30, 220));

        // Tiles
        for (int x = 0; x < mapW; x++)
        {
            for (int y = 0; y < mapH; y++)
            {
                var vis = _fog.GetVisibility(x, y);
                Color c;
                if (vis == Visibility.Unseen)
                    c = new Color(10, 10, 18, 0);
                else if (vis == Visibility.Explored)
                    c = new Color(60, 60, 80);
                else
                {
                    var t = _tileMap.GetTile(x, y);
                    if (t == TileType.Wall) c = new Color(120, 110, 140);
                    else if (t == TileType.Door) c = new Color(180, 120, 60);
                    else if (t == TileType.StairsDown) c = new Color(80, 200, 200);
                    else c = new Color(180, 180, 220);
                }
                _spriteBatch.Draw(pixel, new Rectangle(mmX + x * mmTile, mmY + y * mmTile, mmTile, mmTile), c);
            }
        }
        // Stairs (always visible if explored)
        for (int x = 0; x < mapW; x++)
            for (int y = 0; y < mapH; y++)
                if (_tileMap.GetTile(x, y) == TileType.StairsDown && _fog.GetVisibility(x, y) != Visibility.Unseen)
                    _spriteBatch.Draw(pixel, new Rectangle(mmX + x * mmTile, mmY + y * mmTile, mmTile, mmTile), new Color(80, 200, 200));
        // Enemies
        foreach (var enemy in _enemies)
            if (enemy.IsAlive && _fog.GetVisibility(enemy.TileX, enemy.TileY) != Visibility.Unseen)
                _spriteBatch.Draw(pixel, new Rectangle(mmX + enemy.TileX * mmTile, mmY + enemy.TileY * mmTile, mmTile, mmTile), new Color(255, 80, 80));
        // Player
        _spriteBatch.Draw(pixel, new Rectangle(mmX + _player.TileX * mmTile, mmY + _player.TileY * mmTile, mmTile, mmTile), new Color(80, 255, 80));
        // Border
        for (int i = 0; i < mmW; i++)
        {
            _spriteBatch.Draw(pixel, new Rectangle(mmX + i, mmY - 2, 1, 2), Color.Black);
            _spriteBatch.Draw(pixel, new Rectangle(mmX + i, mmY + mmH, 1, 2), Color.Black);
        }
        for (int i = 0; i < mmH; i++)
        {
            _spriteBatch.Draw(pixel, new Rectangle(mmX - 2, mmY + i, 2, 1), Color.Black);
            _spriteBatch.Draw(pixel, new Rectangle(mmX + mmW, mmY + i, 2, 1), Color.Black);
        }
    }

    private void DrawEndScreen(string title, Color color)
    {
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

        DrawTextCentered(title, ScreenHeight / 2 - 40, 4f, color);
        DrawTextCentered("Press ENTER to Continue", ScreenHeight / 2 + 40, 1.5f, new Color(150, 140, 170));

        _spriteBatch.End();
    }

    // ==================== TEXT RENDERING ====================
    // Using pixel-based bitmap text since we have no SpriteFont loaded.
    // Each character is drawn as a small block of pixels.

    private static readonly Dictionary<char, bool[,]> CharMap = BuildCharMap();

    private static Dictionary<char, bool[,]> BuildCharMap()
    {
        var map = new Dictionary<char, bool[,]>();

        // 5x7 pixel font definitions for common characters
        AddChar(map, 'A', new[] { " ### ", "#   #", "#   #", "#####", "#   #", "#   #", "#   #" });
        AddChar(map, 'B', new[] { "#### ", "#   #", "#   #", "#### ", "#   #", "#   #", "#### " });
        AddChar(map, 'C', new[] { " ### ", "#   #", "#    ", "#    ", "#    ", "#   #", " ### " });
        AddChar(map, 'D', new[] { "#### ", "#   #", "#   #", "#   #", "#   #", "#   #", "#### " });
        AddChar(map, 'E', new[] { "#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#####" });
        AddChar(map, 'F', new[] { "#####", "#    ", "#    ", "#### ", "#    ", "#    ", "#    " });
        AddChar(map, 'G', new[] { " ### ", "#   #", "#    ", "# ###", "#   #", "#   #", " ### " });
        AddChar(map, 'H', new[] { "#   #", "#   #", "#   #", "#####", "#   #", "#   #", "#   #" });
        AddChar(map, 'I', new[] { " ### ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " });
        AddChar(map, 'J', new[] { "  ###", "   # ", "   # ", "   # ", "   # ", "#  # ", " ## " });
        AddChar(map, 'K', new[] { "#   #", "#  # ", "# #  ", "##   ", "# #  ", "#  # ", "#   #" });
        AddChar(map, 'L', new[] { "#    ", "#    ", "#    ", "#    ", "#    ", "#    ", "#####" });
        AddChar(map, 'M', new[] { "#   #", "## ##", "# # #", "#   #", "#   #", "#   #", "#   #" });
        AddChar(map, 'N', new[] { "#   #", "##  #", "# # #", "#  ##", "#   #", "#   #", "#   #" });
        AddChar(map, 'O', new[] { " ### ", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " });
        AddChar(map, 'P', new[] { "#### ", "#   #", "#   #", "#### ", "#    ", "#    ", "#    " });
        AddChar(map, 'Q', new[] { " ### ", "#   #", "#   #", "#   #", "# # #", "#  # ", " ## #" });
        AddChar(map, 'R', new[] { "#### ", "#   #", "#   #", "#### ", "# #  ", "#  # ", "#   #" });
        AddChar(map, 'S', new[] { " ####", "#    ", "#    ", " ### ", "    #", "    #", "#### " });
        AddChar(map, 'T', new[] { "#####", "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "  #  " });
        AddChar(map, 'U', new[] { "#   #", "#   #", "#   #", "#   #", "#   #", "#   #", " ### " });
        AddChar(map, 'V', new[] { "#   #", "#   #", "#   #", "#   #", " # # ", " # # ", "  #  " });
        AddChar(map, 'W', new[] { "#   #", "#   #", "#   #", "#   #", "# # #", "## ##", "#   #" });
        AddChar(map, 'X', new[] { "#   #", "#   #", " # # ", "  #  ", " # # ", "#   #", "#   #" });
        AddChar(map, 'Y', new[] { "#   #", "#   #", " # # ", "  #  ", "  #  ", "  #  ", "  #  " });
        AddChar(map, 'Z', new[] { "#####", "    #", "   # ", "  #  ", " #   ", "#    ", "#####" });

        AddChar(map, '0', new[] { " ### ", "#   #", "#  ##", "# # #", "##  #", "#   #", " ### " });
        AddChar(map, '1', new[] { "  #  ", " ##  ", "  #  ", "  #  ", "  #  ", "  #  ", " ### " });
        AddChar(map, '2', new[] { " ### ", "#   #", "    #", "  ## ", " #   ", "#    ", "#####" });
        AddChar(map, '3', new[] { " ### ", "#   #", "    #", "  ## ", "    #", "#   #", " ### " });
        AddChar(map, '4', new[] { "   # ", "  ## ", " # # ", "#  # ", "#####", "   # ", "   # " });
        AddChar(map, '5', new[] { "#####", "#    ", "#### ", "    #", "    #", "#   #", " ### " });
        AddChar(map, '6', new[] { " ### ", "#    ", "#    ", "#### ", "#   #", "#   #", " ### " });
        AddChar(map, '7', new[] { "#####", "    #", "   # ", "  #  ", " #   ", " #   ", " #   " });
        AddChar(map, '8', new[] { " ### ", "#   #", "#   #", " ### ", "#   #", "#   #", " ### " });
        AddChar(map, '9', new[] { " ### ", "#   #", "#   #", " ####", "    #", "    #", " ### " });

        AddChar(map, ' ', new[] { "     ", "     ", "     ", "     ", "     ", "     ", "     " });
        AddChar(map, ':', new[] { "     ", "  #  ", "  #  ", "     ", "  #  ", "  #  ", "     " });
        AddChar(map, '-', new[] { "     ", "     ", "     ", "#####", "     ", "     ", "     " });
        AddChar(map, '.', new[] { "     ", "     ", "     ", "     ", "     ", " ##  ", " ##  " });
        AddChar(map, ',', new[] { "     ", "     ", "     ", "     ", "  #  ", "  #  ", " #   " });
        AddChar(map, '!', new[] { "  #  ", "  #  ", "  #  ", "  #  ", "  #  ", "     ", "  #  " });
        AddChar(map, '?', new[] { " ### ", "#   #", "    #", "  ## ", "  #  ", "     ", "  #  " });
        AddChar(map, '/', new[] { "    #", "   # ", "   # ", "  #  ", " #   ", " #   ", "#    " });
        AddChar(map, '=', new[] { "     ", "     ", "#####", "     ", "#####", "     ", "     " });
        AddChar(map, '+', new[] { "     ", "  #  ", "  #  ", "#####", "  #  ", "  #  ", "     " });
        AddChar(map, '(', new[] { "  #  ", " #   ", "#    ", "#    ", "#    ", " #   ", "  #  " });
        AddChar(map, ')', new[] { "  #  ", "   # ", "    #", "    #", "    #", "   # ", "  #  " });

        // Lowercase (simple: same as uppercase for this bitmap font)
        for (char c = 'A'; c <= 'Z'; c++)
        {
            if (map.ContainsKey(c))
                map[(char)(c + 32)] = map[c];
        }

        return map;
    }

    private static void AddChar(Dictionary<char, bool[,]> map, char c, string[] rows)
    {
        int h = rows.Length;
        int w = rows[0].Length;
        var grid = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w && x < rows[y].Length; x++)
                grid[x, y] = rows[y][x] == '#';
        map[c] = grid;
    }

    /// <summary>
    /// Draw a string at a given pixel position using the bitmap font.
    /// </summary>
    private void DrawText(string text, Vector2 pos, float scale, Color color)
    {
        float cx = pos.X;
        float charW = 5 * scale;
        float charH = 7 * scale;
        var pixel = TextureFactory.Pixel;

        foreach (char ch in text)
        {
            if (CharMap.TryGetValue(ch, out var glyph))
            {
                int gw = glyph.GetLength(0);
                int gh = glyph.GetLength(1);
                for (int gy = 0; gy < gh; gy++)
                {
                    for (int gx = 0; gx < gw; gx++)
                    {
                        if (glyph[gx, gy])
                        {
                            _spriteBatch.Draw(pixel,
                                new Rectangle(
                                    (int)(cx + gx * scale),
                                    (int)(pos.Y + gy * scale),
                                    (int)scale + 1,
                                    (int)scale + 1),
                                color);
                        }
                    }
                }
            }
            cx += (charW + scale); // char width + 1px spacing
        }
    }

    /// <summary>
    /// Draw text centered horizontally on screen.
    /// </summary>
    private void DrawTextCentered(string text, float y, float scale, Color color)
    {
        float charW = 5 * scale + scale; // each char = 5 pixels + 1 spacing
        float totalW = text.Length * charW;
        float x = (ScreenWidth - totalW) / 2f;
        DrawText(text, new Vector2(x, y), scale, color);
    }
}
