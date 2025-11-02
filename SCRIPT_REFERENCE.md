# 📚 Infinite Haus - Script Reference

Quick reference guide for all scripts in the project.

---

## 📁 Scripts by Category

### 🎮 Core Systems (Assets/Scripts/Core/)

#### **GameManager.cs**
**Purpose**: Central coordinator for all game systems
**Key Methods**:
- `StartNewGame()` - Initiates new run
- `EndGame(string reason)` - Handles game over
- `ChangeState(GameState)` - Updates game state

**Events**:
- `OnGameStateChanged(GameState)`

---

#### **Bootstrap.cs**
**Purpose**: Initial game setup and scene loading
**Key Methods**:
- `InitializeSystems()` - Sets up persistent systems
- `LoadNextScene()` - Transitions to main menu

---

#### **InputManager.cs**
**Purpose**: Centralized input handling
**Key Methods**:
- `GetJumpDown()` - Jump button pressed
- `GetJump()` - Jump button held
- `GetJumpUp()` - Jump button released
- `GetPauseDown()` - Pause pressed
- `RebindJump(KeyCode)` - Rebind jump key

**Properties**:
- `jumpKey`, `pauseKey`, `leftDoorKey`, `rightDoorKey`

---

### 🏃 Player System (Assets/Scripts/Player/)

#### **PlayerController.cs**
**Purpose**: Player movement, jumping, and physics
**Key Methods**:
- `EnableMovement()` - Allow player control
- `DisableMovement()` - Lock player control
- `SetSpeed(float)` - Modify run speed
- `SetSpeedMultiplier(float)` - Apply speed modifier
- `TriggerHurt()` - Visual hurt state
- `TriggerDeath()` - Death state

**Events**:
- `OnJump`, `OnLand`, `OnHurt`, `OnDeath`
- `OnSpeedChanged(float)`

**Properties**:
- `IsGrounded`, `IsJumping`, `IsFalling`, `IsRunning`, `CurrentSpeed`

---

#### **PlayerHealth.cs**
**Purpose**: Health management and damage
**Key Methods**:
- `TakeDamage(int amount)` - Apply damage
- `Heal(int amount)` - Restore health
- `IncreaseMaxHealth(int)` - Permanent HP upgrade
- `InstantKill()` - Instant death (pitfalls)

**Events**:
- `OnHealthChanged(current, max)`
- `OnDamageTaken(damage)`
- `OnDeath`
- `OnHealthIncreased(newMax)`

**Properties**:
- `CurrentHealth`, `MaxHealth`, `IsInvincible`

---

#### **PlayerAnimator.cs**
**Purpose**: Manages player animations
**Animator Parameters** (must be set in Unity Animator):
- `IsRunning` (Bool)
- `IsJumping` (Bool)
- `IsFalling` (Bool)
- `IsGrounded` (Bool)
- `Speed` (Float)
- `Hurt` (Trigger)
- `Death` (Trigger)
- `Jump` (Trigger)
- `Land` (Trigger)

---

### 👻 Monster System (Assets/Scripts/Monsters/)

#### **ChaseSystem.cs**
**Purpose**: Manages Chase Meter and monster coordination
**Key Methods**:
- `IncreaseChaseMeter(float)` - Add to chase
- `ReduceChaseMeter(float)` - Reduce chase
- `OnPlayerDamaged()` - Called when player hurt
- `OnLanternZoneEntered()` - Reduce chase
- `OnChaseCrystalCollected(float)` - Freeze chase
- `StartChase()` / `StopChase()` / `ResetChase()`

**Events**:
- `OnChaseMeterChanged(float)`
- `OnPlayerCaught`
- `OnChaseMeterReduced(float)`
- `OnChaseFrozen` / `OnChaseUnfrozen`

**Properties**:
- `ChaseMeter` (0-100), `ChasePercentage` (0-1), `IsFrozen`

---

#### **Shadow.cs**
**Purpose**: Constant pursuer from left with rubberband AI
**Key Methods**:
- `SetAggression(float)` - 0-1 intensity from ChaseSystem
- `Activate()` / `Deactivate()`
- `SetBaseSpeed(float)` - Difficulty scaling

**Events**:
- `OnPlayerCaught`

---

#### **Crawler.cs**
**Purpose**: Ambushes from ceiling/walls, applies slow
**Key Methods**:
- `SetAggression(float)` - Affects ambush frequency
- `SetAmbushFrequency(float)` - Time between ambushes

**Events**:
- `OnPlayerSlowed(float duration)`

---

