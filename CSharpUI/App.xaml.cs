using System.Windows;
using ThreeDBuilder.Services;

namespace ThreeDBuilder;

public partial class App : Application
{
    public static PythonBridge? PythonBridge { get; private set; }
    public static TranslationService Translations { get; } = new();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Start Python geometry backend
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
                MessageBoxImage.Warning
            );
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        PythonBridge?.Stop();
        base.OnExit(e);
    }
}
