# 3D-Builder für 3D-Druck

Eine einfach bedienbare Desktop-Anwendung zum Erstellen und Bearbeiten von 3D-Modellen für den 3D-Druck.

**An easy-to-use desktop application for creating and editing 3D models for 3D printing.**

---

## 🇩🇪 Deutsch

### Features
- **15 Grundformen**: Quader, Kugel, Zylinder, Kegel, Torus, Prisma, Pyramide, Rohr, Ellipsoid, Halbkugel, L-Profil, T-Profil, Sternform, Polygon, Gewinde-Zylinder
- Alle Parameter frei in **mm** einstellbar
- **Kanten abrunden** (Fillet) und **Fasen** (Chamfer)
- **Boolean-Operationen**: Vereinigung, Subtraktion, Schnitt
- **STL importieren und exportieren** (direkt in Slicer wie PrusaSlicer öffnen)
- **OpenSCAD Editor** mit Live-Vorschau
- **Undo / Redo**
- **Zweisprachig**: Deutsch / English

### Systemvoraussetzungen

| Komponente | Version | Link |
|---|---|---|
| .NET Runtime | 8.0 oder neuer | https://dotnet.microsoft.com/download |
| Python | 3.10 oder neuer | https://python.org |
| CadQuery | 2.3+ | via pip |
| OpenSCAD | beliebig (optional) | https://openscad.org |

> **Windows-Nutzer**: .NET 8 ist oft schon vorinstalliert. Python muss ggf. extra installiert werden.

### Installation (Windows)

**Schritt 1 – Python installieren**
```
https://python.org → Download → Python 3.11 (oder neuer) → installieren
Wichtig: "Add Python to PATH" aktivieren!
```

**Schritt 2 – CadQuery installieren**
```cmd
pip install cadquery
```
> Hinweis: CadQuery hat viele Abhängigkeiten – Installation dauert ca. 2–5 Minuten.

**Schritt 3 – .NET 8 Runtime installieren** (falls nicht vorhanden)
```
https://dotnet.microsoft.com/download/dotnet/8.0
Dann ".NET Runtime" herunterladen und installieren
```

**Schritt 4 – OpenSCAD installieren** (optional, nur für SCAD-Editor)
```
https://openscad.org/downloads.html
Windows Installer herunterladen und installieren
```

**Schritt 5 – Anwendung kompilieren und starten**
```powershell
# Im Projektordner (PowerShell):
.\build.ps1

# Anwendung starten:
.\dist\app\3D-Builder.exe
```

### Bedienung

| Aktion | Beschreibung |
|---|---|
| **Form auswählen** (links) | Klick auf eine Form → wird in 3D-Ansicht hinzugefügt |
| **Maße ändern** (rechts) | Werte im Eigenschaften-Panel eingeben → "Aktualisieren" klicken |
| **Kante abrunden** | Fillet-Radius eingeben → "Fillet anwenden" klicken |
| **Fase** | Chamfer-Größe eingeben → "Chamfer anwenden" klicken |
| **Zwei Objekte verbinden** | Beide in Objektliste auswählen → Vereinigung/Subtraktion/Schnitt |
| **STL exportieren** | Datei → Als STL exportieren (für Slicer-Software) |
| **OpenSCAD** | Tab "OpenSCAD Editor" → Code schreiben → "Vorschau" klicken |
| **3D-Ansicht** | Maus-Rad: Zoom, Rechtsklick+Ziehen: Drehen, Mitteltaste: Pan |
| **Rückgängig** | Strg+Z |
| **Wiederholen** | Strg+Y |

---

## 🇬🇧 English

### Features
- **15 shape types**: Box, Sphere, Cylinder, Cone, Torus, Prism, Pyramid, Tube, Ellipsoid, Hemisphere, L-Profile, T-Profile, Star, Polygon, Threaded Cylinder
- All parameters freely adjustable in **mm**
- **Round edges** (Fillet) and **Chamfer**
- **Boolean operations**: Union, Subtract, Intersect
- **Import and export STL** (open directly in slicer like PrusaSlicer)
- **OpenSCAD Editor** with live preview
- **Undo / Redo**
- **Bilingual**: German / English

### System Requirements

| Component | Version | Link |
|---|---|---|
| .NET Runtime | 8.0 or newer | https://dotnet.microsoft.com/download |
| Python | 3.10 or newer | https://python.org |
| CadQuery | 2.3+ | via pip |
| OpenSCAD | any (optional) | https://openscad.org |

### Installation (Windows)

**Step 1 – Install Python**
```
https://python.org → Download → Python 3.11 (or newer) → install
Important: Check "Add Python to PATH"!
```

**Step 2 – Install CadQuery**
```cmd
pip install cadquery
```
> Note: CadQuery has many dependencies — installation takes approx. 2–5 minutes.

**Step 3 – Install .NET 8 Runtime** (if not already installed)
```
https://dotnet.microsoft.com/download/dotnet/8.0
Download ".NET Runtime" and install it
```

**Step 4 – Install OpenSCAD** (optional, for SCAD editor only)
```
https://openscad.org/downloads.html
Download Windows installer and install
```

**Step 5 – Build and run**
```powershell
# In the project folder (PowerShell):
.\build.ps1

# Start the application:
.\dist\app\3D-Builder.exe
```

### How to use

| Action | Description |
|---|---|
| **Select shape** (left panel) | Click a shape → it appears in the 3D view |
| **Change dimensions** (right panel) | Enter values → click "Update" |
| **Round edges** | Enter fillet radius → click "Apply Fillet" |
| **Chamfer** | Enter chamfer size → click "Apply Chamfer" |
| **Combine two objects** | Select both in object list → Union/Subtract/Intersect |
| **Export STL** | File → Export as STL (for slicer software) |
| **OpenSCAD** | Tab "OpenSCAD Editor" → write code → click "Preview" |
| **3D view** | Scroll: zoom, Right-click+drag: rotate, Middle button: pan |
| **Undo** | Ctrl+Z |
| **Redo** | Ctrl+Y |

### Installation (Linux / macOS)

```bash
# Python + CadQuery
pip install cadquery

# .NET 8 SDK: https://learn.microsoft.com/dotnet/core/install/linux

# Build
bash build.sh

# Run
./dist/app/3D-Builder
```

---

## Technology Stack

| Layer | Technology |
|---|---|
| **GUI / UI** | C# WPF + HelixToolkit.WPF |
| **3D View** | HelixToolkit (OpenGL via WPF) |
| **Geometry Engine** | Python + CadQuery (OpenCASCADE) |
| **OpenSCAD Integration** | OpenSCAD CLI |
| **IPC Communication** | JSON via stdin/stdout |
| **Packaging** | dotnet publish + Python |

## Project Structure

```
3D-Builder-for-Printer/
├── CSharpUI/                  # C# WPF Application (UI)
│   ├── ThreeDBuilder.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── Views/                 # XAML UI panels
│   ├── ViewModels/            # MVVM ViewModel logic
│   ├── Models/                # Data models
│   └── Services/              # PythonBridge, Translations, Commands
├── PythonBackend/             # Python geometry server
│   ├── server.py              # JSON IPC server
│   ├── shapes.py              # 15 shape generators (CadQuery)
│   ├── operations.py          # Fillet/Chamfer/Boolean ops
│   └── scad_bridge.py         # OpenSCAD CLI integration
├── build.ps1                  # Windows build script
├── build.sh                   # Linux/macOS build script
└── README.md
```
