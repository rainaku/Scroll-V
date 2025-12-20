using SmoothScroll.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace SmoothScroll
{
    public partial class MainWindow : Window
    {
        private SmoothScrollManager? _manager;
        private bool _isRunning = true;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Get or create manager
            _manager = ((App)Application.Current).GetManager();
            if (_manager == null)
            {
                _manager = new SmoothScrollManager();
            }

            // Load settings
            LoadSettings();

            // Start the manager
            _manager.Start();
            _isRunning = true;

            // Subscribe to events
            _manager.StatusChanged += Manager_StatusChanged;
            _manager.ScrollActivity += Manager_ScrollActivity;

            UpdateUI();
        }

        private void LoadSettings()
        {
            if (_manager == null) return;

            var settings = _manager.Settings;
            EnableToggle.IsChecked = settings.Enabled;
            
            // Check registry for actual startup state
            StartupToggle.IsChecked = App.IsStartWithWindowsEnabled();
            AccelerationToggle.IsChecked = settings.UseAcceleration;

            SmoothnessSlider.Value = settings.SmoothnessFactor * 100;
            SpeedSlider.Value = settings.ScrollMultiplier * 100;
            FrictionSlider.Value = settings.FrictionFactor * 100;

            // Set easing combo box
            foreach (ComboBoxItem item in EasingComboBox.Items)
            {
                if (item.Tag?.ToString() == settings.EasingFunction.ToString())
                {
                    EasingComboBox.SelectedItem = item;
                    break;
                }
            }

            // Load excluded apps
            RefreshExcludedAppsList();
        }

        private void RefreshExcludedAppsList()
        {
            if (_manager == null) return;
            
            var count = _manager.Settings.ExcludedApps?.Count ?? 0;
            ExcludedAppsCount.Text = count == 0 
                ? "No apps excluded" 
                : $"{count} app{(count > 1 ? "s" : "")} excluded";
        }

        private void Manager_StatusChanged(object? sender, string status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusDescription.Text = status;
            });
        }

        private void Manager_ScrollActivity(object? sender, ScrollActivityEventArgs e)
        {
            // Could show activity indicator or log
        }

        private void UpdateUI()
        {
            if (_isRunning)
            {
                StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#10B981"));
                StatusText.Text = "Running";
                StatusDescription.Text = "SmoothScroll is active and working";
                ToggleButton.Content = "Pause";
            }
            else
            {
                StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
                StatusText.Text = "Paused";
                StatusDescription.Text = "SmoothScroll is paused";
                ToggleButton.Content = "Resume";
            }
        }

        #region Window Controls

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            HideWindow();
        }

        #endregion

        #region Settings Controls

        private void ToggleButton_Click(object sender, RoutedEventArgs e)
        {
            _manager?.Toggle();
            _isRunning = !_isRunning;
            UpdateUI();
        }

        private void EnableToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_manager == null) return;
            _manager.Settings.Enabled = EnableToggle.IsChecked == true;
            _manager.ApplySettings();
            SaveSettings();
        }

        private void StartupToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_manager == null) return;
            bool enabled = StartupToggle.IsChecked == true;
            _manager.Settings.StartWithWindows = enabled;
            App.SetStartWithWindows(enabled);
            SaveSettings();
        }

        private void AccelerationToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (_manager == null) return;
            _manager.Settings.UseAcceleration = AccelerationToggle.IsChecked == true;
            _manager.ApplySettings();
            SaveSettings();
        }

        private void SmoothnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_manager == null || SmoothnessValue == null) return;

            double value = SmoothnessSlider.Value / 100.0;
            _manager.Settings.SmoothnessFactor = value;
            _manager.ApplySettings();

            // Update label
            if (value < 0.08) SmoothnessValue.Text = "Very Smooth";
            else if (value < 0.12) SmoothnessValue.Text = "Smooth";
            else if (value < 0.18) SmoothnessValue.Text = "Medium";
            else if (value < 0.25) SmoothnessValue.Text = "Responsive";
            else SmoothnessValue.Text = "Instant";

            SaveSettings();
        }

        private void SpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_manager == null || SpeedValue == null) return;

            double value = SpeedSlider.Value / 100.0;
            // Set both ScrollMultiplier (base speed) and AccelerationFactor (rapid scroll boost)
            _manager.Settings.ScrollMultiplier = value;
            _manager.Settings.AccelerationFactor = 1.0 + (value - 1.0) * 0.5; // Proportional acceleration
            _manager.ApplySettings();

            SpeedValue.Text = $"{value:F1}x";
            SaveSettings();
        }

        private void FrictionSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_manager == null || FrictionValue == null) return;

            double value = FrictionSlider.Value / 100.0;
            _manager.Settings.FrictionFactor = value;
            _manager.ApplySettings();

            if (value < 0.85) FrictionValue.Text = "Low Momentum";
            else if (value < 0.90) FrictionValue.Text = "Medium";
            else if (value < 0.95) FrictionValue.Text = "High Momentum";
            else FrictionValue.Text = "Very High";

            SaveSettings();
        }

        private void EasingComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_manager == null || EasingComboBox.SelectedItem == null) return;

            var item = (ComboBoxItem)EasingComboBox.SelectedItem;
            if (Enum.TryParse<EasingType>(item.Tag?.ToString(), out var easing))
            {
                _manager.Settings.EasingFunction = easing;
                _manager.ApplySettings();
                SaveSettings();
            }
        }

        #endregion

        #region Per-App Settings

        private void BrowseAppsButton_Click(object sender, RoutedEventArgs e)
        {
            if (_manager == null) return;

            var dialog = new RunningAppsDialog(_manager.Settings);
            dialog.Owner = this;
            dialog.ShowDialog();
            
            // Refresh the count and save
            RefreshExcludedAppsList();
            SaveSettings();
        }

        #endregion

        #region System Tray

        private void TrayIcon_TrayMouseDoubleClick(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void TrayOpen_Click(object sender, RoutedEventArgs e)
        {
            ShowWindow();
        }

        private void ShowWindow()
        {
            Visibility = Visibility.Visible;
            Show();
            WindowState = WindowState.Normal;
            Activate();
            Topmost = true;  // Bring to front
            Topmost = false; // Allow other windows on top again
        }

        public void HideWindow()
        {
            Visibility = Visibility.Hidden;
            Hide();
        }

        private void TrayToggle_Click(object sender, RoutedEventArgs e)
        {
            _manager?.Toggle();
            _isRunning = !_isRunning;
            UpdateUI();
        }

        private void TrayExit_Click(object sender, RoutedEventArgs e)
        {
            TrayIcon.Dispose();
            _manager?.Dispose();
            Application.Current.Shutdown();
        }

        #endregion

        private void SaveSettings()
        {
            _manager?.SaveSettings();
        }

        protected override void OnClosed(EventArgs e)
        {
            TrayIcon.Dispose();
            base.OnClosed(e);
        }
    }
}