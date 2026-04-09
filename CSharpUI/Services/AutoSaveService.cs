using System;
using System.IO;
using System.Timers;
using System.Threading.Tasks;

namespace ThreeDBuilder.Services;

public class AutoSaveService : IDisposable
{
    private readonly Func<Task> _saveCallback;
    private readonly SettingsService _settings;
    private System.Timers.Timer? _timer;
    private bool _disposed;

    public AutoSaveService(Func<Task> saveCallback, SettingsService settings)
    {
        _saveCallback = saveCallback ?? throw new ArgumentNullException(nameof(saveCallback));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void Start()
    {
        Stop();
        if (!_settings.Current.AutoSaveEnabled) return;

        var intervalMs = _settings.Current.AutoSaveIntervalMinutes * 60_000;
        if (intervalMs <= 0) intervalMs = 600_000; // fallback 10 min

        _timer = new System.Timers.Timer(intervalMs);
        _timer.Elapsed += OnTimerElapsed;
        _timer.AutoReset = true;
        _timer.Start();
    }

    public void Stop()
    {
        if (_timer == null) return;
        _timer.Stop();
        _timer.Elapsed -= OnTimerElapsed;
        _timer.Dispose();
        _timer = null;
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        try
        {
            var savePath = _settings.Current.AutoSavePath;
            if (string.IsNullOrWhiteSpace(savePath)) return;

            Directory.CreateDirectory(savePath);
            await _saveCallback();
        }
        catch
        {
            // ignore autosave errors silently
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Stop();
    }
}
