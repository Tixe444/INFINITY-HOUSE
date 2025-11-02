# 🏚️ Infinite Haus - Setup Guide

## 📋 Prerequisites

- **Unity 2021.3 LTS or later**
- **Unity Input System package** (for enhanced input handling)
- **TextMeshPro** (usually included by default)
- Basic understanding of Unity Editor

---

## 🚀 Quick Start

### 1. Project Setup

1. Open Unity Hub
2. Create a new **2D project** or open this project
3. Ensure the following packages are installed:
   - TextMeshPro
   - 2D Sprite
   - 2D Tilemap Editor (optional)

### 2. Scene Structure

Create the following scenes in your project:

#### **Boot Scene** (`Scenes/Boot.unity`)
- Create empty GameObject: `Bootstrap`
  - Add component: `Bootstrap.cs`
  - Configure: Set menu scene name to "MainMenu"

#### **Main Menu Scene** (`Scenes/MainMenu.unity`)
- Create UI Canvas
  - Add title text, play button, settings button, quit button
  - Hook up buttons to scene transitions

#### **Game Scene** (`Scenes/Game.unity`)
This is the main gameplay scene. Set it up as follows:

**Core Managers (Empty GameObjects):**
- `GameManager` → Add `GameManager.cs`
- `ChaseSystem` → Add `ChaseSystem.cs`
- `RewardManager` → Add `RewardManager.cs`
- `CorridorManager` → Add `CorridorManager.cs`
- `ThemeManager` → Add `ThemeManager.cs`
- `SettingsManager` → Add `SettingsManager.cs`
- `InputManager` → Add `InputManager.cs`

**Player:**
- Create 2D Sprite GameObject: `Player`
  - Add `Rigidbody2D` (set to Dynamic, gravity scale 2-3)
  - Add `BoxCollider2D`
  - Add `PlayerController.cs`
  - Add `PlayerHealth.cs`
  - Add `PlayerAnimator.cs` (requires Animator component)
  - Tag as "Player"

**Monsters:**
- Create empty: `Monsters`
  - Child: `Shadow` → Add `Shadow.cs`
  - Child: `Crawler` → Add `Crawler.cs`
  - Child: `Mimic` → Add `Mimic.cs`

**UI:**
- Create UI Canvas: `UI`
  - Child: `HUD` → Add `HUDManager.cs`
  - Child: `StartCountdown` → Add `StartCountdown.cs`
  - Child: `GameOverScreen` → Add `GameOverScreen.cs`
  - Add to root: `UIController.cs`

**Camera:**
- Main Camera:
  - Add smooth follow script (or Cinemachine)
  - Set to follow Player

---

## 🎨 Creating Assets

### ScriptableObjects

#### **1. Theme Configuration**

Right-click in Project → Create → Infinite Haus → Theme Config

Create a "Spooky Cartoon Theme":
- **Theme Name**: "Spooky Cartoon"
- **Primary Color**: Purple (R:0.4, G:0.2, B:0.6)
- **Secondary Color**: Orange (R:1, G:0.5, B:0)
- Assign sprites for backgrounds, doors, hazards
- Assign audio clips for music and SFX

#### **2. Difficulty Profiles**

Create → Infinite Haus → Difficulty Profile

Create three profiles:
- **Easy** (minDistance: 0)
  - Player Speed: 5
  - Chase Fill Rate: 1
  - Hazards Per Chunk: 2

- **Medium** (minDistance: 100)
  - Player Speed: 6
  - Chase Fill Rate: 1.5
  - Hazards Per Chunk: 3

- **Hard** (minDistance: 200)
  - Player Speed: 7
  - Chase Fill Rate: 2
  - Hazards Per Chunk: 5

#### **3. Corridor Sets**

Create → Infinite Haus → Corridor Set

Create corridor sets for each difficulty:
- Assign corridor chunk prefabs
- Link to appropriate DifficultyProfile
- Set spawn weights (if needed)

---

## 🧱 Creating Prefabs

### Corridor Chunk Prefab

1. Create empty GameObject: `CorridorChunk_01`
2. Add `CorridorChunk.cs`
3. Add child objects:
   - `StartPoint` (empty at 0,0,0)
   - `EndPoint` (empty at 20,0,0)
   - `HazardSpawns` (empty parent with multiple spawn point children)
   - `CollectibleSpawns` (same structure)
   - `DoorSpawns` (at end of corridor)