#### **Mimic.cs**
**Purpose**: Disguises as doors/objects, triggers traps
**Key Methods**:
- `SetDisguise(DisguiseType)` - Door, Collectible, Platform
- `Activate()` / `Deactivate()`
- `ShouldSpawnMimic(chance, aggression)` - Static helper

**Events**:
- `OnPlayerDamaged(int)`

**Enum**: `DisguiseType` - Door, Collectible, Platform

---

### ⚠️ Hazard System (Assets/Scripts/Hazards/)

#### **HazardBase.cs** (Abstract)
**Purpose**: Base class for all hazards
**Key Methods**:
- `Activate()` / `Deactivate()`
- `SetDamage(int)`
- `SetSprite(Sprite)` - Theme system
- `Reset()` - Return to initial state

**Events**:
- `OnHazardTriggered(GameObject)`

---

#### **Spike.cs**
**Inherits**: `HazardBase`
**Behavior**: Contact damage with optional cooldown

---

#### **Goo.cs**
**Inherits**: `HazardBase`
**Behavior**: Slows movement, reduces jump, optional DoT

---

#### **CrumblingPlatform.cs**
**Inherits**: `HazardBase`
**Behavior**: Falls after delay, optional respawn
**Settings**: `crumbleDelay`, `fallSpeed`, `canRespawn`

---

#### **FallingCeiling.cs**
**Inherits**: `HazardBase`
**Behavior**: Falls from ceiling when player enters trigger
**Settings**: `fallSpeed`, `warningDuration`, `fallDistance`

---

#### **Pitfall.cs**
**Inherits**: `HazardBase`
**Behavior**: Instant death on contact
**Settings**: `instantKill` (bool)

---

#### **LanternZone.cs**
**Purpose**: Safe area that reduces Chase Meter
**Settings**: `chaseReduction`, `singleUse`

---

### 💎 Collectible System (Assets/Scripts/Collectibles/)

#### **CollectibleBase.cs** (Abstract)
**Purpose**: Base class for all collectibles
**Key Methods**:
- `Reactivate()` - Re-enable collectible
- `SetSprite(Sprite)` - Theme system

**Events**:
- `OnCollected(CollectibleBase)`

**Settings**: `enableFloatAnimation`, `enableRotation`

---

#### **SoulShard.cs**
**Inherits**: `CollectibleBase`
**Purpose**: Primary collectible, awards points
**Value**: 10 points (configurable)

**Events**:
- `OnPointsAwarded(int)`

---

#### **Relic.cs**
**Inherits**: `CollectibleBase`
**Purpose**: Permanent HP upgrade (3 = +1 Max HP)
**Enum**: `RelicTier` - Common, Rare, Epic, Legendary

**Events**:
- `OnRelicCollected`

---

#### **ChaseCrystal.cs**
**Inherits**: `CollectibleBase`
**Purpose**: Freezes Chase Meter temporarily
**Settings**: `freezeDuration` (3s), `chaseReduction` (30)

**Events**:
- `OnChaseFrozen(float)`

---

#### **RewardManager.cs**
**Purpose**: Tracks all collectibles and rewards
**Key Methods**:
- `CollectSoulShard(int)` - Add soul shard
- `CollectRelic()` - Add relic
- `CollectChaseCrystal()` - Add crystal
- `AddScore(int)` - Add to score
- `CompleteRun(float distance)` - End run
- `GetRunSummary()` - Get stats

**Events**:
- `OnSoulShardCollected(count)`
- `OnRelicCollected(current, required)`
- `OnChaseCrystalCollected`
- `OnScoreChanged(score)`
- `OnHPUpgradeEarned(newMax)`
- `OnRunCompleted(shards, relics, crystals)`

**Properties**:
- `CurrentRunSoulShards`, `CurrentRunRelics`, `CurrentRunScore`
- `TotalRelicsCollected`, `PermanentHPUpgrades`

---

### 🏗️ Level System (Assets/Scripts/Level/)

#### **CorridorManager.cs**
**Purpose**: Procedural level generation
**Key Methods**:
- `GenerateNextChunk()` - Spawn corridor
- `ResetLevel()` - Clear all chunks
- `OnDoorChosen(index, type)` - Door selection handler

**Events**:
- `OnChunkGenerated(count)`
- `OnDifficultyIncreased(multiplier)`
- `OnDistanceChanged(distance)`

**Properties**:
- `TotalDistance`, `ChunksGenerated`

---

#### **CorridorChunk.cs**
**Purpose**: Individual corridor segment
**Key Methods**:
- `SpawnHazards(prefabs[], count)`
- `SpawnCollectibles(prefabs[], count)`
- `SpawnDoors(prefab, count)`
- `Cleanup()` - Destroy spawned objects

