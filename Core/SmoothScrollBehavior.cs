using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScrollV.Core
{
    /// <summary>
    /// Provides ultra-smooth scrolling behavior for ScrollViewer controls.
    /// Uses frame-based animation with lerp for buttery smooth scrolling.
    /// </summary>
    public class SmoothScrollBehavior
    {
        private readonly ScrollViewer _scrollViewer;
        private double _targetVerticalOffset;
        private double _currentVerticalOffset;
        private bool _isAnimating;
        
        // Smooth scroll parameters - tuned for maximum smoothness
        private const double ScrollSpeed = 1.0;        // Scroll sensitivity
        private const double SmoothFactor = 0.12;      // Lower = smoother (0.08-0.15 recommended)
        private const double StopThreshold = 0.5;      // Stop animating when difference is tiny
        
        public SmoothScrollBehavior(ScrollViewer scrollViewer)
        {
            _scrollViewer = scrollViewer ?? throw new ArgumentNullException(nameof(scrollViewer));
            _targetVerticalOffset = _scrollViewer.VerticalOffset;
            _currentVerticalOffset = _scrollViewer.VerticalOffset;
            
            // Hook up the mouse wheel event
            _scrollViewer.PreviewMouseWheel += OnPreviewMouseWheel;
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            
            // Calculate scroll delta (negative = scroll down, positive = scroll up)
            // Delta is typically 120 per notch, so we normalize it
            double delta = -e.Delta * ScrollSpeed;
            
            // Update target offset with bounds checking
            _targetVerticalOffset = Math.Max(0, Math.Min(
                _scrollViewer.ScrollableHeight,
                _targetVerticalOffset + delta));
            
            // Start animation if not already running
            StartAnimation();
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            // Sync offsets when scroll changes from external sources (like scrollbar drag)
            if (!_isAnimating && Math.Abs(e.VerticalChange) > 0.1)
            {
                _targetVerticalOffset = _scrollViewer.VerticalOffset;
                _currentVerticalOffset = _scrollViewer.VerticalOffset;
            }
        }

        private void StartAnimation()
        {
            if (_isAnimating) return;
            
            _isAnimating = true;
            _currentVerticalOffset = _scrollViewer.VerticalOffset;
            CompositionTarget.Rendering += OnRendering;
        }

        private void StopAnimation()
        {
            _isAnimating = false;
            CompositionTarget.Rendering -= OnRendering;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            // Calculate the difference between current and target
            double diff = _targetVerticalOffset - _currentVerticalOffset;
            
            // If we're close enough, snap to target and stop
            if (Math.Abs(diff) < StopThreshold)
            {
                _currentVerticalOffset = _targetVerticalOffset;
                _scrollViewer.ScrollToVerticalOffset(_targetVerticalOffset);
                StopAnimation();
                return;
            }
            
            // Lerp (Linear interpolation) for smooth movement
            // Each frame, move a percentage of the remaining distance
            _currentVerticalOffset += diff * SmoothFactor;
            
            // Apply the scroll
            _scrollViewer.ScrollToVerticalOffset(_currentVerticalOffset);
        }

        /// <summary>
        /// Detaches the behavior from the ScrollViewer.
        /// </summary>
        public void Detach()
        {
            StopAnimation();
            _scrollViewer.PreviewMouseWheel -= OnPreviewMouseWheel;
            _scrollViewer.ScrollChanged -= OnScrollChanged;
        }

        /// <summary>
        /// Attaches smooth scrolling behavior to a ScrollViewer.
        /// </summary>
        public static SmoothScrollBehavior Attach(ScrollViewer scrollViewer)
        {
            return new SmoothScrollBehavior(scrollViewer);
        }
    }
}
