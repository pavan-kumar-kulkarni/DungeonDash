// ------------------------------------------------------------------
// TextureFactory — Runtime pixel-art texture generation
// Creates colored tile textures without any external image files.
// Each texture is a small square with shading for depth.
// ------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonDash;

public static class TextureFactory
{
    public const int TileSize = 32;

    private static Texture2D _wallTex;
    private static Texture2D _floorTex;
    private static Texture2D _stairsTex;
    private static Texture2D _doorTex;
    private static Texture2D _pixelTex;
    private static Texture2D _playerTex;
    private static Texture2D _skeletonTex;
    private static Texture2D _goblinTex;
    private static Texture2D _demonTex;
    private static Texture2D _healthPotionTex;
    private static Texture2D _attackGemTex;
    private static Texture2D _defenseGemTex;
    private static Texture2D _goldTex;

    public static Texture2D Wall => _wallTex;
    public static Texture2D Floor => _floorTex;
    public static Texture2D Stairs => _stairsTex;
    public static Texture2D Door => _doorTex;
    public static Texture2D Pixel => _pixelTex;
    public static Texture2D Player => _playerTex;

    /// <summary>
    /// Call once during LoadContent to generate all tile textures.
    /// </summary>
    public static void Initialize(GraphicsDevice gd)
    {
        _pixelTex = CreateSolid(gd, 1, 1, Color.White);
        _wallTex = CreateWallTexture(gd);
        _floorTex = CreateFloorTexture(gd);
        _stairsTex = CreateStairsTexture(gd);
        _doorTex = CreateDoorTexture(gd);
        _playerTex = CreatePlayerTexture(gd);
        _skeletonTex = CreateEnemyTexture(gd, new Color(200, 200, 190), new Color(160, 160, 150), new Color(80, 80, 70));
        _goblinTex = CreateEnemyTexture(gd, new Color(80, 180, 60), new Color(50, 130, 35), new Color(30, 80, 20));
        _demonTex = CreateEnemyTexture(gd, new Color(200, 50, 50), new Color(150, 30, 30), new Color(100, 15, 15));
        _healthPotionTex = CreateHealthPotionTexture(gd);
        _attackGemTex = CreateGemTexture(gd, new Color(255, 100, 30), new Color(200, 60, 10));
        _defenseGemTex = CreateGemTexture(gd, new Color(60, 130, 255), new Color(30, 80, 200));
        _goldTex = CreateGoldTexture(gd);
    }

    /// <summary>
    /// Returns the texture for a given enemy type.
    /// </summary>
    public static Texture2D GetEnemyTexture(EnemyType type)
    {
        return type switch
        {
            EnemyType.Skeleton => _skeletonTex,
            EnemyType.Goblin => _goblinTex,
            EnemyType.Demon => _demonTex,
            _ => _skeletonTex
        };
    }

    /// <summary>
    /// Returns the texture matching a given tile type.
    /// </summary>
    public static Texture2D GetTexture(TileType type)
    {
        return type switch
        {
            TileType.Wall => _wallTex,
            TileType.Floor => _floorTex,
            TileType.StairsDown => _stairsTex,
            TileType.Door => _doorTex,
            _ => _floorTex
        };
    }

    /// <summary>
    /// Returns the texture for a given item type.
    /// </summary>
    public static Texture2D GetItemTexture(ItemType type)
    {
        return type switch
        {
            ItemType.HealthPotion => _healthPotionTex,
            ItemType.AttackGem => _attackGemTex,
            ItemType.DefenseGem => _defenseGemTex,
            ItemType.Gold => _goldTex,
            _ => _goldTex
        };
    }

    // --- texture builders ---

