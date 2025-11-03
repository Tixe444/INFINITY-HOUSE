# 🔍 DIAGNOSE: Unity Blauer Bildschirm Problem

## ❗ WICHTIGE FRAGEN - Bitte beantworte diese:

### 1. Welche Unity Version verwendest du GENAU?
```
Öffne Unity Hub → Installationen → Welche Version ist installiert?
Erforderlich: Unity 2022.3.x LTS
```

### 2. WIE öffnest du das Projekt?
**Option A** (Richtig):
- Unity Hub öffnen
- Projects Tab
- "Add" oder "Open" klicken
- Den INFINITY-HOUSE **ORDNER** auswählen (nicht eine einzelne Datei!)
- Projekt in der Liste anklicken

**Option B** (Falsch - funktioniert nicht):
- Direkt Unity.exe öffnen
- Datei → Open Project
- Eine .unity Datei auswählen ❌ FALSCH!

### 3. Hast du diese Befehle ausgeführt?
```bash
cd INFINITY-HOUSE
git pull origin claude/infinite-haus-game-setup-011CUjncBBm46xcRhvQDTyYh
rm -rf Library/
rm -rf Temp/
```

### 4. Was siehst du GENAU?
- [ ] Blauer Bildschirm im Game-View
- [ ] Blauer Bildschirm im Scene-View
- [ ] Unity stürzt ab
- [ ] Unity lädt endlos ("Loading...")
- [ ] Fehlermeldungen in der Console

---

## 🛠️ SCHRITT-FÜR-SCHRITT ANLEITUNG

### Schritt 1: Unity komplett schließen
```bash
# Stelle sicher, dass Unity NICHT läuft
# Schließe Unity Hub UND Unity Editor
```

### Schritt 2: Git Pull
```bash
cd /pfad/zu/INFINITY-HOUSE
git pull origin claude/infinite-haus-game-setup-011CUjncBBm46xcRhvQDTyYh
```

Erwartete Ausgabe:
```
Already up to date.
```
ODER
```
Updating bfc9964..XXXXXX
```

### Schritt 3: Verifiziere Szenen-Dateien
```bash
ls -la Assets/Scenes/
```

Erwartete Ausgabe:
```
00_Boot.unity
00_Boot.unity.meta
02_Run.unity
02_Run.unity.meta
```

### Schritt 4: Cache komplett löschen
```bash
rm -rf Library/
rm -rf Temp/
rm -rf obj/
rm -rf .vs/
```

**WARUM?** Unity cached alte, falsche Referenzen. Diese MÜSSEN weg!

### Schritt 5: Unity Version prüfen
```
Unity Hub → Installationen

Erforderlich: 2022.3.x LTS
Beispiele: 2022.3.10f1, 2022.3.15f1, etc.
```

Falls FALSCHE Version installiert:
1. Unity Hub → Installationen → "Install Editor"
2. Wähle "2022.3 LTS" → "Recommended Release"
3. Warte bis Installation fertig ist

### Schritt 6: Projekt öffnen (RICHTIGE Methode)
```
1. Unity Hub öffnen
2. Tab "Projects" anklicken
3. Button "Add" oder "Open" klicken
4. Den ORDNER "INFINITY-HOUSE" auswählen (nicht eine .unity Datei!)
5. In der Projektliste auf "INFINITY-HOUSE" klicken
```

### Schritt 7: Warten während Import
```
Unity öffnet Projekt
→ "Importing Assets..." (1-5 Minuten beim ersten Mal!)
→ Fortschrittsbalken läuft durch
→ WARTE bis der Balken fertig ist!
```

---

## 📋 ERWARTETES ERGEBNIS

### Im Unity Editor solltest du sehen:

**Hierarchy Panel** (links):
```
00_Boot
  └── Main Camera
```

**Project Panel** (unten):
```
Assets/
  └── Scenes/
      ├── 00_Boot (Unity Icon)
      └── 02_Run (Unity Icon)
```

**Scene View** (Mitte):
- Ein Skybox-Hintergrund (blau-grau Gradient)
- Camera Gizmo

**Game View** (Mitte):
- Skybox-Hintergrund

**Console Panel** (unten):
- 0 Errors
- Eventuell Warnings (okay)

---

## 🐛 FEHLERSUCHE

### Problem: Immer noch blauer Bildschirm

#### Test 1: Welche Szene ist geladen?
```
Im Unity Editor oben in der Toolbar:
Steht dort "00_Boot" oder "02_Run"?

Falls dort "Untitled" oder "SampleScene" steht:
→ Assets/Scenes/ im Project Panel öffnen
→ Doppelklick auf "00_Boot"
```

#### Test 2: Console Errors?
```
Unity Editor → Window → General → Console
Gibt es rote Fehlermeldungen?

BITTE KOPIERE ALLE ERRORS UND SENDE SIE MIR!
```

