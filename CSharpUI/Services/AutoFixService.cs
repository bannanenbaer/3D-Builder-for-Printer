using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using ThreeDBuilder.Models;

namespace ThreeDBuilder.Services
{
    /// <summary>
    /// AutoFix Service - Optimiert 3D-Modelle automatisch für optimale Druckergebnisse
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
        /// Analysiert ein Modell auf Druck-Probleme
        /// </summary>
        public async Task<OptimizationReport> AnalyzeModel(string modelId)
        {
            var report = new OptimizationReport();

            try
            {
                // Rufe Python-Backend auf für detaillierte Analyse
                var analysisResult = await _pythonBridge.SendAsync(
                    "analyze_model",
                    new { model_id = modelId }
                );

                // Parse results
                if (analysisResult.ContainsKey("sharp_edges"))
                {
                    report.HasSharpEdges = analysisResult["sharp_edges"]?.Value<bool>() ?? false;
                    if (report.HasSharpEdges)
                    {
                        report.Issues.Add("⚠️ Scharfe Kanten gefunden - können zu Druckfehlern führen");
                        report.Recommendations.Add("✓ Fillet mit 1-2mm Radius anwenden");
                        report.EstimatedPrintSuccess -= 15;
                    }
                }

                if (analysisResult.ContainsKey("small_holes"))
                {
                    report.HasSmallHoles = analysisResult["small_holes"]?.Value<bool>() ?? false;
                    if (report.HasSmallHoles)
                    {
                        report.Issues.Add("⚠️ Kleine Löcher gefunden - können verstopfen");
                        report.Recommendations.Add("✓ Kleine Löcher füllen oder vergrößern");
                        report.EstimatedPrintSuccess -= 10;
                    }
                }

                if (analysisResult.ContainsKey("thin_walls"))
                {
                    report.HasThinWalls = analysisResult["thin_walls"]?.Value<bool>() ?? false;
                    if (report.HasThinWalls)
                    {
                        report.Issues.Add("⚠️ Zu dünne Wände erkannt - können reißen");
                        report.Recommendations.Add("✓ Wandstärke auf mindestens 1.5mm erhöhen");
                        report.EstimatedPrintSuccess -= 20;
                    }
                }

                if (analysisResult.ContainsKey("non_manifold"))
                {
                    report.HasNonManifoldGeometry = analysisResult["non_manifold"]?.Value<bool>() ?? false;
                    if (report.HasNonManifoldGeometry)
                    {
                        report.Issues.Add("⚠️ Nicht-manifold Geometrie - kann zu Druckfehlern führen");
                        report.Recommendations.Add("✓ Geometrie reparieren und bereinigen");
                        report.EstimatedPrintSuccess -= 25;
                    }
                }

                // Ensure minimum success rate
                report.EstimatedPrintSuccess = Math.Max(report.EstimatedPrintSuccess, 50f);
            }
            catch (Exception ex)
            {
                report.Issues.Add($"❌ Fehler bei der Analyse: {ex.Message}");
            }

            return report;
        }

        /// <summary>
        /// Führt automatische Optimierungen durch
        /// </summary>
        public async Task<bool> AutoFixModel(string modelId, AutoFixOptions? options = null)
        {
            options ??= new AutoFixOptions();

            try
            {
                // Schritt 1: Analysiere das Modell
                var report = await AnalyzeModel(modelId);

                // Schritt 2: Wende Optimierungen an
                var optimizations = new List<string>();

                if (report.HasSharpEdges && options.FilletRadius > 0)
                {
                    optimizations.Add($"Fillet mit {options.FilletRadius}mm Radius anwenden");
                    await _pythonBridge.SendAsync(
                        "apply_fillet",
                        new { model_id = modelId, radius = options.FilletRadius }
                    );
                }

                if (report.HasSmallHoles && options.RemoveSmallHoles)
                {
                    optimizations.Add($"Kleine Löcher (< {options.MinHoleSize}mm) füllen");
                    await _pythonBridge.SendAsync(
                        "fill_small_holes",
                        new { model_id = modelId, min_size = options.MinHoleSize }
                    );
                }

                if (report.HasThinWalls)
                {
                    optimizations.Add($"Wandstärke auf mindestens {options.MinWallThickness}mm erhöhen");
                    await _pythonBridge.SendAsync(
                        "thicken_walls",
                        new { model_id = modelId, min_thickness = options.MinWallThickness }
                    );
                }

                if (report.HasNonManifoldGeometry && options.FixNonManifold)
                {
                    optimizations.Add("Nicht-manifold Geometrie reparieren");
                    await _pythonBridge.SendAsync(
                        "fix_non_manifold",
                        new { model_id = modelId }
                    );
                }

                if (options.SmoothMesh)
                {
                    optimizations.Add("Mesh glätten für bessere Oberflächenqualität");
                    await _pythonBridge.SendAsync(
                        "smooth_mesh",
                        new { model_id = modelId }
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
        /// Gibt Empfehlungen für optimale Druckergebnisse
        /// </summary>
        public List<string> GetPrintingRecommendations(OptimizationReport report)
        {
            var recommendations = new List<string>();

            if (report.EstimatedPrintSuccess < 70)
            {
                recommendations.Add("🔴 Druckergebnis könnte problematisch sein - AutoFix empfohlen!");
            }
            else if (report.EstimatedPrintSuccess < 85)
            {
                recommendations.Add("🟡 Einige Optimierungen könnten hilfreich sein");
            }
            else
            {
                recommendations.Add("🟢 Modell sieht gut aus für den Druck!");
            }

            recommendations.Add($"Geschätzte Druckqualität: {report.EstimatedPrintSuccess:F0}%");

            if (report.HasSharpEdges)
                recommendations.Add("💡 Tipp: Scharfe Kanten können zu Druckfehlern führen. Nutze Fillet!");

            if (report.HasThinWalls)
                recommendations.Add("💡 Tipp: Zu dünne Wände können reißen. Erhöhe die Wandstärke!");

            if (report.HasSmallHoles)
                recommendations.Add("💡 Tipp: Kleine Löcher können verstopfen. Vergrößere sie oder fülle sie!");

            return recommendations;
        }

        /// <summary>
        /// Optimiert ein Modell für einen bestimmten 3D-Drucker
        /// </summary>
        public async Task<bool> OptimizeForPrinter(string modelId, PrinterProfile printer)
        {
            var options = new AutoFixOptions
            {
                FilletRadius         = (float)(printer.NozzleDiameter * 3.0),
                MinWallThickness     = (float)printer.MinWallThickness,
                MinHoleSize          = (float)(printer.NozzleDiameter * 2.0)
            };

            return await AutoFixModel(modelId, options);
        }
    }
}
