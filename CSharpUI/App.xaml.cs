using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using ThreeDBuilder.Services;

namespace ThreeDBuilder;

public partial class App : Application
{
    public static PythonBridge? PythonBridge { get; private set; }
    public static TranslationService Translations { get; } = new();

    // Static constructor runs before ANYTHING else – catches XAML load failures too
    static App()
    {
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception ex)
                ShowCrashDialog(ex);
        };
    }

    public App()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        WriteLog("App constructor called – startup begins");
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        WriteLog("OnStartup called");
        base.OnStartup(e);

        // Only apply non-default themes on startup.
        // Dark is already defined in App.xaml — applying it again via ThemeApplier
        // would replace brushes with the same values but risk picking up a wrong
        // System-light detection before the window is fully loaded.
        var settings = Services.SettingsService.Instance.Current;
        if (settings.ThemeMode != "Dark")
            Services.ThemeApplier.Apply(settings);

        StartPythonAsync();
    }

    private async void StartPythonAsync()
    {
        try
        {
            PythonBridge = new PythonBridge();
            bool started = await PythonBridge.StartAsync();
            if (!started)
                MessageBox.Show(
                    "Python geometry backend could not be started.\n\n" +
                    "The app will open but geometry operations will be unavailable.",
                    "Backend Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Python backend error: {ex.Message}", "Warning",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        PythonBridge?.Stop();
        base.OnExit(e);
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ShowCrashDialog(e.Exception);
        e.Handled = true;
    }

    internal static void WriteLog(string message)
    {
        try
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "3DBuilderPro");
            Directory.CreateDirectory(dir);
            File.AppendAllText(Path.Combine(dir, "startup.log"),
                $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n");
        }
        catch { }
    }

    private static void ShowCrashDialog(Exception ex)
    {
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DBuilderPro", "crash.log");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.WriteAllText(logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n");
        }
        catch { }

        try
        {
            var msg = ex.Message;
            var inner = ex.InnerException;
            while (inner != null)
            {
                msg += $"\n→ {inner.Message}";
                inner = inner.InnerException;
            }
            MessageBox.Show($"3D Builder Pro crashed:\n\n{msg}\n\nLog: {logPath}",
                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch { }
    }
}
