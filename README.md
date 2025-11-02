# 🏚️ INFINITE HAUS

A Unity 2D pixel-art endless runner set in a surreal, ever-shifting haunted house.

## 🎮 Overview

INFINITE HAUS combines fast-paced auto-run platforming with psychological horror, light roguelike structure, and a modular corridor system. Players must escape an infinite maze of haunted hallways while being pursued by three unique supernatural entities.

## 🎯 Core Features

- **Auto-run platforming** with tight jump physics (coyote time, jump buffering)
- **Three unique monsters** with different behaviors (Shadow, Crawler, Mimic)
- **Dynamic Chase Meter** system (0-100%)
- **Procedural corridor generation** with modular chunks
- **Door choice mechanics** affecting difficulty & rewards
- **Collectibles & permanent upgrades** (Soul Shards, Relics, Chase Crystals)
- **Cosmetic shop system** with Diamond currency (💎)
- **Cross-platform support** (PC/Mac/iOS/Android)
- **Gesture-based mobile controls** (swipe & tap)
- **Modular theme system** (ScriptableObject-based)
- **Complete save/load system** with cloud-ready architecture
- **Object pooling** for optimized performance

## 📁 Project Structure

```
Assets/
├── Scripts/
│   ├── Core/              # Game managers, bootstrap, object pooling
│   ├── Player/            # Player controller, health, animations
│   ├── Monsters/          # AI for Shadow, Crawler, Mimic
│   ├── Hazards/           # All trap types
│   ├── Collectibles/      # Rewards and pickups
│   ├── Level/             # Corridor generation, doors
│   ├── UI/                # HUD, menus, countdown, shop
│   ├── Shop/              # Shop system, cosmetics, currency
│   ├── Input/             # Cross-platform input, gestures
│   ├── Theme/             # Theme management
│   ├── Save/              # Save system
│   └── Data/              # ScriptableObject definitions
├── Prefabs/               # All game prefabs
├── ScriptableObjects/     # Theme configs, difficulty profiles, cosmetics
├── Sprites/               # Pixel art assets
├── Audio/                 # Music and SFX
└── Scenes/                # Game scenes
```

## 🎮 Controls

### Desktop (PC/Mac)
- **W**: Jump
- **A**: Left Door
- **D**: Right Door
- **Tab** or **I**: Open Shop
- **Escape**: Pause Menu

### Mobile (iOS/Android)
- **Swipe Up**: Jump
- **Swipe Left**: Left Door
- **Swipe Right**: Right Door
- **Tap Top-Left Corner**: Open Shop
- **Tap Top-Right Corner**: Pause Menu

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

### Shop System
Complete cosmetic shop with premium currency:
- **Diamond Currency (💎)**: Earn through gameplay or IAP
- **8 Cosmetic Types**: Skins, trails, UI themes, music, filters, animations
- **5 Rarity Tiers**: Common → Mythic with color coding
- **Ownership Tracking**: Persistent across sessions
- **Live Application**: Equip cosmetics instantly
- **Milestone Rewards**: Earn diamonds for achievements

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
- Cross-platform compatible (desktop & mobile)
- Performance optimized with object pooling

## 📚 Documentation

- **[SETUP_GUIDE.md](SETUP_GUIDE.md)**: Complete Unity setup instructions
- **[ARCHITECTURE.md](ARCHITECTURE.md)**: System architecture and design patterns
- **[SCRIPT_REFERENCE.md](SCRIPT_REFERENCE.md)**: Complete API documentation
- **[FINALIZATION_GUIDE.md](FINALIZATION_GUIDE.md)**: Shop system & cross-platform setup

## 🎯 Performance Targets

- **Desktop**: 60 FPS minimum (uncapped)
- **Mobile**: 60 FPS on mid-range devices (iPhone 8+, Galaxy S9+)
- **Touch Response**: <100ms input lag
- **Memory**: <200MB heap on mobile
- **Draw Calls**: <50 per frame

## 📝 License

Created for educational/portfolio purposes.

---

**Built with Unity & C#**
**Cross-Platform Ready** • **Shop System** • **Mobile Optimized** 🎮