    private static Texture2D CreateSolid(GraphicsDevice gd, int w, int h, Color color)
    {
        var tex = new Texture2D(gd, w, h);
        var data = new Color[w * h];
        for (int i = 0; i < data.Length; i++)
            data[i] = color;
        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateWallTexture(GraphicsDevice gd)
    {
        int s = TileSize;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];

        var baseColor = new Color(60, 55, 70);
        var darkEdge = new Color(35, 30, 45);
        var highlight = new Color(80, 75, 90);
        var brick = new Color(50, 45, 60);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color c = baseColor;

                // Dark border (1px)
                if (x == 0 || y == 0 || x == s - 1 || y == s - 1)
                    c = darkEdge;
                // Brick pattern - horizontal lines
                else if (y == 8 || y == 16 || y == 24)
                    c = darkEdge;
                // Brick pattern - vertical offsets
                else if (y < 8 && x == 16)
                    c = darkEdge;
                else if (y > 8 && y < 16 && x == 8)
                    c = darkEdge;
                else if (y > 16 && y < 24 && x == 24)
                    c = darkEdge;
                else if (y > 24 && x == 12)
                    c = darkEdge;
                // Top-left highlight on each brick
                else if ((y == 1 || y == 9 || y == 17 || y == 25) && x > 1)
                    c = highlight;
                // Subtle noise
                else if ((x + y) % 7 == 0)
                    c = brick;

                data[y * s + x] = c;
            }
        }

        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateFloorTexture(GraphicsDevice gd)
    {
        int s = TileSize;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];

        var baseColor = new Color(30, 28, 35);
        var accent = new Color(38, 35, 42);
        var dot = new Color(24, 22, 28);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color c = baseColor;

                // Subtle grid pattern
                if (x == 0 || y == 0)
                    c = dot;
                // Random-looking noise
                else if ((x * 7 + y * 13) % 23 == 0)
                    c = accent;
                else if ((x * 3 + y * 11) % 29 == 0)
                    c = dot;

                data[y * s + x] = c;
            }
        }

        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateStairsTexture(GraphicsDevice gd)
    {
        int s = TileSize;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];

        var bg = new Color(30, 28, 35);
        var stair = new Color(80, 160, 200);
        var stairDark = new Color(50, 110, 140);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color c = bg;

                // Draw stair steps (5 steps going down-right)
                int step = (x + y) / 8;
                if (step >= 1 && step <= 5)
                {
                    if ((x + y) % 8 < 2)
                        c = stairDark;
                    else
                        c = stair;
                }

                // Border
                if (x == 0 || y == 0 || x == s - 1 || y == s - 1)
                    c = stairDark;

                data[y * s + x] = c;
            }
        }

        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreatePlayerTexture(GraphicsDevice gd)
    {
        int s = TileSize;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];

        var transparent = Color.Transparent;
        var skin = new Color(220, 180, 140);
        var armor = new Color(60, 120, 200);
        var armorDark = new Color(40, 80, 150);
        var helmet = new Color(180, 180, 190);
        var eye = new Color(255, 255, 255);
        var pupil = new Color(30, 30, 30);
        var boot = new Color(80, 60, 40);
        var sword = new Color(200, 200, 210);
        var swordHilt = new Color(160, 120, 40);

        // Fill transparent
        for (int i = 0; i < data.Length; i++)
            data[i] = transparent;

        // Draw a small knight character (centered in 32x32)
        // Helmet (rows 4-9)
        for (int x = 11; x <= 20; x++)
            for (int y = 4; y <= 5; y++)
                data[y * s + x] = helmet;
        for (int x = 10; x <= 21; x++)
            for (int y = 6; y <= 9; y++)
                data[y * s + x] = helmet;

        // Face visor opening (rows 7-8)
        for (int x = 12; x <= 19; x++)
            for (int y = 7; y <= 8; y++)
                data[y * s + x] = skin;

        // Eyes
        data[7 * s + 13] = eye; data[7 * s + 14] = pupil;
        data[7 * s + 17] = eye; data[7 * s + 18] = pupil;

        // Body armor (rows 10-20)
        for (int x = 10; x <= 21; x++)
            for (int y = 10; y <= 20; y++)
                data[y * s + x] = armor;
        // Armor shading (left edge darker)
        for (int y = 10; y <= 20; y++)
        {
            data[y * s + 10] = armorDark;
            data[y * s + 11] = armorDark;
        }
        // Belt
        for (int x = 10; x <= 21; x++)
            data[16 * s + x] = swordHilt;

        // Arms (extend from body)
        for (int y = 11; y <= 17; y++)
        {
            data[y * s + 8] = armor;
            data[y * s + 9] = armor;
            data[y * s + 22] = armor;
            data[y * s + 23] = armor;
        }
        // Hands
        data[17 * s + 8] = skin; data[17 * s + 9] = skin;
        data[17 * s + 22] = skin; data[17 * s + 23] = skin;

        // Sword in right hand
        for (int y = 8; y <= 17; y++)
            data[y * s + 24] = sword;
        data[17 * s + 24] = swordHilt;
        data[18 * s + 24] = swordHilt;

        // Legs (rows 21-26)
        for (int y = 21; y <= 26; y++)
        {
            data[y * s + 12] = armor; data[y * s + 13] = armor;
            data[y * s + 14] = armorDark;
            data[y * s + 17] = armorDark;
            data[y * s + 18] = armor; data[y * s + 19] = armor;
        }

        // Boots (rows 27-29)
        for (int y = 27; y <= 29; y++)
        {
            data[y * s + 11] = boot; data[y * s + 12] = boot;
            data[y * s + 13] = boot; data[y * s + 14] = boot;
            data[y * s + 17] = boot; data[y * s + 18] = boot;
            data[y * s + 19] = boot; data[y * s + 20] = boot;
        }

        tex.SetData(data);
        return tex;
    }

    private static Texture2D CreateDoorTexture(GraphicsDevice gd)
    {
        int s = TileSize;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];

        var frame = new Color(100, 70, 40);
        var wood = new Color(130, 90, 50);
        var knob = new Color(200, 180, 60);
        var bg = new Color(30, 28, 35);

        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                Color c = bg;

                // Door frame
                if (x >= 4 && x < s - 4 && y >= 2 && y < s - 2)
                {
                    c = wood;
                    // Frame border
                    if (x == 4 || x == s - 5 || y == 2 || y == s - 3)
                        c = frame;
                    // Vertical plank line
                    if (x == 16 && y > 3 && y < s - 3)
                        c = frame;
                    // Door knob
                    if (x >= 20 && x <= 22 && y >= 15 && y <= 17)
                        c = knob;
                }

                data[y * s + x] = c;
            }
        }

        tex.SetData(data);
        return tex;
    }

    /// <summary>
    /// Create a generic enemy texture using three color tones.
    /// Draws a small humanoid creature with horns/spikes.
    /// </summary>
    private static Texture2D CreateEnemyTexture(GraphicsDevice gd, Color body, Color dark, Color accent)
    {
        int s = TileSize;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];
        var transparent = Color.Transparent;
        var eye = new Color(255, 50, 50); // Red glowing eyes

        for (int i = 0; i < data.Length; i++)
            data[i] = transparent;

        // Horns/spikes (rows 2-5)
        data[2 * s + 10] = accent; data[2 * s + 21] = accent;
        data[3 * s + 11] = accent; data[3 * s + 20] = accent;
        data[4 * s + 12] = dark; data[4 * s + 19] = dark;

        // Head (rows 5-11)
        for (int x = 12; x <= 19; x++)
            for (int y = 5; y <= 11; y++)
                data[y * s + x] = body;
        // Dark edges of head
        for (int y = 5; y <= 11; y++)
        {
            data[y * s + 12] = dark;
            data[y * s + 19] = dark;
        }
        data[5 * s + 13] = dark; data[5 * s + 18] = dark;

        // Eyes (row 8)
        data[8 * s + 14] = eye; data[8 * s + 15] = eye;
        data[8 * s + 17] = eye; data[8 * s + 18] = eye;

        // Mouth (row 10)
        for (int x = 14; x <= 17; x++)
            data[10 * s + x] = dark;

        // Body (rows 12-22)
        for (int x = 10; x <= 21; x++)
            for (int y = 12; y <= 22; y++)
                data[y * s + x] = body;
        // Body shading
        for (int y = 12; y <= 22; y++)
        {
            data[y * s + 10] = dark; data[y * s + 11] = dark;
            data[y * s + 20] = dark; data[y * s + 21] = dark;
        }

        // Arms (rows 13-19)
        for (int y = 13; y <= 19; y++)
        {
            data[y * s + 8] = body; data[y * s + 9] = dark;
            data[y * s + 22] = dark; data[y * s + 23] = body;
        }
        // Claws
        data[19 * s + 7] = accent; data[19 * s + 8] = accent;
        data[19 * s + 23] = accent; data[19 * s + 24] = accent;

        // Legs (rows 23-28)
        for (int y = 23; y <= 28; y++)
        {
            data[y * s + 12] = body; data[y * s + 13] = dark;
            data[y * s + 14] = body;
            data[y * s + 17] = body;
            data[y * s + 18] = dark; data[y * s + 19] = body;
        }

        // Feet (row 29)
        for (int x = 11; x <= 14; x++) data[29 * s + x] = dark;
        for (int x = 17; x <= 20; x++) data[29 * s + x] = dark;

        tex.SetData(data);
        return tex;
    }

    /// <summary>
    /// Create a health potion texture (24x24, red bottle with heart).
    /// </summary>
    private static Texture2D CreateHealthPotionTexture(GraphicsDevice gd)
    {
        int s = 24;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];
        var red = new Color(220, 40, 40);
        var darkRed = new Color(160, 20, 20);
        var glass = new Color(255, 100, 100);
        var cork = new Color(160, 120, 60);

        // Cork (rows 2-4)
        for (int x = 9; x <= 14; x++)
            for (int y = 2; y <= 4; y++)
                data[y * s + x] = cork;

        // Bottle neck (rows 5-7)
        for (int x = 10; x <= 13; x++)
            for (int y = 5; y <= 7; y++)
                data[y * s + x] = glass;

        // Bottle body (rows 8-19)
        for (int x = 6; x <= 17; x++)
            for (int y = 8; y <= 19; y++)
                data[y * s + x] = red;
        // Highlight
        for (int y = 9; y <= 14; y++)
            data[y * s + 8] = glass;
        // Dark edge
        for (int y = 8; y <= 19; y++)
        {
            data[y * s + 6] = darkRed;
            data[y * s + 17] = darkRed;
        }

        // Heart in center (rows 11-15)
        var white = new Color(255, 200, 200);
        data[11 * s + 9] = white; data[11 * s + 10] = white;
        data[11 * s + 13] = white; data[11 * s + 14] = white;
        for (int x = 9; x <= 14; x++) data[12 * s + x] = white;
        for (int x = 9; x <= 14; x++) data[13 * s + x] = white;
        for (int x = 10; x <= 13; x++) data[14 * s + x] = white;
        data[15 * s + 11] = white; data[15 * s + 12] = white;

        // Bottom (row 20)
        for (int x = 6; x <= 17; x++)
            data[20 * s + x] = darkRed;

        tex.SetData(data);
        return tex;
    }

    /// <summary>
    /// Create a gem texture (24x24, diamond shape).
    /// </summary>
    private static Texture2D CreateGemTexture(GraphicsDevice gd, Color bright, Color dark)
    {
        int s = 24;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];
        var shine = new Color(
            Math.Min(255, bright.R + 80),
            Math.Min(255, bright.G + 80),
            Math.Min(255, bright.B + 80));

        // Diamond shape: centered, rows 4-19
        int cx = 12;
        for (int y = 4; y <= 11; y++)
        {
            int halfW = y - 4;
            for (int x = cx - halfW; x <= cx + halfW; x++)
                data[y * s + x] = (x < cx) ? bright : dark;
        }
        for (int y = 12; y <= 19; y++)
        {
            int halfW = 19 - y;
            for (int x = cx - halfW; x <= cx + halfW; x++)
                data[y * s + x] = (x < cx) ? bright : dark;
        }

        // Shine highlight
        data[6 * s + 11] = shine; data[7 * s + 11] = shine;
        data[7 * s + 10] = shine; data[8 * s + 10] = shine;

        tex.SetData(data);
        return tex;
    }

    /// <summary>
    /// Create a gold coin texture (24x24, yellow circle).
    /// </summary>
    private static Texture2D CreateGoldTexture(GraphicsDevice gd)
    {
        int s = 24;
        var tex = new Texture2D(gd, s, s);
        var data = new Color[s * s];
        var gold = new Color(255, 215, 0);
        var darkGold = new Color(200, 160, 0);
        var shine = new Color(255, 245, 150);

        int cx = 12, cy = 12, r = 8;
        for (int y = 0; y < s; y++)
        {
            for (int x = 0; x < s; x++)
            {
                int dx = x - cx, dy = y - cy;
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist <= r)
                {
                    if (dist >= r - 1)
                        data[y * s + x] = darkGold;
                    else if (dx + dy < -3)
                        data[y * s + x] = shine;
                    else
                        data[y * s + x] = gold;
                }
            }
        }

        // Dollar sign
        data[10 * s + 12] = darkGold;
        data[11 * s + 11] = darkGold; data[11 * s + 12] = darkGold; data[11 * s + 13] = darkGold;
        data[12 * s + 11] = darkGold; data[12 * s + 12] = darkGold;
        data[13 * s + 12] = darkGold; data[13 * s + 13] = darkGold;
        data[14 * s + 11] = darkGold; data[14 * s + 12] = darkGold; data[14 * s + 13] = darkGold;

        tex.SetData(data);
        return tex;
    }
}
