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

public class AssistantViewModel : INotifyPropertyChanged
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
        set { _userInput = value; OnPropertyChanged(); _sendCommand?.RaiseCanExecuteChanged(); }
    }

    public ObservableCollection<ChatMessage> Messages { get; } = new();

    private bool _isTyping;
    public bool IsTyping
    {
        get => _isTyping;
        set { _isTyping = value; OnPropertyChanged(); }
    }

    // ── Commands ──────────────────────────────────────────────────────────

    private RelayCommand? _sendCommand;
    public ICommand SendCommand => _sendCommand!;

    // ── Constructor ───────────────────────────────────────────────────────

    public AssistantViewModel(PythonBridge? bridge, MainViewModel mainVm)
    {
        _bridge = bridge;
        _mainVm = mainVm;

        _sendCommand = new RelayCommand(
            _ => _ = SendAsync(),
            _ => !string.IsNullOrWhiteSpace(UserInput) && !IsTyping
        );

        // Greet on first open
        AddBotMessage("Hallo! 👋 Ich bin Brixl, dein 3D-Assistent! Ich kann:\n• Fragen zur Bedienung beantworten\n• Formen aus Beschreibungen erstellen\n• Objekte überarbeiten und optimieren\n• Modelle auf Druckfehler analysieren\n\nTipp: Trage in den Einstellungen einen Anthropic API-Schlüssel ein für volle KI-Power! 🚀");

        // Tea break reminder after 2 hours
        StartTeaBreakTimer();
    }

    // ── Send message ──────────────────────────────────────────────────────

    private async Task SendAsync()
    {
        var text = UserInput.Trim();
        if (string.IsNullOrEmpty(text)) return;

        UserInput = "";
        AddUserMessage(text);
        IsTyping = true;
        _sendCommand?.RaiseCanExecuteChanged();

        try
        {
            // Build scene context for Claude
            var contextMsg = BuildSceneContext(text);
            var rawResponse = await _ai.AskAsync(contextMsg);
            var (display, action) = ParseActionFromResponse(rawResponse);

            AddBotMessage(display);

            if (action != null)
                await ExecuteActionAsync(action);
        }
        catch (Exception ex)
        {
            AddBotMessage($"Ups, da ist was schiefgelaufen 😅 — {ex.Message}");
        }
        finally
        {
            IsTyping = false;
            _sendCommand?.RaiseCanExecuteChanged();
        }
    }

    // ── Scene context ─────────────────────────────────────────────────────

    private string BuildSceneContext(string userMessage)
    {
        var parts = new List<string>();

        if (_mainVm.SceneObjects.Count > 0)
        {
            var names = new List<string>();
            foreach (var obj in _mainVm.SceneObjects)
                names.Add($"{obj.Name} ({obj.ShapeType})");
            parts.Add($"[Szene: {_mainVm.SceneObjects.Count} Objekte: {string.Join(", ", names)}]");
        }

        if (_mainVm.SelectedObject != null)
        {
            var sel = _mainVm.SelectedObject;
            var paramStr = new List<string>();
            foreach (var kv in sel.Params)
                paramStr.Add($"{kv.Key}={kv.Value}");
            parts.Add($"[Ausgewählt: {sel.Name} ({sel.ShapeType}), Parameter: {string.Join(", ", paramStr)}]");
        }

        if (parts.Count > 0)
            return string.Join(" ", parts) + "\n" + userMessage;

        return userMessage;
    }

    // ── Action parsing ────────────────────────────────────────────────────
    // Claude can include an action block at the end:
    //   [AKTION:{"cmd":"create","shape":"box","params":{"width":20,"height":20,"depth":20}}]

    private static readonly Regex _actionRegex =
        new(@"\[AKTION:(\{.*?\})\]", RegexOptions.Singleline);

    private static (string Display, JObject? Action) ParseActionFromResponse(string raw)
    {
        var m = _actionRegex.Match(raw);
        if (!m.Success) return (raw.Trim(), null);

        var display = _actionRegex.Replace(raw, "").Trim();
        try
        {
            var action = JObject.Parse(m.Groups[1].Value);
            return (display, action);
        }
        catch { return (raw.Trim(), null); }
    }

    // ── Execute scene actions from Claude ─────────────────────────────────

    private async Task ExecuteActionAsync(JObject action)
    {
        var cmd = action["cmd"]?.ToString();
        switch (cmd)
        {
            case "create":
            {
                var shapeType = action["shape"]?.ToString() ?? "box";
                var paramsNode = action["params"] as JObject;
                var @params = new Dictionary<string, object>();
                if (paramsNode != null)
                    foreach (var kv in paramsNode)
                        if (kv.Value != null)
                            @params[kv.Key] = ((JValue)kv.Value).Value ?? 0.0;
                await Application.Current.Dispatcher.InvokeAsync(async () =>
                    await _mainVm.AddShapeAsync(shapeType, @params));
                AddBotMessage("✅ Form wurde in die Szene eingefügt!");
                break;
            }
            case "update":
            {
                var paramsNode = action["params"] as JObject;
                if (paramsNode != null && _mainVm.SelectedObject != null)
                {
                    foreach (var kv in paramsNode)
                        if (kv.Value != null)
                            _mainVm.SelectedObject.Params[kv.Key] = ((JValue)kv.Value).Value ?? 0.0;
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                        await _mainVm.UpdateSelectedShapeAsync());
                    AddBotMessage("✅ Objekt wurde aktualisiert!");
                }
                break;
            }
            case "fillet":
            {
                if (double.TryParse(action["radius"]?.ToString(), out var r))
                {
                    _mainVm.FilletRadius = r;
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                        await _mainVm.ApplyFilletAsync());
                    AddBotMessage($"✅ Fillet mit Radius {r}mm angewendet!");
                }
                break;
            }
            case "chamfer":
            {
                if (double.TryParse(action["size"]?.ToString(), out var s))
                {
                    _mainVm.ChamferSize = s;
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                        await _mainVm.ApplyChamferAsync());
                    AddBotMessage($"✅ Chamfer mit Größe {s}mm angewendet!");
                }
                break;
            }
            case "delete":
                Application.Current.Dispatcher.Invoke(() => _mainVm.DeleteSelected());
                AddBotMessage("✅ Ausgewähltes Objekt gelöscht!");
                break;
        }
    }

    // ── AddShapeAsync overload with optional params ───────────────────────
    // Calls the MainViewModel method passing custom params

    // ── Tea break timer ───────────────────────────────────────────────────

    private void StartTeaBreakTimer()
    {
        var timer = new Timer(_ =>
        {
            Application.Current.Dispatcher.Invoke(() =>
                AddBotMessage("☕ Du arbeitest schon 2 Stunden! Ich glaube du verdienst eine Tee-Pause. Dein Modell läuft nicht weg, versprochen! 🫖✨")
            );
        }, null, TimeSpan.FromHours(2), Timeout.InfiniteTimeSpan);

        // Keep reference so GC doesn't collect it
        GC.KeepAlive(timer);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private void AddBotMessage(string text) =>
        Messages.Add(new ChatMessage { Text = text, IsUser = false });

    private void AddUserMessage(string text) =>
        Messages.Add(new ChatMessage { Text = text, IsUser = true });

    protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
