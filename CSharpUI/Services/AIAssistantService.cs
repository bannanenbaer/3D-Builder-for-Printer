using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace ThreeDBuilder.Services;

/// <summary>
/// Brixl's built-in brain — no API key, no internet required.
/// Understands natural language commands for 3D modelling tasks.
/// </summary>
public class AIAssistantService
{
    private readonly List<(string Role, string Text)> _history = new();

    // ── Public API ────────────────────────────────────────────────────────

    public Task<string> AskAsync(string input)
    {
        _history.Add(("user", input));
        var response = Process(input);
        _history.Add(("assistant", response));
        // Trim oldest entries to stay within the 30-entry limit
        while (_history.Count > 30)
            _history.RemoveAt(0);
        return Task.FromResult(response);
    }

    public void ClearHistory() => _history.Clear();

    // ── Intent detection + dispatch ───────────────────────────────────────

    private string Process(string raw)
    {
        var q = raw.ToLowerInvariant().Trim();

        // Greeting
        if (IsGreeting(q))
            return Pick("Hallo! 😊 Wie kann ich dir helfen?",
                        "Hi! Was möchtest du heute erstellen? 🎯",
                        "Hey! Ich bin bereit — beschreib mir einfach was du brauchst! ✨");

        // Create shape
        if (TryParseCreateShape(q, raw, out var createAction, out var createMsg))
            return createMsg + "\n" + Embed(createAction!);

        // Update / change params
        if (TryParseUpdateShape(q, out var updateAction, out var updateMsg))
            return updateMsg + "\n" + Embed(updateAction!);

        // Fillet
        if (TryParseFillet(q, out var filletAction, out var filletMsg))
            return filletMsg + "\n" + Embed(filletAction!);

        // Chamfer
        if (TryParseChamfer(q, out var chamferAction, out var chamferMsg))
            return chamferMsg + "\n" + Embed(chamferAction!);

        // Delete
        if (q.Contains("lösch") || q.Contains("entfern") || q.Contains("delete") || q.Contains("weg damit"))
            return "Ich lösche das ausgewählte Objekt! 🗑️\n" +
                   Embed(new JObject { ["cmd"] = "delete" });

        // Navigation help
        if (IsNavigationQuestion(q))  return GetNavigationHelp(q);

        // Help / explain feature
        if (IsHelpQuestion(q))        return GetHelpAnswer(q);

        // Shape info
        if (IsShapeQuestion(q))       return GetShapeInfo(q);

        // Export / import
        if (q.Contains("export") || q.Contains("stl") || q.Contains("speicher"))
            return "Zum Exportieren als STL einfach **Ctrl+S** drücken oder das Speichern-Symbol in der Toolbar klicken. 💾 Die Datei kannst du dann direkt in PrusaSlicer, Cura oder Bambu Studio slicen!";

        if (q.Contains("import") || q.Contains("laden") || q.Contains("öffnen") || q.Contains("3mf"))
            return "Klick auf das **Ordner-Symbol** in der Toolbar oder geh zu Datei → Öffnen. 📂 Ich unterstütze STL und 3MF Dateien!";

        // Undo/redo
        if (q.Contains("rückgängig") || q.Contains("undo") || q.Contains("ctrl+z"))
            return "Rückgängig machen: **Ctrl+Z** ↩️\nWiederholen: **Ctrl+Y** ↪️\nAuch über Menü → Bearbeiten erreichbar!";

        // OpenSCAD
        if (q.Contains("scad") || q.Contains("openscad"))
            return "Den **OpenSCAD-Editor** findest du im zweiten Tab oben! 💡 Schreib parametrischen Code und klick auf 'Vorschau' — das Ergebnis erscheint sofort im 3D-Viewer. Sehr mächtig für komplexe Geometrien!";

        // Fallback: be honest + helpful
        return GetFallback(q);
    }

    // ── Create shape parser ───────────────────────────────────────────────

