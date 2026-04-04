# 🤖 3D Builder Pro - Intelligente 3D-CAD für den 3D-Druck

Eine professionelle, benutzerfreundliche Desktop-Anwendung zum Erstellen und Optimieren von 3D-Modellen für den 3D-Druck - mit KI-gestütztem Assistenten und automatischer Optimierung.

**A professional desktop application for creating and optimizing 3D models for 3D printing - with AI-powered assistant and automatic optimization.**

---

## 🇩🇪 Deutsch

### ✨ Features

**3D-Modellierung:**
- 🎨 **15 parametrische Grundformen**: Quader, Kugel, Zylinder, Kegel, Torus, Prisma, Pyramide, Rohr, Ellipsoid, Halbkugel, L-Profil, T-Profil, Sternform, Polygon, Gewinde-Zylinder
- 📐 Alle Parameter frei in **mm** einstellbar
- 🔧 **Kanten abrunden** (Fillet) und **Fasen** (Chamfer)
- ⚙️ **Boolean-Operationen**: Vereinigung, Subtraktion, Schnitt
- 📝 **OpenSCAD Editor** mit Live-Vorschau

**Intelligente Optimierung:**
- 🤖 **KI-Assistent** mit süßem animiertem Maskottchen
- 🎯 **AutoFix-Funktion**: Automatische Modell-Optimierung für perfekte Druckergebnisse
- 🔍 **Intelligente Analyse**: Erkennt Druckprobleme (scharfe Kanten, dünne Wände, Löcher)
- 🎨 **Pinsel-Animation**: Der Assistent markiert Fehler mit Farbkodierung
- 🔨 **Hammer-Animation**: Repariert Probleme automatisch
- 📦 **Klebeband-Animation**: Fixiert und optimiert die Geometrie

**Benutzerfreundlichkeit:**
- 💾 **Undo / Redo** für alle Operationen (mit Vor/Zurück-Buttons)
- 🖨️ **STL importieren und exportieren** (direkt in Slicer wie PrusaSlicer öffnen)
- 🌍 **Zweisprachig**: Deutsch / English
- 🎨 **Elegantes dunkles Design** mit modernem UI
- 💡 **Automatische Tipps und Tutorials** vom Assistenten

---

## 📦 Installation (Windows - Empfohlen)

### ⚡ Schnellstart mit Installer

Die einfachste Methode! Der Installer kümmert sich um alles.

**Schritt 1: Installer herunterladen**
```
Besuche: https://github.com/bannanenbaer/3D-Builder-for-Printer/releases
Lade "3DBuilderPro.msi" herunter
```

**Schritt 2: Installer ausführen**
- Doppelklick auf `3DBuilderPro.msi`
- Folge den Anweisungen des Installers
- Wähle die Komponenten aus:
  - ✅ **Hauptanwendung** (erforderlich)
  - ✅ **Python Backend** (erforderlich)
  - ✅ **Dokumentation** (optional)

**Schritt 3: Anwendung starten**
- Desktop-Shortcut doppelklicken
- Oder: Start-Menü → 3D Builder Pro

**Das war's! 🎉**

---

### 🔧 Manuelle Installation (für Entwickler)

Falls du den Installer nicht verwenden möchtest oder das Projekt selbst kompilieren willst:

**Voraussetzungen:**
| Komponente | Version | Link |
|---|---|---|
| .NET SDK | 8.0 oder neuer | https://dotnet.microsoft.com/download |
| Python | 3.10 oder neuer | https://python.org |
| CadQuery | 2.3+ | via pip |
| WiX Toolset | 3.11 (optional) | https://wixtoolset.org |

**Installation:**

1. **Python installieren**
   ```
   https://python.org → Download → Python 3.11+ → installieren
   ⚠️ Wichtig: "Add Python to PATH" aktivieren!
   ```

2. **CadQuery installieren**
   ```cmd
   pip install cadquery
   ```
   > Hinweis: Installation dauert ca. 2–5 Minuten (viele Abhängigkeiten)

3. **.NET 8 SDK installieren**
   ```
   https://dotnet.microsoft.com/download/dotnet/8.0
   → ".NET SDK" herunterladen und installieren
   ```

4. **Projekt kompilieren**
   ```powershell
   # Im Projektordner (PowerShell):
   .\build.ps1
   
   # Anwendung starten:
   .\dist\app\ThreeDBuilder.exe
   ```

5. **Installer erstellen (optional)**
   ```powershell
   # WiX Toolset muss installiert sein
   .\build.ps1
   # → Erstellt 3DBuilderPro.msi
   ```

---

## 🎯 Erste Schritte

### 1. Objekt erstellen
- Wähle eine Form aus dem **Shape-Panel** (links)
- Gib die Maße in **mm** ein
- Die Form erscheint in der **3D-Ansicht**

