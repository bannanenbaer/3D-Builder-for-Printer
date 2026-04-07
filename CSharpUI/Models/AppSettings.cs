namespace ThreeDBuilder.Models;

public class AppSettings
{
    public string ThemeMode { get; set; } = "Dark"; // Dark, Light, System
    public string Language { get; set; } = "de";
    public double FontScale { get; set; } = 1.0; // 0.8-1.5
    public double PrinterBedWidth { get; set; } = 220;
    public double PrinterBedDepth { get; set; } = 220;
    public string PrinterPreset { get; set; } = "Ender 3";
    public string AutoSavePath { get; set; } = "";
    public int AutoSaveIntervalMinutes { get; set; } = 10;
    public bool AutoSaveEnabled { get; set; } = true;
    public string ColorBlindMode { get; set; } = "None"; // None, Protanopia, Deuteranopia, Tritanopia, Monochromacy
    public bool IsAssistantEnabled { get; set; } = true;
    public bool AnimationsEnabled { get; set; } = true;
    public bool AutoSuggestionsEnabled { get; set; } = true;
}
