using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ThreeDBuilder.Views
{
    public partial class MascotAnimationView : UserControl
    {
        private Storyboard _currentStoryboard;

        public MascotAnimationView()
        {
            InitializeComponent();
        }

        public enum ToolType
        {
            None,
            Brush,      // Für Analyse/Farbkodierung
            Hammer,     // Für Reparatur/Optimierung
            Tape        // Für Zusammenkleben/Fixieren
        }

        /// <summary>
        /// Startet die Mascot-Animation mit dem angegebenen Werkzeug
        /// </summary>
        public void StartAnimation(ToolType tool)
        {
            // Stop current animation
            _currentStoryboard?.Stop();

            // Hide all tools
            BrushTool.Visibility = Visibility.Collapsed;
            HammerTool.Visibility = Visibility.Collapsed;
            TapeTool.Visibility = Visibility.Collapsed;

            // Show selected tool and start animation
            switch (tool)
            {
                case ToolType.Brush:
                    BrushTool.Visibility = Visibility.Visible;
                    _currentStoryboard = (Storyboard)Resources["BrushAnimation"];
                    break;

                case ToolType.Hammer:
                    HammerTool.Visibility = Visibility.Visible;
                    _currentStoryboard = (Storyboard)Resources["HammerAnimation"];
                    break;

                case ToolType.Tape:
                    TapeTool.Visibility = Visibility.Visible;
                    _currentStoryboard = (Storyboard)Resources["TapeAnimation"];
                    break;

                case ToolType.None:
                default:
                    _currentStoryboard = (Storyboard)Resources["WalkAnimation"];
                    break;
            }

            _currentStoryboard?.Begin();
        }

        /// <summary>
        /// Stoppt die aktuelle Animation
        /// </summary>
        public void StopAnimation()
        {
            _currentStoryboard?.Stop();
            BrushTool.Visibility = Visibility.Collapsed;
            HammerTool.Visibility = Visibility.Collapsed;
            TapeTool.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Setzt die Animation zurück
        /// </summary>
        public void ResetAnimation()
        {
            StopAnimation();
            MascotCanvas.SetValue(Canvas.LeftProperty, 0.0);
        }

        /// <summary>
        /// Dependency Property für die Werkzeugauswahl
        /// </summary>
        public static readonly DependencyProperty CurrentToolProperty =
            DependencyProperty.Register(
                "CurrentTool",
                typeof(ToolType),
                typeof(MascotAnimationView),
                new PropertyMetadata(ToolType.None, OnCurrentToolChanged));

        public ToolType CurrentTool
        {
            get => (ToolType)GetValue(CurrentToolProperty);
            set => SetValue(CurrentToolProperty, value);
        }

        private static void OnCurrentToolChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MascotAnimationView view && e.NewValue is ToolType tool)
            {
                view.StartAnimation(tool);
            }
        }

        /// <summary>
        /// Dependency Property für die Animationsgeschwindigkeit
        /// </summary>
        public static readonly DependencyProperty AnimationSpeedProperty =
            DependencyProperty.Register(
                "AnimationSpeed",
                typeof(double),
                typeof(MascotAnimationView),
                new PropertyMetadata(1.0, OnAnimationSpeedChanged));

        public double AnimationSpeed
        {
            get => (double)GetValue(AnimationSpeedProperty);
            set => SetValue(AnimationSpeedProperty, value);
        }

        private static void OnAnimationSpeedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is MascotAnimationView view && e.NewValue is double speed)
            {
                if (view._currentStoryboard != null)
                {
                    view._currentStoryboard.SpeedRatio = speed;
                }
            }
        }
    }
}
