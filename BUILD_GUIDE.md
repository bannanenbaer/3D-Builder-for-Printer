# 🔨 3D Builder Pro - Build & Installer Anleitung

**Vollständige Schritt-für-Schritt Anleitung zum Erstellen des Installers unter Windows**

---

## 📋 Voraussetzungen

Bevor du startest, stelle sicher, dass du folgendes installiert hast:

### 1. Visual Studio 2022 (oder neuer)
- **Download**: https://visualstudio.microsoft.com/de/downloads/
- **Version**: Community (kostenlos) oder Professional
- **Workloads** (während Installation):
  - ✅ .NET Desktop Development
  - ✅ Desktop Development with C++

### 2. .NET 8 SDK
- **Download**: https://dotnet.microsoft.com/en-us/download/dotnet/8.0
- **Version**: .NET 8.0 SDK (nicht nur Runtime!)
- **Verify**: Öffne PowerShell und gib ein:
  ```powershell
  dotnet --version
  ```
  Sollte `8.0.x` anzeigen

### 3. WiX Toolset 3.14 (oder neuer)
- **Download**: https://github.com/wixtoolset/wix3/releases
- **Datei**: `wix314.exe` (oder neuere Version)
- **Installation**: Einfach ausführen und Standard-Optionen wählen

### 4. Visual Studio Extension für WiX
- Öffne **Visual Studio**
- Gehe zu: **Extensions** → **Manage Extensions**
- Suche nach: **"WiX Toolset"**
- Klick **Download** (dann Visual Studio neu starten)

### 5. Git (optional, aber empfohlen)
- **Download**: https://git-scm.com/download/win
- Für Versionskontrolle und Commits

---

## ✅ Installations-Checkliste

Führe diese Befehle in **PowerShell** (als Admin) aus:

```powershell
# Prüfe .NET Version
dotnet --version

# Sollte anzeigen: 8.0.x oder höher
```

```powershell
# Prüfe Visual Studio Installation
"C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\devenv.exe" -?

# Sollte Visual Studio Hilfe anzeigen
```

```powershell
# Prüfe WiX Installation
wix --version

# Sollte die WiX Version anzeigen
```

**Wenn alle drei Befehle funktionieren → Du bist bereit! ✅**

---

## 🚀 Schritt 1: Repository klonen/öffnen

### Option A: Mit Git (empfohlen)

```powershell
# Öffne PowerShell im gewünschten Verzeichnis
cd C:\Users\[DeinBenutzername]\Documents

# Klone das Repository
git clone https://github.com/bannanenbaer/3D-Builder-for-Printer.git

# Wechsle ins Verzeichnis
cd 3D-Builder-for-Printer
```

### Option B: Manuell herunterladen

1. Öffne: https://github.com/bannanenbaer/3D-Builder-for-Printer
2. Klick **Code** → **Download ZIP**
3. Entpacke die ZIP-Datei
4. Öffne PowerShell im entpackten Ordner

---

## 🔨 Schritt 2: Projekt in Visual Studio öffnen

1. **Visual Studio starten**
2. **File** → **Open** → **Project/Solution**
3. Navigiere zu: `3D-Builder-for-Printer\CSharpUI\CSharpUI.csproj`
4. Klick **Open**
5. Warte, bis das Projekt geladen ist (2-3 Minuten)

**Du solltest jetzt die Projektstruktur im Solution Explorer sehen:**
```
3D-Builder-for-Printer
├── CSharpUI/
│   ├── Views/
│   ├── ViewModels/
│   ├── Services/
│   └── CSharpUI.csproj
├── Installer/
│   ├── Product.wxs
│   └── Installer.wixproj
└── PythonBackend/
```

---

## 🏗️ Schritt 3: Projekt kompilieren

### Option A: In Visual Studio (empfohlen für erste Mal)

1. **Solution Explorer** öffnen (rechts)
2. Rechtsklick auf **CSharpUI** → **Build**
3. Warte, bis der Build abgeschlossen ist
4. **Output** sollte zeigen: `Build succeeded`

