using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ThreeDBuilder.Views
{
    public partial class MascotAnimationView : UserControl
    {
        private Storyboard? _currentAnim;
        private Storyboard? _idleFloat;
        private Storyboard? _blink;
        private Storyboard? _tailSwing;

        public enum ToolType { None, Brush, Hammer, Tape, Jump, Excited, Thinking, Happy }

        public MascotAnimationView()
        {
            InitializeComponent();
            Loaded += (_, _) => StartIdleAnimations();
        }

        // ── Idle animations (always playing) ─────────────────────────────

        private void StartIdleAnimations()
        {
            _idleFloat = (Storyboard)Resources["IdleFloat"];
            _blink     = (Storyboard)Resources["BlinkAnimation"];
            _tailSwing = (Storyboard)Resources["SwingAnimation"];
            _idleFloat?.Begin(this, true);
            _blink?.Begin(this, true);
            _tailSwing?.Begin(this, true);
        }

        // ── Tool-specific animations ──────────────────────────────────────

        public void StartAnimation(ToolType tool)
        {
            _currentAnim?.Stop();

            var key = tool switch
            {
                ToolType.Brush    => "BrushAnimation",
                ToolType.Hammer   => "HammerAnimation",
                ToolType.Tape     => "TapeAnimation",
                ToolType.Jump     => "JumpAnimation",
                ToolType.Excited  => "ExcitedAnimation",
                ToolType.Thinking => "ThinkingAnimation",
                ToolType.Happy    => "HappyAnimation",
                _                 => "WalkAnimation",
            };

            _currentAnim = (Storyboard)Resources[key];
            _currentAnim?.Begin(this, true);
        }

        public void StopAnimation()
        {
            _currentAnim?.Stop();
            _currentAnim = null;
        }

        public void ResetAnimation()
        {
            StopAnimation();
            RootTranslate.X = 0;
            RootTranslate.Y = 0;
        }

        // ── DependencyProperties ──────────────────────────────────────────

        public static readonly DependencyProperty CurrentToolProperty =
            DependencyProperty.Register(nameof(CurrentTool), typeof(ToolType),
                typeof(MascotAnimationView),
                new PropertyMetadata(ToolType.None, (d, e) =>
                    ((MascotAnimationView)d).StartAnimation((ToolType)e.NewValue)));

        public ToolType CurrentTool
        {
            get => (ToolType)GetValue(CurrentToolProperty);
            set => SetValue(CurrentToolProperty, value);
        }

        public static readonly DependencyProperty AnimationSpeedProperty =
            DependencyProperty.Register(nameof(AnimationSpeed), typeof(double),
                typeof(MascotAnimationView),
                new PropertyMetadata(1.0, (d, e) =>
                {
                    if (((MascotAnimationView)d)._currentAnim is { } sb)
                        sb.SpeedRatio = (double)e.NewValue;
                }));

        public double AnimationSpeed
        {
            get => (double)GetValue(AnimationSpeedProperty);
            set => SetValue(AnimationSpeedProperty, value);
        }
    }
}
