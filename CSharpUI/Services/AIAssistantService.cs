using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ThreeDBuilder.Services;

/// <summary>
/// Connects to the Claude API (Anthropic) for intelligent assistant responses.
/// Falls back to smart offline keyword matching if no API key is set.
/// </summary>
public class AIAssistantService
{
    private static readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(30) };

    private const string AnthropicUrl  = "https://api.anthropic.com/v1/messages";
    private const string ModelId       = "claude-haiku-4-5-20251001";

    private static readonly string _keyFile = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "3DBuilderPro", "anthropic_key.txt");

    private readonly List<(string Role, string Content)> _history = new();

    // ── System prompt ─────────────────────────────────────────────────────

    private const string SystemPrompt = @"Du bist Brixl, ein freundlicher und verspielter KI-Assistent für die App ""3D Builder Pro"" — ein parametrisches 3D-Modellierungstool speziell für den 3D-Druck.

Du kennst alle Features der App perfekt:

FORMEN (linke Leiste):
• Würfel (box): width, height, depth in mm
• Kugel (sphere): radius
• Zylinder (cylinder): radius, height
• Kegel (cone): radius_bottom, radius_top, height
• Torus (torus): radius_major, radius_minor
• Prisma (prism): sides, radius, height
• Pyramide (pyramid): base_size, height
• Rohr (tube): radius_outer, radius_inner, height
• Ellipsoid (ellipsoid): rx, ry, rz
• Halbkugel (hemisphere): radius
• L-Profil (l_profile): width, height, thickness, length
• T-Profil (t_profile): width, height, thickness, length
• Stern (star): outer_r, inner_r, points, height
• Polygon: sides, radius, height
• Gewindezylinder (thread_cyl): radius, height, pitch

BEARBEITUNGEN (rechte Leiste):
• Fillet: rundet Kanten ab, Radius in mm
• Chamfer: schräge Fase, Größe in mm
• Boolean Union: zwei Formen zusammenfügen
• Boolean Subtraktion: Form aus anderer herausschneiden
• Boolean Schnitt: nur überlappenden Bereich behalten

WEITERE FEATURES:
• Import: STL und 3MF Dateien (Ordner-Symbol oder Ctrl+S)
• Export: STL (Ctrl+S)
• OpenSCAD-Editor (zweiter Tab) für parametrischen Code
• Undo/Redo: Ctrl+Z / Ctrl+Y
• Duplizieren: Ctrl+D
• Löschen: Entf-Taste
• 3D-Navigation: Rechtsklick+Ziehen = drehen, Mausrad = zoomen
• Automatische Updates über Update-Button in der Toolbar

DEINE PERSÖNLICHKEIT:
• Freundlich, verspielt, begeistert von 3D-Druck
• Antworte immer auf Deutsch, kurz (max. 3-4 Sätze)
• Nutze gelegentlich passende Emojis (🎯 ✨ 🔧 📦 💡 🎨 🌟)
• Sei ermutigend und positiv

AKTIONEN — wenn du eine Szenen-Aktion ausführen sollst, FÜGE AM ENDE deiner Antwort einen Block hinzu:
• Form erstellen:   [AKTION:{"cmd":"create","shape":"box","params":{"width":20,"height":20,"depth":20}}]
• Objekt ändern:    [AKTION:{"cmd":"update","params":{"radius":15}}]
• Fillet anwenden:  [AKTION:{"cmd":"fillet","radius":2}]
• Chamfer:          [AKTION:{"cmd":"chamfer","size":1.5}]
• Löschen:          [AKTION:{"cmd":"delete"}]