### Option B: Mit PowerShell

```powershell
# Im Projekt-Verzeichnis
cd C:\Users\[DeinBenutzername]\Documents\3D-Builder-for-Printer

# Restore Abhängigkeiten
dotnet restore CSharpUI/CSharpUI.csproj

# Kompiliere im Release-Modus
dotnet build CSharpUI/CSharpUI.csproj -c Release
```

**Erwartete Ausgabe:**
```
Build started...
...
Build succeeded.
```

---

## 📦 Schritt 4: Installer kompilieren

### Option A: In Visual Studio

1. **Solution Explorer** öffnen
2. Rechtsklick auf **Installer** → **Build**
3. Warte auf Completion
4. **Output** sollte zeigen: `Build succeeded`

### Option B: Mit PowerShell

```powershell
# Im Projekt-Verzeichnis
cd C:\Users\[DeinBenutzername]\Documents\3D-Builder-for-Printer

# Kompiliere den Installer
dotnet build Installer/Installer.wixproj -c Release
```

**Erwartete Ausgabe:**
```
Building project "Installer.wixproj"...
...
Build succeeded.
```

---

## 🎯 Schritt 5: Installer-Datei finden

Nach erfolgreichem Build sollte die `.msi` Datei hier sein:

```
C:\Users\[DeinBenutzername]\Documents\3D-Builder-for-Printer\Installer\bin\Release\3DBuilderPro.msi
```

**Prüfe, ob die Datei existiert:**

```powershell
# Navigiere zum Output-Verzeichnis
cd "C:\Users\[DeinBenutzername]\Documents\3D-Builder-for-Printer\Installer\bin\Release"

# Liste die Dateien auf
ls -la

# Du solltest sehen:
# - 3DBuilderPro.msi (die Installer-Datei)
# - Weitere .wixpdb Dateien
```

**Dateigröße prüfen:**
```powershell
(Get-Item "3DBuilderPro.msi").Length / 1MB

# Sollte zwischen 50-200 MB sein
```

---

## 🧪 Schritt 6: Installer testen

### Test 1: Installer starten

```powershell
# Im Release-Verzeichnis
.\3DBuilderPro.msi
```

**Was sollte passieren:**
1. Windows Installer öffnet sich
2. "3D Builder Pro" Willkommensbildschirm
3. Lizenzvereinbarung
4. Installationsoptionen
5. "Installieren" Button

### Test 2: Installation durchführen

1. Klick **Weiter** durch alle Bildschirme
2. Wähle Installationsort (Standard OK)
3. Wähle Komponenten:
   - ✅ Hauptanwendung
   - ✅ Python Backend
   - ✅ Dokumentation
4. Klick **Installieren**
5. Warte auf Completion (2-3 Minuten)
6. Klick **Fertig**

### Test 3: Anwendung starten

```powershell
# Starte die installierte Anwendung
& "C:\Program Files\3D Builder Pro\3DBuilderPro.exe"
```

**Was sollte passieren:**
1. Anwendung startet
2. 3D-Viewer wird angezeigt
3. Keine Fehler in der Console
4. Alle Panels sind sichtbar

### Test 4: Deinstallation testen

```powershell
# Öffne Systemsteuerung
control appwiz.cpl

# Oder: Settings → Apps → Apps & Features
# Suche nach "3D Builder Pro"
# Klick "Uninstall"
# Bestätige Deinstallation
```

**Was sollte passieren:**
1. Anwendung wird entfernt
2. Alle Dateien gelöscht
3. Registry-Einträge entfernt
4. Desktop-Shortcut weg

---

## 📤 Schritt 7: Installer verteilen

### Option A: GitHub Releases

1. Öffne: https://github.com/bannanenbaer/3D-Builder-for-Printer
2. Gehe zu: **Releases**
3. Klick **Create a new release**
4. **Tag version**: `v1.0.0` (oder neuere Version)
5. **Release title**: `3D Builder Pro v1.0.0`
6. **Description**: 
   ```
   ## Features
   - KI-gestützter Assistent
   - AutoFix-Optimierung
   - STL-Export
   - OpenSCAD-Editor
   
   ## Installation
   Lade 3DBuilderPro.msi herunter und führe aus.
   ```
