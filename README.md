# 🏚️ INFINITE HAUS

A Unity 2D pixel-art endless runner set in a surreal, ever-shifting haunted house.

## 🎮 Overview

INFINITE HAUS combines fast-paced auto-run platforming with psychological horror, light roguelike structure, and a modular corridor system. Players must escape an infinite maze of haunted hallways while being pursued by three unique supernatural entities.

## 🎯 Core Features

- **Auto-run platforming** with tight jump physics
- **Three unique monsters** with different behaviors
- **Dynamic Chase Meter** system
- **Procedural corridor generation**
- **Door choice mechanics** affecting difficulty
- **Collectibles & permanent upgrades**
- **Modular theme system** (ScriptableObject-based)
- **Complete save/load system**

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Core/              # Game managers, bootstrap
│   ├── Player/            # Player controller, health, animations
│   ├── Monsters/          # AI for Shadow, Crawler, Mimic
│   ├── Hazards/           # All trap types
│   ├── Collectibles/      # Rewards and pickups
│   ├── Level/             # Corridor generation, doors
│   ├── UI/                # HUD, menus, countdown
│   ├── Theme/             # Theme management
│   ├── Save/              # Save system
│   └── Data/              # ScriptableObject definitions
├── Prefabs/               # All game prefabs
├── ScriptableObjects/     # Theme configs, difficulty profiles
├── Sprites/               # Pixel art assets
├── Audio/                 # Music and SFX
└── Scenes/                # Game scenes
```

## 🎮 Controls

- **Space/Up Arrow**: Jump
- **Left/Right Arrow**: Choose door
- **Escape**: Pause menu

## 🧩 Systems

### Player System
- Auto-run with configurable speed
- Variable jump height (hold to jump higher)
- Coyote time for forgiving platforming
- Jump buffering for responsive input
- Invincibility frames after damage

### Chase System
- **The Shadow**: Constant pursuer with rubberband AI
- **The Crawler**: Ceiling/wall ambusher with slow debuff
- **The Mimic**: Disguises as doors/objects
- Chase Meter fills from mistakes and over time

### Hazards
- Spikes, Goo puddles, Crumbling platforms
- Falling ceilings, Pitfalls, Lantern zones
- Mimic doors (fake traps)

### Rewards
- **Soul Shards**: +10 points, unlock cosmetics
- **Rare Relics**: Permanent +1 Max HP (collect 3)
- **Chase Crystals**: Freeze Chase Meter for 3s

### Theme System
All visual/audio elements defined via ScriptableObjects:
- Color palettes
- Background sprites (parallax)
- Tilesets and hazard skins
- Music & SFX
- UI skins
- Overlay effects (fog, VHS)

## 🚀 Getting Started

1. Open project in Unity 2021.3 or later
2. Install Unity Input System package
3. Open `Scenes/Boot` scene
4. Press Play

## 🛠️ Development Guidelines

- All visuals exposed for drag-and-drop in Unity Editor
- Event-driven architecture (C# Actions/UnityEvents)
- ScriptableObject-based configuration
- Clean separation of concerns (no UI in gameplay scripts)
- Extensive use of `[Header]` and `[Tooltip]` attributes

## 📝 License

Created for educational/portfolio purposes.

---

**Built with Unity & C#**