**Properties**:
- `StartPosition`, `EndPosition`, `chunkLength`

---

#### **DoorHandler.cs**
**Purpose**: Door selection at corridor end
**Key Methods**:
- `Highlight()` / `Unhighlight()`
- `Choose()` - Select this door
- `Lock()` / `Unlock()`
- `SetDoorType(DoorType)`

**Events**:
- `OnDoorChosen(index, doorType)`

**Enum**: `DoorType` - Normal, Easy, Hard, Mystery, Safe, Locked

---

### 🎨 UI System (Assets/Scripts/UI/)

#### **UIController.cs**
**Purpose**: Main UI coordinator
**Key Methods**:
- `StartGame()` - Begin countdown
- `ShowGameOver(distance, score, summary, reason)`
- `TogglePause()` / `Pause()` / `Resume()`
- `UpdateChaseMeter(float)`
- `UpdateHealth(current, max)`
- `UpdateDistance(float)`
- `UpdateSoulShards(count)`, `UpdateRelicTracker(current, required)`
- `FlashMilestone(Color)`

**Events**:
- `OnGameStarted`, `OnGamePaused`, `OnGameResumed`

---

#### **HUDManager.cs**
**Purpose**: In-game HUD display
**Key Methods**:
- `UpdateChaseMeter(float)` - Update chase bar
- `InitializeHearts(int max)` - Create heart icons
- `UpdateHealth(current, max)` - Update hearts
- `UpdateDistance(float)` - Update distance counter
- `UpdateSoulShards(int)` - Update shard count
- `UpdateRelicTracker(current, required)` - Update "2/3"
- `FlashMilestone(Color)` - Screen flash effect

---

#### **StartCountdown.cs**
**Purpose**: "Ready → Go" sequence
**Key Methods**:
- `StartCountdownSequence()` - Begin countdown
- `Reset()` - Reset for replay

**Events**:
- `OnCountdownComplete`

**Settings**: `readyDuration`, `goDuration`, `fadeInDuration`

---

#### **GameOverScreen.cs**
**Purpose**: End run summary
**Key Methods**:
- `Show(distance, score, summary, reason)` - Display screen
- `Hide()` - Hide screen

**Events**:
- `OnRetry`, `OnMainMenu`

---

### 🎨 Theme System (Assets/Scripts/Theme/)

#### **ThemeManager.cs**
**Purpose**: Theme switching and application
**Key Methods**:
- `LoadTheme(int index)` - Load by index
- `LoadTheme(ThemeConfig)` - Load specific theme
- `SwitchToNextTheme()` - Cycle themes
- `SwitchToRandomTheme()` - Random theme
- `OnDoorPassed()` - Auto-switch handler
- `SetMusicVolume(float)`, `SetAmbientVolume(float)`

**Events**:
- `OnThemeChanged(ThemeConfig)`

---

#### **ThemeApplicator.cs**
**Purpose**: Applies theme to individual objects
**Key Methods**:
- `ApplyTheme(ThemeConfig)` - Update visuals
- `RefreshTheme()` - Manually update

**Enum**: `ThemeSpriteType` - BackgroundFar/Mid/Near, Tileset, Door, Spike, Goo, etc.

---

### 💾 Save System (Assets/Scripts/Save/)

#### **SaveSystem.cs** (Static)
**Purpose**: File I/O for save data
**Key Methods**:
- `SaveGame(SaveData)` - Write to disk
- `LoadGame()` - Read from disk (returns SaveData)
- `SaveExists()` - Check if file exists
- `DeleteSave()` - Remove save file
- `QuickSave(SaveData)` - PlayerPrefs backup
- `QuickLoad()` - PlayerPrefs load

---

#### **SaveData.cs**
**Purpose**: Serializable save data structure
**Fields**:
- Progress: `highScore`, `bestDistance`, `totalRuns`
- Upgrades: `totalRelicsCollected`, `permanentHPUpgrades`
- Settings: `masterVolume`, `musicVolume`, `sfxVolume`
- Input: `jumpKey`, `pauseKey`, etc.
- Stats: `totalSoulShardsCollected`, `totalDistanceTraveled`

**Methods**:
- `CreateDefault()` - Create new save
- `UpdateLastPlayed()` - Set timestamp
- `IsCosmeticUnlocked(id)` - Check unlock
- `UnlockCosmetic(id)` - Unlock item

---

