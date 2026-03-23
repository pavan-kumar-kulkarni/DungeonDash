
# 🗝️ DungeonDash

A **procedural roguelike dungeon crawler** built from scratch in **MonoGame 3.8** and **C# (.NET 8)**. All graphics and sound effects are generated at runtime—no external assets required.

> Built as a portfolio work sample showcasing C# and MonoGame expertise.

---

## 🎮 Gameplay

Descend through procedurally generated dungeons, battling enemies and collecting items. Each floor is unique, with fog of war, turn-based movement, and enemy AI. Level up, gather gold, and survive to reach the final floor!

**Controls:** WASD / Arrow keys to move | **Tab** minimap | **R** restart | **N** new dungeon | **ESC** menu

---

## 🛠 Technical Highlights

### Architecture & Patterns
- **Procedural dungeon generation** — BSP rooms, corridors, and random item/enemy placement
- **Turn-based system** — player and enemies act in sequence, with smooth grid movement
- **Clean separation of concerns** — Player, Enemy, Item, FogOfWar, and DungeonGenerator classes
- **Combat, XP, and leveling** — stats, damage, and progression

### Rendering & UI
- **All graphics generated at runtime** — no sprites or external images
- **Custom 5x7 bitmap font** — pixel-perfect text rendering
- **HUD and minimap** — real-time stats, minimap with fog/exploration

### Audio System
- **Procedural sound effects** — all SFX generated in code using MonoGame's SoundEffect API
- **No audio files** — move, attack, pickup, level up, death, and stairs SFX

### Game Feel & Polish
- **Smooth camera follow** — lerped camera for player focus
- **Floating combat messages** — damage, XP, and level-up popups
- **Enemy AI** — idle, patrol, chase, and attack states
- **Fog of war** — recursive shadowcasting for visibility

---

## 📁 Project Structure

```
DungeonDash/
├── Game1.cs            # Main game loop, input, HUD, minimap
├── DungeonGenerator.cs # BSP dungeon, room/corridor logic
├── Player.cs           # Player movement, stats, animation
├── Enemy.cs            # Enemy AI, combat, animation
├── Item.cs             # Pickups, item logic
├── FogOfWar.cs         # Visibility, shadowcasting
├── TextureFactory.cs   # Runtime texture/font generation
├── SoundFactory.cs     # Procedural SFX
├── ...
```

---

## 🚀 How to Run
1. Install [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) and [MonoGame 3.8](https://www.monogame.net/downloads/).
2. Clone this repo:
   ```
   git clone https://github.com/pavan-kumar-kulkarni/DungeonDash.git
   cd DungeonDash
   ```
3. Build and run:
   ```
   dotnet run
   ```

---

## 📚 Portfolio
This project was created for a Handshake AI Game Developer portfolio. All code, graphics, and audio are original and generated at runtime. See also:
- [CrystalRunner (Godot)](https://github.com/pavan-kumar-kulkarni/CrystalRunner)
- [NovaStrike (Godot)](https://github.com/pavan-kumar-kulkarni/NovaStrike)
- [Portfolio Website](https://github.com/pavan-kumar-kulkarni/portfolio)

---
© 2026 Pavan Kumar Kulkarni. All rights reserved.