Mögliche shape-Werte: box, sphere, cylinder, cone, torus, prism, pyramid, tube, ellipsoid, hemisphere, l_profile, t_profile, star, polygon, thread_cyl
Verwende den AKTION-Block NUR wenn der Nutzer explizit eine Form erstellen oder etwas ändern möchte.";

    // ── API key management ────────────────────────────────────────────────

    public static string? LoadApiKey()
    {
        try { return File.Exists(_keyFile) ? File.ReadAllText(_keyFile).Trim() : null; }
        catch { return null; }
    }

    public static void SaveApiKey(string key)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_keyFile)!);
        File.WriteAllText(_keyFile, key.Trim());
    }

    // ── Main entry point ──────────────────────────────────────────────────

    public async Task<string> AskAsync(string userMessage)
    {
        _history.Add(("user", userMessage));

        string response;
        var apiKey = LoadApiKey();
        if (!string.IsNullOrWhiteSpace(apiKey))
            response = await CallClaudeAsync(apiKey);
        else
            response = GetOfflineResponse(userMessage);

        _history.Add(("assistant", response));

        // Keep last 20 turns to avoid token overflow
        while (_history.Count > 20)
            _history.RemoveAt(0);

        return response;
    }

    public void ClearHistory() => _history.Clear();

    // ── Claude API call ───────────────────────────────────────────────────

    private async Task<string> CallClaudeAsync(string apiKey)
    {
        var messages = _history
            .Select(h => new { role = h.Role, content = h.Content })
            .ToArray();

        var body = JsonSerializer.Serialize(new
        {
            model       = ModelId,
            max_tokens  = 512,
            system      = SystemPrompt,
            messages
        });

        using var req = new HttpRequestMessage(HttpMethod.Post, AnthropicUrl)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        };
        req.Headers.Add("x-api-key",          apiKey);
        req.Headers.Add("anthropic-version",  "2023-06-01");

        HttpResponseMessage resp;
        try { resp = await _http.SendAsync(req); }
        catch (Exception ex) { return $"Verbindungsfehler: {ex.Message}"; }

        var json = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode)
            return $"API-Fehler ({(int)resp.StatusCode}) — bitte API-Schlüssel in den Einstellungen prüfen. 🔑";

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("content")[0]
                .GetProperty("text")
                .GetString() ?? "Keine Antwort erhalten.";
        }
        catch { return "Antwort konnte nicht gelesen werden."; }
    }

    // ── Offline keyword fallback ──────────────────────────────────────────

    private static string GetOfflineResponse(string input)
    {
        var q = input.ToLowerInvariant();

        if (q.Contains("hallo") || q.Contains("hi ") || q.Contains("hey") || q.Contains("moin"))
            return "Hallo! 👋 Ich bin Brixl! Trag in den Einstellungen deinen Anthropic API-Schlüssel ein, dann werde ich so schlau wie Claude! Ohne Schlüssel kann ich nur einfache Fragen beantworten.";

        if (q.Contains("api") || q.Contains("schlüssel") || q.Contains("key") || q.Contains("einstellungen"))
            return "Geh in die Einstellungen (Zahnrad-Symbol) und trage deinen Anthropic API-Schlüssel ein! 🔑 Schlüssel bekommst du kostenlos auf console.anthropic.com.";

        if (q.Contains("würfel") || q.Contains("box") || q.Contains("quader"))
            return "Würfel/Quader: Klick auf 'Würfel' in der linken Leiste. 📦 Parameter: width (Breite), height (Höhe), depth (Tiefe) — alles in Millimetern!";

        if (q.Contains("kugel") || q.Contains("sphere"))
            return "Kugel: Klick auf 'Kugel' links. 🌐 Einziger Parameter: radius (Radius in mm). Perfekt für Köpfe, Knöpfe oder Dekorationen!";

        if (q.Contains("zylinder"))
            return "Zylinder: links auf 'Zylinder' klicken. 🔵 Parameter: radius und height in mm. Ideal für Bolzen, Becher oder Halter!";

        if (q.Contains("kegel") || q.Contains("cone"))
            return "Kegel: radius_bottom (unten), radius_top (oben, 0 = Spitze), height. 🔺 Mit radius_top > 0 wird's ein Kegelstumpf!";

        if (q.Contains("fillet") || q.Contains("rundung") || q.Contains("kante abrund"))
            return "Fillet rundet Kanten ab! 🎯 Objekt auswählen → Radius einstellen → 'Fillet anwenden' klicken. Macht Drucke stabiler und professioneller!";

        if (q.Contains("chamfer") || q.Contains("fase") || q.Contains("abschrägung"))
            return "Chamfer macht schräge Fasen an Kanten. ✨ Objekt auswählen → Größe einstellen → 'Chamfer anwenden'. Sieht technisch super aus!";

        if (q.Contains("boolean") || q.Contains("vereinig") || q.Contains("subtrahier") || q.Contains("verschmelz"))
            return "Boolean-Operationen 🔧: Vereinigung = Formen zusammenfügen, Subtraktion = Form herausschneiden, Schnitt = nur Überlappung behalten. Zwei Objekte auswählen und Button drücken!";

        if (q.Contains("stl") || q.Contains("export") || q.Contains("speicher"))
            return "STL exportieren: Ctrl+S oder das Speichern-Symbol in der Toolbar! 💾 STL-Dateien kannst du direkt in PrusaSlicer, Cura oder Bambu Studio slicen.";

        if (q.Contains("import") || q.Contains("laden") || q.Contains("3mf") || q.Contains("öffnen"))
            return "Import: Ordner-Symbol in der Toolbar oder Menü Datei → Öffnen. 📂 Unterstützt: STL und 3MF Dateien!";

        if (q.Contains("scad") || q.Contains("openscad") || q.Contains("code"))
            return "OpenSCAD-Editor: zweiter Tab oben! 💡 Schreib parametrischen Code, klick 'Vorschau' und sieh das Ergebnis sofort im 3D-Viewer. Sehr mächtig!";

        if (q.Contains("dreh") || q.Contains("rotier") || q.Contains("ansicht") || q.Contains("kamera") || q.Contains("naviga"))
            return "3D-Navigation: 🖱️ Rechtsklick+Ziehen = drehen, Mausrad = zoomen, Linksklick = Objekt auswählen. Im Menü 'Ansicht' gibt es vorgefertigte Kamerapositionen!";

        if (q.Contains("update") || q.Contains("version") || q.Contains("aktualisier"))
            return "Update-Button in der Toolbar: prüft GitHub auf neue Versionen und installiert automatisch! ☁️ Nach dem Update startet die App neu mit der neuen Version.";

        if (q.Contains("undo") || q.Contains("rückgängig") || q.Contains("ctrl+z"))
            return "Rückgängig/Wiederholen: Ctrl+Z und Ctrl+Y ↩️ Oder über Menü Bearbeiten. Mehrere Schritte sind möglich!";

        if (q.Contains("torus") || q.Contains("ring") || q.Contains("donut"))
            return "Torus = Ring/Donut-Form! 🍩 Parameter: radius_major (Außenradius des Rings) und radius_minor (Dicke des Rings). Perfekt für Ringe oder Dichtungen!";

        if (q.Contains("stern") || q.Contains("star"))
            return "Stern: outer_r (Außenspitzen), inner_r (Einbuchtungen), points (Anzahl Zacken), height (Höhe). ⭐ Für Schilder, Deko oder Sternförmige Halterungen!";

        if (q.Contains("rohr") || q.Contains("tube") || q.Contains("hohl"))
            return "Rohr/Tube: radius_outer (Außen), radius_inner (Innen), height. 🔵 Perfekt für Schläuche, Halter oder alles mit Hohlraum!";

        return "Gute Frage! 🤔 Für die beste Hilfe bitte in den Einstellungen einen Anthropic API-Schlüssel eintragen — dann bin ich so schlau wie Claude! Ohne Schlüssel kann ich nur einfache Fragen beantworten. Schlüssel gibt's auf console.anthropic.com.";
    }
}
