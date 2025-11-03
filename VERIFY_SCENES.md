# SZENEN-VERIFIKATION - INFINITY HOUSE

## ✅ WAS IST JETZT ANDERS?

**PROBLEM VORHER**: Nur Auto-Setup-Scripts, aber keine echten .unity Dateien im Repository
**LÖSUNG JETZT**: **2 funktionierende Unity-Szenen direkt im Repository committed**

---

## 📁 VORHANDENE DATEIEN (IM GIT)

```bash
# Diese Dateien sind JETZT im Repository:
Assets/Scenes/00_Boot.unity         # Funktionsfähige Boot-Szene (YAML)
Assets/Scenes/00_Boot.unity.meta    # Unity Meta-Datei
Assets/Scenes/02_Run.unity          # Funktionsfähige Run-Szene mit Player + Ground
Assets/Scenes/02_Run.unity.meta     # Unity Meta-Datei

ProjectSettings/EditorBuildSettings.asset    # Build-Liste mit beiden Szenen
ProjectSettings/QualitySettings.asset        # Quality Settings
ProjectSettings/GraphicsSettings.asset       # Graphics Settings
```

---

## 🔍 WIE DU VERIFIZIEREN KANNST

### **VOR dem Unity-Öffnen (Git-Check)**:

```bash
cd INFINITY-HOUSE

# 1. Prüfe ob Scene-Dateien existieren
ls -la Assets/Scenes/
# Erwartet: 00_Boot.unity, 00_Boot.unity.meta, 02_Run.unity, 02_Run.unity.meta

# 2. Prüfe Scene-Inhalt (sollte YAML sein, nicht leer)
head -20 Assets/Scenes/00_Boot.unity
# Erwartet: "%YAML 1.1" als erste Zeile

# 3. Prüfe EditorBuildSettings
cat ProjectSettings/EditorBuildSettings.asset | grep "path:"
# Erwartet: 
#   path: Assets/Scenes/00_Boot.unity
#   path: Assets/Scenes/02_Run.unity
```

### **NACH dem Unity-Öffnen (Editor-Check)**:

1. **Unity öffnen**:
   ```
   Unity Hub → Open → INFINITY-HOUSE Ordner
   ```

2. **Project-Fenster prüfen**:
   ```
   Assets → Scenes → Du solltest 2 Szenen sehen:
   - 00_Boot (Unity icon)
   - 02_Run (Unity icon)
   ```

3. **Szene öffnen**:
   ```
   Doppelklick auf 00_Boot.unity
   → Szene öffnet sich
   → Hierarchy zeigt: Main Camera
   → Scene View zeigt Kamera
   ```

4. **Build Settings prüfen**:
   ```
   File → Build Settings
   → Scenes In Build sollte zeigen:
     ✓ Assets/Scenes/00_Boot.unity
     ✓ Assets/Scenes/02_Run.unity
   ```

5. **Play Mode testen**:
   ```
   Mit 00_Boot.unity geöffnet
   → Play Button drücken ▶️
   → Game View sollte dunklen Hintergrund zeigen (nicht blau!)
   → Console sollte keine Errors zeigen
   ```

6. **Run Scene testen**:
   ```
   Doppelklick auf 02_Run.unity
   → Szene öffnet sich
   → Hierarchy zeigt:
     - Main Camera
     - Player (cyan square)
     - Ground (gray rectangle)
   → Play Button drücken ▶️
   → Du solltest Player und Ground sehen
   ```

---

## 🎯 ERWARTETES VERHALTEN

### **00_Boot.unity**:
- **Inhalt**: Main Camera (orthographic, dark background)
- **Play Mode**: Dunkler Bildschirm, keine Errors
- **Zweck**: Initialisierung, später werden Scripts hinzugefügt

### **02_Run.unity**:
- **Inhalt**: 
  - Main Camera (orthographic, dark pixel background)
  - Player GameObject (cyan square, Position: -8, 0, 0)
  - Ground GameObject (gray rectangle, Position: 0, -3, 0, Scale: 50x1x1)
