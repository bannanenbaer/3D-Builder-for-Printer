# 3D Builder Pro

Eine professionelle Desktop-Anwendung zum Erstellen und Optimieren von 3D-Modellen für den 3D-Druck – mit animiertem KI-Maskottchen **Brixl**, automatischer Fehlerkorrektur und integriertem Update-System.

---

## Features

### 3D-Modellierung
- **15 parametrische Grundformen**: Quader, Kugel, Zylinder, Kegel, Torus, Prisma, Pyramide, Rohr, Ellipsoid, Halbkugel, L-Profil, T-Profil, Sternform, Polygon, Gewinde-Zylinder
- Alle Parameter frei in **mm** einstellbar
- **Kanten abrunden** (Fillet) und **Fasen** (Chamfer)
- **Boolean-Operationen**: Vereinigung, Subtraktion, Schnitt
- **OpenSCAD Editor** mit Live-Vorschau
- **Undo / Redo** für alle Operationen

### Brixl – Das Maskottchen
- Animiertes Maskottchen läuft durch den 3D-Viewport
- **Chat-Panel**: Stelle Brixl direkt Fragen (lokale KI oder Claude API)
- Sprechblasen mit Tipps und Reaktionen
- Zeigt Fehler im Modell mit Pinsel-Animation an
- Repariert Probleme mit Hammer-Animation

### AutoFix
- Erkennt Druckprobleme automatisch (scharfe Kanten, dünne Wände, Löcher, nicht-manifold Geometrie)
- Behebt Fehler mit einem Klick
- Farbkodierte Fehlermarkierung im Viewport
- Qualitätsanalyse mit Score

### Weitere Features
- **STL & 3MF Import/Export** (direkt in PrusaSlicer öffnen)
- **Zweisprachig**: Deutsch / English
- **Update-System**: Prüft GitHub Releases automatisch, lädt neue Version herunter
- **Erstes-Start-Tutorial** von Brixl
- Elegantes dunkles Design (Material Design 3)

---

## Installation (Windows)

### Schnellstart mit Installer

1. Gehe zu: `https://github.com/bannanenbaer/3D-Builder-for-Printer/releases`
2. Lade `3DBuilderPro-Setup.exe` herunter
3. Doppelklick – der Installer kümmert sich um alles (inkl. Python)

### Manuell / Entwickler

**Voraussetzungen:**

| Komponente | Version |
|---|---|
| .NET SDK | 8.0+ |
| Python | 3.10+ |
| CadQuery | 2.3+ |

```powershell
# Python-Abhängigkeiten
pip install cadquery

# Projekt bauen
.\build.ps1

# Starten
.\dist\app\ThreeDBuilder.exe
```

---

## Bedienung

| Aktion | Beschreibung |
|---|---|
| Form erstellen | Im linken Panel auf Form klicken |
| Maße ändern | Werte im Properties-Panel eingeben |
| AutoFix starten | AutoFix-Panel → "Start AutoFix!" |
| Undo/Redo | Buttons in der Toolbar |
| Fillet/Chamfer | Radius eingeben → "Apply" |
| Boolean | Zwei Objekte wählen → Union/Subtract/Intersect |
| STL exportieren | File → Export |
| 3D-Ansicht | Scroll: Zoom, Rechtsklick+Drag: Drehen, Mittelklick: Pan |
| Brixl fragen | Chat-Panel öffnen und Frage eingeben |
| Update prüfen | Toolbar → Update-Button |

---

## Technologie

| Schicht | Technologie |
|---|---|
| GUI / UI | C# WPF + Material Design 3 |
| 3D-Ansicht | HelixToolkit (OpenGL via WPF) |
| Geometrie-Engine | Python + CadQuery (OpenCASCADE) |
| KI-Assistent | Claude API (optional, lokal als Fallback) |
| Animationen | WPF Storyboards |
| Installer | WiX Toolset (setup.exe) |

---

## Projektstruktur

```
3D-Builder-for-Printer/
├── CSharpUI/
│   ├── Views/              # XAML UI (MainWindow, Panels, MascotView)
│   ├── ViewModels/         # MVVM-Logik
│   ├── Services/           # KI, AutoFix, Undo/Redo, Update, PythonBridge
│   ├── Models/             # SceneObject, ChatMessage, QualityReport
│   └── ThreeDBuilder.csproj
├── PythonBackend/
│   ├── server.py           # JSON-IPC Server
│   ├── shapes.py           # 15 Formgeneratoren
│   ├── operations.py       # Fillet/Chamfer/Boolean/AutoFix
│   └── scad_bridge.py      # OpenSCAD-Integration
├── Installer/              # WiX Installer (setup.exe)
├── build.ps1               # Build-Skript
└── README.md
```

---

## Updates veröffentlichen

Der Installer wird **lokal gebaut** und **manuell als Release** hochgeladen:

```powershell
# Setup.exe bauen
.\build.ps1

# Dann auf GitHub:
# Releases → New Release → Tag: "latest" → setup.exe hochladen
```

Die App prüft beim Klick auf "Update" automatisch diesen Release.

---

## Lizenz

MIT License – frei verwendbar und anpassbar.

---

**Viel Spaß beim Drucken!**