    private static bool TryParseCreateShape(string q, string raw,
        out JObject? action, out string msg)
    {
        action = null; msg = "";

        bool isCreate = q.Contains("erstell") || q.Contains("mach") || q.Contains("generier")
                     || q.Contains("füg") || q.Contains("add") || q.Contains("neu") || q.Contains("create");

        var shapeType = DetectShapeKeyword(q);
        if (shapeType == null || !isCreate) return false;

        var @params = ExtractDimensions(q, shapeType);
        action = new JObject
        {
            ["cmd"]    = "create",
            ["shape"]  = shapeType,
            ["params"] = JObject.FromObject(@params),
        };
        msg = $"Ich erstelle einen {ShapeDisplayName(shapeType)} für dich! 🎉";
        if (@params.Count > 0)
        {
            var parts = new List<string>();
            foreach (var kv in @params) parts.Add($"{kv.Key}: {kv.Value}mm");
            msg += $"\n   Parameter: {string.Join(", ", parts)}";
        }
        return true;
    }

    // ── Update shape parser ───────────────────────────────────────────────

    private static bool TryParseUpdateShape(string q, out JObject? action, out string msg)
    {
        action = null; msg = "";

        bool isUpdate = q.Contains("ändere") || q.Contains("setze") || q.Contains("mach")
                     || q.Contains("update") || q.Contains("verändere") || q.Contains("adjust");
        if (!isUpdate) return false;

        // Look for parameter assignments like "radius 30", "höhe 50mm", "breite auf 20"
        var @params = ExtractParamAssignments(q);
        if (@params.Count == 0) return false;

        action = new JObject
        {
            ["cmd"]    = "update",
            ["params"] = JObject.FromObject(@params),
        };
        var parts = new List<string>();
        foreach (var kv in @params) parts.Add($"{kv.Key}={kv.Value}");
        msg = $"Ich aktualisiere das ausgewählte Objekt: {string.Join(", ", parts)} ✏️";
        return true;
    }

    // ── Fillet / chamfer parsers ──────────────────────────────────────────

    private static bool TryParseFillet(string q, out JObject? action, out string msg)
    {
        action = null; msg = "";
        if (!q.Contains("fillet") && !q.Contains("abrund") && !q.Contains("rund") &&
            !q.Contains("kante")) return false;

        var r = ExtractFirstNumber(q, fallback: 2.0);
        action = new JObject { ["cmd"] = "fillet", ["radius"] = r };
        msg = $"Ich runde die Kanten mit Radius {r}mm ab! ✨";
        return true;
    }

    private static bool TryParseChamfer(string q, out JObject? action, out string msg)
    {
        action = null; msg = "";
        if (!q.Contains("chamfer") && !q.Contains("fase") && !q.Contains("abschrä")) return false;

        var s = ExtractFirstNumber(q, fallback: 1.5);
        action = new JObject { ["cmd"] = "chamfer", ["size"] = s };
        msg = $"Ich erstelle eine Fase mit Größe {s}mm! 🔧";
        return true;
    }

    // ── Q&A helpers ───────────────────────────────────────────────────────

    private static bool IsGreeting(string q) =>
        q is "hallo" or "hi" or "hey" or "moin" or "servus" ||
        q.StartsWith("hallo ") || q.StartsWith("hi ") || q.StartsWith("hey ");

    private static bool IsNavigationQuestion(string q) =>
        q.Contains("dreh") || q.Contains("rotier") || q.Contains("ansicht") ||
        q.Contains("zoom") || q.Contains("kamera") || q.Contains("navigi");

    private static bool IsHelpQuestion(string q) =>
        q.Contains("wie") || q.Contains("was ist") || q.Contains("erklär") ||
        q.Contains("hilf") || q.Contains("help") || q.Contains("was kann");

    private static bool IsShapeQuestion(string q) =>
        q.Contains("parameter") || q.Contains("was bedeutet") || q.Contains("wozu") ||
        q.Contains("unterschied");