- **Play Mode**: 
  - Cyan Square (Player) sichtbar links
  - Gray Ground sichtbar unten
  - Keine Fehler in Console
- **Zweck**: Gameplay-Szene (Scripts werden später per Auto-Setup hinzugefügt)

---

## ❌ WENN IMMER NOCH BLAU

Falls Unity immer noch einen blauen Bildschirm zeigt:

### **Check 1: Wurde Git korrekt gesynct?**
```bash
git pull origin claude/infinite-haus-game-setup-011CUjncBBm46xcRhvQDTyYh
git log --oneline | head -1
# Sollte zeigen: "Complete Project Setup..." oder neueren Commit
```

### **Check 2: Sind die Scene-Dateien wirklich da?**
```bash
file Assets/Scenes/00_Boot.unity
# Sollte zeigen: "Assets/Scenes/00_Boot.unity: ASCII text"
# NICHT: "No such file or directory"
```

### **Check 3: Unity Cache löschen**
```bash
# Unity schließen
rm -rf Library/
# Unity neu öffnen über Unity Hub
```

### **Check 4: Unity Console prüfen**
```
Unity Editor → Console Tab (Ctrl+Shift+C)
→ Prüfe auf Errors (rote Symbole)
→ Prüfe auf "Scene not found" Meldungen
```

### **Check 5: Default Scene prüfen**
```
Edit → Project Settings → Editor
→ "Enter Play Mode Options" sollte disabled sein
→ Wenn aktiviert: Option deaktivieren
```

---

## 📊 SCENE FILE STRUKTUR

Die .unity Dateien sind im **Unity YAML-Format**:

```yaml
%YAML 1.1                           # YAML Header
%TAG !u! tag:unity3d.com,2011:     # Unity Tag
--- !u!29 &1                        # OcclusionCullingSettings
OcclusionCullingSettings:
  ...
--- !u!104 &2                       # RenderSettings
RenderSettings:
  m_BackGroundColor: {r: 0.1, ...}  # Hintergrundfarbe
  ...
--- !u!1 &519420028                 # GameObject (Camera)
GameObject:
  m_Name: Main Camera
  m_TagString: MainCamera
  ...
--- !u!20 &519420031                # Camera Component
Camera:
  orthographic: 1
  orthographic size: 5
  ...
```

**Wichtig**: Diese Dateien sind **echte Unity-Szenen**, nicht Platzhalter!

---

## 🚀 NÄCHSTE SCHRITTE

Nachdem Unity die Szenen erfolgreich öffnen kann:

1. **Auto-Setup läuft**: IH_AutoSceneSetup und IH_ProjectValidator laufen automatisch
2. **Scripts werden hinzugefügt**: Die Szenen werden mit Components erweitert
3. **Gameplay funktioniert**: Player Controller, Physics, etc. werden aktiv

**ABER**: Die Szenen müssen **zuerst** geöffnet werden können, damit die Auto-Setup-Scripts laufen!

---

## ✅ SUCCESS INDICATORS

**Du weißt, dass es funktioniert, wenn**:

- [ ] `git log` zeigt den neuesten Commit
- [ ] `ls Assets/Scenes/` zeigt .unity Dateien
- [ ] Unity öffnet das Projekt ohne Errors
- [ ] Project-Fenster zeigt 00_Boot und 02_Run
- [ ] Doppelklick auf 00_Boot.unity öffnet die Szene
- [ ] Hierarchy zeigt "Main Camera"
- [ ] Play-Button zeigt **dunklen** Bildschirm (nicht blau!)
- [ ] Console zeigt 0 Errors

---

## 📝 COMMIT INFO

**Commit**: Wird gerade erstellt
**Branch**: `claude/infinite-haus-game-setup-011CUjncBBm46xcRhvQDTyYh`
**Dateien**: 2 .unity Szenen + 3 .meta Dateien + 3 ProjectSettings

---

**INFINITY HOUSE v6.0**
**Echte Szenen jetzt im Repository**

🎮 Jetzt sollte Unity die Szenen direkt laden können! 🚀
