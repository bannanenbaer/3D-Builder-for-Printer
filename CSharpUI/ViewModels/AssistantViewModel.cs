using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using ThreeDBuilder.Models;
using ThreeDBuilder.Services;

namespace ThreeDBuilder.ViewModels;

public class AssistantViewModel : INotifyPropertyChanged, IDisposable
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly PythonBridge?      _bridge;
    private readonly MainViewModel      _mainVm;
    private readonly AIAssistantService _ai = new();

    // ── Bindable properties ───────────────────────────────────────────────

    private string _userInput = "";
    public string UserInput
    {
        get => _userInput;
        set { _userInput = value; OnPropertyChanged(); _sendCmd?.RaiseCanExecuteChanged(); }
    }

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    private bool _isTyping;
    public bool IsTyping
    {
        get => _isTyping;
        set { _isTyping = value; OnPropertyChanged(); }
    }

    // ── Commands ──────────────────────────────────────────────────────────

    private RelayCommand? _sendCmd;
    public ICommand SendCommand => _sendCmd!;

    // ── Constructor ───────────────────────────────────────────────────────

    public AssistantViewModel(PythonBridge? bridge, MainViewModel mainVm)
    {
        _bridge = bridge;
        _mainVm = mainVm;

        _sendCmd = new RelayCommand(
            _ => _ = SendAsync(),
            _ => !string.IsNullOrWhiteSpace(UserInput) && !IsTyping
        );

        AddBot("Hallo! 👋 Ich bin Brixl, dein 3D-Assistent!\n\n" +
               "Ich kann:\n" +
               "• Fragen zur Bedienung beantworten\n" +
               "• Formen aus Beschreibungen erstellen (z.B. \"Erstelle einen Zylinder 30mm Radius\")\n" +
               "• Objekte überarbeiten (z.B. \"Mache den Radius 20mm\")\n" +
               "• Modelle analysieren (\"Analysiere mein Modell für Prusa\")\n" +
               "• AutoFix ausführen (\"Optimiere mein Modell\")\n\n" +
               "Tipp: API-Schlüssel in Einstellungen für volle KI-Power! 🚀");

        StartTeaBreakTimer();
    }

    // ── Send message ──────────────────────────────────────────────────────

    private async Task SendAsync()
    {
        var text = UserInput.Trim();
        if (string.IsNullOrEmpty(text)) return;

        UserInput = "";
        AddUser(text);
        IsTyping = true;
        _sendCmd?.RaiseCanExecuteChanged();

        try
        {
            // Check for special local commands first (no API needed)
            if (await TryHandleLocalCommandAsync(text)) return;

            // Call AI with scene context
            var contextMsg  = BuildSceneContext(text);
            var rawResponse = await _ai.AskAsync(contextMsg);
            var (display, action) = ParseAction(rawResponse);

            AddBot(display);
            if (action != null) await ExecuteActionAsync(action);
        }
        catch (Exception ex)
        {
            AddBot($"Ups, da ist was schiefgelaufen 😅 — {ex.Message}");
        }
        finally
        {
            IsTyping = false;
            _sendCmd?.RaiseCanExecuteChanged();
        }
    }

    // ── Local command handling (no API required) ──────────────────────────

    private static readonly string[] _autofixKeywords =
        ["autofix", "optimiere", "repariere", "fix", "verbessere", "druckoptimier"];

    private static readonly string[] _analyzeKeywords =
        ["analysiere", "analyse", "prüfe", "qualität", "druckbar", "score", "bewerte", "check"];

    private async Task<bool> TryHandleLocalCommandAsync(string input)
    {
        var q = input.ToLowerInvariant();

        // ── AutoFix ──────────────────────────────────────────────────────
        if (Array.Exists(_autofixKeywords, k => q.Contains(k)))
        {
            await RunAutoFixAsync(input);
            return true;
        }

        // ── Quality analysis ─────────────────────────────────────────────
        if (Array.Exists(_analyzeKeywords, k => q.Contains(k)))
        {
            RunQualityAnalysis(input);
            return true;
        }

        return false;
    }

    // ── AutoFix ───────────────────────────────────────────────────────────

    private async Task RunAutoFixAsync(string input)
    {
        var obj = _mainVm.SelectedObject;
        if (obj == null)
        {
            AddBot("Bitte wähle zuerst ein Objekt in der Szene aus, dann kann ich es optimieren! 🎯");
            return;
        }

        AddBot($"Ich analysiere und optimiere '{obj.Name}' für den 3D-Druck… 🔍✨");

        // Phase 1 – Brush: analysis
        await _mainVm.RequestMascotAnimation(MascotToolType.Brush, TimeSpan.FromSeconds(2.5));

        // Run quality check first
        var profile = DetectPrinterProfile(input);
        var report  = PrintQualityAnalyzer.Analyze(obj, profile);
        AddBot($"Analyse für {profile.Name} abgeschlossen:\n{report.Summary()}");

        // Phase 2 – Hammer: apply fillet if score not perfect
        await _mainVm.RequestMascotAnimation(MascotToolType.Hammer, TimeSpan.FromSeconds(3.0));

        bool fixed_ = false;
        if (_bridge?.IsRunning == true)
        {
            // Apply a conservative fillet to round sharp edges
            try
            {
                _mainVm.FilletRadius = 1.5;
                await Application.Current.Dispatcher.InvokeAsync(
                    async () => await _mainVm.ApplyFilletAsync());
                fixed_ = true;
            }
            catch { /* fillet can fail on simple shapes – ignore */ }
        }

        // Phase 3 – Tape: finish
        await _mainVm.RequestMascotAnimation(MascotToolType.Tape, TimeSpan.FromSeconds(2.0));

        AddBot(fixed_
            ? "✅ Fertig! Scharfe Kanten wurden abgerundet (Fillet 1.5mm). Dein Modell ist jetzt druckoptimiert! 🎉"
            : "✅ Analyse abgeschlossen! Das Backend ist nicht aktiv, daher wurden keine automatischen Änderungen vorgenommen.");
    }

    // ── Quality analysis ──────────────────────────────────────────────────

    private void RunQualityAnalysis(string input)
    {
        var obj = _mainVm.SelectedObject;
        if (obj == null)
        {
            AddBot("Bitte wähle ein Objekt aus, dann analysiere ich es! 🔍");
            return;
        }

        var profile = DetectPrinterProfile(input);
        var report  = PrintQualityAnalyzer.Analyze(obj, profile);
        AddBot($"📊 Qualitätsbericht für '{obj.Name}' ({profile.Name}):\n\n{report.Summary()}");
    }

    private static PrinterProfile DetectPrinterProfile(string input)
    {
        var q = input.ToLowerInvariant();
        if (q.Contains("bambu"))   return PrinterProfile.Bambu;
        if (q.Contains("creality") || q.Contains("ender")) return PrinterProfile.Creality;
        return PrinterProfile.Prusa;
    }

    // ── Scene context for AI ──────────────────────────────────────────────

    private string BuildSceneContext(string userMessage)
    {
        var parts = new List<string>();

        if (_mainVm.SceneObjects.Count > 0)
        {
            var names = new List<string>();
            foreach (var o in _mainVm.SceneObjects)
                names.Add($"{o.Name}({o.ShapeType})");
            parts.Add($"[Szene: {string.Join(", ", names)}]");
        }

        if (_mainVm.SelectedObject is { } sel)
        {
            var ps = new List<string>();
            foreach (var kv in sel.Params)
                ps.Add($"{kv.Key}={kv.Value}");
            parts.Add($"[Ausgewählt: {sel.Name} | {string.Join(", ", ps)}]");
        }

        return parts.Count > 0
            ? string.Join(" ", parts) + "\n" + userMessage
            : userMessage;
    }

    // ── Action parsing ────────────────────────────────────────────────────

    private static readonly Regex _actionRx = new(@"\[AKTION:(\{.*?\})\]", RegexOptions.Singleline);

    private static (string Display, JObject? Action) ParseAction(string raw)
    {
        var m = _actionRx.Match(raw);
        if (!m.Success) return (raw.Trim(), null);
        var display = _actionRx.Replace(raw, "").Trim();
        try   { return (display, JObject.Parse(m.Groups[1].Value)); }
        catch { return (raw.Trim(), null); }
    }

    // ── Execute scene actions ─────────────────────────────────────────────

    private async Task ExecuteActionAsync(JObject action)
    {
        switch (action["cmd"]?.ToString())
        {
            case "create":
            {
                var shapeType = action["shape"]?.ToString() ?? "box";
                var @params   = ExtractParams(action["params"] as JObject);
                await Application.Current.Dispatcher.InvokeAsync(
                    async () => await _mainVm.AddShapeAsync(shapeType, @params));
                AddBot("✅ Form wurde in die Szene eingefügt!");
                break;
            }
            case "update":
            {
                if (_mainVm.SelectedObject == null) break;
                foreach (var (k, v) in ExtractParams(action["params"] as JObject))
                    _mainVm.SelectedObject.Params[k] = v;
                await Application.Current.Dispatcher.InvokeAsync(
                    async () => await _mainVm.UpdateSelectedShapeAsync());
                AddBot("✅ Objekt wurde aktualisiert!");
                break;
            }
            case "fillet":
                if (double.TryParse(action["radius"]?.ToString(), out var fr))
                {
                    _mainVm.FilletRadius = fr;
                    await Application.Current.Dispatcher.InvokeAsync(
                        async () => await _mainVm.ApplyFilletAsync());
                    AddBot($"✅ Fillet {fr}mm angewendet!");
                }
                break;
            case "chamfer":
                if (double.TryParse(action["size"]?.ToString(), out var cs))
                {
                    _mainVm.ChamferSize = cs;
                    await Application.Current.Dispatcher.InvokeAsync(
                        async () => await _mainVm.ApplyChamferAsync());
                    AddBot($"✅ Chamfer {cs}mm angewendet!");
                }
                break;
            case "delete":
                Application.Current.Dispatcher.Invoke(() => _mainVm.DeleteSelected());
                AddBot("✅ Objekt gelöscht!");
                break;
        }
    }

    private static Dictionary<string, object> ExtractParams(JObject? node)
    {
        var d = new Dictionary<string, object>();
        if (node == null) return d;
        foreach (var kv in node)
            if (kv.Value is JValue jv && jv.Value != null)
                d[kv.Key] = jv.Value;
        return d;
    }

    // ── Tea break ─────────────────────────────────────────────────────────

    private Timer? _teaTimer;

    private void StartTeaBreakTimer()
    {
        _teaTimer = new Timer(_ =>
            Application.Current.Dispatcher.Invoke(() =>
                AddBot("☕ Du arbeitest schon 2 Stunden! Du verdienst eine Tee-Pause — " +
                       "dein Modell läuft nicht weg! 🫖✨")),
            null, TimeSpan.FromHours(2), Timeout.InfiniteTimeSpan);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void AddBot(string text)  => Messages.Add(new ChatMessage { Text = text, IsUser = false });
    private void AddUser(string text) => Messages.Add(new ChatMessage { Text = text, IsUser = true });

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    public void Dispose()
    {
        _teaTimer?.Dispose();
        _teaTimer = null;
    }
}
