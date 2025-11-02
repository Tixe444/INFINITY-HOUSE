# 🎉 Infinite Haus - Finalization Guide

Complete guide for the finalized Infinite Haus codebase with cross-platform support, shop system, and performance optimizations.

---

## ✅ What's New in This Update

### 1. 📱 **Full Cross-Platform Support**
- **Desktop (PC/Mac)**: WASD keyboard controls
- **Mobile (iOS/Android)**: Gesture-based touch controls (swipe, tap)
- Automatic platform detection
- Unified input abstraction

### 2. 🛍️ **Cosmetic Shop System**
- **Diamond Currency**: Premium currency (💎)
- **8 Cosmetic Types**: Player skins, trails, UI themes, music, filters, and more
- **Rarity System**: Common, Rare, Epic, Legendary, Mythic
- **Full Save Integration**: Owned and equipped cosmetics persist
- **Scrollable Shop UI**: Grid-based item display with filters

### 3. 🕹️ **Enhanced Input System**
#### Desktop Controls:
- **W** → Jump
- **A** → Left Door
- **D** → Right Door
- **Tab** or **I** → Shop
- **Esc** → Pause

#### Mobile Gestures:
- **Swipe Up** → Jump
- **Swipe Left** → Left Door
- **Swipe Right** → Right Door
- **Tap Top-Left Corner** → Shop
- **Tap Top-Right Corner** → Pause

### 4. 🎯 **Performance Optimizations**
- **Object Pooling System**: Reduces instantiation/GC overhead
- **60 FPS Target**: Optimized for mid-range mobile devices
- **Efficient Update Loops**: Minimal allocations

### 5. 💾 **Extended Save System**
- Diamond balance tracking
- Cosmetic ownership and equipped items
- Total diamonds earned/spent statistics
- Cross-session persistence

---

## 📦 New Scripts Added

### Shop System (`Assets/Scripts/Shop/`)
1. **CosmeticItem.cs** - ScriptableObject for cosmetic definitions
2. **DiamondCurrencyManager.cs** - Manages premium currency
3. **ShopManager.cs** - Handles shop logic and ownership
4. **ShopUIController.cs** - Shop UI display and interaction

### Input System (`Assets/Scripts/Input/`)
1. **PlatformInputManager.cs** - Unified cross-platform input
2. **SwipeDetector.cs** - Mobile gesture detection

### Performance (`Assets/Scripts/Core/`)
1. **ObjectPool.cs** - Generic object pooling system

### Updated Scripts
- **SaveData.cs** - Added diamond and cosmetic fields
- **HUDManager.cs** - Added diamond display and shop button

---

## 🔧 Unity Setup Instructions

### Step 1: Scene Setup

#### A. Create Shop UI
1. In your **Game scene**, add a new **Canvas** (if not already present)
2. Inside Canvas, create:
   ```
   Canvas
   ├── HUD (existing)
   │   ├── DiamondDisplay (TextMeshPro)
   │   └── ShopButton (Button)
   └── ShopPanel (new)
       ├── Header
       │   ├── Title Text
       │   ├── Diamond Count
       │   └── Close Button
       ├── CategoryFilters (Horizontal Layout)
       │   ├── AllButton
       │   ├── SkinsButton
       │   ├── TrailsButton
       │   ├── UIThemesButton
       │   ├── MusicButton
       │   └── FiltersButton
       ├── ScrollView
       │   └── Content (Grid Layout Group)
       │       └── ShopItemPrefab (instantiated at runtime)
       ├── DetailPanel
       │   ├── ItemIcon
       │   ├── ItemName
       │   ├── ItemDescription
       │   ├── Cost Text
       │   ├── Purchase Button
       │   ├── Equip Button
       │   └── Unequip Button
       └── InsufficientFundsPanel
   ```

#### B. Create Shop Item Prefab
Create a prefab: `ShopItemPrefab`
```
ShopItemPrefab (with ShopItemUI component)
├── Background (Image with RarityBorder)
├── Icon (Image)
├── Name (TextMeshPro)
├── Cost (TextMeshPro)
├── OwnedIndicator (Image - checkmark)
├── EquippedIndicator (Image - star)
└── SelectButton (Button)
```