4. Add floor/wall sprites
5. Save as prefab

### Door Prefab

1. Create 2D Sprite: `Door`
2. Add `BoxCollider2D` (set as trigger)
3. Add `DoorHandler.cs`
4. Assign door sprites
5. Save as prefab

### Hazard Prefabs

Create prefabs for each hazard type:
- `Spike` → Add `Spike.cs`
- `Goo` → Add `Goo.cs`
- `CrumblingPlatform` → Add `CrumblingPlatform.cs`
- `FallingCeiling` → Add `FallingCeiling.cs`
- `Pitfall` → Add `Pitfall.cs`
- `LanternZone` → Add `LanternZone.cs`

Each needs:
- SpriteRenderer
- Collider2D (trigger for most)
- Particle effects (optional)

### Collectible Prefabs

- `SoulShard` → Add `SoulShard.cs`
- `Relic` → Add `Relic.cs`
- `ChaseCrystal` → Add `ChaseCrystal.cs`

Each needs:
- SpriteRenderer
- CircleCollider2D (trigger)
- Optional particle effects

---

## 🔗 Linking References

### In GameManager

Drag and drop references:
- Player Controller
- Player Health
- Chase System
- Reward Manager
- Corridor Manager
- UI Controller
- Theme Manager
- Settings Manager

### In CorridorManager

- Assign Corridor Sets array
- Assign Door Prefab
- Assign Hazard Prefabs array
- Assign Collectible Prefabs array
- Reference Player Transform

### In ChaseSystem

- Reference Shadow, Crawler, Mimic

### In ThemeManager

- Assign Available Themes array
- Reference Main Camera
- Music/Ambient audio sources (auto-created if not assigned)

---

## ⚙️ Unity Settings

### Physics2D Settings

Edit → Project Settings → Physics2D:
- Gravity: -20 to -30 (for heavier feel)
- Create layers:
  - Player
  - Ground
  - Hazards
  - Collectibles
  - Monsters

### Layer Collision Matrix

Configure what collides with what:
- Player: Ground, Hazards, Collectibles, Monsters
- Hazards: Player only
- Collectibles: Player only

### Tags

Create tags:
- Player
- Ground
- Hazard
- Collectible
- Monster

---

## 🎮 Testing

### Quick Test Checklist

1. **Boot → Menu Flow**
   - Run Boot scene
   - Should auto-load Main Menu

2. **Start Game**
   - Click Play in Main Menu
   - Should show countdown
   - Player should start moving after "Go"

3. **Basic Movement**
   - Press Space to jump
   - Player should auto-run right

4. **Chase System**
   - Chase Meter should gradually fill
   - Shadow should follow from left

5. **Collectibles**
   - Run into Soul Shard → Should collect
   - HUD should update

6. **Hazards**
   - Touch spike → Should take damage
   - Health should update on HUD

7. **Death**
   - Lose all health OR fill Chase Meter to 100%
   - Should show Game Over screen

---

## 🐛 Common Issues

### Player falls through floor
- Check Rigidbody2D collision detection (set to Continuous)
- Verify ground layer in PlayerController

### No UI appears
- Ensure Canvas is set to Screen Space - Overlay
- Check that UIController has all references assigned

### Corridor generation not working
- Verify CorridorSet has prefabs assigned
- Check that CorridorManager has corridor sets

### Audio not playing
- Check ThemeManager has audio sources
- Verify ThemeConfig has audio clips assigned
- Check volume settings in SettingsManager

### Save system not working
- Check console for file path
- Ensure write permissions on directory
- Try PlayerPrefs fallback (QuickSave/QuickLoad)

---

## 📚 Next Steps

1. **Create pixel art sprites** for all game elements
2. **Set up animations** for player, monsters, hazards
3. **Add background music** (lo-fi spooky beats)
4. **Design corridor chunks** with interesting layouts
5. **Polish UI** with proper fonts and effects
6. **Tune difficulty** for best game feel
7. **Add more themes** for variety
8. **Implement cosmetic unlocks**

---

## 💡 Tips

- Start with **simple corridor designs** and iterate
- Test **one system at a time** for easier debugging
- Use **Debug.Log** liberally during development
- Create a **test scene** for individual component testing
- **Save frequently** and use version control (Git)

---

Happy haunting! 👻
