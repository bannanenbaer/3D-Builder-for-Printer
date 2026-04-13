using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;
using ThreeDBuilder.Models;
using ThreeDBuilder.ViewModels;

namespace ThreeDBuilder.Views;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    // O(1) lookup: object ID → viewport visual
    private readonly Dictionary<string, ModelVisual3D> _visualMap = new();

    // Tracks the position/rotation baked into each object's current STL file.
    // WASD movement applies a delta transform on top of this baked-in transform
    // so no Python round-trip is needed for movement.
    private readonly Dictionary<string, (double X, double Y, double Z, double RotZ)> _bakedTransforms = new();

    // Set to true after the first ZoomExtents so subsequent adds/updates don't
    // reset the camera.  Reset when the whole scene is cleared.
    private bool _hasZoomedOnce;

    // Debounce timer: fires 600 ms after the last WASD step to recompute CSG previews.
    private System.Windows.Threading.DispatcherTimer? _csgDebounce;

    private const double MoveStep = 1.0;  // mm per key press
    private const double RotStep  = 5.0;  // degrees per D key press

    public MainWindow()
    {
        InitializeComponent();
        _vm = new MainViewModel();

        // Default camera: Z-up, isometric view matching the printer's coordinate system
        if (Viewport3D.Camera is PerspectiveCamera startCam)
        {
            startCam.Position     = new Point3D(80, -80, 80);
            startCam.LookDirection = new Vector3D(-1, 1, -1);
            startCam.UpDirection  = new Vector3D(0, 0, 1);
        }
        DataContext = _vm;

        // Show tutorial on first launch
        CheckAndShowTutorial();

        // Wire viewport events
        _vm.ObjectAdded   += obj =>
        {
            // Refresh colour when IsSubtractor or IsSelected toggles.
            // Guard: skip if the object has already been removed from the scene
            // (avoids a ghost visual when Delete fires IsSelected=false AFTER
            //  ObjectRemoved is queued but BEFORE it runs on the dispatcher).
            obj.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(SceneObject.IsSubtractor))
                    Dispatcher.InvokeAsync(async () =>
                    {
                        if (!_vm.SceneObjects.Contains(obj)) return; // object was deleted
                        RemoveFromViewport(obj.Id);
                        AddStlToViewport(obj);
                        await UpdateCsgPreviewsAsync();
                    });
                else if (e.PropertyName == nameof(SceneObject.IsSelected))
                    // Update colour/highlight in-place — do NOT remove+re-add the visual
                    // or the WASD delta transform is lost and the object snaps back.
                    Dispatcher.InvokeAsync(() =>
                    {
                        if (!_vm.SceneObjects.Contains(obj)) return;
                        UpdateObjectAppearance(obj);
                    });
            };
            Dispatcher.InvokeAsync(async () =>
            {
                AddStlToViewport(obj);
                await UpdateCsgPreviewsAsync();
            });
        };
        _vm.ObjectUpdated += obj => Dispatcher.InvokeAsync(async () =>
        {
            RemoveFromViewport(obj.Id);
            AddStlToViewport(obj);
            await UpdateCsgPreviewsAsync();
        });
        _vm.ObjectRemoved += obj => Dispatcher.InvokeAsync(() => RemoveFromViewport(obj.Id));
        _vm.SceneCleared  += ()  => Dispatcher.InvokeAsync(ClearViewport);

        // Apply saved font scale immediately and whenever user changes it
        ApplyFontScale(Services.SettingsService.Instance.Current.FontScale);
        SettingsViewModel.FontScaleChanged += scale => Dispatcher.InvokeAsync(() => ApplyFontScale(scale));

        // Update window title when language changes
        SettingsViewModel.LanguageChanged += _ => Dispatcher.InvokeAsync(() => Title = App.Translations.T("app_title"));

        // Wire viewport mascot animation
        _vm.MascotAnimationRequested += AnimateViewportMascotAsync;
    }

    private void ApplyFontScale(double scale)
    {
        RootContentGrid.LayoutTransform = Math.Abs(scale - 1.0) < 0.01
            ? Transform.Identity
            : new ScaleTransform(scale, scale);
    }

    // ── 3D Viewport ───────────────────────────────────────────────────────

    private void AddStlToViewport(SceneObject obj)
    {
        if (obj.StlPath == null || !File.Exists(obj.StlPath)) return;
        try
        {
            var reader = new StLReader();
            var modelGroup = reader.Read(obj.StlPath);

            Color color;
            if (obj.IsSubtractor)
                color = obj.IsSelected
                    ? Color.FromArgb(200, 255, 80, 80)
                    : Color.FromArgb(140, 239, 68, 68);
            else
                color = obj.IsSelected
                    ? Color.FromRgb(0, 150, 255)
                    : Color.FromRgb(130, 190, 220);

            var brush    = new SolidColorBrush(color);
            var material = new DiffuseMaterial(brush);

            if (modelGroup is Model3DGroup grp)
                foreach (GeometryModel3D gm in grp.Children.OfType<GeometryModel3D>())
                    gm.Material = gm.BackMaterial = material;

            var visual = new ModelVisual3D { Content = modelGroup };
            visual.SetValue(TagProperty, $"obj_{obj.Id}");
            _visualMap[obj.Id] = visual;
            // Record the position/rotation that Python already baked into this STL.
            // WASD movement will apply only the delta on top.
            _bakedTransforms[obj.Id] = (obj.PosX, obj.PosY, obj.PosZ, obj.RotZ);

            // Bounding-box edge highlight for selected objects
            if (obj.IsSelected)
                visual.Children.Add(CreateSelectionBox(modelGroup.Bounds));

            Viewport3D.Children.Add(visual);
            // Zoom to fit only once per scene (first object ever added).
            // Using a flag instead of Count==1 because Remove+Add during regeneration
            // would momentarily drop Count to 0 and re-trigger an unwanted zoom.
            if (!_hasZoomedOnce)
            {
                _hasZoomedOnce = true;
                Viewport3D.ZoomExtents(400);
            }
        }
        catch (Exception ex) { _vm.StatusText = $"Fehler: {ex.Message}"; }
    }

    /// <summary>Draws a cyan wireframe bounding box around the given bounds (12 edges).</summary>
    private static LinesVisual3D CreateSelectionBox(Rect3D b)
    {
        double x0 = b.X,        x1 = b.X + b.SizeX;
        double y0 = b.Y,        y1 = b.Y + b.SizeY;
        double z0 = b.Z,        z1 = b.Z + b.SizeZ;

        var pts = new Point3DCollection(24)
        {
            // Bottom face
            new(x0,y0,z0), new(x1,y0,z0),
            new(x1,y0,z0), new(x1,y1,z0),
            new(x1,y1,z0), new(x0,y1,z0),
            new(x0,y1,z0), new(x0,y0,z0),
            // Top face
            new(x0,y0,z1), new(x1,y0,z1),
            new(x1,y0,z1), new(x1,y1,z1),
            new(x1,y1,z1), new(x0,y1,z1),
            new(x0,y1,z1), new(x0,y0,z1),
            // Verticals
            new(x0,y0,z0), new(x0,y0,z1),
            new(x1,y0,z0), new(x1,y0,z1),
            new(x1,y1,z0), new(x1,y1,z1),
            new(x0,y1,z0), new(x0,y1,z1),
        };

        return new LinesVisual3D { Points = pts, Thickness = 1.5,
            Color = Color.FromRgb(0, 220, 255) }; // cyan
    }

    private void RemoveFromViewport(string objId)
    {
        if (_visualMap.TryGetValue(objId, out var visual))
        {
            _visualMap.Remove(objId);
            _bakedTransforms.Remove(objId);
            Viewport3D.Children.Remove(visual);
        }
    }

    /// <summary>
    /// Updates colour and selection box of an existing visual in-place.
    /// Called when IsSelected changes so the visual is never removed/re-added,
    /// which would destroy any pending WASD delta transform.
    /// </summary>
    private void UpdateObjectAppearance(SceneObject obj)
    {
        if (!_visualMap.TryGetValue(obj.Id, out var visual)) return;

        Color color;
        if (obj.IsSubtractor)
            color = obj.IsSelected
                ? Color.FromArgb(200, 255, 80, 80)
                : Color.FromArgb(140, 239, 68, 68);
        else
            color = obj.IsSelected
                ? Color.FromRgb(0, 150, 255)
                : Color.FromRgb(130, 190, 220);

        var material = new DiffuseMaterial(new SolidColorBrush(color));

        if (visual.Content is Model3DGroup grp)
            foreach (GeometryModel3D gm in grp.Children.OfType<GeometryModel3D>())
                gm.Material = gm.BackMaterial = material;

        // Refresh selection-box wireframe
        visual.Children.Clear();
        if (obj.IsSelected && visual.Content is Model3DGroup grp2)
            visual.Children.Add(CreateSelectionBox(grp2.Bounds));
    }

    // ── WASD movement helpers ─────────────────────────────────────────────

    /// <summary>
    /// Applies only the delta between the current SceneObject position/rotation
    /// and the position that is already baked into the STL file.
    /// This gives instant visual feedback without re-generating the mesh via Python.
    /// </summary>
    private void ApplyDeltaTransform(SceneObject obj)
    {
        if (!_visualMap.TryGetValue(obj.Id, out var visual)) return;
        if (!_bakedTransforms.TryGetValue(obj.Id, out var baked)) return;

        double dX    = obj.PosX  - baked.X;
        double dY    = obj.PosY  - baked.Y;
        double dZ    = obj.PosZ  - baked.Z;
        double dRotZ = obj.RotZ  - baked.RotZ;

        bool hasTranslate = Math.Abs(dX) > 0.0001 || Math.Abs(dY) > 0.0001 || Math.Abs(dZ) > 0.0001;
        bool hasRotate    = Math.Abs(dRotZ) > 0.0001;

        if (!hasTranslate && !hasRotate)
        {
            visual.Transform = Transform3D.Identity;
            return;
        }

        var group = new Transform3DGroup();
        if (hasRotate)
            // Rotate around the object's own baked center, not the world origin.
            // Without CenterX/Y the object would orbit around (0,0) instead of spinning in place.
            group.Children.Add(new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 0, 1), dRotZ),
                baked.X, baked.Y, baked.Z));
        if (hasTranslate)
            group.Children.Add(new TranslateTransform3D(dX, dY, dZ));
        visual.Transform = group;
    }

    /// <summary>
    /// Returns camera-forward and camera-right vectors projected onto the XY build plate.
    /// Used so WASD movement is always relative to the current camera view.
    /// </summary>
    private (Vector3D fwd, Vector3D rgt) GetCameraXYDirections()
    {
        if (Viewport3D.Camera is not PerspectiveCamera cam)
            return (new Vector3D(0, 1, 0), new Vector3D(1, 0, 0));

        var look = cam.LookDirection;
        var up   = cam.UpDirection;

        // Project forward onto the build plate (XY plane, ignore Z component)
        var fwd = new Vector3D(look.X, look.Y, 0);
        if (fwd.Length < 0.01) fwd = new Vector3D(0, 1, 0);
        fwd.Normalize();

        // Camera right = cross(look, up), then project onto XY
        var rgt3 = Vector3D.CrossProduct(look, up);
        var rgt  = new Vector3D(rgt3.X, rgt3.Y, 0);
        if (rgt.Length < 0.01) rgt = new Vector3D(1, 0, 0);
        rgt.Normalize();

        // Snap both directions to the nearest cardinal grid axis (pure X or pure Y).
        // Without snapping the camera angle causes diagonal movement on the grid.
        fwd = Math.Abs(fwd.X) >= Math.Abs(fwd.Y)
            ? new Vector3D(Math.Sign(fwd.X), 0, 0)
            : new Vector3D(0, Math.Sign(fwd.Y), 0);
        rgt = Math.Abs(rgt.X) >= Math.Abs(rgt.Y)
            ? new Vector3D(Math.Sign(rgt.X), 0, 0)
            : new Vector3D(0, Math.Sign(rgt.Y), 0);

        return (fwd, rgt);
    }

    // ── Keyboard handler (WASD movement, always from camera perspective) ──

    private void OnSceneKeyDown(object sender, KeyEventArgs e)
    {
        // If a TextBox (e.g. input bar, parameter fields) has focus → do nothing
        if (Keyboard.FocusedElement is TextBox) return;

        var obj = _vm.SelectedObject;
        if (obj == null) return;

        // Only handle plain WASD/Y without modifiers (Ctrl+Y = Redo must still work)
        if (e.KeyboardDevice.Modifiers != ModifierKeys.None) return;

        // Snapshot the scene before every movement step so Ctrl+Z can undo each step.
        _vm.MarkUndoPoint();

        var (fwd, rgt) = GetCameraXYDirections();
        bool handled = true;

        switch (e.Key)
        {
            case Key.W: // hoch – von der Bauplatte weg (Z+)
                obj.PosZ += MoveStep;
                break;
            case Key.A: // rechts – camera-right auf der Bauplatte
                obj.PosX += rgt.X * MoveStep;
                obj.PosY += rgt.Y * MoveStep;
                break;
            case Key.S: // links – camera-left auf der Bauplatte
                obj.PosX -= rgt.X * MoveStep;
                obj.PosY -= rgt.Y * MoveStep;
                break;
            case Key.Y: // runter – zur Bauplatte hin (Z-)
                obj.PosZ -= MoveStep;
                break;
            case Key.D: // gegen Uhrzeigersinn (aus Kamerasicht = um Z-Achse positiv)
                obj.RotZ += RotStep;
                break;
            default:
                handled = false;
                break;
        }

        if (handled)
        {
            ApplyDeltaTransform(obj);
            e.Handled = true;
            // Recompute CSG preview 600 ms after the last keypress (debounced)
            if (_vm.SceneObjects.Any(o => o.IsSubtractor))
                ScheduleCsgUpdate();
        }
    }

    // ── Left-click hit test: select object ───────────────────────────────

    private void OnViewportMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        // Find the inner WPF Viewport3D inside the HelixViewport3D wrapper
        var innerVp = FindVisualChild<System.Windows.Controls.Viewport3D>(Viewport3D);
        if (innerVp == null) return;

        var pos    = e.GetPosition(innerVp);
        string? hitId = null;

        VisualTreeHelper.HitTest(
            innerVp,
            null,
            result =>
            {
                if (result is RayMeshGeometry3DHitTestResult)
                {
                    var hitVisual = result.VisualHit as ModelVisual3D;
                    foreach (var kv in _visualMap)
                    {
                        if (kv.Value == hitVisual || IsVisualDescendant(kv.Value, hitVisual))
                        {
                            hitId = kv.Key;
                            return HitTestResultBehavior.Stop;
                        }
                    }
                }
                return HitTestResultBehavior.Continue;
            },
            new PointHitTestParameters(pos));

        if (hitId != null)
        {
            var clicked = _vm.SceneObjects.FirstOrDefault(o => o.Id == hitId);
            if (clicked != null)
            {
                bool shift = (Keyboard.Modifiers & ModifierKeys.Shift) != 0;
                if (shift)
                    _vm.ToggleObjectSelection(clicked);
                else
                    _vm.SelectedObject = clicked;
            }
        }
        // Note: clicking empty space does NOT deselect (preserves current selection)
    }

    // ── Visual tree helpers ───────────────────────────────────────────────

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T t) return t;
            var found = FindVisualChild<T>(child);
            if (found != null) return found;
        }
        return null;
    }

    private static bool IsVisualDescendant(DependencyObject ancestor, DependencyObject? element)
    {
        var current = element;
        while (current != null)
        {
            if (current == ancestor) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }

    private void ClearViewport()
    {
        foreach (var visual in _visualMap.Values)
            Viewport3D.Children.Remove(visual);
        _visualMap.Clear();
        _bakedTransforms.Clear();
        _hasZoomedOnce = false;
    }

    // ── Menu / toolbar handlers ───────────────────────────────────────────

    private void OnQuit(object sender, RoutedEventArgs e) => Close();

    private void OnOpenSettings(object sender, RoutedEventArgs e)
    {
        var win = new Window
        {
            Title             = "Einstellungen",
            Width             = 420,
            Height            = 680,
            Background        = System.Windows.Media.Brushes.Transparent,
            WindowStyle       = WindowStyle.ToolWindow,
            ResizeMode        = ResizeMode.NoResize,
            Owner             = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
        };
        var panel = new SettingsPanel { DataContext = _vm.SettingsVM };
        win.Content = panel;
        win.ShowDialog();
    }

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

    // ── Viewport mascot animation ─────────────────────────────────────────

    private async Task AnimateViewportMascotAsync(MascotToolType toolType, TimeSpan duration)
    {
        await Dispatcher.InvokeAsync(() =>
        {
            var tool = toolType switch
            {
                MascotToolType.Brush  => MascotAnimationView.ToolType.Brush,
                MascotToolType.Hammer => MascotAnimationView.ToolType.Hammer,
                MascotToolType.Tape   => MascotAnimationView.ToolType.Tape,
                _                     => MascotAnimationView.ToolType.None,
            };

            ViewportMascot.StopAnimation();
            ViewportMascot.StartAnimation(tool);
            ViewportMascot.Visibility = Visibility.Visible;

            double vpWidth = ViewportMascotLayer.ActualWidth;
            Canvas.SetLeft(ViewportMascot, -100);
            Canvas.SetBottom(ViewportMascot, 18);

            var sb = new Storyboard();
            var anim = new DoubleAnimation
            {
                From               = -100,
                To                 = vpWidth + 20,
                Duration           = duration,
                AccelerationRatio  = 0.2,
                DecelerationRatio  = 0.2,
            };
            Storyboard.SetTarget(anim, ViewportMascot);
            Storyboard.SetTargetProperty(anim, new PropertyPath(Canvas.LeftProperty));
            sb.Children.Add(anim);

            sb.Completed += (_, _) =>
            {
                ViewportMascot.Visibility = Visibility.Collapsed;
                ViewportMascot.StopAnimation();
            };
            sb.Begin();
        });

        await Task.Delay(duration + TimeSpan.FromMilliseconds(100));
    }

    // ── Tutorial (Mascot-gesteuert) ───────────────────────────────────────

    private static readonly (string Title, string Text, MascotAnimationView.ToolType Tool)[] TutorialSteps =
    {
        ("Willkommen bei 3D Builder Pro!",
         "Ich führe dich kurz durch die wichtigsten Funktionen. Klicke auf 'Weiter' um loszulegen!",
         MascotAnimationView.ToolType.Happy),

        ("◀  Linke Leiste — Formen",
         "Hier findest du alle 3D-Formen: Würfel, Kugel, Zylinder, Kegel und viele mehr.\nKlicke eine Form an, um sie in die Szene einzufügen.",
         MascotAnimationView.ToolType.Brush),

        ("▶  Rechte Leiste — Eigenschaften",
         "Wähle ein Objekt aus und passe hier Größe, Position und Drehung an.\nMit Fillet & Chamfer rundest du Kanten ab.",
         MascotAnimationView.ToolType.Tape),

        ("⌨️  Objekte bewegen (WASD)",
         "Wähle ein Objekt aus und benutze:\n• A / S: Links / Rechts (kamerarelativ, am Gitter)\n• W / Y: Hoch / Runter (Z-Achse)\n• D: Drehen um Z-Achse",
         MascotAnimationView.ToolType.None),

        ("🔴  Leerblöcke (Subtraktoren)",
         "Erstelle eine Form und aktiviere 'Leerblock' in den Eigenschaften.\nSchiebe sie in ein Objekt — der Überschneidungsbereich wird automatisch ausgehöhlt.",
         MascotAnimationView.ToolType.Hammer),

        ("🖱️  3D-Navigation",
         "• Rechtsklick + Ziehen: Ansicht drehen\n• Mausrad: Rein- und Rauszoomen\n• Linksklick auf ein Objekt: Auswählen",
         MascotAnimationView.ToolType.None),

        ("📂  Importieren & Exportieren",
         "Klicke auf das Ordner-Symbol in der Toolbar um STL- oder 3MF-Dateien zu laden.\nMit dem Speichern-Symbol exportierst du dein Modell als STL.",
         MascotAnimationView.ToolType.Tape),

        ("⌨️  Nützliche Tastenkürzel",
         "• Ctrl+Z / Ctrl+Y: Rückgängig / Wiederholen\n• Ctrl+S: Als STL exportieren\n• Delete: Ausgewähltes Objekt löschen\n• Ctrl+D: Objekt duplizieren\n\nTutorial jederzeit über Hilfe → Tutorial neu starten öffnen.",
         MascotAnimationView.ToolType.Excited),
    };

    private int _tutorialStep;

    private void CheckAndShowTutorial()
    {
        string flagFile = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DBuilderPro", "tutorial_shown.flag");
        if (!File.Exists(flagFile))
        {
            _tutorialStep = 0;
            ShowTutorialStep(0);
            TutorialOverlay.Visibility = Visibility.Visible;
        }
    }

    private void ShowTutorialStep(int step)
    {
        if (step < 0 || step >= TutorialSteps.Length) return;
        var (title, text, tool) = TutorialSteps[step];
        TutorialStepTitle.Text = title;
        TutorialStepText.Text = text;
        TutorialMascot.StartAnimation(tool);

        bool isFirst = step == 0;
        bool isLast  = step == TutorialSteps.Length - 1;

        TutorialPrevBtn.Visibility   = isFirst ? Visibility.Collapsed : Visibility.Visible;
        TutorialNextBtn.Visibility   = isLast  ? Visibility.Collapsed : Visibility.Visible;
        TutorialFinishBtn.Visibility = isLast  ? Visibility.Visible   : Visibility.Collapsed;

        // Rebuild step dots
        TutorialDotsList.Items.Clear();
        for (int i = 0; i < TutorialSteps.Length; i++)
        {
            bool active = i == step;
            TutorialDotsList.Items.Add(new System.Windows.Shapes.Ellipse
            {
                Width  = active ? 10 : 7,
                Height = active ? 10 : 7,
                Margin = new Thickness(4, 0, 4, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Fill   = new SolidColorBrush(active
                    ? Color.FromRgb(59, 130, 246)
                    : Color.FromRgb(55, 65, 81)),
            });
        }
    }

    private void OnTutorialNext(object sender, RoutedEventArgs e)
    {
        if (_tutorialStep < TutorialSteps.Length - 1)
            ShowTutorialStep(++_tutorialStep);
    }

    private void OnTutorialPrev(object sender, RoutedEventArgs e)
    {
        if (_tutorialStep > 0)
            ShowTutorialStep(--_tutorialStep);
    }

    private void OnTutorialDismiss(object sender, RoutedEventArgs e)
    {
        TutorialMascot.StopAnimation();
        TutorialOverlay.Visibility = Visibility.Collapsed;
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "3DBuilderPro");
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "tutorial_shown.flag"), DateTime.Now.ToString());
    }

    /// <summary>Re-open tutorial from the Hilfe menu (ignores the flag file).</summary>
    private void OnShowTutorial(object sender, RoutedEventArgs e)
    {
        _tutorialStep = 0;
        ShowTutorialStep(0);
        TutorialOverlay.Visibility = Visibility.Visible;
    }

    // ── CSG Preview (Subtractor visualization) ────────────────────────────

    /// <summary>
    /// Fires UpdateCsgPreviewsAsync 600 ms after the last call (debounced).
    /// Used by WASD movement so Python isn't called on every single key press.
    /// </summary>
    private void ScheduleCsgUpdate()
    {
        _csgDebounce?.Stop();
        if (_csgDebounce == null)
        {
            _csgDebounce = new System.Windows.Threading.DispatcherTimer
                { Interval = TimeSpan.FromMilliseconds(600) };
            _csgDebounce.Tick += async (_, _) =>
            {
                _csgDebounce.Stop();
                await UpdateCsgPreviewsAsync();
            };
        }
        _csgDebounce.Start();
    }

    /// <summary>
    /// For every non-subtractor, non-imported object in the scene, ask Python to
    /// cut all active subtractors from it and update the visual geometry in-place.
    /// When no subtractors exist the visuals are already correct (original STLs).
    /// </summary>
    private async Task UpdateCsgPreviewsAsync()
    {
        var bridge = App.PythonBridge;
        if (bridge?.IsRunning != true) return;

        var subtractors = _vm.SceneObjects
            .Where(o => o.IsSubtractor && o.ShapeType != "imported")
            .ToList();

        if (subtractors.Count == 0) return; // nothing to cut

        var subDicts = subtractors.Select(s => (object)s.ToBackendDict()).ToList();

        foreach (var baseObj in _vm.SceneObjects
            .Where(o => !o.IsSubtractor && o.ShapeType != "imported")
            .ToList())
        {
            if (!_visualMap.TryGetValue(baseObj.Id, out var visual)) continue;
            try
            {
                var res = await bridge.CutWithSubtractorsAsync(
                    baseObj.ToBackendDict(), subDicts);

                if (res["status"]?.ToString() != "ok") continue;
                var stlPath = res["stl_path"]?.ToString();
                if (stlPath == null || !File.Exists(stlPath)) continue;

                var reader   = new StLReader();
                var newGroup = reader.Read(stlPath);

                var color = baseObj.IsSelected
                    ? Color.FromRgb(0, 150, 255)
                    : Color.FromRgb(130, 190, 220);
                var material = new DiffuseMaterial(new SolidColorBrush(color));
                if (newGroup is Model3DGroup grp)
                    foreach (GeometryModel3D gm in grp.Children.OfType<GeometryModel3D>())
                        gm.Material = gm.BackMaterial = material;

                // Replace geometry in-place.  The CSG result is already at the
                // absolute world position, so clear any pending WASD delta transform.
                visual.Content   = newGroup;
                visual.Transform = Transform3D.Identity;
                _bakedTransforms[baseObj.Id] =
                    (baseObj.PosX, baseObj.PosY, baseObj.PosZ, baseObj.RotZ);

                visual.Children.Clear();
                if (baseObj.IsSelected && newGroup is Model3DGroup grp2)
                    visual.Children.Add(CreateSelectionBox(grp2.Bounds));
            }
            catch { /* ignore individual CSG errors */ }
        }
    }
}
