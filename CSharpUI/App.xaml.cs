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

    protected override void OnStartup(StartupEventArgs e)
    {
        // Global exception handlers – show error instead of silent crash
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        base.OnStartup(e);

        // Start Python backend asynchronously (does not block window startup)
        StartPythonAsync();
    }

    private async void StartPythonAsync()
    {
        try
        {
            PythonBridge = new PythonBridge();
            bool started = await PythonBridge.StartAsync();
            if (!started)
            {
                MessageBox.Show(
                    "Python geometry backend could not be started.\n\n" +
                    "Please ensure Python 3.10+ and CadQuery are installed:\n" +
                    "  pip install cadquery\n\n" +
                    "The app will open but geometry operations will be unavailable.",
                    "Backend Warning",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
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

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
            ShowCrashDialog(ex);
    }

    private static void ShowCrashDialog(Exception ex)
    {
        string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DBuilderPro", "crash.log");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
            File.WriteAllText(logPath,
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n{ex}\n");
        }
        catch { /* ignore log write failure */ }

        MessageBox.Show(
            $"3D Builder Pro crashed:\n\n{ex.Message}\n\nLog: {logPath}",
            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
    }
}

