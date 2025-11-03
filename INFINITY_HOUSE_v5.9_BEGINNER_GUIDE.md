# INFINITE HAUS v5.9 - Beginner's Guide
## Biome & Market Update

**Willkommen zu INFINITE HAUS!** Dieses Dokument erklärt die Struktur des Projekts für Unity-Einsteiger.

---

## 📁 Projekt-Struktur

```
Assets/
├── _Project/                    # Hauptprojekt-Ordner
│   ├── Addressables/           # Ladbare Assets (Musik, Sprites)
│   ├── Materials/              # Materialien für Renderer
│   ├── Prefabs/                # Wiederverwendbare Objekte
│   ├── Scenes/                 # Alle Szenen
│   │   ├── 00_Boot.unity       # Startszene (lädt alles)
│   │   ├── 01_MainMenu.unity   # Hauptmenü
│   │   ├── 02_Run.unity        # Gameplay (Autorun)
│   │   ├── 03_Market.unity     # Shop/Market
│   │   └── 99_DevTest.unity    # Entwickler-Test
│   ├── Scripts/                # Alle C# Scripts
│   │   ├── Data/               # ScriptableObjects
│   │   ├── Game/               # Core Gameplay
│   │   ├── Economy/            # Währungen (Shards, Dust, XP)
│   │   ├── Player/             # Spieler-Controller
│   │   ├── Monsters/           # Chaser-Logik
│   │   └── UI/                 # User Interface
│   ├── ScriptableObjects/      # Instanzen der Configs
│   └── Settings/               # Unity-Einstellungen
```

---

## 🎮 Core Loop (Wie das Spiel funktioniert)

### 1. **Start**
- Spiel startet in `00_Boot.unity`
- Lädt `01_MainMenu.unity` (additive)
- Spieler drückt "Start" → lädt `02_Run.unity`

### 2. **Run Phase**
```
Player spawnt offscreen
    ↓
Autorun startet (6.2m/s)
    ↓
Obstacles spawnen (Spikes, Goo, Gaps)
    ↓
Player sammelt Shards
    ↓
Chaser verfolgt (Chase Meter steigt)
    ↓
Nach ~60s → Doors spawnen (Links/Rechts)
```

### 3. **Door Choice**
- **Left Door**: Style Boost + Biome Swap
- **Right Door**: Loot + Risk Increase

### 4. **Biome Transition**
```
Biome A → smooth transition (0.6-1.2s) → Biome B
- Musik crossfadet
- Farben interpolieren
- Movement-Parameter smoothen
- Momentum bleibt erhalten!
```

### 5. **Death**
- Slow-mo → Flash → Freeze-Frame
- Results HUD zeigt: Distance, Shards, Style, XP, Dust
- Retry / Market / Home

---

## 🏔️ Biome System

### Was ist ein Biome?
Ein **Biome** definiert das "Gefühl" eines Korridors:
- Visuals (Farbe, Parallax, Lighting)
- Music & SFX
- Movement Feel (Gravity, Friction, Air Control)
- Obstacles & Shard Frequency

### ScriptableObject: `IH_BiomeConfig`

```csharp
// Beispiel: Dark Pixel Biome
biomeID = "dark_pixel"
biomeName = "Dark Pixel"
backgroundColor = Color.black
gravityMult = 1.0  // Normal gravity
speedMult = 1.0-1.2 // Leicht schneller
jumpCoyoteMs = 90 // Coyote Time
```

### Movement Parameters

| Parameter | Was macht es? | Bereich |
|-----------|---------------|---------|
| `gravityMult` | Schwerkraft-Stärke | 0.8-1.5 |
| `airControlMult` | Kontrolle in der Luft | 0.3-1.0 |
| `frictionMult` | Boden-Reibung | 0.5-2.0 |
| `speedMult` | Geschwindigkeits-Bonus | 0.8-1.5 |
| `jumpCoyoteMs` | Sprung-Gnade nach Kante | 80-120ms |
| `jumpBufferMs` | Sprung-Puffer vor Boden | 80-120ms |

