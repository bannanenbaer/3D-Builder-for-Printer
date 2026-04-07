using System;
using System.IO;
using System.Text.Json;
using ThreeDBuilder.Models;

namespace ThreeDBuilder.Services;

public class SettingsService
{
    private static SettingsService? _instance;
    public static SettingsService Instance => _instance ??= new SettingsService();

    private static readonly string SettingsDir =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "3DBuilderPro");
    private static readonly string SettingsFile =
        Path.Combine(SettingsDir, "settings.json");

    private AppSettings _current = new();
    public AppSettings Current => _current;

    private SettingsService()
    {
        Load();
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                var json = File.ReadAllText(SettingsFile);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded != null)
                    _current = loaded;
            }
            else
            {
                _current = new AppSettings();
                Save();
            }
        }
        catch
        {
            _current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(SettingsDir);
            var json = JsonSerializer.Serialize(_current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFile, json);
        }
        catch
        {
            // ignore save errors
        }
    }
}
