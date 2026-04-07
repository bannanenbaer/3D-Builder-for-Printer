using System;
using System.Windows;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;
using ThreeDBuilder.Models;

namespace ThreeDBuilder.Services;

/// <summary>
/// Applies theme (Dark/Light/System) and colorblind palette to the running WPF application.
/// All changes go through Application.Current.Resources so DynamicResource bindings update live.
/// </summary>
public static class ThemeApplier
{
    // Default dark palette – restored when mode is "None"
    private static readonly (string Key, string Hex)[] _defaultColors =
    {
        ("PanelBackground",  "#0F1419"),
        ("SidebarBackground","#1A1F2E"),
        ("ButtonHighlight",  "#3B82F6"),
        ("AccentBrush",      "#60A5FA"),
        ("TextPrimary",      "#F8FAFC"),
        ("TextSecondary",    "#94A3B8"),
        ("SeparatorBrush",   "#2D3748"),
        ("SuccessBrush",     "#10B981"),
        ("WarningBrush",     "#F59E0B"),
        ("ErrorBrush",       "#EF4444"),
        ("InfoBrush",        "#06B6D4"),
    };

    private static readonly (string Key, string Hex)[] _lightColors =
    {
        ("PanelBackground",  "#F1F5F9"),
        ("SidebarBackground","#E2E8F0"),
        ("ButtonHighlight",  "#2563EB"),
        ("AccentBrush",      "#3B82F6"),
        ("TextPrimary",      "#0F172A"),
        ("TextSecondary",    "#475569"),
        ("SeparatorBrush",   "#CBD5E1"),
        ("SuccessBrush",     "#059669"),
        ("WarningBrush",     "#D97706"),
        ("ErrorBrush",       "#DC2626"),
        ("InfoBrush",        "#0891B2"),
    };

    public static void Apply(AppSettings settings)
    {
        ApplyBaseTheme(settings.ThemeMode);
        ApplyColorBlindMode(settings.ColorBlindMode);
    }

    public static void ApplyBaseTheme(string themeMode)
    {
        bool isDark = themeMode switch
        {
            "Light"  => false,
            "System" => !IsSystemLightTheme(),
            _        => true, // "Dark" and anything unknown
        };

        // Update MaterialDesign base theme
        try
        {
            var paletteHelper = new PaletteHelper();
            var theme = paletteHelper.GetTheme();
            theme.SetBaseTheme(isDark ? BaseTheme.Dark : BaseTheme.Light);
            paletteHelper.SetTheme(theme);
        }
        catch { /* graceful – MD theme is cosmetic only */ }

        // Update custom app-wide color resources
        var colors = isDark ? _defaultColors : _lightColors;
        var res = Application.Current.Resources;
        foreach (var (key, hex) in colors)
            res[key] = MakeBrush(hex);
    }

    public static void ApplyColorBlindMode(string mode)
    {
        // Wong colorblind-safe palette (doi:10.1038/nmeth.1618)
        var res = Application.Current.Resources;

        switch (mode)
        {
            case "Protanopia":   // Red-blind: swap reds for orange, use deep blue as accent
                res["AccentBrush"]    = MakeBrush("#56B4E9"); // sky blue
                res["ButtonHighlight"]= MakeBrush("#0072B2"); // deep blue
                res["SuccessBrush"]   = MakeBrush("#009E73"); // bluish green
                res["ErrorBrush"]     = MakeBrush("#E69F00"); // orange (not red)
                res["WarningBrush"]   = MakeBrush("#F0E442"); // yellow
                res["InfoBrush"]      = MakeBrush("#56B4E9");
                break;

            case "Deuteranopia": // Green-blind: swap greens for sky blue
                res["AccentBrush"]    = MakeBrush("#56B4E9");
                res["ButtonHighlight"]= MakeBrush("#0072B2");
                res["SuccessBrush"]   = MakeBrush("#56B4E9"); // blue instead of green
                res["ErrorBrush"]     = MakeBrush("#D55E00"); // vermillion
                res["WarningBrush"]   = MakeBrush("#F0E442");
                res["InfoBrush"]      = MakeBrush("#CC79A7");
                break;

            case "Tritanopia":   // Blue-blind: swap blues for reddish purple
                res["AccentBrush"]    = MakeBrush("#CC79A7"); // reddish purple
                res["ButtonHighlight"]= MakeBrush("#D55E00"); // vermillion
                res["SuccessBrush"]   = MakeBrush("#009E73");
                res["ErrorBrush"]     = MakeBrush("#D55E00");
                res["WarningBrush"]   = MakeBrush("#E69F00");
                res["InfoBrush"]      = MakeBrush("#CC79A7");
                break;

            case "Monochromacy": // Full grayscale
                res["AccentBrush"]    = MakeBrush("#B0B0B0");
                res["ButtonHighlight"]= MakeBrush("#888888");
                res["SuccessBrush"]   = MakeBrush("#AAAAAA");
                res["ErrorBrush"]     = MakeBrush("#444444");
                res["WarningBrush"]   = MakeBrush("#777777");
                res["InfoBrush"]      = MakeBrush("#999999");
                break;

            default: // "None" – restore accent/status defaults from current base theme
                break; // colors already set by ApplyBaseTheme
        }
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static SolidColorBrush MakeBrush(string hex)
    {
        var color = (Color)ColorConverter.ConvertFromString(hex);
        return new SolidColorBrush(color); // NOT frozen — must be mutable for live updates
    }

    private static bool IsSystemLightTheme()
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int v && v == 1;
        }
        catch { return false; } // default to dark
    }
}
