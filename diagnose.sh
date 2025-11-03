#!/bin/bash
# INFINITY HOUSE - Unity Projekt Diagnose Script

echo "================================================"
echo "INFINITY HOUSE - Unity Projekt Diagnose"
echo "================================================"
echo ""

echo "=== Git Status ==="
git log --oneline -5
echo ""
git status
echo ""

echo "=== Szenen-Dateien ==="
ls -lh Assets/Scenes/ 2>/dev/null || echo "FEHLER: Assets/Scenes/ nicht gefunden!"
echo ""

echo "=== Scene Format Check ==="
if [ -f "Assets/Scenes/00_Boot.unity" ]; then
    head -5 Assets/Scenes/00_Boot.unity
else
    echo "FEHLER: 00_Boot.unity nicht gefunden!"
fi
echo ""

echo "=== Meta File Check ==="
if [ -f "Assets/Scenes/00_Boot.unity.meta" ]; then
    cat Assets/Scenes/00_Boot.unity.meta
else
    echo "FEHLER: 00_Boot.unity.meta nicht gefunden!"
fi
echo ""

echo "=== Build Settings Check ==="
if [ -f "ProjectSettings/EditorBuildSettings.asset" ]; then
    grep -A2 "00_Boot" ProjectSettings/EditorBuildSettings.asset
else
    echo "FEHLER: EditorBuildSettings.asset nicht gefunden!"
fi
echo ""

echo "=== Project Settings Check ==="
if [ -f "ProjectSettings/ProjectSettings.asset" ]; then
    grep "templateDefaultScene" ProjectSettings/ProjectSettings.asset
else
    echo "FEHLER: ProjectSettings.asset nicht gefunden!"
fi
echo ""

echo "=== ProjectVersion Check ==="
if [ -f "ProjectSettings/ProjectVersion.txt" ]; then
    cat ProjectSettings/ProjectVersion.txt
else
    echo "FEHLER: ProjectVersion.txt nicht gefunden!"
fi
echo ""

echo "=== Packages Manifest Check ==="
if [ -f "Packages/manifest.json" ]; then
    echo "manifest.json existiert ✓"
    grep -E "(inputsystem|render-pipelines)" Packages/manifest.json
else
    echo "FEHLER: Packages/manifest.json nicht gefunden!"
fi
echo ""

echo "=== Library Check ==="
if [ -d "Library" ]; then
    echo "⚠️  Library/ Ordner existiert (sollte gelöscht werden!)"
    echo "   Größe: $(du -sh Library/ 2>/dev/null | cut -f1)"
else
    echo "✓ Library/ nicht vorhanden (gut!)"
fi
echo ""

echo "=== Aktueller Pfad ==="
pwd
echo ""

echo "================================================"
echo "DIAGNOSE ABGESCHLOSSEN"
echo "================================================"
echo ""
echo "📋 Bitte kopiere diese KOMPLETTE Ausgabe und sende sie!"
echo ""
