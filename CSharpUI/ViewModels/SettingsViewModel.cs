using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Win32;
using ThreeDBuilder.Services;

namespace ThreeDBuilder.ViewModels;

public class SettingsViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    private readonly SettingsService _svc = SettingsService.Instance;

    // ── Update + Python status ────────────────────────────────────────────
    public UpdateViewModel UpdateVM { get; }

    public bool IsPythonRunning => App.PythonBridge?.IsRunning == true;
    public string PythonStatusText => IsPythonRunning
        ? "Python-Backend: Verbunden"
        : "Python-Backend: Nicht verfügbar (cadquery via pip installieren)";
    public System.Windows.Media.Brush PythonStatusBrush => IsPythonRunning
        ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0x10, 0xB9, 0x81))
        : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0xEF, 0x44, 0x44));

    // ── Available options ─────────────────────────────────────────────────
    public List<string> AvailableThemes { get; } = new() { "Dunkel", "Hell", "System" };
    public List<string> AvailableLanguages { get; } = new() { "Deutsch", "English" };
    public List<string> AvailableColorBlindModes { get; } = new()
        { "Keine", "Protanopie", "Deuteranopie", "Tritanopie", "Monochromie" };
    public List<string> AvailablePrinterPresets { get; } = new()
        { "Ender 3", "Prusa MK4", "Bambu X1", "CR-10", "Anycubic Kobra", "Benutzerdefiniert" };

    private static readonly Dictionary<string, (double W, double D)> _presetDimensions = new()
    {
        ["Ender 3"]          = (220, 220),
        ["Prusa MK4"]        = (250, 210),
        ["Bambu X1"]         = (256, 256),
        ["CR-10"]            = (300, 300),
        ["Anycubic Kobra"]   = (220, 220),
        ["Benutzerdefiniert"] = (0, 0),
    };

    // Map internal ThemeMode value ↔ display string
    private static readonly Dictionary<string, string> _themeModeToDisplay = new()
    {
        ["Dark"]   = "Dunkel",
        ["Light"]  = "Hell",
        ["System"] = "System",
    };
    private static readonly Dictionary<string, string> _displayToThemeMode = new()
    {
        ["Dunkel"] = "Dark",
        ["Hell"]   = "Light",
        ["System"] = "System",
    };

    // Map internal ColorBlindMode ↔ display string
    private static readonly Dictionary<string, string> _colorBlindToDisplay = new()
    {
        ["None"]         = "Keine",
        ["Protanopia"]   = "Protanopie",
        ["Deuteranopia"] = "Deuteranopie",
        ["Tritanopia"]   = "Tritanopie",
        ["Monochromacy"] = "Monochromie",
    };
    private static readonly Dictionary<string, string> _displayToColorBlind = new()
    {
        ["Keine"]        = "None",
        ["Protanopie"]   = "Protanopia",
        ["Deuteranopie"] = "Deuteranopia",
        ["Tritanopie"]   = "Tritanopia",
        ["Monochromie"]  = "Monochromacy",
    };

    // ── Theme ─────────────────────────────────────────────────────────────
    public string ThemeMode
    {
        get => _themeModeToDisplay.TryGetValue(_svc.Current.ThemeMode, out var d) ? d : "Dunkel";
        set
        {
            if (_displayToThemeMode.TryGetValue(value, out var m))
                _svc.Current.ThemeMode = m;
            else
                _svc.Current.ThemeMode = value;
            SaveAndNotify();
        }
    }

    // ── Language ──────────────────────────────────────────────────────────
    public string Language
    {
        get => _svc.Current.Language == "en" ? "English" : "Deutsch";
        set
        {
            _svc.Current.Language = value == "English" ? "en" : "de";
            SaveAndNotify();
        }
    }

    // ── Font scale ────────────────────────────────────────────────────────
    public double FontScale
    {
        get => _svc.Current.FontScale;
        set
        {
            _svc.Current.FontScale = value;
            SaveAndNotify();
            OnPropertyChanged(nameof(FontScalePercent));
        }
    }

    public int FontScalePercent
    {
        get => (int)Math.Round(_svc.Current.FontScale * 100);
        set
        {
            _svc.Current.FontScale = Math.Clamp(value / 100.0, 0.8, 1.5);
            SaveAndNotify();
            OnPropertyChanged(nameof(FontScale));
        }
    }

    // ── Printer bed ───────────────────────────────────────────────────────
    public double PrinterBedWidth
    {
        get => _svc.Current.PrinterBedWidth;
        set
        {
            _svc.Current.PrinterBedWidth = value;
            SaveAndNotify();
        }
    }

    public double PrinterBedDepth
    {
        get => _svc.Current.PrinterBedDepth;
        set
        {
            _svc.Current.PrinterBedDepth = value;
            SaveAndNotify();
        }
    }

    public string PrinterPreset
    {
        get => _svc.Current.PrinterPreset;
        set
        {
            _svc.Current.PrinterPreset = value;
            if (value != "Benutzerdefiniert" && _presetDimensions.TryGetValue(value, out var dim))
            {
                _svc.Current.PrinterBedWidth = dim.W;
                _svc.Current.PrinterBedDepth = dim.D;
                OnPropertyChanged(nameof(PrinterBedWidth));
                OnPropertyChanged(nameof(PrinterBedDepth));
            }
            SaveAndNotify();
        }
    }

    // ── AutoSave ──────────────────────────────────────────────────────────
    public string AutoSavePath
    {
        get => _svc.Current.AutoSavePath;
        set
        {
            _svc.Current.AutoSavePath = value;
            SaveAndNotify();
        }
    }

    public int AutoSaveIntervalMinutes
    {
        get => _svc.Current.AutoSaveIntervalMinutes;
        set
        {
            _svc.Current.AutoSaveIntervalMinutes = Math.Clamp(value, 1, 60);
            SaveAndNotify();
        }
    }

    public bool AutoSaveEnabled
    {
        get => _svc.Current.AutoSaveEnabled;
        set
        {
            _svc.Current.AutoSaveEnabled = value;
            SaveAndNotify();
        }
    }

    // ── ColorBlind ────────────────────────────────────────────────────────
    public string ColorBlindMode
    {
        get => _colorBlindToDisplay.TryGetValue(_svc.Current.ColorBlindMode, out var d) ? d : "Keine";
        set
        {
            if (_displayToColorBlind.TryGetValue(value, out var m))
                _svc.Current.ColorBlindMode = m;
            else
                _svc.Current.ColorBlindMode = value;
            SaveAndNotify();
        }
    }

    // ── Assistant ─────────────────────────────────────────────────────────
    public bool IsAssistantEnabled
    {
        get => _svc.Current.IsAssistantEnabled;
        set
        {
            _svc.Current.IsAssistantEnabled = value;
            SaveAndNotify();
        }
    }

    public bool AnimationsEnabled
    {
        get => _svc.Current.AnimationsEnabled;
        set
        {
            _svc.Current.AnimationsEnabled = value;
            SaveAndNotify();
        }
    }

    public bool AutoSuggestionsEnabled
    {
        get => _svc.Current.AutoSuggestionsEnabled;
        set
        {
            _svc.Current.AutoSuggestionsEnabled = value;
            SaveAndNotify();
        }
    }

    // ── Commands ──────────────────────────────────────────────────────────
    public ICommand ApplyThemeCommand { get; }
    public ICommand OpenAutoSaveFolderCommand { get; }
    public ICommand BrowseAutoSavePathCommand { get; }

    public SettingsViewModel(UpdateViewModel updateVM)
    {
        UpdateVM = updateVM;
        ApplyThemeCommand = new RelayCommand(ExecuteApplyTheme);
        OpenAutoSaveFolderCommand = new RelayCommand(ExecuteOpenAutoSaveFolder);
        BrowseAutoSavePathCommand = new RelayCommand(ExecuteBrowseAutoSavePath);
    }

    private void ExecuteApplyTheme()
    {
        // Theme application hook — actual theme switching wired up in App startup
        _svc.Save();
    }

    private void ExecuteOpenAutoSaveFolder()
    {
        var path = _svc.Current.AutoSavePath;
        if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
        {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }
    }

    private void ExecuteBrowseAutoSavePath()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "AutoSave-Ordner auswählen",
        };
        if (!string.IsNullOrWhiteSpace(_svc.Current.AutoSavePath))
            dialog.InitialDirectory = _svc.Current.AutoSavePath;

        if (dialog.ShowDialog() == true)
        {
            AutoSavePath = dialog.FolderName;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────
    private void SaveAndNotify([CallerMemberName] string? name = null)
    {
        _svc.Save();
        OnPropertyChanged(name);
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