### 2. Objekt optimieren
- Klick auf **"Modell analysieren"** im AutoFix-Panel
- Der Assistent prüft auf Druckprobleme
- Klick **"AutoFix starten!"** für automatische Optimierung
- Beobachte, wie das Maskottchen dein Modell mit Pinsel, Hammer und Klebeband optimiert! 🤖

### 3. Weitere Bearbeitung
- **Fillet/Chamfer**: Kanten abrunden/abfasen
- **Boolean-Operationen**: Objekte kombinieren
- **OpenSCAD Editor**: Komplexe Formen mit Code erstellen

### 4. Exportieren
- **STL exportieren**: Datei → Exportieren
- Öffne direkt in PrusaSlicer oder deinem Slicer

---

## 🎮 Bedienung

| Aktion | Beschreibung |
|---|---|
| **Form auswählen** | Klick auf Shape im linken Panel |
| **Maße ändern** | Werte im Eigenschaften-Panel eingeben |
| **AutoFix starten** | AutoFix-Panel → "AutoFix starten!" |
| **Zurück/Vorwärts** | Undo/Redo Buttons im AutoFix-Panel |
| **Objekt auswählen** | Dropdown im AutoFix-Panel für einzelne Objekte |
| **Fillet anwenden** | Radius eingeben → "Fillet anwenden" |
| **Chamfer anwenden** | Größe eingeben → "Chamfer anwenden" |
| **Boolean-Op** | Zwei Objekte auswählen → Union/Subtract/Intersect |
| **STL exportieren** | Datei → Exportieren |
| **3D-Ansicht** | Rad: Zoom, Rechtsklick+Ziehen: Drehen, Mitteltaste: Pan |
| **Assistent fragen** | Klick auf Assistenten-Panel für Tipps |

---

## 🤖 KI-Assistent

Der süße Assistent hilft dir bei jedem Schritt:

### Features:
- 💬 **Fragen beantworten**: Frag den Assistenten nach Features
- 📚 **Tutorials**: Automatische Tipps für Anfänger
- 🎨 **Formen generieren**: Beschreibe eine Form, der Assistent erstellt sie
- 💡 **Automatische Vorschläge**: Tipps basierend auf deinen Aktionen

### Konfiguration:
- **Einstellungen** → **3D-Assistent**
- Aktiviere/Deaktiviere den Assistenten
- Konfiguriere OpenAI API (optional, für erweiterte Features)

---

## 🔧 AutoFix - Automatische Optimierung

Die AutoFix-Funktion optimiert dein Modell automatisch für perfekte Druckergebnisse:

### Was AutoFix macht:
1. 🔍 **Analysiert** das Modell auf Druckprobleme
2. 🎨 **Markiert** Fehler mit Farbkodierung (Pinsel-Animation)
3. 🔨 **Repariert** Probleme (Hammer-Animation)
4. 📦 **Fixiert** die Geometrie (Klebeband-Animation)

### Erkannte Probleme:
- ⚠️ Scharfe Kanten → werden abgerundet (Fillet)
- ⚠️ Dünne Wände → werden verdickt
- ⚠️ Kleine Löcher → werden gefüllt
- ⚠️ Nicht-manifold Geometrie → wird repariert

### Drucker-Profile:
- 🖨️ **Prusa i3 MK3S+** (voreingestellt)
- 🖨️ **Creality Ender 3 V2**
- 🖨️ **Bambu Lab X1**

---

## ⚙️ Systemvoraussetzungen

### Minimum:
- **OS**: Windows 10 (64-bit)
- **RAM**: 4 GB
- **Festplatte**: 500 MB freier Speicherplatz
- **GPU**: DirectX 11 kompatibel

### Empfohlen:
- **OS**: Windows 11 (64-bit)
- **RAM**: 8 GB oder mehr
- **Festplatte**: 1 GB freier Speicherplatz
- **GPU**: Moderne dedizierte Grafikkarte

---

## 🐛 Fehlerbehebung

### "Python nicht gefunden"
```
→ Installiere Python 3.10+ von https://python.org
→ Aktiviere "Add Python to PATH" während der Installation
→ Starte die Anwendung neu
```

### "CadQuery nicht installiert"
```cmd
pip install cadquery
```

### "3D-Viewer zeigt nichts"
```
→ Aktualisiere deine GPU-Treiber
→ Starte die Anwendung neu
```

### "Assistent antwortet nicht"
```
→ Prüfe deine Internetverbindung
→ Prüfe deinen OpenAI API-Schlüssel (falls konfiguriert)
→ Starte die Anwendung neu
```

---

## 📚 Dokumentation

- **INSTALLATION.md** - Detaillierte Installationsanleitung
- **ASSISTANT_GUIDE.md** - Benutzerhandbuch für den KI-Assistenten
- **GitHub Issues** - Für Bugs und Feature-Requests

---

## 🇬🇧 English

### ✨ Features

