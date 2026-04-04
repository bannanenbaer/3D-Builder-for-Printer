# 📖 3D Builder Pro - Installationshandbuch

**Vollständige Anleitung für Anfänger und Fortgeschrittene**

---

## 🚀 Schnellstart (Windows) - 5 Minuten

### Schritt 1: Installer herunterladen
1. Öffne: https://github.com/bannanenbaer/3D-Builder-for-Printer/releases
2. Suche nach **"3DBuilderPro.msi"**
3. Klick auf "Download"
4. Warte, bis der Download abgeschlossen ist

### Schritt 2: Installer ausführen
1. Öffne deinen **Downloads-Ordner**
2. Doppelklick auf **"3DBuilderPro.msi"**
3. Windows zeigt eine Sicherheitsfrage → Klick **"Ja"** oder **"Ausführen"**

### Schritt 3: Installationsassistent folgen
1. Willkommensbildschirm → Klick **"Weiter"**
2. Lizenzvereinbarung → Häkchen setzen → Klick **"Weiter"**
3. Installationsort (Standard ist OK) → Klick **"Weiter"**
4. Komponenten auswählen:
   - ✅ **Hauptanwendung** (erforderlich)
   - ✅ **Python Backend** (erforderlich)
   - ✅ **Dokumentation** (optional)
5. Klick **"Installieren"**
6. Warte, bis die Installation abgeschlossen ist (2-3 Minuten)
7. Klick **"Fertig"**

### Schritt 4: Anwendung starten
- **Option A**: Desktop-Shortcut doppelklicken
- **Option B**: Start-Menü → "3D Builder Pro"
- **Option C**: Suche nach "3D Builder Pro"

**Fertig! 🎉 Die Anwendung startet jetzt.**

---

## ⚙️ Systemvoraussetzungen

### Minimum:
- **Windows 10** (64-bit) oder neuer
- **4 GB RAM**
- **500 MB** freier Speicherplatz
- **Internetverbindung** (für erste Installation)

### Empfohlen:
- **Windows 11** (64-bit)
- **8 GB RAM** oder mehr
- **1 GB** freier Speicherplatz
- **Moderne GPU** (NVIDIA/AMD)

### Nicht unterstützt:
- ❌ Windows 7 oder älter
- ❌ 32-bit Windows
- ❌ macOS (nur Windows)
- ❌ Linux (nur Windows)

---

## 🔧 Was der Installer installiert

Der Installer installiert automatisch:

1. **3D Builder Pro Anwendung**
   - C# WPF Desktop-Anwendung
   - 3D-Viewer und Editor
   - Benutzeroberfläche

2. **Python Backend**
   - Python 3.11 Runtime
   - CadQuery Bibliothek
   - Geometry Engine
   - OpenSCAD Integration

3. **Dokumentation** (optional)
   - Benutzerhandbuch
   - Tutorials
   - Beispiele

4. **Shortcuts & Registry**
   - Desktop-Shortcut
   - Start-Menü-Einträge
   - Datei-Zuordnungen (.stl)

---

## 📋 Schritt-für-Schritt Anleitung mit Screenshots

### Phase 1: Download

```
1. Browser öffnen → GitHub releases Seite
2. "3DBuilderPro.msi" suchen
3. "Download" klicken
4. Datei speichern
```

### Phase 2: Installation

```
1. Downloads-Ordner öffnen
2. "3DBuilderPro.msi" doppelklicken
3. Sicherheitsfrage → "Ja" klicken
4. Installationsassistent folgen
5. Komponenten auswählen (Standard OK)
6. "Installieren" klicken
7. Warten (2-3 Minuten)
8. "Fertig" klicken
```

### Phase 3: Erste Verwendung

```
1. Desktop-Shortcut doppelklicken
2. Anwendung startet (erste Initialisierung)
3. Willkommensbildschirm
4. Erste Form erstellen
5. AutoFix ausprobieren
```

---

## 🐛 Häufige Probleme & Lösungen

### Problem 1: "Windows hat das Programm blockiert"

**Lösung:**
1. Klick auf **"Weitere Informationen"**
2. Klick auf **"Trotzdem ausführen"**
3. Installation startet

### Problem 2: "Python nicht gefunden"

**Lösung:**
1. Deinstalliere die Anwendung
2. Installiere Python 3.11+ manuell von https://python.org
3. Aktiviere **"Add Python to PATH"**
4. Installiere die Anwendung erneut

### Problem 3: "CadQuery Fehler"

**Lösung:**
```cmd
pip install --upgrade cadquery
```

### Problem 4: "3D-Viewer zeigt nichts"

**Lösung:**
1. Aktualisiere deine GPU-Treiber
2. Starte die Anwendung neu
3. Versuche ein neues Projekt

### Problem 5: "Anwendung startet nicht"

**Lösung:**
1. Deinstalliere die Anwendung
2. Starte den Computer neu
3. Installiere erneut
4. Kontaktiere Support, falls Problem persists

---

## 🔄 Deinstallation

### Methode 1: Über Systemsteuerung

1. **Einstellungen** öffnen
2. **Apps** → **Apps & Features**
3. Nach **"3D Builder Pro"** suchen
4. Klick auf **"Deinstallieren"**
5. Bestätige die Deinstallation
6. Starte den Computer neu (optional)

### Methode 2: Über Installer

1. Downloads-Ordner öffnen
2. **3DBuilderPro.msi** doppelklicken
3. Wähle **"Entfernen"**
4. Bestätige
5. Fertig

### Methode 3: Manuell

