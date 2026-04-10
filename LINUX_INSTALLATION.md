# 3D Builder for Printer – Linux Installationsanleitung

> **Linux-Edition** – vollständig in Python (PyQt5) neu geschrieben.  
> Kein .NET / WPF / Windows erforderlich.

---

## Inhaltsverzeichnis

1. [Systemvoraussetzungen](#1-systemvoraussetzungen)
2. [Schnellstart (empfohlen)](#2-schnellstart)
3. [Manuelle Installation](#3-manuelle-installation)
4. [CadQuery installieren](#4-cadquery-installieren)
5. [OpenSCAD installieren (optional)](#5-openscad-installieren-optional)
6. [Claude AI-Assistent einrichten (optional)](#6-claude-ai-assistent-optional)
7. [Anwendung starten](#7-anwendung-starten)
8. [Desktop-Verknüpfung erstellen](#8-desktop-verknüpfung)
9. [Standalone-Binary erstellen](#9-standalone-binary)
10. [Fehlerbehebung](#10-fehlerbehebung)
11. [Deinstallation](#11-deinstallation)

---

## 1. Systemvoraussetzungen

| Komponente | Mindestanforderung |
|---|---|
| **Betriebssystem** | Ubuntu 22.04 / Debian 12 / Fedora 38 / Arch Linux oder neuer |
| **Python** | 3.10 oder neuer |
| **RAM** | 4 GB (8 GB empfohlen) |
| **Festplatte** | 2 GB frei (inkl. CadQuery + Qt) |
| **Grafik** | OpenGL 2.0+ (für 3D-Viewport) |
| **Display** | X11 oder XWayland |

---

## 2. Schnellstart

```bash
# 1. Repository klonen
git clone https://github.com/bannanenbaer/3d-builder-for-printer.git
cd 3d-builder-for-printer

# 2. Build-Skript ausführen (installiert alle Abhängigkeiten)
chmod +x build.sh
./build.sh

# 3. Anwendung starten
source .venv/bin/activate
python main.py
```

Das war's. Das Skript legt automatisch ein virtuelles Python-Environment unter `.venv/` an.

---

## 3. Manuelle Installation

### 3.1 System-Pakete (Ubuntu / Debian)

```bash
sudo apt update
sudo apt install -y \
    python3 python3-pip python3-venv \
    python3-pyqt5 python3-opengl \
    libgl1-mesa-glx libglib2.0-0 libxrender1 \
    libxkbcommon-x11-0 libxcb-icccm4 libxcb-image0 \
    libxcb-keysyms1 libxcb-randr0 libxcb-render-util0 \
    libxcb-xinerama0 libxcb-xfixes0
```

### 3.1b System-Pakete (Fedora / RHEL)

```bash
sudo dnf install -y \
    python3 python3-pip \
    python3-qt5 mesa-libGL mesa-libEGL \
    libxkbcommon-x11
```

### 3.1c System-Pakete (Arch Linux)

```bash
sudo pacman -S --needed \
    python python-pip \
    python-pyqt5 mesa \
    libxkbcommon-x11
```

### 3.2 Virtuelles Environment anlegen

```bash
cd /pfad/zum/3d-builder-for-printer
python3 -m venv .venv
source .venv/bin/activate
pip install --upgrade pip
```

### 3.3 Python-Abhängigkeiten installieren

```bash
pip install -r requirements.txt
```

**Installierte Pakete:**

| Paket | Zweck |
|---|---|
| `PyQt5` | GUI-Framework |
| `pyvista` | 3D-Mesh-Bibliothek |
| `pyvistaqt` | Qt-Integration für PyVista |
| `matplotlib` | Fallback-3D-Renderer |
| `numpy` | Numerische Berechnungen |
| `cadquery` | CAD-Geometrie-Engine (OpenCASCADE) |

---

## 4. CadQuery installieren

CadQuery ist die primäre Geometrie-Engine. Die Installation kann über `pip` oder `conda` erfolgen.

### Option A: pip (Standard)

```bash
pip install cadquery
```

> Hinweis: CadQuery enthält vorkompilierte OpenCASCADE-Binärdateien (~300 MB).
> Die Installation kann einige Minuten dauern.

### Option B: conda (empfohlen bei Problemen)

```bash
# Miniconda installieren falls noch nicht vorhanden:
# https://docs.conda.io/en/latest/miniconda.html

conda create -n 3dbuilder python=3.11
conda activate 3dbuilder
conda install -c conda-forge cadquery
pip install PyQt5 pyvistaqt pyvista matplotlib numpy
```

### Überprüfung

```bash
python -c "import cadquery as cq; print('CadQuery OK:', cq.__version__)"
```

**Ohne CadQuery** funktioniert die App mit reduziertem Funktionsumfang:
- OpenSCAD-Fallback: alle Formen werden über OpenSCAD gerendert
- Purer Python-Fallback: einfache Box-Approximation (kein Fillet/Chamfer/Boolean)

---

## 5. OpenSCAD installieren (optional)

OpenSCAD dient als Fallback-Geometrie-Engine und ermöglicht den SCAD-Editor.

### Ubuntu / Debian

```bash
sudo apt install -y openscad
```

### Fedora

```bash
sudo dnf install -y openscad
```

### Arch Linux

```bash
sudo pacman -S openscad
```

### AppImage (neueste Version)

```bash
# Neueste Version von https://openscad.org/downloads.html herunterladen
chmod +x OpenSCAD-*.AppImage
sudo mv OpenSCAD-*.AppImage /usr/local/bin/openscad
```

### Überprüfung

```bash
openscad --version
```

---

## 6. Claude AI-Assistent (optional)

Der Brixl-Assistent kann mit der Claude API verbunden werden für intelligentere Antworten.

### 6.1 Paket installieren

```bash
pip install anthropic
```

### 6.2 API-Key einrichten

1. Anwendung starten
2. Einstellungen-Panel öffnen (rechts)
3. **Anbieter** auf `Claude API` ändern
4. **API-Key** eintragen (Format: `sk-ant-…`)
5. **Einstellungen speichern** klicken

API-Keys erhältst du unter: https://console.anthropic.com/

---

## 7. Anwendung starten

### Standardweg

```bash
cd /pfad/zum/3d-builder-for-printer
source .venv/bin/activate
python main.py
```

### Wayland-Nutzer

Falls die App unter Wayland nicht startet:

```bash
QT_QPA_PLATFORM=xcb python main.py
```

### Alias anlegen (bequemer Start)

```bash
# In ~/.bashrc oder ~/.zshrc einfügen:
alias 3dbuilder='cd /pfad/zum/3d-builder-for-printer && source .venv/bin/activate && python main.py'

# Danach:
source ~/.bashrc
3dbuilder
```

---

## 8. Desktop-Verknüpfung

```bash
# Startskript erstellen
cat > /usr/local/bin/3dbuilder << 'EOF'
#!/usr/bin/env bash
cd /pfad/zum/3d-builder-for-printer
exec .venv/bin/python main.py "$@"
EOF
chmod +x /usr/local/bin/3dbuilder

# .desktop-Datei für Anwendungsmenü
mkdir -p ~/.local/share/applications
cat > ~/.local/share/applications/3dbuilder.desktop << 'EOF'
[Desktop Entry]
Version=1.0
Type=Application
Name=3D Builder for Printer
Comment=3D-Modellierungswerkzeug für den 3D-Druck
Exec=/usr/local/bin/3dbuilder
Icon=applications-graphics
Terminal=false
Categories=Graphics;3DGraphics;Engineering;
Keywords=3D;CAD;Druck;STL;OpenSCAD;
EOF

update-desktop-database ~/.local/share/applications
```

> Ersetze `/pfad/zum/3d-builder-for-printer` mit dem tatsächlichen Pfad.

---

## 9. Standalone-Binary erstellen

Um die App ohne installiertes Python zu verteilen:

```bash
source .venv/bin/activate
./build.sh --bundle
```

Die fertige Binary liegt dann unter `dist/3dbuilder` und kann direkt ausgeführt werden:

```bash
./dist/3dbuilder
```

---

## 10. Fehlerbehebung

### Problem: `ModuleNotFoundError: No module named 'PyQt5'`

```bash
# Im venv:
pip install PyQt5

# Oder systemweit (Ubuntu):
sudo apt install python3-pyqt5
```

### Problem: `qt.qpa.plugin: Could not load the Qt platform plugin "xcb"`

```bash
sudo apt install -y \
    libxcb-icccm4 libxcb-image0 libxcb-keysyms1 \
    libxcb-randr0 libxcb-render-util0 libxcb-xinerama0 \
    libxcb-xfixes0 libxkbcommon-x11-0

# Debug-Info:
QT_DEBUG_PLUGINS=1 python main.py 2>&1 | head -40
```

### Problem: Schwarzes / leeres 3D-Viewport-Fenster

```bash
# OpenGL-Treiber prüfen:
glxinfo | grep "OpenGL version"

# Mesa-Treiber installieren (Intel/AMD):
sudo apt install libgl1-mesa-glx mesa-utils

# Für VMs / Remote-Desktops (Software-Rendering):
LIBGL_ALWAYS_SOFTWARE=1 python main.py
```

### Problem: `ImportError: libGL.so.1: cannot open shared object file`

```bash
sudo apt install libgl1-mesa-glx
# oder:
sudo apt install libgl1
```

### Problem: CadQuery-Import schlägt fehl

```bash
# Abhängigkeiten prüfen:
python -c "import cadquery" 2>&1

# Conda-Variante versuchen:
conda install -c conda-forge cadquery

# Falls OpenCASCADE fehlt:
sudo apt install libocc-occt-dev
```

### Problem: Wayland – App startet nicht

```bash
# XCB-Backend erzwingen:
QT_QPA_PLATFORM=xcb python main.py

# Permanent in ~/.bashrc:
export QT_QPA_PLATFORM=xcb
```

### Problem: Sehr langsame 3D-Darstellung

```bash
# Hardware-Beschleunigung prüfen:
glxinfo | grep "direct rendering"
# → "direct rendering: Yes" = gut

# Falls "No": GPU-Treiber installieren
# NVIDIA:
sudo apt install nvidia-driver-535
# AMD:
sudo apt install xserver-xorg-video-amdgpu
```

### Problem: Fehler bei `pyvistaqt`

```bash
# Alternative: matplotlib-Fallback nutzen (automatisch)
# pyvistaqt deinstallieren um Fallback zu erzwingen:
pip uninstall pyvistaqt pyvista

# Oder: VTK-Version korrigieren
pip install vtk==9.2.6 pyvista pyvistaqt
```

### Log-Ausgabe aktivieren

```bash
# Vollständige Fehlerausgabe:
python main.py 2>&1 | tee 3dbuilder.log
```

---

## 11. Deinstallation

```bash
# Virtuelles Environment löschen
rm -rf /pfad/zum/3d-builder-for-printer/.venv

# Desktop-Verknüpfung entfernen
rm -f ~/.local/share/applications/3dbuilder.desktop
rm -f /usr/local/bin/3dbuilder

# Einstellungen entfernen
rm -rf ~/.config/3dbuilder

# Repository löschen
rm -rf /pfad/zum/3d-builder-for-printer
```

---

## Architektur-Überblick (Linux-Edition)

```
main.py                    ← Einstiegspunkt (python main.py)
app/
├── main_window.py         ← Hauptfenster (PyQt5 QMainWindow)
├── viewport.py            ← 3D-Viewport (pyvistaqt oder matplotlib)
├── scene.py               ← Szenen-Datenmodell + Undo/Redo
├── theme.py               ← Dark/Light Theme
├── panels/
│   ├── shape_panel.py     ← Form-Erstellung (15 parametrische Formen)
│   ├── properties_panel.py← Eigenschaften + Fillet/Chamfer/Boolean
│   ├── scad_panel.py      ← OpenSCAD-Editor mit Syntax-Highlighting
│   ├── autofix_panel.py   ← Druckqualitätsanalyse & AutoFix
│   ├── assistant_panel.py ← Brixl KI-Assistent
│   └── settings_panel.py  ← Einstellungen
└── services/
    ├── geometry_service.py← Wrapper um CadQuery/OpenSCAD-Backend
    ├── ai_service.py      ← KI-Assistent (lokal + Claude API)
    └── settings_service.py← Persistente Einstellungen (JSON)
PythonBackend/
├── shapes.py              ← 15 parametrische Formen (CadQuery)
├── operations.py          ← Fillet, Chamfer, Boolean
├── scad_bridge.py         ← OpenSCAD-Integration
└── server.py              ← JSON-IPC Server (für externe Clients)
```

**Geometrie-Engine Fallback-Kette:**
1. **CadQuery** (bevorzugt) – volle Funktionalität inkl. Fillet/Chamfer
2. **OpenSCAD** – alle Formen, kein Fillet/Chamfer
3. **Purer Python** – Box-Approximation (Notfall-Fallback)

---

*Bei Problemen: Issue öffnen unter https://github.com/bannanenbaer/3d-builder-for-printer/issues*