    private static string GetNavigationHelp(string q)
    {
        if (q.Contains("zoom")) return "Zoom: **Mausrad** drehen — rein oder raus. 🖱️";
        if (q.Contains("dreh") || q.Contains("rotier"))
            return "Ansicht drehen: **Rechtsklick gedrückt halten** und Maus bewegen. 🖱️ Linker Mausklick wählt ein Objekt aus.";
        return "3D-Navigation:\n• **Rechtsklick + Ziehen**: Ansicht drehen\n• **Mausrad**: Zoomen\n• **Linksklick**: Objekt auswählen\n• Menü Ansicht: vorgefertigte Perspektiven 🎯";
    }

    private static string GetHelpAnswer(string q)
    {
        if (q.Contains("fillet") || q.Contains("kante") || q.Contains("rund"))
            return "**Fillet** rundet scharfe Kanten ab. 🎯 Objekt auswählen → Radius einstellen → 'Fillet anwenden'. Macht Drucke stabiler und professioneller!";
        if (q.Contains("chamfer") || q.Contains("fase"))
            return "**Chamfer** erstellt eine schräge Fase. ✨ Perfekt für technische Teile oder wenn du Kanten 45° anschrägen willst!";
        if (q.Contains("boolean"))
            return "**Boolean-Operationen**: Zwei Objekte auswählen und dann:\n• **Vereinigung**: Formen zusammenfügen\n• **Subtraktion**: Form herausschneiden\n• **Schnitt**: Nur Überlappung behalten 🔧";
        if (q.Contains("autofix") || q.Contains("optimier"))
            return "**AutoFix**: Tippe einfach 'Optimiere mein Modell' — ich analysiere und verbessere es automatisch! 🤖✨";
        if (q.Contains("was kann") || q.Contains("hilf"))
            return "Ich kann:\n• Formen erstellen (z.B. 'Erstelle einen Würfel 30mm')\n• Objekte ändern (z.B. 'Ändere den Radius auf 20mm')\n• Kanten abrunden ('Fillet 2mm anwenden')\n• Modelle analysieren ('Analysiere mein Modell')\n• AutoFix ausführen ('Optimiere mein Modell')\n• Bedienungsfragen beantworten 💡";
        return "Ich helfe dir gerne! Beschreib was du erstellen oder machen möchtest. Du kannst ganz natürlich schreiben, z.B. 'Ich brauche einen Würfel mit 30mm Seitenlänge' 😊";
    }

    private static string GetShapeInfo(string q)
    {
        var shape = DetectShapeKeyword(q);
        return shape switch
        {
            "box"        => "**Würfel/Quader**: width (Breite), height (Höhe), depth (Tiefe) — alles in mm. 📦",
            "sphere"     => "**Kugel**: nur radius (Radius) in mm. 🌐 Perfekt für runde Dekorationen!",
            "cylinder"   => "**Zylinder**: radius und height in mm. 🔵 Ideal für Bolzen, Becher, Halter.",
            "cone"       => "**Kegel**: radius_bottom (unten), radius_top (oben, 0=Spitze), height. 🔺",
            "torus"      => "**Torus/Ring**: radius_major (Ring-Radius), radius_minor (Rohr-Dicke). 🍩",
            "tube"       => "**Rohr**: radius_outer (außen), radius_inner (innen), height. 🔩 Für Schläuche und Hohlkörper!",
            "thread_cyl" => "**Gewindezylinder**: radius, height, pitch (Gewinde-Steigung in mm). 🔩",
            "star"       => "**Stern**: outer_r (Spitzen), inner_r (Einbuchtungen), points (Zacken), height. ⭐",
            _ => "Ich kenne diese Formen: Würfel, Kugel, Zylinder, Kegel, Torus, Prisma, Pyramide, Rohr, Ellipsoid, Halbkugel, L-Profil, T-Profil, Stern, Polygon, Gewindezylinder. 💡 Einfach nach einer fragen!"
        };
    }