---

### Step 2: Manager Setup

#### A. GameManager Scene Setup
In your **Game scene**, create empty GameObjects:

```
--- MANAGERS ---
├── GameManager (existing)
├── ShopManager (new)
│   └── Add ShopManager.cs
├── DiamondCurrencyManager (new)
│   └── Add DiamondCurrencyManager.cs
└── InputManager (new)
    └── Add PlatformInputManager.cs
    └── Add SwipeDetector.cs (child component)
```

#### B. Link References

**ShopManager:**
- Assign `availableCosmetics[]` array with all CosmeticItem ScriptableObjects
- Assign `defaultPlayerSkin` and `defaultTrail`
- Reference `DiamondCurrencyManager`

**ShopUIController** (on ShopPanel):
- Assign all UI elements (ShopPanel, ScrollView, DetailPanel, etc.)
- Assign `shopItemPrefab`
- Assign audio clips (purchase, equip, error)

**HUDManager:**
- Assign `diamondCountText` (TextMeshPro in top-right)
- Assign `shopButton` (Button in top-left or tap area)

**PlatformInputManager:**
- Assign `swipeDetector` component (or leave null to auto-create)
- Configure key bindings if desired

---

### Step 3: Create Cosmetic Items

#### A. Create ScriptableObjects

Right-click in Project → **Create → Infinite Haus → Shop → Cosmetic Item**

Create example cosmetics:

**Player Skin Example:**
- **Cosmetic ID**: `skin_ghost`
- **Display Name**: "Ghost Skin"
- **Type**: PlayerSkin
- **Rarity**: Rare
- **Diamond Cost**: 150
- **Preview Icon**: Assign sprite
- **Player Skin Sprite**: Assign sprite
- **Animator Override**: (optional)

**Soul Trail Example:**
- **Cosmetic ID**: `trail_fire`
- **Display Name**: "Fire Trail"
- **Type**: SoulTrail
- **Rarity**: Epic
- **Diamond Cost**: 200
- **Preview Icon**: Assign sprite
- **Trail Effect Prefab**: Assign particle prefab

**UI Theme Example:**
- **Cosmetic ID**: `theme_neon`
- **Display Name**: "Neon Theme"
- **Type**: UITheme
- **Rarity**: Legendary
- **Diamond Cost**: 300
- **Preview Icon**: Assign sprite
- **UI Theme Config**: Assign ThemeConfig ScriptableObject

#### B. Assign to ShopManager
- Add all created CosmeticItem ScriptableObjects to `ShopManager.availableCosmetics[]`

---

### Step 4: Canvas Scaler Setup

For proper cross-platform UI scaling:

1. Select your **Canvas**
2. Set **Canvas Scaler** component:
   - **UI Scale Mode**: Scale With Screen Size
   - **Reference Resolution**: 1920x1080 (or your target)
   - **Screen Match Mode**: Match Width Or Height
   - **Match**: 0.5 (balances width/height)

This ensures UI scales correctly on all aspect ratios (4:3, 16:9, 18:9, 21:9).

---

### Step 5: Input Integration

#### A. Update PlayerController

Replace direct `Input.GetKey` calls with event subscriptions:

```csharp
// In PlayerController.cs Start():
private PlatformInputManager inputManager;

void Start()
{
    inputManager = PlatformInputManager.Instance;
    if (inputManager != null)
    {
        inputManager.OnJumpPressed += HandleJump;
        inputManager.OnJumpReleased += HandleJumpRelease;
    }
}

void OnDestroy()
{
    if (inputManager != null)
    {
        inputManager.OnJumpPressed -= HandleJump;
        inputManager.OnJumpReleased -= HandleJumpRelease;
    }
}

void HandleJump()
{
    // Jump logic here
}

void HandleJumpRelease()
{
    // Variable jump height logic
}
```

#### B. Update Door Selection