**Coyote Time**: Du kannst noch springen, kurz NACHDEM du eine Kante verlassen hast (fühlt sich fairer an).

**Jump Buffer**: Wenn du zu früh drückst (BEVOR du landest), merkt sich das Spiel den Input (fühlt sich responsiver an).

### Smooth Transitions

**Das Wichtigste**: Beim Biome-Wechsel bleibt dein **Momentum erhalten**!

```csharp
// IH_BiomeManager.cs macht das:
1. Speichert current Movement Params
2. Lädt target Movement Params
3. Interpoliert smooth über 0.6-1.2s:
   - Gravity: Lerp(current, target, t)
   - Friction: Lerp(current, target, t)
   - etc.
4. Music crossfadet parallel
5. Farben blenden smooth
```

**Resultat**: Kein "Ruckler", kein Momentum-Verlust, fühlt sich fluid an!

---

## 💎 Duplicate → Shards System

### Problem
Du bekommst ein Item, das du schon hast (Duplicate). Was nun?

### Lösung: Auto-Convert to Shards!

```
Duplicate erkannt
    ↓
Auto-Convert aktiviert
    ↓
Conversion Rates:
  - Common: 1 Shard
  - Rare: 3 Shards
  - Epic: 8 Shards
  - Legendary: 20 Shards
    ↓
Satisfying UX:
  1. Modal erscheint
  2. Particle-Effekt (Cone zum Shard-Icon)
  3. Haptic Feedback (0.7s vibration)
  4. Counter animiert hoch (ease-out)
  5. SFX: Glass Chime → Whoosh → Coin Tick
    ↓
Shards in Wallet!
```

### Code-Beispiel

```csharp
// IH_DuplicateConverter.cs
void ConvertDuplicate(string itemID, RarityTier rarity, Sprite icon)
{
    int shardsEarned = GetShardsForRarity(rarity);

    // Add to economy
    economyService.AddShards(shardsEarned);

    // Show modal with FX
    ShowConversionModalCoroutine(itemID, shardsEarned, rarity, icon);
}
```

---

## 🛒 Shop / Market System

### Tabs
1. **Avatars** - Charaktere
2. **Trails** - Visuelle Effekte
3. **Boosts** - Temporäre Power-Ups

### Offer Rotation
- **6 Slots** im Shop
- **24h Refresh** (oder manuell mit Diamonds)
- **Min Rarity** filter (nur Rare+)
- **No Dupes** in aktueller Rotation

### Preview System
```
Item Card klicken
    ↓
Preview-Szene lädt (mini-run 8-10s)
    ↓
Zeigt Item in Action:
  - Avatar rennt
  - Trail progressiert T1→T2→T3
  - Obstacles spawnen
    ↓
"Buy" oder "Back"
```

### Preisformel

```csharp
finalPrice = basePrice * rarityMult * (1 + ownedCount * growthFactor)

Beispiel:
- Common Skin: 100 Dust
- Rare Skin: 300 Dust (3x)
- Epic Skin: 900 Dust (9x)
- Legendary Skin: 2700 Dust (27x)

Wenn du schon 2 hast:
finalPrice = 100 * 1 * (1 + 2 * 0.1) = 120 Dust
```

---

## 💰 Economy (Währungen)

### 3 Currencies

| Currency | Wie kriegen? | Wofür? |
|----------|--------------|--------|
| **Shards** | Duplicates | Gacha Pulls |
| **Dust** | Run rewards | Shop Käufe |
| **XP** | Distance traveled | Level Up |

### IH_EconomyService

