# 🏗️ Infinite Haus - Architecture Documentation

## 📐 System Overview

Infinite Haus is built on a **modular, event-driven architecture** that separates concerns and allows for easy extension. All systems communicate through C# events and follow Unity best practices.

---

## 🎯 Core Design Principles

1. **Separation of Concerns** - Each system handles one responsibility
2. **Event-Driven Communication** - Systems communicate via events, not direct references
3. **ScriptableObject Configuration** - Data-driven design for easy tuning
4. **Editor-Friendly** - All settings exposed with tooltips and headers
5. **Extensibility** - Easy to add new themes, hazards, corridors
6. **Clean Code** - Well-commented, consistent naming, logical structure

---

## 🧩 System Architecture

```
┌─────────────────────────────────────────────────────────┐
│                     GameManager                          │
│  (Coordinates all systems, manages game state)           │
└───────────────┬─────────────────────────────────────────┘
                │
    ┌───────────┼───────────┬──────────┬─────────┬────────┐
    │           │           │          │         │        │
┌───▼────┐ ┌───▼────┐ ┌───▼────┐ ┌──▼───┐ ┌───▼──┐ ┌──▼──┐
│ Player │ │ Chase  │ │ Level  │ │  UI  │ │Theme │ │Save │
│ System │ │ System │ │ System │ │System│ │System│ │ Sys │
└────────┘ └────────┘ └────────┘ └──────┘ └──────┘ └─────┘
```

---

## 📦 System Breakdown

### 1. **Player System**
**Location**: `Assets/Scripts/Player/`

**Components:**
- `PlayerController.cs` - Movement, jumping, auto-run
- `PlayerHealth.cs` - Health management, damage, death
- `PlayerAnimator.cs` - Animation state control

**Responsibilities:**
- Handle player input (via InputManager)
- Execute jump physics with coyote time and buffering
- Manage health and invincibility
- Trigger animation states
- Fire events for UI and game systems

**Events:**
- `OnJump`, `OnLand`, `OnHurt`, `OnDeath`
- `OnHealthChanged(current, max)`

**Key Features:**
- Variable jump height
- Coyote time (0.15s grace period)
- Jump buffering (0.2s input queue)
- Invincibility frames after damage
- Speed multipliers (for hazards like Goo)

---

### 2. **Chase System**
**Location**: `Assets/Scripts/Monsters/`

**Components:**
- `ChaseSystem.cs` - Central chase meter manager
- `Shadow.cs` - Constant pursuer from left
- `Crawler.cs` - Ceiling/wall ambusher
- `Mimic.cs` - Disguised trap enemy

**Responsibilities:**
- Manage Chase Meter (0-100%)
- Control monster aggression levels
- Handle player caught condition
- Apply chase penalties for mistakes
- Coordinate all three monsters

**Chase Meter Mechanics:**
- Fills passively over time
- Increases on damage (+15)
- Increases on missed jumps (+5)
- Reduced in Lantern Zones (-20)
- Frozen by Chase Crystals (3s)

**Monster Behaviors:**

**Shadow:**
- Rubberband speed logic
- Faster when player is far ahead
- Slower when close to player
- Aggression increases speed
- Fades in as chase intensifies

**Crawler:**
- Periodic ambushes from ceiling/walls
- Warning flash before attack
- Applies slow debuff (50% speed, 3s)
- Frequency increases with aggression

**Mimic:**
- Disguises as doors or collectibles
- Reveals when player touches
- Deals damage and increases chase
- Spawn chance scales with difficulty

---

### 3. **Hazard System**
**Location**: `Assets/Scripts/Hazards/`

**Base Class:**
- `HazardBase.cs` - Common hazard functionality

**Hazard Types:**

| Hazard | Effect | Special Behavior |
|--------|--------|------------------|
| Spike | 1 damage | Simple contact damage |
| Goo | Slow (50% speed) | Reduces jump height, optional DoT |
| Crumbling Platform | Falls after 0.3s | Warning shake, respawns if configured |
| Falling Ceiling | 1 damage | Warning flash, triggers on proximity |
| Pitfall | Instant death | Game over on contact |
| Lantern Zone | Reduces Chase (-20) | Safe area, single-use |

**Design Pattern:**
All hazards inherit from `HazardBase` and override:
- `TriggerHazard(GameObject player)` - Core effect logic
- `Activate()` / `Deactivate()` - Enable/disable state
- `Reset()` - Return to initial state

---

### 4. **Reward System**
**Location**: `Assets/Scripts/Collectibles/`

**Components:**
- `CollectibleBase.cs` - Base collectible class
- `SoulShard.cs` - Points collectible
- `Relic.cs` - Permanent HP upgrade item
- `ChaseCrystal.cs` - Chase meter freeze item
- `RewardManager.cs` - Tracks all rewards

**Collectible Types:**

**Soul Shard:**
- Awards 10 points each
- Primary collectible
- Used to unlock cosmetics
- Floating animation

