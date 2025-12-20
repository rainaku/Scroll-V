using System;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace SmoothScroll.Core
{
    /// <summary>
    /// Engine that handles smooth scroll animation with easing functions
    /// </summary>
    public class SmoothScrollEngine : IDisposable
    {
        #region Win32 API

        [DllImport("user32.dll")]
        private static extern void mouse_event(uint dwFlags, int dx, int dy, int dwData, UIntPtr dwExtraInfo);

        private const uint MOUSEEVENTF_WHEEL = 0x0800;
        private const uint MOUSEEVENTF_HWHEEL = 0x01000;
        private const int WHEEL_DELTA = 120;

        #endregion

        private readonly DispatcherTimer _animationTimer;
        private double _remainingScroll = 0;
        private double _initialScroll = 0; // Track initial scroll for progress calculation
        private bool _isScrolling = false;
        private IntPtr _targetWindow = IntPtr.Zero;
        private bool _isHorizontal = false;
        private DateTime _lastScrollTime = DateTime.MinValue;
        private DateTime _scrollStartTime = DateTime.MinValue;
        private bool _disposed = false;
        private double _accumulatedDelta = 0; // For sub-pixel scrolling

        // Scroll settings
        public double SmoothnessFactor { get; set; } = 0.08; // Lower = smoother (0.05 - 0.2)
        public double AccelerationFactor { get; set; } = 1.2; // Multiplier for scroll delta
        public double FrictionFactor { get; set; } = 0.92; // Higher = more momentum (0.85 - 0.98)
        public double MinVelocityThreshold { get; set; } = 0.1; // Very low for smooth stop
        public int AnimationInterval { get; set; } = 8; // ~120fps for smoother animation
        public EasingType EasingFunction { get; set; } = EasingType.EaseOutQuad;
        public bool UseAcceleration { get; set; } = true;
        public double MaxVelocity { get; set; } = 400;
        public bool IsEnabled { get; set; } = true;

        // Smooth stopping settings
        public double SmoothStopThreshold { get; set; } = 30; // Start smooth stop when below this
        public double MinScrollStep { get; set; } = 0.3; // Minimum scroll per frame for smooth stop

        public SmoothScrollEngine()
        {
            _animationTimer = new DispatcherTimer(DispatcherPriority.Send)
            {
                Interval = TimeSpan.FromMilliseconds(AnimationInterval)
            };
            _animationTimer.Tick += AnimationTimer_Tick;
        }

        public void AddScrollDelta(short delta, IntPtr window, bool isHorizontal)
        {
            if (!IsEnabled) return;

            _targetWindow = window;
            _isHorizontal = isHorizontal;

            // Calculate acceleration based on rapid scrolling
            double acceleratedDelta = delta * AccelerationFactor;

            if (UseAcceleration)
            {
                var timeSinceLastScroll = (DateTime.Now - _lastScrollTime).TotalMilliseconds;
                if (timeSinceLastScroll < 150) // Rapid scrolling
                {
                    double accelerationBoost = 1.0 + Math.Min(1.5, (150 - timeSinceLastScroll) / 100.0);
                    acceleratedDelta *= accelerationBoost;
                }
            }

            _lastScrollTime = DateTime.Now;

            // Add to remaining scroll with velocity consideration
            if (Math.Sign(_remainingScroll) == Math.Sign(acceleratedDelta) || Math.Abs(_remainingScroll) < 1)
            {
                _remainingScroll += acceleratedDelta;
            }
            else
            {
                // Change direction - reset momentum
                _remainingScroll = acceleratedDelta;
            }

            // Clamp max velocity
            _remainingScroll = Math.Clamp(_remainingScroll, -MaxVelocity * 15, MaxVelocity * 15);
            _initialScroll = Math.Abs(_remainingScroll);

            if (!_isScrolling)
            {
                _isScrolling = true;
                _scrollStartTime = DateTime.Now;
                _animationTimer.Start();
            }
        }

        private void AnimationTimer_Tick(object? sender, EventArgs e)
        {
            if (!IsEnabled || Math.Abs(_remainingScroll) < MinVelocityThreshold)
            {
                StopScrolling();
                return;
            }

            try
            {
                // Calculate scroll amount for this frame using adaptive easing
                double scrollAmount = CalculateAdaptiveScrollStep();

                // Accumulate sub-pixel scrolling
                _accumulatedDelta += scrollAmount;

                // Only send scroll when we have at least 1 unit
                int intScroll = (int)_accumulatedDelta;
                if (intScroll != 0)
                {
                    ApplyScroll(intScroll);
                    _accumulatedDelta -= intScroll;
                }

                // Update remaining scroll
                _remainingScroll -= scrollAmount;

                // Apply friction - more friction when close to stopping for smoother end
                double dynamicFriction = CalculateDynamicFriction();
                _remainingScroll *= dynamicFriction;
            }
            catch
            {
                // Safety - stop on any error
                StopScrolling();
            }
        }

        private double CalculateAdaptiveScrollStep()
        {
            double absRemaining = Math.Abs(_remainingScroll);
            
            // When close to stopping, use a gentler approach
            if (absRemaining < SmoothStopThreshold)
            {
                // Logarithmic slowdown for very smooth stop
                double progress = absRemaining / SmoothStopThreshold;
                double smoothFactor = 0.05 + progress * 0.1; // 0.05 to 0.15
                double step = _remainingScroll * smoothFactor;
                
                // Ensure minimum movement to avoid feeling stuck
                if (Math.Abs(step) < MinScrollStep && absRemaining > MinVelocityThreshold)
                {
                    step = Math.Sign(_remainingScroll) * MinScrollStep;
                }
                return step;
            }
            
            // Normal scrolling with easing
            double t = SmoothnessFactor;
            double easedT = ApplyEasing(t);
            return _remainingScroll * easedT;
        }

        private double CalculateDynamicFriction()
        {
            double absRemaining = Math.Abs(_remainingScroll);
            
            // Higher friction (slower decay) when close to stopping
            if (absRemaining < SmoothStopThreshold)
            {
                // Gradually increase friction as we slow down
                double progress = 1 - (absRemaining / SmoothStopThreshold);
                return FrictionFactor + (0.98 - FrictionFactor) * progress; // Approach 0.98
            }
            
            return FrictionFactor;
        }

        private double ApplyEasing(double t)
        {
            return EasingFunction switch
            {
                EasingType.Linear => t,
                EasingType.EaseOutQuad => t * (2 - t),
                EasingType.EaseOutCubic => 1 - Math.Pow(1 - t, 3),
                EasingType.EaseOutExpo => t == 1 ? 1 : 1 - Math.Pow(2, -10 * t),
                EasingType.EaseOutCirc => Math.Sqrt(1 - Math.Pow(t - 1, 2)),
                EasingType.EaseInOutQuad => t < 0.5 ? 2 * t * t : 1 - Math.Pow(-2 * t + 2, 2) / 2,
                EasingType.EaseOutElastic => t == 0 ? 0 : t == 1 ? 1 :
                    Math.Pow(2, -10 * t) * Math.Sin((t * 10 - 0.75) * (2 * Math.PI / 3)) + 1,
                EasingType.EaseOutBack => 1 + 2.70158 * Math.Pow(t - 1, 3) + 1.70158 * Math.Pow(t - 1, 2),
                _ => t * (2 - t)
            };
        }

        private void ApplyScroll(int delta)
        {
            if (delta == 0) return;

            // Use mouse_event with our signature to identify injected events
            uint flags = _isHorizontal ? MOUSEEVENTF_HWHEEL : MOUSEEVENTF_WHEEL;
            mouse_event(flags, 0, 0, delta, MouseHook.ScrollSignature);
        }

        private void StopScrolling()
        {
            _animationTimer.Stop();
            _isScrolling = false;
            _remainingScroll = 0;
            _initialScroll = 0;
            _accumulatedDelta = 0;
        }

        public void Reset()
        {
            StopScrolling();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _animationTimer.Stop();
                }
                _disposed = true;
            }
        }
    }

    public enum EasingType
    {
        Linear,
        EaseOutQuad,
        EaseOutCubic,
        EaseOutExpo,
        EaseOutCirc,
        EaseInOutQuad,
        EaseOutElastic,
        EaseOutBack
    }
}