    private static string GetFallback(string q)
    {
        // Last-resort helpful suggestions based on keywords
        if (q.Contains("print") || q.Contains("druck"))
            return "Für den 3D-Druck: Exportiere dein Modell als STL (Ctrl+S) und öffne es im Slicer (PrusaSlicer, Cura, Bambu Studio). Ich kann das Modell vorher auf Druckqualität analysieren — schreib einfach 'Analysiere mein Modell'! 🖨️";

        if (q.Contains("support") || q.Contains("stütz"))
            return "Stützstrukturen werden im **Slicer** (z.B. PrusaSlicer) hinzugefügt, nicht in der App. Ich kann aber prüfen ob dein Modell überhaupt Supports braucht — schreib 'Analysiere mein Modell'! 🏗️";

        return Pick(
            "Hmm, das habe ich noch nicht gelernt. 🤔 Versuch es so: 'Erstelle einen [Form] mit [Maße]' oder 'Optimiere mein Modell'!",
            "Ich bin noch im Wachstum! 🌱 Probier: 'Analysiere mein Modell', 'Erstelle einen Würfel 20mm' oder frag nach einer bestimmten Funktion!",
            "Ich verstehe das noch nicht ganz. 😅 Schreib z.B. 'Erstelle einen Zylinder Radius 15mm Höhe 40mm' — mit konkreten Maßen klappt es am besten!"
        );
    }

    // ── Extraction helpers ────────────────────────────────────────────────

    private static readonly Dictionary<string, string> _shapeKeywords = new()
    {
        ["würfel"] = "box",    ["box"] = "box",     ["quader"] = "box",    ["kubus"] = "box",
        ["kugel"]  = "sphere", ["sphere"] = "sphere", ["ball"] = "sphere",
        ["zylinder"] = "cylinder", ["cylinder"] = "cylinder", ["walze"] = "cylinder",
        ["kegel"] = "cone",    ["cone"] = "cone",
        ["torus"] = "torus",   ["ring"] = "torus",  ["donut"] = "torus",
        ["prisma"] = "prism",  ["prism"] = "prism",
        ["pyramide"] = "pyramid", ["pyramid"] = "pyramid",
        ["rohr"] = "tube",     ["tube"] = "tube",   ["schlauch"] = "tube",
        ["ellipsoid"] = "ellipsoid",
        ["halbkugel"] = "hemisphere", ["hemisphere"] = "hemisphere",
        ["l-profil"] = "l_profile", ["l profil"] = "l_profile",
        ["t-profil"] = "t_profile", ["t profil"] = "t_profile",
        ["stern"] = "star",    ["star"] = "star",
        ["polygon"] = "polygon",
        ["gewinde"] = "thread_cyl", ["thread"] = "thread_cyl", ["schraube"] = "thread_cyl",
    };

    private static string? DetectShapeKeyword(string q)
    {
        foreach (var kv in _shapeKeywords)
            if (q.Contains(kv.Key)) return kv.Value;
        return null;
    }

    private static string ShapeDisplayName(string type) => type switch
    {
        "box"        => "Würfel",
        "sphere"     => "Kugel",
        "cylinder"   => "Zylinder",
        "cone"       => "Kegel",
        "torus"      => "Torus/Ring",
        "prism"      => "Prisma",
        "pyramid"    => "Pyramide",
        "tube"       => "Rohr",
        "ellipsoid"  => "Ellipsoid",
        "hemisphere" => "Halbkugel",
        "l_profile"  => "L-Profil",
        "t_profile"  => "T-Profil",
        "star"       => "Stern",
        "polygon"    => "Polygon",
        "thread_cyl" => "Gewindezylinder",
        _ => type
    };