**Rare Relic:**
- Spawns after difficult jumps
- Collect 3 in a run = +1 Max HP permanently
- Saved across sessions
- Rotating animation with color-coded rarity

**Chase Crystal:**
- Freezes Chase Meter for 3 seconds
- Reduces chase by 30
- Pulsing glow effect
- Strategic relief item

**Permanent Upgrades:**
- Tracked in `RewardManager`
- Saved to disk via `SaveSystem`
- Applied on game start
- Shown in Game Over summary

---

### 5. **Level System**
**Location**: `Assets/Scripts/Level/`

**Components:**
- `CorridorManager.cs` - Procedural generation
- `CorridorChunk.cs` - Individual corridor segment
- `DoorHandler.cs` - Door selection logic

**Procedural Generation:**

```
Player Position → Generate Ahead → Despawn Behind
     ↓
 [Chunk 1] [Chunk 2] [Chunk 3] [Chunk 4]
     ↑         ↑         ↑         ↑
   Despawn   Active   Spawning  Pre-spawn
```

**Corridor Chunks:**
- Prefab-based modular design
- Contains spawn points for:
  - Hazards
  - Collectibles
  - Doors
- Length defined in `CorridorChunk.chunkLength`
- Difficulty scaling via DifficultyProfile

**Door System:**
- 2 doors per chunk end (Left/Right)
- Door types affect next corridor:
  - **Normal** - Standard progression
  - **Easy** - Fewer hazards
  - **Hard** - More hazards, better rewards
  - **Mystery** - Random difficulty
  - **Safe** - No hazards (rare)

**Difficulty Scaling:**
- Based on distance traveled
- New CorridorSet every 100m
- Difficulty multiplier increases
- More hazards, faster monsters

---

### 6. **UI System**
**Location**: `Assets/Scripts/UI/`

**Components:**
- `UIController.cs` - Main UI coordinator
- `HUDManager.cs` - In-game HUD
- `StartCountdown.cs` - "Ready → Go" sequence
- `GameOverScreen.cs` - End run summary

**HUD Elements:**

```
┌────────────────────────────────────────┐
│ HP: ❤❤❤    Chase: [████░░░░] 45%     │
│ Distance: 127m    Souls: 23           │
│ Relics: 2/3       Theme: Spooky       │
└────────────────────────────────────────┘
```

**Start Sequence:**
1. Fade in from black
2. Show "READY" (1s)
3. Show "GO!" (0.5s)
4. Enable player movement
5. Start background music

**Game Over Display:**
- Final score
- Distance traveled
- Collectibles summary
- High score comparison
- Permanent upgrades earned
- Retry / Main Menu buttons

---

### 7. **Theme System**
**Location**: `Assets/Scripts/Theme/`

**Components:**
- `ThemeManager.cs` - Theme switching and application
- `ThemeApplicator.cs` - Applies theme to individual objects
- `ThemeConfig.cs` (ScriptableObject) - Theme data

**Theme Configuration:**
A ThemeConfig contains:
- Color palette (primary, secondary, ambient, UI)
- Background sprites (parallax layers)
- Tilesets and hazard skins
- Music and SFX
- UI skin (heart sprites, colors)
- Visual effects (fog, VHS, particles)

**How Themes Work:**

1. **ThemeManager** loads active theme
2. Applies to:
   - Camera background color
   - Ambient lighting
   - Background music
   - Sound effects
3. Notifies all **ThemeApplicators**
4. Each applicator updates its GameObject

**Theme Switching:**
- Manual selection in settings
- Auto-switch every N doors (optional)
- Random theme each run
- Saved preference

---

### 8. **Save System**
**Location**: `Assets/Scripts/Save/`

**Components:**
- `SaveSystem.cs` - File I/O operations
- `SaveData.cs` - Serializable data structure
- `SettingsManager.cs` - Settings application

**What Gets Saved:**

**Progress:**
- High score
- Best distance
- Total runs / deaths
- Total relics collected
- Permanent HP upgrades
- Unlocked cosmetics

**Settings:**
- Audio volumes (master, music, SFX)
- Resolution and fullscreen
- Input key bindings
- Theme preferences

**Statistics:**
- Total collectibles
- Total distance traveled
- Total doors opened
- Play time tracking

**Save Format:**
- Primary: JSON file in persistent data path
- Backup: PlayerPrefs for critical data
- Auto-save on game over
- Manual save in settings

**File Location:**
```
Windows: C:/Users/[User]/AppData/LocalLow/[Company]/InfiniteHaus/InfiniteHausSaves/
Mac: ~/Library/Application Support/[Company]/InfiniteHaus/InfiniteHausSaves/
```

---

### 9. **Core System**
**Location**: `Assets/Scripts/Core/`

**Components:**
- `GameManager.cs` - Central coordinator
- `Bootstrap.cs` - Initialization
- `InputManager.cs` - Input handling

**GameManager Responsibilities:**
- Initialize all systems
- Subscribe to events
- Coordinate game flow
- Handle state transitions
- Trigger saves