In your door selection system, subscribe to:
```csharp
inputManager.OnLeftDoorSelected += SelectLeftDoor;
inputManager.OnRightDoorSelected += SelectRightDoor;
```

---

### Step 6: Object Pooling (Optional but Recommended)

For frequently spawned objects (particles, collectibles):

1. Create empty GameObject: `ParticlePool`
2. Add `ObjectPool.cs` component
3. Assign:
   - **Prefab**: Your particle effect prefab
   - **Initial Size**: 10-20
   - **Can Grow**: true
   - **Max Size**: 50

Usage in code:
```csharp
ObjectPool pool = FindObjectOfType<ObjectPool>();
GameObject particle = pool.Get(position, rotation);
// Object automatically returns after lifetime
```

---

### Step 7: Testing

#### Desktop Testing:
1. Play in Unity Editor
2. Use **WASD** for controls
3. Press **Tab** to open shop
4. Press **D** (debug key) to add 100 diamonds

#### Mobile Testing:
1. Build to iOS/Android device
2. Use swipe gestures
3. Tap corners for shop/pause
4. Verify touch responsiveness

#### Shop Testing:
1. Open shop (Tab or tap)
2. Select a cosmetic item
3. Purchase with diamonds
4. Equip and verify visual change
5. Check save persistence (restart game)

---

## 🎨 Visual Customization

### Theme-Aware Shop UI
The shop automatically adapts to your current theme. Ensure:
- Shop panel background matches theme colors
- Rarity borders use `CosmeticItem.GetRarityColor()`
- Diamond icon is visible against all backgrounds

### Mobile-Specific UI
For mobile builds:
- Increase button sizes (minimum 60x60 pixels)
- Add visual feedback for taps (scale animation)
- Test on various screen sizes (phones, tablets, foldables)

---

## ⚙️ Performance Tips

### 1. Object Pooling
Pool these objects:
- Particle effects (soul shard pickups, jump effects)
- Collectibles (if respawning)
- Hazard particles

### 2. Mobile Optimization
- Use **2D Sprite Atlases** to reduce draw calls
- Keep **Pixel Perfect Camera** settings for crisp pixel art
- Limit **active ParticleSystems** to 5-10 max
- Use **Quality Settings** to adjust for device tier

### 3. UI Performance
- Use **Canvas Groups** for show/hide instead of SetActive
- Pool shop item prefabs if scrolling large lists
- Disable `Raycast Target` on non-interactive UI elements

---

## 🐛 Troubleshooting

### Issue: Diamonds not saving
**Solution**: Check `SaveSystem.GetSavePath()` has write permissions. Use `QuickSave` as fallback.

### Issue: Swipes not detected on mobile
**Solution**:
- Verify `SwipeDetector` is active
- Check `minSwipeDistance` isn't too high (try 30-50px)
- Ensure no UI elements block touch input

### Issue: Shop items not displaying
**Solution**:
- Verify `ShopManager.availableCosmetics[]` is populated
- Check `ShopItemPrefab` has `ShopItemUI` component
- Ensure `ShopUIController.shopItemsContainer` is assigned

### Issue: Cosmetics not applying
**Solution**:
- Check `ShopManager.LoadShopData()` is called on Start
- Verify cosmetic assets (sprites, prefabs) are assigned
- Ensure player/theme managers are in scene

### Issue: Input not working
**Solution**:
- Check platform detection in `PlatformInputManager`
- Use `forceDesktopInput` or `forceMobileInput` for testing
- Verify input events are subscribed

---

## 📊 Statistics & Debugging

### Diamond Currency Debug Commands
In Debug builds, press:
- **D** → Add 100 diamonds
- **R** → Reset diamonds to starting amount

### Shop Debug Info
Enable `Debug.Log` in:
- `ShopManager.TryPurchaseCosmetic()` - Purchase attempts
- `ShopManager.EquipCosmetic()` - Equip operations
- `DiamondCurrencyManager.SaveDiamonds()` - Save operations

### Input Debug
- `SwipeDetector.showSwipeTrail` → Visualize swipes in editor
- Corner tap areas shown in editor when `showSwipeTrail` enabled