#### **SettingsManager.cs**
**Purpose**: Settings application and management
**Key Methods**:
- `LoadSettings()` - Load from save
- `SaveSettings()` - Write to save
- `ApplySettings()` - Apply all settings
- `SetMasterVolume(float)`, `SetMusicVolume(float)`, `SetSFXVolume(float)`
- `SetFullscreen(bool)`, `SetResolution(w, h, fullscreen)`
- `ResetToDefaults()` - Restore defaults

**Events**:
- `OnSettingsChanged(SaveData)`

---

### 📊 Data (ScriptableObjects) (Assets/Scripts/Data/)

#### **ThemeConfig.cs**
**Purpose**: Complete visual/audio theme configuration
**Fields**:
- Identity: `themeName`, `description`
- Colors: `primaryColor`, `secondaryColor`, `ambientColor`, `uiTintColor`
- Backgrounds: `backgroundFar/Mid/Near`, `parallaxSpeeds`
- Tilesets: `tileset`, `decorativeProps[]`
- Doors: `doorSprite`, `doorHighlightSprite`
- Hazards: `spikeSprite`, `gooSprite`, `crumblePlatformSprite`, etc.
- Audio: `backgroundMusic`, `ambientSounds[]`, `jumpSFX`, `hurtSFX`, etc.
- UI: `uiFontColor`, `heartFullSprite`, `heartEmptySprite`, `chaseMeterColor`
- Effects: `fogOverlay`, `screenOverlayColor`, `enableVHSEffect`, `enableScreenShake`

---

#### **DifficultyProfile.cs**
**Purpose**: Difficulty parameters for game progression
**Fields**:
- Identity: `difficultyName`, `minDistance`
- Movement: `playerSpeed`, `scrollSpeedMultiplier`
- Chase: `shadowBaseSpeed`, `chaseMeterFillRate`, `chasePenaltyOnDamage`
- Monsters: `crawlerAmbushFrequency`, `mimicSpawnChance`
- Hazards: `hazardsPerChunk`, `trapActiveChance`
- Rewards: `soulShardsPerChunk`, `relicSpawnChance`, `chaseCrystalsPerSection`
- Level: `corridorLength`, `doorChoiceCount`
- Combat: `baseDamage`, `invincibilityDuration`

---

#### **CorridorSet.cs**
**Purpose**: Collection of corridor prefabs for difficulty tier
**Fields**:
- Identity: `setName`, `difficultyProfile`
- Corridors: `corridorPrefabs[]`, `spawnWeights[]`
- Special: `specialCorridors[]`, `specialCorridorChance`
- Metadata: `averageLength`, `hazardSpacing`

**Methods**:
- `GetRandomCorridor(allowSpecial)` - Returns random prefab

---

## 🎯 Unity Animator Parameters

Create an Animator Controller for the Player with these parameters:

| Parameter | Type | Purpose |
|-----------|------|---------|
| IsRunning | Bool | Player is running on ground |
| IsJumping | Bool | Player is in jump state |
| IsFalling | Bool | Player is falling (velocity < 0) |
| IsGrounded | Bool | Player is on ground |
| Speed | Float | Current movement speed |
| Hurt | Trigger | Player took damage |
| Death | Trigger | Player died |
| Jump | Trigger | Player jumped |
| Land | Trigger | Player landed |

---

## 📋 Layer Setup

Create these layers in Unity:
1. **Default** (0) - General objects
2. **Player** (8) - Player character
3. **Ground** (9) - Floor/walls
4. **Hazards** (10) - All hazards
5. **Collectibles** (11) - Collectibles
6. **Monsters** (12) - Chase monsters
7. **UI** (5) - UI elements

---

## 🏷️ Tag Setup

Create these tags:
- **Player** - Player GameObject
- **Ground** - Floor/platforms
- **Hazard** - Damage sources
- **Collectible** - Pickups
- **Monster** - Enemies

---

## 🔊 Audio Mixer Groups (Optional)

If using Unity Audio Mixer:
- **Master** - Root group
  - **Music** - Background music
  - **SFX** - Sound effects
  - **Ambient** - Ambient sounds

Expose parameters:
- `MasterVolume`
- `MusicVolume`
- `SFXVolume`

---

## ⚙️ Recommended Unity Packages

- **TextMeshPro** - Better text rendering
- **2D Pixel Perfect** - Crisp pixel art
- **Cinemachine** - Camera follow (optional)
- **Unity UI** - Built-in UI system
- **Post Processing Stack v2** - Visual effects (optional)

---

## 🚀 Execution Order

No special script execution order required. Event-driven architecture handles dependencies automatically.

---

This reference covers all 50+ scripts in the Infinite Haus project!
