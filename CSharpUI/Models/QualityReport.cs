using System.Collections.Generic;

namespace ThreeDBuilder.Models;

public class QualityReport
{
    public int    Score       { get; set; } = 100;   // 0-100
    public string Grade       => Score >= 85 ? "🟢 Sehr gut"
                               : Score >= 60 ? "🟡 Okay"
                               :               "🔴 Probleme";
    public List<string> Issues      { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();

    public string Summary()
    {
        var lines = new List<string>
        {
            $"**Druckqualitäts-Analyse: {Grade} ({Score}/100)**"
        };
        if (Issues.Count == 0)
        {
            lines.Add("✅ Keine Probleme gefunden — Modell ist druckbereit!");
        }
        else
        {
            lines.Add("⚠️ Gefundene Probleme:");
            foreach (var i in Issues)   lines.Add($"  • {i}");
            lines.Add("💡 Empfehlungen:");
            foreach (var s in Suggestions) lines.Add($"  • {s}");
        }
        return string.Join("\n", lines);
    }
}

public class PrinterProfile
{
    public string Name             { get; set; } = "";
    public double NozzleDiameter   { get; set; } = 0.4;
    public double LayerHeight      { get; set; } = 0.2;
    public double MinWallThickness { get; set; } = 1.2;   // 3× nozzle
    public double MaxBridgeLength  { get; set; } = 60.0;

    public static readonly PrinterProfile Prusa = new()
    {
        Name = "Prusa MK4", NozzleDiameter = 0.4, LayerHeight = 0.2,
        MinWallThickness = 1.2, MaxBridgeLength = 60
    };
    public static readonly PrinterProfile Bambu = new()
    {
        Name = "Bambu Lab X1", NozzleDiameter = 0.4, LayerHeight = 0.15,
        MinWallThickness = 1.2, MaxBridgeLength = 70
    };
    public static readonly PrinterProfile Creality = new()
    {
        Name = "Creality Ender 3", NozzleDiameter = 0.4, LayerHeight = 0.2,
        MinWallThickness = 1.6, MaxBridgeLength = 50
    };

    public static PrinterProfile FromName(string name) => name.ToLower() switch
    {
        var n when n.Contains("prusa") => Prusa,
        var n when n.Contains("bambu") => Bambu,
        var n when n.Contains("creality") || n.Contains("ender") => Creality,
        _ => Prusa
    };

    public static List<PrinterProfile> GetCommonPrinters() =>
        new() { Prusa, Bambu, Creality };
}
