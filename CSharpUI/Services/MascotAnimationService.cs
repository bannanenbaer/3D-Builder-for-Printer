using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ThreeDBuilder.Views;

namespace ThreeDBuilder.Services
{
    /// <summary>
    /// Service zur Verwaltung der Mascot-Animationen
    /// </summary>
    public class MascotAnimationService
    {
        private readonly MascotAnimationView _mascotView;

        public MascotAnimationService(MascotAnimationView mascotView)
        {
            _mascotView = mascotView ?? throw new ArgumentNullException(nameof(mascotView));
        }

        /// <summary>
        /// Animationssequenz für die Analyse (mit Pinsel)
        /// </summary>
        public async Task PlayAnalysisAnimation()
        {
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Brush);
            await Task.Delay(3000); // 3 Sekunden Animation
            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Animationssequenz für die Reparatur (mit Hammer)
        /// </summary>
        public async Task PlayRepairAnimation()
        {
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Hammer);
            await Task.Delay(4000); // 4 Sekunden Animation
            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Animationssequenz für die Fixierung (mit Klebeband)
        /// </summary>
        public async Task PlayFixAnimation()
        {
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Tape);
            await Task.Delay(3500); // 3.5 Sekunden Animation
            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Komplette AutoFix-Animationssequenz
        /// </summary>
        public async Task PlayCompleteAutoFixAnimation()
        {
            // Phase 1: Analyse mit Pinsel
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Brush);
            await Task.Delay(2000);

            // Phase 2: Reparatur mit Hammer
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Hammer);
            await Task.Delay(2500);

            // Phase 3: Fixierung mit Klebeband
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Tape);
            await Task.Delay(2000);

            // Finale: Zufriedenes Laufen
            _mascotView.StartAnimation(MascotAnimationView.ToolType.None);
            await Task.Delay(1500);

            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Schnelle Analyse-Animation
        /// </summary>
        public async Task PlayQuickAnalysis()
        {
            _mascotView.AnimationSpeed = 1.5;
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Brush);
            await Task.Delay(2000);
            _mascotView.StopAnimation();
            _mascotView.AnimationSpeed = 1.0;
        }

        /// <summary>
        /// Langsame, detaillierte Reparatur-Animation
        /// </summary>
        public async Task PlayDetailedRepair()
        {
            _mascotView.AnimationSpeed = 0.7;
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Hammer);
            await Task.Delay(5000);
            _mascotView.StopAnimation();
            _mascotView.AnimationSpeed = 1.0;
        }

        /// <summary>
        /// Benutzerdefinierte Animationssequenz
        /// </summary>
        public async Task PlayCustomSequence(List<(MascotAnimationView.ToolType tool, int durationMs)> sequence)
        {
            foreach (var (tool, duration) in sequence)
            {
                _mascotView.StartAnimation(tool);
                await Task.Delay(duration);
            }
            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Erfolgs-Animation (Happy Dance)
        /// </summary>
        public async Task PlaySuccessAnimation()
        {
            // Schnelles Laufen als Freude-Animation
            _mascotView.AnimationSpeed = 2.0;
            _mascotView.StartAnimation(MascotAnimationView.ToolType.None);
            await Task.Delay(1500);
            _mascotView.StopAnimation();
            _mascotView.AnimationSpeed = 1.0;
        }

        /// <summary>
        /// Fehler-Animation (Trauriges Gesicht)
        /// </summary>
        public async Task PlayErrorAnimation()
        {
            _mascotView.StopAnimation();
            // Könnte später mit Gesichtsausdrücken erweitert werden
            await Task.Delay(500);
        }

        /// <summary>
        /// Setzt alle Animationen zurück
        /// </summary>
        public void ResetAll()
        {
            _mascotView.ResetAnimation();
            _mascotView.AnimationSpeed = 1.0;
        }

        /// <summary>
        /// Brixl hüpft fröhlich — 2s
        /// </summary>
        public async Task PlayJumpAnimation()
        {
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Jump);
            await Task.Delay(2000);
            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Brixl ist aufgeregt — beide Arme hoch, hüpfen — 3s
        /// </summary>
        public async Task PlayExcitedAnimation()
        {
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Excited);
            await Task.Delay(3000);
            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Brixl denkt nach — Arm an Kinn, langsames Wippen — 4s
        /// </summary>
        public async Task PlayThinkingAnimation()
        {
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Thinking);
            await Task.Delay(4000);
            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Brixl tanzt vor Freude — Tanzbewegung — 3s
        /// </summary>
        public async Task PlayHappyAnimation()
        {
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Happy);
            await Task.Delay(3000);
            _mascotView.StopAnimation();
        }

        /// <summary>
        /// Begrüßungsanimation — Wave dann Jump
        /// </summary>
        public async Task PlayGreetingAnimation()
        {
            // Phase 1: Winken
            _mascotView.StartAnimation(MascotAnimationView.ToolType.None); // Walk = freundliches Laufen
            await Task.Delay(1200);

            // Phase 2: Hüpfen
            _mascotView.StartAnimation(MascotAnimationView.ToolType.Jump);
            await Task.Delay(1500);

            _mascotView.StopAnimation();
        }
    }
}
