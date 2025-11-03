# INFINITY HOUSE - Quick Start Guide

**Sofortiger Start nach Download**

---

## ⚡ 3-Schritte-Start

```bash
# 1. Klonen
git clone https://github.com/Tixe444/INFINITY-HOUSE.git

# 2. Öffnen
Unity Hub → Open → INFINITY-HOUSE Ordner

# 3. Spielen
Unity Editor → Play Button ▶️
```

**✅ Fertig!** Szenen werden automatisch erstellt.

---

## 🔄 Was passiert automatisch?

Beim ersten Unity-Start führt **IH_AutoSceneSetup** aus:

### ✅ Szenen erstellen
- `00_Boot.unity` - Init + Core Services
- `01_Menu.unity` - Hauptmenü
- `02_Run.unity` - Gameplay (Autorunner)

### ✅ Build Settings
- 3 Szenen zur Build-Liste
- Boot als Startszene

### ✅ Player Settings
- Input Handling: Both
- Company: TixeStudio
- Product: INFINITY HOUSE

### ✅ Boot-Szene öffnen
- Bereit zum Play!

---

## 📺 Erwartete Console-Ausgabe

```
[INFINITY HOUSE] First-time setup: Creating scenes...
[Setup] ✅ Created Boot scene: Assets/Scenes/00_Boot.unity
[Setup] ✅ Created Main scene: Assets/Scenes/02_Run.unity
[Setup] ✅ Created Menu scene: Assets/Scenes/01_Menu.unity
[Setup] Build Settings configured
[INFINITY HOUSE] ✅ Setup complete! Press Play to start the game.
```

---

## 🎮 Steuerung

| Aktion | Taste |
|--------|-------|
| Springen | Space / Klick |
| Tür Links | Pfeil Links |
| Tür Rechts | Pfeil Rechts |

---

## ❌ Troubleshooting

### Blauer Screen beim Play?

```
1. Menu Bar → Infinity House → Force Scene Setup
2. Unity neu starten
3. Play erneut
```

### "Missing Reference" Fehler?

```
1. Öffne 02_Run.unity
2. IH_BiomeManager → Assign Player + Camera
3. Szene speichern
```

### Keine Eingabe?

```
Edit → Project Settings → Player
→ Active Input Handling → "Both"
Unity neu starten
```

---

## ✅ Checklist

- [ ] Unity 2022.3 LTS
- [ ] Console: "Setup complete!"
- [ ] Assets/Scenes/ hat 3 .unity Files
- [ ] Build Settings zeigt 3 Szenen

---

**Mehr Infos?**
- README_BUILD.md - Build-Prozess
- INFINITY_HOUSE_v6.0_FINAL_VARIANT.md - Game Design

---

**INFINITY HOUSE v6.0**
🎮 Just Open & Play! 🚀