**Game States:**
```
Menu → Countdown → Playing → GameOver
  ↑        ↓          ↓         ↓
  └────────┴──────────┴─────────┘
       (Retry / Quit)
```

**Event Flow Example:**
```
Player dies
    ↓
PlayerHealth.OnDeath
    ↓
GameManager.HandlePlayerDeath()
    ↓
EndGame("Killed by hazard")
    ↓
RewardManager.CompleteRun()
    ↓
SaveSystem.SaveGame()
    ↓
UIController.ShowGameOver()
```

---

## 🔄 Data Flow Diagram

```
┌──────────────┐
│ ScriptableObjects │  (ThemeConfig, DifficultyProfile, CorridorSet)
└───────┬──────┘
        ↓
┌───────▼──────────┐
│   GameManager     │ ← Initializes all systems
└───────┬──────────┘
        │
    ┌───┼───┬───────┬────────┐
    ↓   ↓   ↓       ↓        ↓
┌───▼─┐ │ ┌─▼──┐ ┌──▼───┐ ┌─▼──┐
│Input│ │ │Chase│ │Reward│ │ UI │
└──┬──┘ │ └──┬──┘ └──┬───┘ └──┬─┘
   ↓    ↓    ↓       ↓        ↑
┌──▼────▼────▼───────▼────────┴─┐
│         Player System           │
└─────────────────────────────────┘
```

---

## 🎨 Design Patterns Used

1. **Singleton Pattern**
   - InputManager (optional)
   - GameManager coordination

2. **Observer Pattern**
   - C# Events for system communication
   - Decoupled architecture

3. **Component Pattern**
   - Unity GameObject/Component system
   - Modular, reusable scripts

4. **Strategy Pattern**
   - Different door types
   - Monster behaviors

5. **Factory Pattern**
   - Collectible spawning
   - Corridor generation

6. **Object Pool Pattern**
   - Corridor chunk reuse (implicit)
   - Particle effect reuse

---

## 🚀 Extension Points

### Adding New Hazards
1. Create class inheriting `HazardBase`
2. Override `TriggerHazard()`
3. Add to hazard prefabs array
4. Done!

### Adding New Collectibles
1. Inherit from `CollectibleBase`
2. Override `Collect()`
3. Add to RewardManager if needed
4. Create prefab

### Adding New Themes
1. Create new ThemeConfig ScriptableObject
2. Assign all sprites and audio
3. Add to ThemeManager's available themes
4. Auto-applies to all ThemeApplicators

### Adding New Monsters
1. Create monster script
2. Implement `SetAggression(float)` method
3. Reference in ChaseSystem
4. Subscribe to chase events

---

## 📚 Class Hierarchy

```
MonoBehaviour
├── PlayerController
├── PlayerHealth
├── PlayerAnimator
├── ChaseSystem
│   ├── Shadow
│   ├── Crawler
│   └── Mimic
├── HazardBase
│   ├── Spike
│   ├── Goo
│   ├── CrumblingPlatform
│   ├── FallingCeiling
│   └── Pitfall
├── CollectibleBase
│   ├── SoulShard
│   ├── Relic
│   └── ChaseCrystal
├── CorridorManager
├── CorridorChunk
├── DoorHandler
├── UIController
│   ├── HUDManager
│   ├── StartCountdown
│   └── GameOverScreen
├── ThemeManager
├── ThemeApplicator
├── RewardManager
├── SettingsManager
├── GameManager
├── Bootstrap
└── InputManager

ScriptableObject
├── ThemeConfig
├── DifficultyProfile
└── CorridorSet
```

---

## 🔧 Performance Considerations

1. **Corridor Despawning**
   - Only 3-4 chunks active at once
   - Old chunks destroyed to free memory

2. **Object Pooling**
   - Particle effects reused
   - Consider pooling for collectibles

3. **Event Unsubscription**
   - All events unsubscribed in OnDestroy
   - Prevents memory leaks

4. **Minimal Update Loops**
   - Only active systems update
   - Disabled components don't run

5. **Asset Optimization**
   - Use sprite atlases
   - Compress audio
   - Low poly count for pixel art

---

## 🧪 Testing Strategy

1. **Unit Testing**
   - Test individual components
   - Mock dependencies with interfaces

2. **Integration Testing**
   - Test system interactions
   - Event flow validation

3. **Playtest Checklist**
   - All collectibles work
   - All hazards function
   - Chase system balanced
   - UI updates correctly
   - Save/load reliable

---

## 📝 Naming Conventions

- **Scripts**: PascalCase (`PlayerController.cs`)
- **Variables**: camelCase (`currentHealth`)
- **Constants**: UPPER_SNAKE_CASE (`MAX_HEALTH`)
- **Events**: OnEventName (`OnPlayerDeath`)
- **Prefabs**: PascalCase (`SoulShard`)
- **Scenes**: PascalCase (`Game.unity`)

---

This architecture provides a solid foundation for Infinite Haus while remaining flexible for future expansion!