```csharp
// Singleton - von überall zugreifbar
IH_EconomyService.Instance.AddShards(10);
IH_EconomyService.Instance.AddDust(500);
IH_EconomyService.Instance.AddXP(100);

// Checken
int myShards = IH_EconomyService.Instance.GetShards();

// Ausgeben (gibt true bei Erfolg)
bool success = IH_EconomyService.Instance.TrySpendDust(300);
```

### Events

```csharp
IH_EconomyService.Instance.OnShardsChanged += (newAmount) =>
{
    Debug.Log($"Shards: {newAmount}");
    UpdateUI();
};

IH_EconomyService.Instance.OnLevelUp += (newLevel) =>
{
    Debug.Log($"Level Up! → {newLevel}");
    ShowLevelUpPopup();
};
```

---

## 🎨 ScriptableObjects (Configs)

### Warum ScriptableObjects?
- **Designer-freundlich**: Keine Code-Änderungen nötig
- **Wiederverwendbar**: Einmal erstellen, überall benutzen
- **Performance**: Keine Instanziierung im Speicher

### Erstellen

1. **Rechtsklick im Project-Fenster**
2. `Create → Infinity House → Biome Config`
3. Datei benennen (z.B. `BiomeConfig_DarkPixel`)
4. Inspector: Werte eintragen
5. Script referenzieren:

```csharp
[SerializeField] private IH_BiomeConfig myBiome;
```

### Wichtige Configs

| Config | Wofür? |
|--------|--------|
| `IH_BiomeConfig` | Biome-Settings (Visuals, Movement, Obstacles) |
| `IH_FloorConfig` | Floor-Range, Biome-Auswahl, Difficulty Curve |
| `IH_ObstacleSet` | Obstacle-Liste mit Weights |

---

## 🧩 Core Systems (für Fortgeschrittene)

### IH_BiomeManager
**Was**: Verwaltet Biome-Auswahl und Transitions
**Wo**: Im Scene-Root (02_Run)
**Setup**:
```
1. FloorConfig zuweisen
2. Camera-Referenz
3. MoveTuner (Player) referenzieren
```

**Wichtigste Funktion**:
```csharp
public void SelectNextBiome(float doorBias)
{
    // doorBias: -1 = left, +1 = right
    currentFloor++;
    IH_BiomeConfig selected = floorConfig.SelectBiome(currentFloor, doorBias);
    TransitionToBiome(selected);
}
```

### IH_DuplicateConverter
**Was**: Convertiert Duplicates → Shards mit UX
**Wo**: Im Market-Scene (03_Market)
**Setup**:
```
1. Conversion Rates einstellen
2. Particle System zuweisen
3. SFX zuweisen
4. Modal UI referenzieren
```

### IH_EconomyService
**Was**: Zentrale Währungsverwaltung (Singleton)
**Wo**: Automatisch erstellt (DontDestroyOnLoad)
**Setup**: Keine! Einfach benutzen via `.Instance`

---

## 🚀 Quick Start (Spielen)

### Option A: Play Mode
1. Öffne `00_Boot.unity`
2. Play drücken
3. MainMenu erscheint → Start klicken
4. Spiel startet!

### Option B: Direct Run Test
1. Öffne `02_Run.unity`
2. Play drücken (testet nur Run-Phase)

---

## 🛠️ Häufige Aufgaben

### Neues Biome erstellen

1. **Create Config**:
   ```
   Create → Infinity House → Biome Config
   Name: BiomeConfig_MyBiome
   ```

2. **Werte eintragen**:
   - ID: `my_biome`
   - Name: `My Biome`
   - Background Color: Deine Farbe
   - Movement Params: Experimentieren!

3. **Obstacle Set zuweisen**:
   - Create → Infinity House → Obstacle Set
   - Prefabs hinzufügen (Spikes, Goo, etc.)
   - Im BiomeConfig referenzieren

4. **In FloorConfig hinzufügen**:
   - Öffne bestehende FloorConfig
   - Candidate Biomes → Add Element
   - BiomeRef = dein neues Biome
   - Weight = 1.0 (gleiche Chance wie andere)