7. **Attach files**: Ziehe `3DBuilderPro.msi` hierher
8. Klick **Publish release**

### Option B: Manuell hochladen

1. Kopiere `3DBuilderPro.msi` auf einen USB-Stick
2. Oder: Lade auf einen Cloud-Service hoch (Google Drive, OneDrive, etc.)
3. Teile den Download-Link

---

## 🐛 Häufige Fehler & Lösungen

### Fehler 1: "WiX Toolset nicht gefunden"

**Lösung:**
```powershell
# Installiere WiX Toolset neu
# Download: https://github.com/wixtoolset/wix3/releases

# Oder: Installiere via Chocolatey
choco install wixtoolset
```

### Fehler 2: ".NET 8 SDK nicht gefunden"

**Lösung:**
```powershell
# Installiere .NET 8 SDK
# Download: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

# Prüfe Installation
dotnet --version
```

### Fehler 3: "Visual Studio Build Tools nicht gefunden"

**Lösung:**
```powershell
# Installiere Visual Studio Build Tools
# Download: https://visualstudio.microsoft.com/downloads/
# Wähle: "Desktop Development with C++"
```

### Fehler 4: "Installer ist zu groß"

**Lösung:**
- Prüfe, ob alle Dateien notwendig sind
- Komprimiere Python-Abhängigkeiten
- Entferne Debug-Symbole im Release-Build

### Fehler 5: "Installer startet nicht"

**Lösung:**
```powershell
# Führe mit Admin-Rechten aus
Start-Process "3DBuilderPro.msi" -Verb RunAs
```

---

## 📊 Build-Prozess Übersicht

```
┌─────────────────────────────────────┐
│ 1. Voraussetzungen prüfen           │
│    (Visual Studio, .NET 8, WiX)     │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ 2. Repository klonen/öffnen         │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ 3. Visual Studio öffnen             │
│    (CSharpUI.csproj laden)          │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ 4. Projekt kompilieren (Release)    │
│    (dotnet build -c Release)        │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ 5. Installer kompilieren            │
│    (WiX Toolset)                    │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ 6. 3DBuilderPro.msi erstellt        │
│    (im bin/Release Ordner)          │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ 7. Installer testen                 │
│    (Installation & Deinstallation)  │
└────────────┬────────────────────────┘
             │
             ▼
┌─────────────────────────────────────┐
│ 8. Auf GitHub Releases hochladen    │
│    (oder verteilen)                 │
└─────────────────────────────────────┘
```

---

## ⚡ Schnell-Befehle (PowerShell)

```powershell
# Alles in einem Schritt bauen
cd C:\Users\[DeinBenutzername]\Documents\3D-Builder-for-Printer
dotnet restore CSharpUI/CSharpUI.csproj
dotnet build CSharpUI/CSharpUI.csproj -c Release
dotnet build Installer/Installer.wixproj -c Release

# Installer testen
.\Installer\bin\Release\3DBuilderPro.msi

# Installer-Verzeichnis öffnen
explorer .\Installer\bin\Release\
```

---

## 📞 Support

Falls du Probleme hast:

1. **Prüfe die Voraussetzungen** (Visual Studio, .NET 8, WiX)
2. **Schau die Fehler-Lösungen** oben an
3. **Öffne ein GitHub Issue**: https://github.com/bannanenbaer/3D-Builder-for-Printer/issues
4. **Kontaktiere den Support**

---

## 🎉 Fertig!

Wenn alles funktioniert hat, hast du einen funktionsfähigen Installer erstellt! 🚀

**Nächste Schritte:**
1. Teste den Installer auf verschiedenen Windows-Versionen
2. Lade ihn auf GitHub Releases hoch
3. Teile den Download-Link mit Benutzern
4. Sammle Feedback und Verbesserungsvorschläge

---

**Viel Erfolg beim Build! 💪**

*Letzte Aktualisierung: April 2026*
