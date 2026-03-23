// ------------------------------------------------------------------
// Item — Collectible pickups scattered throughout the dungeon
//
// Item types:
//   HealthPotion — restores HP
//   AttackGem    — permanently boosts attack
//   DefenseGem   — permanently boosts defense
//   Gold         — score/currency
//   Key          — unlocks special doors (future use)
//
// Items sit on floor tiles. The player picks them up by walking
// over them. Each item has a bobbing animation to stand out.
// ------------------------------------------------------------------
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DungeonDash;

public enum ItemType
{
    HealthPotion,
    AttackGem,
    DefenseGem,
    Gold
}

public class Item
{
    public ItemType Type { get; }
    public int TileX { get; }
    public int TileY { get; }
    public bool Collected { get; set; }

    // Bobbing animation
    private float _bobTimer;
    private readonly float _bobOffset; // Random phase offset so items don't bob in sync

    public Item(ItemType type, int tileX, int tileY, Random rng)
    {
        Type = type;
        TileX = tileX;
        TileY = tileY;
        Collected = false;
        _bobOffset = (float)(rng.NextDouble() * MathF.PI * 2f);
    }

    /// <summary>
    /// Update bobbing animation.
    /// </summary>
    public void Update(float dt)
    {
        _bobTimer += dt;
    }

    /// <summary>
    /// Draw the item at its world position with a gentle bob.
    /// </summary>
    public void Draw(SpriteBatch spriteBatch, Vector2 cameraOffset)
    {
        if (Collected) return;

        var tex = TextureFactory.GetItemTexture(Type);
        float bob = MathF.Sin((_bobTimer + _bobOffset) * 3f) * 3f;
        var pos = new Vector2(
            TileX * TextureFactory.TileSize + cameraOffset.X + 4,  // center in tile
            TileY * TextureFactory.TileSize + cameraOffset.Y + 4 + bob
        );

        spriteBatch.Draw(tex, pos, Color.White);
    }

    /// <summary>
    /// Apply this item's effect to the player. Returns a description string.
    /// </summary>
    public string Apply(Player player)
    {
        switch (Type)
        {
            case ItemType.HealthPotion:
                int healed = Math.Min(10, player.MaxHP - player.HP);
                player.Heal(10);
                return $"+{healed} HP";

            case ItemType.AttackGem:
                player.Attack += 1;
                return "+1 ATK";

            case ItemType.DefenseGem:
                player.Defense += 1;
                return "+1 DEF";

            case ItemType.Gold:
                player.Gold += 15;
                return "+15 GOLD";

            default:
                return "";
        }
    }

    /// <summary>
    /// Color used for the pickup message.
    /// </summary>
    public Color MessageColor => Type switch
    {
        ItemType.HealthPotion => new Color(80, 255, 80),
        ItemType.AttackGem => new Color(255, 130, 50),
        ItemType.DefenseGem => new Color(80, 150, 255),
        ItemType.Gold => new Color(255, 215, 0),
        _ => Color.White
    };
}
