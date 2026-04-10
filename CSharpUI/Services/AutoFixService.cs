using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ThreeDBuilder.Models;

namespace ThreeDBuilder.Services
{
    /// <summary>
    /// AutoFix Service - Analysiert und optimiert 3D-Modelle lokal für optimale Druckergebnisse.
    /// Analyse erfolgt lokal via PrintQualityAnalyzer; Fillet-Fix via Python-Bridge.
    /// </summary>
    public class AutoFixService
    {
        public class OptimizationReport
        {
            public bool HasSharpEdges { get; set; }
            public bool HasSmallHoles { get; set; }
            public bool HasThinWalls { get; set; }
            public bool HasNonManifoldGeometry { get; set; }
            public List<string> Issues { get; set; } = new();
            public List<string> Recommendations { get; set; } = new();
            public float EstimatedPrintSuccess { get; set; } = 100f;
        }

        public class AutoFixOptions
        {
            public float FilletRadius { get; set; } = 1.5f;
            public float MinWallThickness { get; set; } = 1.2f;
            public float MinHoleSize { get; set; } = 2.0f;
            public bool RemoveSmallHoles { get; set; } = true;
            public bool FixNonManifold { get; set; } = true;
            public bool SmoothMesh { get; set; } = true;
        }

        private readonly PythonBridge _pythonBridge;

        public AutoFixService(PythonBridge pythonBridge)
        {
            _pythonBridge = pythonBridge;
        }

        /// <summary>
        /// Analysiert ein SceneObject lokal auf Druck-Probleme (kein Python-Aufruf).
        /// </summary>
        public Task<OptimizationReport> AnalyzeModel(SceneObject obj, PrinterProfile? profile = null)
        {
            profile ??= PrinterProfile.Prusa;
            var report = new OptimizationReport();

            try
            {
                var qr = PrintQualityAnalyzer.Analyze(obj, profile);

                report.EstimatedPrintSuccess = Math.Max(0f, Math.Min(100f, (float)qr.Score));
                report.Issues.AddRange(qr.Issues);

                // Detect fixable issue types from analysis text
                bool hasEdges = obj.ShapeType is not ("sphere" or "hemisphere");
                report.HasThinWalls = qr.Issues.Any(i =>
                    i.Contains("Wandstärke") || i.Contains("dünn") || i.Contains("Sternzacken"));
                report.HasSharpEdges = hasEdges && report.Issues.Count > 0;
                // Holes and non-manifold cannot be detected from params alone

                // Build recommendations with quality prefix
                if (report.EstimatedPrintSuccess >= 85)
                    report.Recommendations.Add("🟢 Modell sieht gut aus für den Druck!");
                else if (report.EstimatedPrintSuccess >= 70)
                    report.Recommendations.Add("🟡 Einige Optimierungen könnten hilfreich sein");
                else
                    report.Recommendations.Add("🔴 Druckergebnis könnte problematisch sein — AutoFix empfohlen!");

                report.Recommendations.Add($"Geschätzte Druckqualität: {report.EstimatedPrintSuccess:F0}%");
                report.Recommendations.AddRange(qr.Suggestions);
            }
            catch (Exception ex)
            {
                report.Issues.Add($"❌ Fehler bei der Analyse: {ex.Message}");
            }

            return Task.FromResult(report);
        }

        /// <summary>
        /// Wendet Fillet-Fix auf das Objekt an (einziger unterstützter automatischer Fix).
        /// </summary>
        public async Task<bool> AutoFixModel(SceneObject obj, AutoFixOptions? options = null)
        {
            options ??= new AutoFixOptions();

            try
            {
                var report = await AnalyzeModel(obj);

                if (report.HasSharpEdges && options.FilletRadius > 0)
                {
                    await _pythonBridge.SendAsync(
                        "apply_fillet",
                        new { @object = obj.ToBackendDict(), radius = options.FilletRadius }
                    );
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AutoFix Error: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Gibt die Empfehlungen aus einem bereits erstellten Bericht zurück.
        /// </summary>
        public List<string> GetPrintingRecommendations(OptimizationReport report)
            => report.Recommendations;

        /// <summary>
        /// Optimiert ein Modell für einen bestimmten 3D-Drucker.
        /// </summary>
        public async Task<bool> OptimizeForPrinter(SceneObject obj, PrinterProfile printer)
        {
            var options = new AutoFixOptions
            {
                FilletRadius     = (float)(printer.NozzleDiameter * 3.0),
                MinWallThickness = (float)printer.MinWallThickness,
                MinHoleSize      = (float)(printer.NozzleDiameter * 2.0)
            };

            return await AutoFixModel(obj, options);
        }
    }
}