### Pool Statistics
```csharp
ObjectPool pool = GetComponent<ObjectPool>();
Debug.Log(pool.GetStatistics());
```

---

## 🚀 Build Settings

### iOS Build:
1. Player Settings → iOS → **Minimum iOS Version**: 12.0+
2. Enable **Auto Graphics API** (Metal)
3. **Target SDK**: Device SDK
4. Test on physical devices (gesture detection requires touch)

### Android Build:
1. Player Settings → Android → **Minimum API Level**: 24 (Android 7.0)
2. **Scripting Backend**: IL2CPP (better performance)
3. **Target Architectures**: ARM64
4. Test on various aspect ratios (18:9, 19.5:9, etc.)

### PC/Mac Build:
1. **Fullscreen Mode**: Windowed (allows resizing)
2. **Resizable Window**: Yes
3. Test keyboard input and rebinding

---

## 📱 Mobile-Specific Features

### Safe Area Support
For devices with notches:
```csharp
// Add to HUDManager or UIController
Rect safeArea = Screen.safeArea;
RectTransform rt = GetComponent<RectTransform>();
rt.anchoredPosition = new Vector2(safeArea.x, safeArea.y);
rt.sizeDelta = new Vector2(safeArea.width, safeArea.height);
```

### Haptic Feedback (Optional)
```csharp
#if UNITY_IOS || UNITY_ANDROID
Handheld.Vibrate(); // On purchase, damage, etc.
#endif
```

---

## 🎯 Next Steps

1. **Create Cosmetic Art**: Design player skins, trails, UI themes
2. **IAP Integration**: Connect `DiamondCurrencyManager.PurchaseDiamondPack()` to Unity IAP
3. **Analytics**: Track shop purchases, most popular cosmetics
4. **Milestone Rewards**: Use `DiamondCurrencyManager.RewardForMilestone()`
5. **Daily Rewards**: Implement daily login bonus system
6. **Seasonal Content**: Add limited-time cosmetics

---

## 📚 Script Reference

### New API Methods

**DiamondCurrencyManager:**
- `AddDiamonds(int)` - Earn diamonds
- `TrySpendDiamonds(int)` - Attempt purchase (returns bool)
- `HasEnoughDiamonds(int)` - Check balance
- `RewardForMilestone(MilestoneType)` - Award for achievements

**ShopManager:**
- `TryPurchaseCosmetic(CosmeticItem)` - Buy cosmetic (returns bool)
- `EquipCosmetic(CosmeticItem)` - Equip owned cosmetic
- `IsOwned(CosmeticItem)` - Check ownership
- `IsEquipped(CosmeticItem)` - Check if equipped
- `GetCosmeticsByType(CosmeticType)` - Filter cosmetics

**PlatformInputManager:**
- `EnableInput()` / `DisableInput()` - Control input state
- Events: `OnJumpPressed`, `OnLeftDoorSelected`, `OnShopPressed`, etc.

**ObjectPool:**
- `Get()` - Get pooled object
- `Return(GameObject)` - Return to pool
- `Preload(int)` - Preload objects

---

## ✅ Finalization Checklist

- [ ] All cosmetic ScriptableObjects created
- [ ] Shop UI fully linked and functional
- [ ] Diamond display appears in HUD
- [ ] Input works on both desktop and mobile
- [ ] Save/load persists diamonds and cosmetics
- [ ] Shop purchases and equips work correctly
- [ ] Canvas scales properly on all resolutions
- [ ] Performance target (60 FPS) achieved on mobile
- [ ] Build tested on physical iOS/Android devices
- [ ] Desktop build tested with keyboard
- [ ] All UI elements positioned for safe areas

---

**Congratulations! Your Infinite Haus game is now fully cross-platform with a complete shop system!** 🎉

For additional support, refer to:
- **ARCHITECTURE.md** - System design details
- **SCRIPT_REFERENCE.md** - Complete API documentation
- **SETUP_GUIDE.md** - Initial game setup

---

**Built with ❤️ for Unity**