#### Test 3: Unity Logs
```
Windows:
C:\Users\<Username>\AppData\Local\Unity\Editor\Editor.log

Linux:
~/.config/unity3d/Editor.log

Mac:
~/Library/Logs/Unity/Editor.log
```

**WICHTIG**: Öffne diese Datei und suche nach:
- "error"
- "failed"
- "could not"

#### Test 4: Projekt-Pfad
```bash
pwd
# Sollte enden mit: /INFINITY-HOUSE

ls -la
# Sollte zeigen:
# - Assets/
# - ProjectSettings/
# - Packages/
```

---

## 📸 SCREENSHOTS HELFEN!

Bitte mache Screenshots von:
1. Unity Hub → Projects Liste (zeigt verfügbare Projekte)
2. Unity Editor → Gesamtansicht nach dem Öffnen
3. Unity Editor → Console Panel (alle Errors)
4. Unity Editor → Project Panel → Assets/Scenes/

---

## 🔬 ERWEITERTE DIAGNOSE

### Kommando 1: Verifiziere Dateiformat
```bash
head -5 Assets/Scenes/00_Boot.unity
```

Sollte zeigen:
```
%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!29 &1
OcclusionCullingSettings:
  m_ObjectHideFlags: 0
```

### Kommando 2: Verifiziere .meta Datei
```bash
cat Assets/Scenes/00_Boot.unity.meta
```

Sollte zeigen:
```
fileFormatVersion: 2
guid: 4ad0f37bc17df2c0bab973c1622c4daf
SceneImporter:  ← WICHTIG: SceneImporter, NICHT DefaultImporter!
```

### Kommando 3: Verifiziere EditorBuildSettings
```bash
grep "00_Boot" ProjectSettings/EditorBuildSettings.asset
```

Sollte zeigen:
```
    path: Assets/Scenes/00_Boot.unity
    guid: 4ad0f37bc17df2c0bab973c1622c4daf
```

### Kommando 4: Verifiziere ProjectSettings
```bash
grep "templateDefaultScene" ProjectSettings/ProjectSettings.asset
```

Sollte zeigen:
```
  templateDefaultScene: Assets/Scenes/00_Boot.unity
```

**NICHT:**
```
  templateDefaultScene: Assets/Scenes/SampleScene.unity  ← FALSCH!
```

---

## ⚠️ HÄUFIGE FEHLER

### Fehler 1: Falscher Ordner geöffnet
```
DU ÖFFNEST: C:/Users/Name/INFINITY-HOUSE/Assets/
RICHTIG IST: C:/Users/Name/INFINITY-HOUSE/
```

### Fehler 2: Einzelne .unity Datei geöffnet
```
DU ÖFFNEST: INFINITY-HOUSE/Assets/Scenes/00_Boot.unity  ← FALSCH!
RICHTIG IST: INFINITY-HOUSE/  ← Den ORDNER öffnen!
```

### Fehler 3: Library/ nicht gelöscht
```
Alter Cache verhindert korrektes Laden
→ Unity SCHLIESSEN
→ rm -rf Library/
→ Unity neu öffnen
```

### Fehler 4: Falsche Unity Version
```
Projekt: 2022.3 LTS
Du hast: 2021.x oder 2023.x  ← Inkompatibel!
→ Installiere 2022.3 LTS via Unity Hub
```

---

## 📞 WENN NICHTS FUNKTIONIERT

Bitte sende mir:

1. **Deine Unity Version**: Unity Hub → Installationen
2. **Console Errors**: Copy/Paste aller roten Fehler
3. **Git Status**:
   ```bash
   cd INFINITY-HOUSE
   git log --oneline -5
   git status
   ```
4. **Datei-Verifikation**:
   ```bash
   ls -la Assets/Scenes/
   head -5 Assets/Scenes/00_Boot.unity
   cat Assets/Scenes/00_Boot.unity.meta
   ```
5. **WIE du das Projekt öffnest**: Schritt für Schritt

---

## ✅ SCHNELL-CHECK

```bash
# Führe diese Befehle aus und sende mir die Ausgabe:

echo "=== Git Status ==="
git log --oneline -3

echo "=== Szenen-Dateien ==="
ls -lh Assets/Scenes/

echo "=== Scene Format ==="
head -5 Assets/Scenes/00_Boot.unity

echo "=== Meta File ==="
cat Assets/Scenes/00_Boot.unity.meta

echo "=== Build Settings ==="
grep -A2 "00_Boot" ProjectSettings/EditorBuildSettings.asset

echo "=== Project Settings ==="
grep "templateDefaultScene" ProjectSettings/ProjectSettings.asset
```

Kopiere die KOMPLETTE Ausgabe und sende sie mir!