5. **Testen**:
   - Play Mode
   - Nach 60s Tür wählen
   - Dein Biome sollte erscheinen können!

### Movement-Gefühl anpassen

Öffne BiomeConfig im Inspector:

**Schwerer/Langsamer**:
```
gravityMult: 1.2
airControlMult: 0.4
speedMult: 0.9
```

**Leichter/Floaty**:
```
gravityMult: 0.8
airControlMult: 0.8
speedMult: 1.1
```

**"Ice" Gefühl**:
```
frictionMult: 0.5 (rutschig!)
speedMult: 1.2
```

**"Precise" Gefühl**:
```
jumpCoyoteMs: 120 (mehr Gnade)
jumpBufferMs: 120 (mehr Buffer)
airControlMult: 0.8 (gute Kontrolle)
```

### Conversion Rates ändern

Öffne `IH_DuplicateConverter` im Inspector:

```
Shards Per Common: 1 → 2 (doppelt so viel)
Shards Per Legendary: 20 → 50 (großzügiger)
```

---

## 📱 Mobile-Specific

### Haptic Feedback
```csharp
#if UNITY_IOS || UNITY_ANDROID
Handheld.Vibrate(); // Simple vibration
#endif
```

### Touch Input
```csharp
if (Input.touchCount > 0)
{
    Touch touch = Input.GetTouch(0);
    if (touch.phase == TouchPhase.Began)
    {
        // Tap = Jump
        playerController.Jump();
    }
}
```

### Performance Targets
- **Frame Time**: <16ms (60 FPS)
- **Trail CPU**: <0.3ms
- **Trail GPU**: <0.5ms
- **Drawcalls**: 1-2

---

## 🐛 Debugging

### Common Issues

**Problem**: Biome wechselt nicht
**Lösung**:
- Check: FloorConfig hat Candidate Biomes?
- Check: BiomeManager hat FloorConfig zugewiesen?
- Console: "No valid biome for floor X"

**Problem**: Movement fühlt sich weird an
**Lösung**:
- Check: IH_BiomeManager hat MoveTuner referenziert?
- Check: Player hat IIH_MoveTuner Interface implementiert?

**Problem**: Duplicates werden nicht converted
**Lösung**:
- Check: IH_EconomyService existiert in Scene?
- Check: autoConvert = true?
- Console: Siehe Debug.Log Ausgaben

### Debug Tools

```csharp
#if UNITY_EDITOR
[ContextMenu("Test Transition")]
private void TestTransition()
{
    biomeManager.SelectNextBiome(1f); // Right door
}

[ContextMenu("Add 100 Shards")]
private void AddShards()
{
    IH_EconomyService.Instance.AddShards(100);
}
#endif
```

---

## 📖 Weiterführende Resourcen

### Unity Basics
- [Unity Learn](https://learn.unity.com/)
- [ScriptableObjects Tutorial](https://unity.com/how-to/architect-game-code-scriptable-objects)
- [Addressables Guide](https://docs.unity3d.com/Packages/com.unity.addressables@latest/)

### INFINITE HAUS Docs
- `AUDIT_REPORT_v5.8.md` - Technical deep-dive
- `RELEASE_NOTES_v5.8.md` - v5.8 changes
- `RELEASE_NOTES_v5.9.md` - This version!

---

## 🎉 Du hast es geschafft!

INFINITE HAUS v5.9 ist modular, performant, und Einsteiger-freundlich aufgebaut.

**Nächste Schritte**:
1. Spiel durchspielen (verstehen wie es funktioniert)
2. Eigenes Biome erstellen
3. Movement-Parameter experimentieren
4. Neue Obstacles bauen
5. Shop-Angebote konfigurieren

Viel Erfolg! 🚀

---

**Made with ❤️ for Unity Beginners**
INFINITE HAUS v5.9 - Biome & Market Update
