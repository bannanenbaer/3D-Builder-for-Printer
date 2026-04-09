using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace ThreeDBuilder.Views
{
    /// <summary>
    /// Animated Brixel Mascot Panel with interactive animations
    /// </summary>
    public partial class BrixelMascotPanel : UserControl
    {
        private Storyboard? _bounceAnimation;
        private Storyboard? _headRotationAnimation;
        private Storyboard? _armWaveAnimation;
        private Storyboard? _glowPulseAnimation;
        private Storyboard? _statusPulseAnimation;

        public BrixelMascotPanel()
        {
            InitializeComponent();
            StartAnimations();
        }

        /// <summary>
        /// Start all Brixel animations
        /// </summary>
        private void StartAnimations()
        {
            if (Resources.TryGetValue("BounceAnimation", out var bounceRes))
            {
                _bounceAnimation = (Storyboard)bounceRes;
                _bounceAnimation?.Begin();
            }

            if (Resources.TryGetValue("HeadRotationAnimation", out var headRes))
            {
                _headRotationAnimation = (Storyboard)headRes;
                _headRotationAnimation?.Begin();
            }

            if (Resources.TryGetValue("ArmWaveAnimation", out var armRes))
            {
                _armWaveAnimation = (Storyboard)armRes;
                _armWaveAnimation?.Begin();
            }

            if (Resources.TryGetValue("GlowPulseAnimation", out var glowRes))
            {
                _glowPulseAnimation = (Storyboard)glowRes;
                _glowPulseAnimation?.Begin();
            }

            if (Resources.TryGetValue("StatusPulseAnimation", out var statusRes))
            {
                _statusPulseAnimation = (Storyboard)statusRes;
                _statusPulseAnimation?.Begin();
            }
        }

        /// <summary>
        /// Stop all animations
        /// </summary>
        public void StopAnimations()
        {
            _bounceAnimation?.Stop();
            _headRotationAnimation?.Stop();
            _armWaveAnimation?.Stop();
            _glowPulseAnimation?.Stop();
            _statusPulseAnimation?.Stop();
        }

        /// <summary>
        /// Resume all animations
        /// </summary>
        public void ResumeAnimations()
        {
            _bounceAnimation?.Resume();
            _headRotationAnimation?.Resume();
            _armWaveAnimation?.Resume();
            _glowPulseAnimation?.Resume();
            _statusPulseAnimation?.Resume();
        }

        /// <summary>
        /// Update Brixel's message
        /// </summary>
        public void SetMessage(string message)
        {
            BrixelMessage.Text = message;
        }

        /// <summary>
        /// Get current input from user
        /// </summary>
        public string GetInput()
        {
            return BrixelInput.Text;
        }

        /// <summary>
        /// Clear input field
        /// </summary>
        public void ClearInput()
        {
            BrixelInput.Clear();
        }

        /// <summary>
        /// Play a special animation (e.g., happy, thinking, celebrating)
        /// </summary>
        public void PlaySpecialAnimation(string animationType)
        {
            switch (animationType)
            {
                case "happy":
                    PlayHappyAnimation();
                    break;
                case "thinking":
                    PlayThinkingAnimation();
                    break;
                case "celebrating":
                    PlayCelebratingAnimation();
                    break;
                case "error":
                    PlayErrorAnimation();
                    break;
            }
        }

        /// <summary>
        /// Happy animation - big smile and bounce
        /// </summary>
        private void PlayHappyAnimation()
        {
            var storyboard = new Storyboard();
            
            // Bigger bounce
            var bounceAnimation = new DoubleAnimationUsingKeyFrames();
            bounceAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(40, KeyTime.FromTimeSpan(System.TimeSpan.Zero)));
            bounceAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(60, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.3))));
            bounceAnimation.KeyFrames.Add(new EasingDoubleKeyFrame(40, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.6))));
            
            Storyboard.SetTarget(bounceAnimation, BrixelBody);
            Storyboard.SetTargetProperty(bounceAnimation, new PropertyPath("(Canvas.Top)"));
            storyboard.Children.Add(bounceAnimation);
            
            storyboard.Begin();
        }

        /// <summary>
        /// Thinking animation - head tilt and pause
        /// </summary>
        private void PlayThinkingAnimation()
        {
            var storyboard = new Storyboard();
            
            // Head tilt
            var headTilt = new DoubleAnimationUsingKeyFrames();
            headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(System.TimeSpan.Zero)));
            headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(-20, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.4))));
            headTilt.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.8))));
            
            Storyboard.SetTarget(headTilt, BrixelHead);
            Storyboard.SetTargetProperty(headTilt, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
            storyboard.Children.Add(headTilt);
            
            storyboard.Begin();
        }

        /// <summary>
        /// Celebrating animation - arms up and spinning
        /// </summary>
        private void PlayCelebratingAnimation()
        {
            var storyboard = new Storyboard();
            
            // Spin animation
            var spin = new DoubleAnimationUsingKeyFrames();
            spin.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(System.TimeSpan.Zero)));
            spin.KeyFrames.Add(new EasingDoubleKeyFrame(360, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(1))));
            
            Storyboard.SetTarget(spin, BrixelHead);
            Storyboard.SetTargetProperty(spin, new PropertyPath("(UIElement.RenderTransform).(RotateTransform.Angle)"));
            storyboard.Children.Add(spin);
            
            storyboard.Begin();
        }

        /// <summary>
        /// Error animation - shake
        /// </summary>
        private void PlayErrorAnimation()
        {
            var storyboard = new Storyboard();
            
            // Shake animation
            var shake = new DoubleAnimationUsingKeyFrames();
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(60, KeyTime.FromTimeSpan(System.TimeSpan.Zero)));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(65, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.1))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(55, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.2))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(65, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.3))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(60, KeyTime.FromTimeSpan(System.TimeSpan.FromSeconds(0.4))));
            
            Storyboard.SetTarget(shake, BrixelBody);
            Storyboard.SetTargetProperty(shake, new PropertyPath("(Canvas.Left)"));
            storyboard.Children.Add(shake);
            
            storyboard.Begin();
        }
    }
}
