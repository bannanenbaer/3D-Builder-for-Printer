# 3D Builder Pro - Installationsanleitung

## Systemanforderungen

### Minimum
- **OS:** Windows 10 oder höher (64-bit)
- **RAM:** 4 GB
- **Festplatte:** 500 MB freier Speicherplatz
- **GPU:** DirectX 11 kompatibel

### Empfohlen
- **OS:** Windows 11 (64-bit)
- **RAM:** 8 GB oder mehr
- **Festplatte:** 1 GB freier Speicherplatz
- **GPU:** Moderne dedizierte Grafikkarte

## Installation

### Option 1: Installer (Empfohlen)

1. **Lade den Installer herunter**
   - Besuche https://github.com/bannanenbaer/3D-Builder-for-Printer/releases
   - Lade `3DBuilderPro.msi` herunter

2. **Führe den Installer aus**
   - Doppelklick auf `3DBuilderPro.msi`
   - Folge den Anweisungen des Installers

3. **Wähle Installationsoptionen**
   - **Hauptanwendung** (erforderlich)
   - **Python Backend** (erforderlich für 3D-Modellierung)
   - **Dokumentation** (optional)

4. **Wähle Installationsverzeichnis**
   - Standard: `C:\Program Files\3D Builder Pro`
   - Du kannst einen anderen Pfad wählen

5. **Installiere**
   - Klick "Installieren"
   - Warte auf die Fertigstellung
   - Klick "Fertig"

6. **Starte die Anwendung**
   - Desktop-Shortcut doppelklicken
   - Oder: Start-Menü → 3D Builder Pro

### Option 2: Portable Version

1. **Lade die portable Version herunter**
   - Besuche https://github.com/bannanenbaer/3D-Builder-for-Printer/releases
   - Lade `3DBuilderPro-portable.zip` herunter

2. **Entpacke die Datei**
   - Rechtsklick → Alle extrahieren
   - Wähle Zielverzeichnis

3. **Starte die Anwendung**
   - Öffne das Verzeichnis
   - Doppelklick auf `ThreeDBuilder.exe`

## Konfiguration

### Erste Schritte

1. **Python Backend prüfen**
   - Öffne Einstellungen
   - Prüfe, ob Python erkannt wird
   - Falls nicht, installiere Python 3.10+

2. **KI-Assistent konfigurieren (Optional)**
   - Öffne Einstellungen → KI-Integration
   - Gib deinen OpenAI API-Schlüssel ein
   - Klick "API-Schlüssel speichern"

3. **Sprache und Theme wählen**
   - Öffne Einstellungen
   - Wähle Sprache (Deutsch/Englisch)
   - Wähle Theme (Dark/Light)

### Python Backend Installation

Das Python Backend wird automatisch installiert. Falls nicht:

1. **Installiere Python 3.10+**
   - Besuche https://python.org
   - Lade Python 3.10 oder höher herunter
   - **Wichtig:** Aktiviere "Add Python to PATH" während der Installation

2. **Installiere CadQuery**
   ```bash
   pip install cadquery
   ```

3. **Installiere weitere Abhängigkeiten**
   ```bash
   pip install -r requirements.txt
   ```

## Deinstallation

### Windows Installer

1. **Öffne Einstellungen**
   - Windows-Taste + I
   - Gehe zu "Apps" → "Apps & Features"

2. **Finde 3D Builder Pro**
   - Suche nach "3D Builder Pro"
   - Klick auf den Eintrag

3. **Deinstalliere**
   - Klick "Deinstallieren"
   - Bestätige die Deinstallation
   - Warte auf die Fertigstellung

### Portable Version

- Lösche einfach das Verzeichnis

## Fehlerbehebung

### "Python nicht gefunden"

**Lösung:**
1. Installiere Python 3.10+ von https://python.org
2. Stelle sicher, dass "Add Python to PATH" aktiviert ist
3. Starte die Anwendung neu

### "CadQuery nicht installiert"

**Lösung:**
1. Öffne die Eingabeaufforderung
2. Führe aus: `pip install cadquery`
3. Warte auf die Installation
4. Starte die Anwendung neu

### "Anwendung startet nicht"

**Lösung:**
1. Prüfe, ob .NET 8 Runtime installiert ist
2. Lade es herunter von https://dotnet.microsoft.com/download
3. Starte die Anwendung neu

### "3D-Viewer zeigt nichts"

**Lösung:**
1. Prüfe deine GPU-Treiber
2. Aktualisiere deine Grafikkartentreiber
3. Versuche, die Anwendung im Kompatibilitätsmodus zu starten

## Support

- **Dokumentation:** Siehe ASSISTANT_GUIDE.md
- **Tutorials:** Öffne die Anwendung und frage den KI-Assistenten
- **Issues:** https://github.com/bannanenbaer/3D-Builder-for-Printer/issues
- **Diskussionen:** https://github.com/bannanenbaer/3D-Builder-for-Printer/discussions

## Aktualisierungen

### Automatische Updates

Die Anwendung prüft automatisch auf Updates. Falls eine neue Version verfügbar ist:

1. Du erhältst eine Benachrichtigung
2. Klick "Jetzt aktualisieren"
3. Die neue Version wird heruntergeladen und installiert
4. Die Anwendung startet neu

### Manuelle Updates

1. Besuche https://github.com/bannanenbaer/3D-Builder-for-Printer/releases
2. Lade die neueste Version herunter
3. Führe den Installer aus
4. Die alte Version wird automatisch aktualisiert

---

**Viel Erfolg bei der Installation! Bei Fragen, frag den KI-Assistenten! 🤖**