    // Extract dimensions like "30mm", "Radius 15", "15 x 20 x 30", "Breite 40"
    private static Dictionary<string, object> ExtractDimensions(string q, string shape)
    {
        var result = new Dictionary<string, object>();

        // Try named param extraction first: "radius 30", "höhe 50mm"
        var named = ExtractParamAssignments(q);
        foreach (var kv in named) result[kv.Key] = kv.Value;

        // XxYxZ pattern: "20x30x40" or "20 x 30 x 40"
        var xyz = Regex.Match(q, @"(\d+(?:\.\d+)?)\s*[x×]\s*(\d+(?:\.\d+)?)\s*[x×]\s*(\d+(?:\.\d+)?)");
        if (xyz.Success && shape == "box")
        {
            result["width"]  = double.Parse(xyz.Groups[1].Value);
            result["depth"]  = double.Parse(xyz.Groups[2].Value);
            result["height"] = double.Parse(xyz.Groups[3].Value);
            return result;
        }

        // WxH pattern: "20x40"
        var wh = Regex.Match(q, @"(\d+(?:\.\d+)?)\s*[x×]\s*(\d+(?:\.\d+)?)");
        if (wh.Success)
        {
            if (shape is "cylinder" or "cone" or "tube" or "prism")
            {
                if (!result.ContainsKey("radius"))  result["radius"]  = double.Parse(wh.Groups[1].Value);
                if (!result.ContainsKey("height"))  result["height"]  = double.Parse(wh.Groups[2].Value);
            }
            else if (shape == "box")
            {
                if (!result.ContainsKey("width"))   result["width"]   = double.Parse(wh.Groups[1].Value);
                if (!result.ContainsKey("height"))  result["height"]  = double.Parse(wh.Groups[2].Value);
            }
            return result;
        }

        // Single number: use as primary dimension
        if (result.Count == 0)
        {
            var num = ExtractFirstNumber(q, fallback: 0);
            if (num > 0)
            {
                switch (shape)
                {
                    case "sphere" or "hemisphere": result["radius"] = num; break;
                    case "box":    result["width"] = num; result["height"] = num; result["depth"] = num; break;
                    case "cylinder" or "prism": result["radius"] = num; break;
                    case "torus":  result["radius_major"] = num; break;
                }
            }
        }

        return result;
    }

    // Extract "radius 30", "höhe 50", "breite 20", etc.
    private static readonly Dictionary<string, string[]> _paramKeywords = new()
    {
        ["radius"]       = ["radius", "r "],
        ["height"]       = ["höhe", "height", "h "],
        ["width"]        = ["breite", "width"],
        ["depth"]        = ["tiefe", "depth"],
        ["radius_outer"] = ["außenradius", "outer", "außen"],
        ["radius_inner"] = ["innenradius", "inner", "innen"],
        ["radius_major"] = ["hauptradius", "major"],
        ["radius_minor"] = ["nebenradius", "minor", "rohrradius"],
        ["radius_top"]   = ["obenradius", "oben", "top"],
        ["radius_bottom"] = ["untenradius", "unten", "bottom"],
        ["pitch"]        = ["steigung", "pitch"],
        ["points"]       = ["zacken", "spitzen", "points"],
    };

    private static Dictionary<string, object> ExtractParamAssignments(string q)
    {
        var result = new Dictionary<string, object>();
        foreach (var (param, keywords) in _paramKeywords)
        {
            foreach (var kw in keywords)
            {
                // Match "keyword 30", "keyword: 30", "keyword = 30", "keyword auf 30"
                var m = Regex.Match(q, kw + @"\s*(?::|=|auf|von)?\s*(\d+(?:\.\d+)?)");
                if (m.Success)
                {
                    result[param] = double.Parse(m.Groups[1].Value);
                    break;
                }
            }
        }
        return result;
    }

    private static double ExtractFirstNumber(string q, double fallback)
    {
        var m = Regex.Match(q, @"(\d+(?:\.\d+)?)");
        return m.Success ? double.Parse(m.Groups[1].Value) : fallback;
    }

    // Embed an action command in the response string
    private static string Embed(JObject action) =>
        $"[AKTION:{action}]";

    // Pick random from options
    private static readonly Random _rnd = new();
    private static string Pick(params string[] options) =>
        options[_rnd.Next(options.Length)];
}
