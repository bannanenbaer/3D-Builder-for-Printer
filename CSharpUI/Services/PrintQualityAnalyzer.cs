using System;
using System.Collections.Generic;
using ThreeDBuilder.Models;

namespace ThreeDBuilder.Services;

/// <summary>
/// Analyzes a SceneObject for 3D-print quality and returns a QualityReport.
/// </summary>
public static class PrintQualityAnalyzer
{
    public static QualityReport Analyze(SceneObject obj, PrinterProfile? profile = null)
    {
        profile ??= PrinterProfile.Prusa;
        var report = new QualityReport();

        switch (obj.ShapeType)
        {
            case "tube":
                CheckWallThickness(report, profile,
                    GetParam(obj, "radius_outer") - GetParam(obj, "radius_inner"));
                CheckAspectRatio(report, GetParam(obj, "radius_outer") * 2,
                    GetParam(obj, "radius_outer") * 2, GetParam(obj, "height"));
                break;

            case "box":
            case "l_profile":
            case "t_profile":
                CheckAspectRatio(report,
                    GetParam(obj, "width"), GetParam(obj, "depth"), GetParam(obj, "height"));
                CheckMinDimension(report, profile,
                    GetParam(obj, "width"), GetParam(obj, "depth"), GetParam(obj, "height"));
                break;

            case "cone":
                var topR    = GetParam(obj, "radius_top");
                var bottomR = GetParam(obj, "radius_bottom");
                var height  = GetParam(obj, "height");
                if (topR > bottomR * 1.5)
                {
                    report.Score -= 25;
                    report.Issues.Add("Stark überhängender Kegel — Stützstrukturen nötig");
                    report.Suggestions.Add("radius_top ≤ radius_bottom + 30° Überhang empfohlen");
                }
                CheckAspectRatio(report, bottomR * 2, bottomR * 2, height);
                break;

            case "sphere":
            case "hemisphere":
                var r = GetParam(obj, "radius");
                if (r < 3)
                {
                    report.Score -= 20;
                    report.Issues.Add($"Sehr kleine Kugel (r={r}mm) — Druckdetail begrenzt");
                    report.Suggestions.Add("Radius ≥ 5mm für saubere Kugeloberflächen");
                }
                if (obj.ShapeType == "hemisphere")
                {
                    report.Issues.Add("Halbkugel: flache Seite als Druckbett-Auflagefläche empfohlen");
                    report.Suggestions.Add("Flache Seite nach unten orientieren — kein Support nötig");
                }
                break;

            case "cylinder":
                CheckAspectRatio(report, GetParam(obj, "radius") * 2,
                    GetParam(obj, "radius") * 2, GetParam(obj, "height"));
                break;

            case "thread_cyl":
                var pitch = GetParam(obj, "pitch");
                var tRadius = GetParam(obj, "radius");
                if (pitch < 1.0)
                {
                    report.Score -= 30;
                    report.Issues.Add($"Gewinde-Steigung {pitch}mm zu fein für FDM-Druck");
                    report.Suggestions.Add("Mindest-Pitch: 1.0mm (besser: 1.5–2.0mm)");
                }
                if (tRadius < 4)
                {
                    report.Score -= 15;
                    report.Issues.Add($"Gewinde-Radius {tRadius}mm sehr klein");
                    report.Suggestions.Add("Radius ≥ 5mm für stabile Gewinde");
                }
                break;

            case "star":
                var innerR = GetParam(obj, "inner_r");
                var outerR = GetParam(obj, "outer_r");
                if (outerR - innerR < profile.MinWallThickness)
                {
                    report.Score -= 20;
                    report.Issues.Add($"Sternzacken-Wandstärke {outerR - innerR:F1}mm zu dünn");
                    report.Suggestions.Add($"Mindest-Wandstärke: {profile.MinWallThickness}mm");
                }
                break;

            case "imported":
                // Can't analyze without mesh data — give neutral score
                report.Issues.Add("Importiertes Modell — automatische Analyse eingeschränkt");
                report.Suggestions.Add("Mesh manuell auf Fehler prüfen (nicht-manifold Flächen, Löcher)");
                report.Score = 70;
                break;
        }

        // General: very flat objects are hard to print
        report.Score = Math.Max(0, Math.Min(100, report.Score));
        return report;
    }

    // ── helpers ───────────────────────────────────────────────────────────

    private static double GetParam(SceneObject obj, string key, double fallback = 10.0)
    {
        if (obj.Params.TryGetValue(key, out var v))
            try { return Convert.ToDouble(v); } catch { }
        return fallback;
    }

    private static void CheckWallThickness(QualityReport r, PrinterProfile p, double thickness)
    {
        if (thickness < p.MinWallThickness)
        {
            r.Score -= 30;
            r.Issues.Add($"Wandstärke {thickness:F1}mm zu dünn für {p.Name}");
            r.Suggestions.Add($"Mindest-Wandstärke: {p.MinWallThickness}mm " +
                              $"(= 3× Düse {p.NozzleDiameter}mm)");
        }
    }

    private static void CheckAspectRatio(QualityReport r, double x, double y, double z)
    {
        var max = Math.Max(x, Math.Max(y, z));
        var min = Math.Min(x, Math.Min(y, z));
        if (min <= 0) return;
        var ratio = max / min;
        if (ratio > 15)
        {
            r.Score -= 25;
            r.Issues.Add($"Extremes Seitenverhältnis {ratio:F0}:1 — Verzugs-/Haftrisiko");
            r.Suggestions.Add("Brim oder Raft in der Slicer-Software aktivieren");
        }
        else if (ratio > 8)
        {
            r.Score -= 10;
            r.Issues.Add($"Hohes Seitenverhältnis {ratio:F0}:1 — Brim empfohlen");
        }
    }

    private static void CheckMinDimension(QualityReport r, PrinterProfile p,
        double x, double y, double z)
    {
        var min = Math.Min(x, Math.Min(y, z));
        if (min < p.NozzleDiameter * 2)
        {
            r.Score -= 20;
            r.Issues.Add($"Kleinste Dimension {min:F1}mm zu klein für Düse {p.NozzleDiameter}mm");
            r.Suggestions.Add($"Mindest-Feature: {p.NozzleDiameter * 2:F1}mm");
        }
    }
}