**3D Modeling:**
- 🎨 **15 parametric shapes**: Box, Sphere, Cylinder, Cone, Torus, Prism, Pyramid, Tube, Ellipsoid, Hemisphere, L-Profile, T-Profile, Star, Polygon, Threaded Cylinder
- 📐 All parameters freely adjustable in **mm**
- 🔧 **Round edges** (Fillet) and **Chamfer**
- ⚙️ **Boolean operations**: Union, Subtract, Intersect
- 📝 **OpenSCAD Editor** with live preview

**Intelligent Optimization:**
- 🤖 **AI Assistant** with cute animated mascot
- 🎯 **AutoFix function**: Automatic model optimization for perfect print results
- 🔍 **Smart analysis**: Detects print problems (sharp edges, thin walls, holes)
- 🎨 **Brush animation**: Assistant marks errors with color coding
- 🔨 **Hammer animation**: Repairs problems automatically
- 📦 **Tape animation**: Fixes and optimizes geometry

**User-Friendly:**
- 💾 **Undo / Redo** for all operations (with Back/Forward buttons)
- 🖨️ **Import and export STL** (open directly in slicers like PrusaSlicer)
- 🌍 **Bilingual**: German / English
- 🎨 **Elegant dark design** with modern UI
- 💡 **Automatic tips and tutorials** from the assistant

---

### 📦 Installation (Windows - Recommended)

#### ⚡ Quick Start with Installer

The easiest way! The installer handles everything.

**Step 1: Download installer**
```
Visit: https://github.com/bannanenbaer/3D-Builder-for-Printer/releases
Download "3DBuilderPro.msi"
```

**Step 2: Run installer**
- Double-click `3DBuilderPro.msi`
- Follow the installer instructions
- Select components:
  - ✅ **Main Application** (required)
  - ✅ **Python Backend** (required)
  - ✅ **Documentation** (optional)

**Step 3: Start application**
- Double-click desktop shortcut
- Or: Start Menu → 3D Builder Pro

**That's it! 🎉**

---

### 🔧 Manual Installation (for Developers)

**Prerequisites:**
| Component | Version | Link |
|---|---|---|
| .NET SDK | 8.0 or newer | https://dotnet.microsoft.com/download |
| Python | 3.10 or newer | https://python.org |
| CadQuery | 2.3+ | via pip |

**Installation:**

1. **Install Python**
   ```
   https://python.org → Download → Python 3.11+ → install
   ⚠️ Important: Enable "Add Python to PATH"!
   ```

2. **Install CadQuery**
   ```cmd
   pip install cadquery
   ```

3. **Install .NET 8 SDK**
   ```
   https://dotnet.microsoft.com/download/dotnet/8.0
   → Download ".NET SDK" and install
   ```

4. **Build project**
   ```powershell
   .\build.ps1
   .\dist\app\ThreeDBuilder.exe
   ```

---

### 🎮 How to Use

| Action | Description |
|---|---|
| **Create shape** | Click shape in left panel |
| **Change dimensions** | Enter values in properties panel |
| **Start AutoFix** | AutoFix panel → "Start AutoFix!" |
| **Undo/Redo** | Use Undo/Redo buttons |
| **Select object** | Dropdown in AutoFix panel |
| **Apply Fillet** | Enter radius → "Apply Fillet" |
| **Apply Chamfer** | Enter size → "Apply Chamfer" |
| **Boolean operation** | Select two objects → Union/Subtract/Intersect |
| **Export STL** | File → Export |
| **3D view** | Scroll: zoom, Right-click+drag: rotate, Middle: pan |
| **Ask assistant** | Click assistant panel for tips |

---

## 🛠️ Technology Stack

| Layer | Technology |
|---|---|
| **GUI / UI** | C# WPF + Material Design |
| **3D View** | HelixToolkit (OpenGL via WPF) |
| **Geometry Engine** | Python + CadQuery (OpenCASCADE) |
| **AI Assistant** | OpenAI API (optional) |
| **Animations** | WPF Storyboards |
| **Packaging** | WiX Installer |

---

## 📁 Project Structure

```
3D-Builder-for-Printer/
├── CSharpUI/                    # C# WPF Application
│   ├── Views/                   # XAML UI components
│   ├── ViewModels/              # MVVM logic
│   ├── Services/                # AI, AutoFix, Undo/Redo
│   └── ThreeDBuilder.csproj
├── PythonBackend/               # Python geometry server
│   ├── server.py                # JSON IPC server
│   ├── shapes.py                # 15 shape generators
│   └── operations.py            # Fillet/Chamfer/Boolean
├── Installer/                   # WiX installer
│   ├── Product.wxs
│   └── Installer.wixproj
├── build.ps1                    # Build script
└── README.md
```

---

## 📝 License

MIT License - Feel free to use and modify!

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

---

## 📧 Support

- 📚 **Documentation**: See INSTALLATION.md and ASSISTANT_GUIDE.md
- 🐛 **Bug Reports**: GitHub Issues
- 💬 **Discussions**: GitHub Discussions
- 🤖 **Ask the Assistant**: Use the built-in AI assistant for help

---

**Made with ❤️ for 3D printing enthusiasts**

🚀 **Happy printing!**
