using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using ThreeDBuilder.Models;
using ThreeDBuilder.ViewModels;

namespace ThreeDBuilder.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();
        DataContext = _vm;

        // Show tutorial on first launch
        CheckAndShowTutorial();

        // Wire viewport events
        _vm.ObjectAdded   += obj => Dispatcher.InvokeAsync(() => AddStlToViewport(obj));
        _vm.ObjectUpdated += obj => Dispatcher.InvokeAsync(() => { RemoveFromViewport(obj.Id); AddStlToViewport(obj); });
        _vm.ObjectRemoved += obj => Dispatcher.InvokeAsync(() => RemoveFromViewport(obj.Id));
        _vm.SceneCleared  += ()  => Dispatcher.InvokeAsync(ClearViewport);
    }

    // ── 3D Viewport ───────────────────────────────────────────────────────

    private void AddStlToViewport(SceneObject obj)
    {
        if (obj.StlPath == null || !File.Exists(obj.StlPath)) return;
        try
        {
            var reader = new StLReader();
            var modelGroup = reader.Read(obj.StlPath);

            var color = obj.IsSelected
                ? Color.FromRgb(0, 120, 212)
                : Color.FromRgb(130, 190, 220);
            var material = new DiffuseMaterial(new SolidColorBrush(color));

            if (modelGroup is Model3DGroup grp)
                foreach (GeometryModel3D gm in grp.Children.OfType<GeometryModel3D>())
                    gm.Material = gm.BackMaterial = material;

            var visual = new ModelVisual3D { Content = modelGroup };
            visual.SetValue(TagProperty, $"obj_{obj.Id}");
            Viewport3D.Children.Add(visual);
            Viewport3D.ZoomExtents(400);
        }
        catch (Exception ex) { _vm.StatusText = $"Fehler: {ex.Message}"; }
    }

    private void RemoveFromViewport(string objId)
    {
        var visual = Viewport3D.Children
            .OfType<ModelVisual3D>()
            .FirstOrDefault(v => v.GetValue(TagProperty) is string t && t == $"obj_{objId}");
        if (visual != null) Viewport3D.Children.Remove(visual);
    }

    private void ClearViewport()
    {
        var toRemove = Viewport3D.Children
            .OfType<ModelVisual3D>()
            .Where(v => v.GetValue(TagProperty) is string t && t.StartsWith("obj_"))
            .ToList();
        foreach (var v in toRemove) Viewport3D.Children.Remove(v);
    }

    // ── Menu / toolbar handlers ───────────────────────────────────────────

    private void OnQuit(object sender, RoutedEventArgs e) => Close();

    private void OnAbout(object sender, RoutedEventArgs e) =>
        MessageBox.Show(
            "3D-Builder für 3D-Druck\n\n" +
            "Technologien:\n" +
            "• C# WPF + HelixToolkit (UI & 3D-Ansicht)\n" +
            "• Python + CadQuery (Geometrie-Engine)\n" +
            "• OpenSCAD (optional, für SCAD-Editor)\n\n" +
            "github.com/bannanenbaer/3d-builder-for-printer",
            _vm.T.T("help_about"),
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );

    private void OnSetDe(object sender, RoutedEventArgs e)
    {
        App.Translations.CurrentLanguage = "de";
        Title = App.Translations.T("app_title");
    }

    private void OnSetEn(object sender, RoutedEventArgs e)
    {
        App.Translations.CurrentLanguage = "en";
        Title = App.Translations.T("app_title");
    }

    private void OnResetView(object sender, RoutedEventArgs e) => Viewport3D.ZoomExtents(500);

    private void OnTopView(object sender, RoutedEventArgs e)
    {
        if (Viewport3D.Camera is System.Windows.Media.Media3D.PerspectiveCamera cam)
        {
            cam.Position = new Point3D(0, 0, 200);
            cam.LookDirection = new Vector3D(0, 0, -1);
            cam.UpDirection = new Vector3D(0, 1, 0);
        }
    }

    private void OnFrontView(object sender, RoutedEventArgs e)
    {
        if (Viewport3D.Camera is System.Windows.Media.Media3D.PerspectiveCamera cam)
        {
            cam.Position = new Point3D(0, -200, 30);
            cam.LookDirection = new Vector3D(0, 1, 0);
            cam.UpDirection = new Vector3D(0, 0, 1);
        }
    }

    private void OnSideView(object sender, RoutedEventArgs e)
    {
        if (Viewport3D.Camera is System.Windows.Media.Media3D.PerspectiveCamera cam)
        {
            cam.Position = new Point3D(200, 0, 30);
            cam.LookDirection = new Vector3D(-1, 0, 0);
            cam.UpDirection = new Vector3D(0, 0, 1);
        }
    }

    private void OnIsometricView(object sender, RoutedEventArgs e)
    {
        if (Viewport3D.Camera is System.Windows.Media.Media3D.PerspectiveCamera cam)
        {
            cam.Position = new Point3D(80, -80, 80);
            cam.LookDirection = new Vector3D(-1, 1, -1);
            cam.UpDirection = new Vector3D(0, 0, 1);
            Viewport3D.ZoomExtents(500);
        }
    }

    // ── Tutorial ──────────────────────────────────────────────────────────

    private void CheckAndShowTutorial()
    {
        string flagFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DBuilderPro", "tutorial_shown.flag");
        if (!File.Exists(flagFile))
        {
            TutorialOverlay.Visibility = Visibility.Visible;
        }
    }

    private void OnTutorialDismiss(object sender, RoutedEventArgs e)
    {
        TutorialOverlay.Visibility = Visibility.Collapsed;
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DBuilderPro");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "tutorial_shown.flag"), DateTime.Now.ToString());
    }
}
