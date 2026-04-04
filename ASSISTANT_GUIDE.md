# 3D Builder Pro - KI-Assistent Benutzerhandbuch

## Überblick

Der **3D-Assistent** ist ein süßer, animierter KI-gestützter Helfer, der dir bei der Verwendung von 3D Builder Pro hilft. Er kann:

- 🤖 **Tipps und Tutorials** geben
- 🎨 **Formen aus Beschreibungen** generieren
- ❓ **Fragen beantworten** zu allen Features
- 💡 **Automatische Vorschläge** machen

## Aktivierung

Der Assistent ist standardmäßig **aktiviert**. Du kannst ihn deaktivieren in:

**Einstellungen → 3D-Assistent → Assistenten aktivieren**

## Funktionen

### 1. Formen aus Beschreibungen erstellen

Beschreibe einfach, welche Form du erstellen möchtest:

**Beispiele:**
- "Erstelle einen roten Zylinder mit 50mm Höhe und 20mm Radius"
- "Mache eine Kugel mit 30mm Durchmesser"
- "Generiere ein L-Profil mit 10mm Dicke"

Der Assistent wird:
1. Die Beschreibung analysieren
2. Eine Vorschau der Form zeigen
3. Dir ermöglichen, Parameter zu ändern
4. Die Form in die Szene einfügen

### 2. Hilfe und Tutorials

Frage den Assistenten nach Features:

**Beispiele:**
- "Wie nutze ich Fillet?"
- "Erklär mir Boolean-Operationen"
- "Wie exportiere ich als STL?"
- "Was ist Chamfer?"

Der Assistent gibt dir verständliche Erklärungen und Tipps.

### 3. Automatische Vorschläge

Wenn **Automatische Vorschläge** aktiviert sind, macht der Assistent Vorschläge basierend auf deinen Aktionen:

- Wenn du eine Form auswählst → Tipps zur Bearbeitung
- Wenn du mehrere Objekte auswählst → Vorschläge für Boolean-Operationen
- Wenn du anfängst → Tutorials für Anfänger

## Einstellungen

### Assistenten aktivieren
Schaltet den Assistenten komplett ein/aus.

### Automatische Vorschläge
Der Assistent macht Vorschläge basierend auf deinen Aktionen.

### Animationen
Schaltet die süßen Animationen des Assistenten ein/aus.

### KI-Integration (Optional)
Für erweiterte Features wie Formgenerierung aus Beschreibungen benötigst du einen **OpenAI API-Schlüssel**:

1. Gehe zu https://platform.openai.com/api-keys
2. Erstelle einen neuen API-Schlüssel
3. Kopiere den Schlüssel
4. Gehe zu **Einstellungen → KI-Integration**
5. Füge deinen API-Schlüssel ein
6. Klick "API-Schlüssel speichern"

**Sicherheit:** Dein API-Schlüssel wird lokal auf deinem Computer gespeichert und nicht weitergegeben.

## Tipps & Tricks

### Bessere Beschreibungen
Je genauer du die Form beschreibst, desto besser ist das Ergebnis:

❌ "Erstelle einen Zylinder"
✅ "Erstelle einen Zylinder mit 50mm Höhe, 20mm Radius und blauer Farbe"

### Mehrere Formen kombinieren
Du kannst mehrere Formen mit Boolean-Operationen kombinieren:
1. Erstelle die erste Form
2. Erstelle die zweite Form
3. Wähle beide aus
4. Nutze Union, Subtract oder Intersect

### Kanten bearbeiten
Nach der Formgenerierung kannst du Kanten mit Fillet abrunden oder mit Chamfer abfasen.

## Fehlerbehebung

### "Assistent antwortet nicht"
- Prüfe, ob der Assistent aktiviert ist
- Prüfe deine Internetverbindung (für KI-Features)
- Prüfe deinen OpenAI API-Schlüssel

### "API-Fehler"
- Überprüfe deinen API-Schlüssel in den Einstellungen
- Stelle sicher, dass dein OpenAI-Konto aktiv ist
- Prüfe dein API-Kontingent auf https://platform.openai.com/account/billing/overview

### "Form wird nicht erstellt"
- Beschreibe die Form genauer
- Nutze Standard-Formen (Box, Sphere, Cylinder, etc.)
- Versuche es mit einfacheren Beschreibungen

## Datenschutz

- Dein API-Schlüssel wird **nur lokal** gespeichert
- Deine Beschreibungen werden an OpenAI übertragen (siehe OpenAI Datenschutz)
- Der Assistent speichert keine Verlaufsdaten

## Support

Für Fragen oder Probleme:
- Konsultiere dieses Handbuch
- Frage den Assistenten direkt
- Besuche https://github.com/bannanenbaer/3D-Builder-for-Printer

---

**Viel Spaß mit deinem 3D-Assistenten! 🤖✨**