1. Öffne **Datei-Explorer**
2. Navigiere zu: `C:\Program Files\3D Builder Pro`
3. Lösche den Ordner
4. Lösche Desktop-Shortcut
5. Öffne **Systemsteuerung** → **Programme** → **Programme deinstallieren**
6. Suche nach "3D Builder Pro" und entferne es

---

## 🎓 Erste Schritte nach Installation

### 1. Anwendung öffnen
- Desktop-Shortcut doppelklicken oder Start-Menü

### 2. Erste Form erstellen
- Klick auf eine Form im linken Panel (z.B. "Zylinder")
- Form erscheint in der 3D-Ansicht
- Gib Maße im rechten Panel ein

### 3. AutoFix ausprobieren
- Klick auf "AutoFix-Panel"
- Wähle dein Objekt aus
- Klick "Modell analysieren"
- Beobachte den Assistenten mit Pinsel, Hammer und Klebeband! 🤖

### 4. Exportieren
- Datei → Exportieren
- Wähle einen Ordner
- Datei wird als .stl gespeichert

### 5. In Slicer öffnen
- Öffne PrusaSlicer oder deinen Slicer
- Datei → Öffnen
- Wähle die exportierte .stl Datei
- Modell ist bereit zum Drucken!

---

## 🔐 Sicherheit & Datenschutz

### Was wird installiert?
- ✅ Nur notwendige Komponenten
- ✅ Keine Spyware oder Adware
- ✅ Keine Telemetrie (außer optional)

### Wo werden Dateien gespeichert?
- **Anwendung**: `C:\Program Files\3D Builder Pro`
- **Einstellungen**: `C:\Users\[Benutzername]\AppData\Local\3DBuilderPro`
- **Projekte**: Wo du sie speicherst (deine Kontrolle)

### Internetverbindung
- ✅ Nur beim ersten Start erforderlich
- ✅ Für KI-Assistent optional
- ✅ Keine automatischen Updates

---

## 💾 Backup & Wiederherstellung

### Projekte sichern
```
1. Öffne "Dieser PC"
2. Navigiere zu: C:\Users\[Benutzername]\Documents\3DBuilderPro
3. Kopiere den Ordner auf USB-Stick oder Cloud
```

### Projekte wiederherstellen
```
1. Kopiere den Sicherungs-Ordner zurück
2. Öffne die Anwendung
3. Datei → Öffnen
4. Wähle das Projekt
```

---

## 🌐 Netzwerk & Firewall

### Falls die Anwendung nicht startet:

1. **Firewall prüfen**
   - Windows Defender Firewall öffnen
   - "Eine App durch die Firewall zulassen" klicken
   - "3D Builder Pro" erlauben

2. **Antivirus prüfen**
   - Falls Antivirus die Anwendung blockiert
   - Antivirus-Einstellungen öffnen
   - "3D Builder Pro" zur Whitelist hinzufügen

---

## 📞 Support & Hilfe

### Dokumentation
- 📖 README.md - Übersicht
- 📖 ASSISTANT_GUIDE.md - KI-Assistent Handbuch
- 📖 Diese Datei - Installationshandbuch

### Online-Ressourcen
- 🌐 GitHub: https://github.com/bannanenbaer/3D-Builder-for-Printer
- 🐛 Issues: https://github.com/bannanenbaer/3D-Builder-for-Printer/issues
- 💬 Discussions: https://github.com/bannanenbaer/3D-Builder-for-Printer/discussions

### Häufig gestellte Fragen (FAQ)
- **F: Kann ich auf macOS/Linux verwenden?**
  - A: Nein, nur Windows. Aber du kannst es selbst kompilieren.

- **F: Kostet die Anwendung etwas?**
  - A: Nein, sie ist kostenlos und Open Source.

- **F: Kann ich den Quellcode ändern?**
  - A: Ja! Es ist Open Source unter MIT Lizenz.

- **F: Wie viel Speicherplatz wird benötigt?**
  - A: ~500 MB für die Anwendung + Speicherplatz für Projekte.

- **F: Funktioniert es offline?**
  - A: Ja, nur der KI-Assistent benötigt Internet.

---

## ✅ Installationsprüfung

Nachdem die Installation abgeschlossen ist, prüfe:

- [ ] Anwendung startet ohne Fehler
- [ ] 3D-Viewer zeigt Szene
- [ ] Formen können erstellt werden
- [ ] AutoFix-Panel ist sichtbar
- [ ] Assistent ist sichtbar
- [ ] STL-Export funktioniert
- [ ] Undo/Redo funktioniert

**Wenn alles grün ist → Installation erfolgreich! 🎉**

---

## 🚀 Nächste Schritte

1. **Tutorials ansehen**
   - Klick auf "Hilfe" in der Anwendung
   - Oder: Assistenten-Panel öffnen

2. **Erste Modelle erstellen**
   - Einfache Formen kombinieren
   - AutoFix ausprobieren
   - Exportieren und drucken

3. **Erweiterte Features erkunden**
   - OpenSCAD Editor
   - Boolean-Operationen
   - Fillet/Chamfer

4. **Community beitreten**
   - GitHub Discussions
   - Deine Modelle teilen
   - Feedback geben

---

## 📝 Lizenz

3D Builder Pro ist unter der **MIT Lizenz** lizenziert.
Du darfst die Anwendung frei verwenden, modifizieren und verteilen.

---

**Viel Spaß mit 3D Builder Pro! 🎨🖨️**

Bei Fragen oder Problemen → GitHub Issues öffnen oder Assistenten fragen! 🤖

---

*Letzte Aktualisierung: April 2026*
*Version: 1.0.0*
