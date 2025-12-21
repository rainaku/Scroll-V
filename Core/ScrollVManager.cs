using System;

namespace ScrollV.Core
{
    /// <summary>
    /// Main manager that coordinates mouse hook and scroll engine
    /// </summary>
    public class ScrollVManager : IDisposable
    {
        private readonly MouseHook _mouseHook;
        private readonly ScrollVEngine _scrollEngine;
        private readonly AppSettings _settings;
        private bool _disposed = false;
        private bool _isPaused = false;

        public event EventHandler<string>? StatusChanged;
        public event EventHandler<ScrollActivityEventArgs>? ScrollActivity;

        public bool IsRunning => !_isPaused;
        public bool IsHookInstalled { get; private set; }
        public AppSettings Settings => _settings;

        public ScrollVManager()
        {
            _settings = AppSettings.Load();
            _mouseHook = new MouseHook();
            _scrollEngine = new ScrollVEngine();

            ApplySettings();

            _mouseHook.MouseScroll += OnMouseScroll;
        }

        public void Start()
        {
            // Install hook if not already installed
            if (!IsHookInstalled)
            {
                try
                {
                    _mouseHook.Start();
                    IsHookInstalled = true;
                }
                catch (Exception ex)
                {
                    StatusChanged?.Invoke(this, $"Failed to start: {ex.Message}");
                    return;
                }
            }

            // Resume if paused
            _isPaused = false;
            _mouseHook.IsPaused = false;
            _scrollEngine.IsEnabled = true;
            StatusChanged?.Invoke(this, "Scroll-V is running");
        }

        public void Stop()
        {
            // Don't uninstall hook, just pause
            _isPaused = true;
            _mouseHook.IsPaused = true;
            _scrollEngine.IsEnabled = false;
            _scrollEngine.Reset();
            StatusChanged?.Invoke(this, "Scroll-V is paused");
        }

        public void Toggle()
        {
            if (_isPaused)
                Start();
            else
                Stop();
        }

        private void OnMouseScroll(object? sender, MouseScrollEventArgs e)
        {
            if (_isPaused) return;

            // Never apply smooth scroll to ourselves
            if (e.ProcessName.Equals("Scroll-V", StringComparison.OrdinalIgnoreCase))
            {
                return; // Let original scroll through
            }

            // Check if app is excluded
            if (_settings.IsAppExcluded(e.ProcessName))
            {
                return; // Let original scroll through
            }

            // Check if smooth scroll is enabled for this app
            var appSettings = _settings.GetAppSettings(e.ProcessName);
            if (appSettings != null && !appSettings.Enabled)
            {
                return;
            }

            // Apply per-app settings if available
            if (appSettings != null)
            {
                ApplyAppSpecificSettings(appSettings);
            }
            else
            {
                ApplySettings();
            }

            // Handle the scroll
            e.Handled = true;

            short adjustedDelta = (short)(e.Delta * _settings.ScrollMultiplier);
            _scrollEngine.AddScrollDelta(adjustedDelta, e.WindowHandle, e.IsHorizontal);

            // Notify about scroll activity
            ScrollActivity?.Invoke(this, new ScrollActivityEventArgs
            {
                ProcessName = e.ProcessName,
                Delta = e.Delta,
                Timestamp = DateTime.Now
            });
        }

        public void ApplySettings()
        {
            _scrollEngine.SmoothnessFactor = _settings.SmoothnessFactor;
            _scrollEngine.AccelerationFactor = _settings.AccelerationFactor;
            _scrollEngine.FrictionFactor = _settings.FrictionFactor;
            _scrollEngine.MomentumFactor = _settings.MomentumFactor;
            _scrollEngine.EasingFunction = _settings.EasingFunction;
            _scrollEngine.UseAcceleration = _settings.UseAcceleration;
            _mouseHook.IsEnabled = _settings.Enabled;
        }

        private void ApplyAppSpecificSettings(AppSpecificSettings appSettings)
        {
            _scrollEngine.SmoothnessFactor = appSettings.SmoothnessFactor ?? _settings.SmoothnessFactor;
            _scrollEngine.AccelerationFactor = appSettings.AccelerationFactor ?? _settings.AccelerationFactor;
            _scrollEngine.EasingFunction = appSettings.EasingFunction ?? _settings.EasingFunction;
        }

        public void SaveSettings()
        {
            _settings.Save();
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
                    _mouseHook.Stop();
                    _mouseHook.Dispose();
                    _scrollEngine.Dispose();
                    _settings.Save();
                }
                _disposed = true;
            }
        }
    }

    public class ScrollActivityEventArgs : EventArgs
    {
        public string ProcessName { get; set; } = string.Empty;
        public short Delta { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
