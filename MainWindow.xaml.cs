using ScrollV.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ScrollV
{
    public partial class MainWindow : Window
    {
        private ScrollVManager? _manager;
        private bool _isRunning = true;
        private LocalizationManager _loc = LocalizationManager.Instance;

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
                _manager = new ScrollVManager();
            }

            // Load settings
            LoadSettings();

            // Initialize localization
            _loc.CurrentLanguage = _manager.Settings.Language;
            _loc.LanguageChanged += OnLanguageChanged;
            UpdateLanguageUI();

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
            GlideSlider.Value = settings.MomentumFactor * 100;

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
                ? _loc["NoAppsExcluded"]
                : string.Format(_loc["AppsExcluded"], count);
        }

        private void Manager_StatusChanged(object? sender, string status)
        {
            // Status description removed from UI
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
            }
            else
            {
                StatusIndicator.Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F59E0B"));
            }
        }

        #region Language

        private void FlagVN_Click(object sender, MouseButtonEventArgs e)
        {
            SetLanguage("vi");
        }

        private void FlagUS_Click(object sender, MouseButtonEventArgs e)
        {
            SetLanguage("en");
        }

        private void SetLanguage(string lang)
        {
            if (_manager == null) return;
            _loc.CurrentLanguage = lang;
            _manager.Settings.Language = lang;
            SaveSettings();
        }

        private void OnLanguageChanged(object? sender, string lang)
        {
            Dispatcher.Invoke(() =>
            {
                UpdateLanguageUI();
                UpdateFlagSelection();
            });
        }

        private void UpdateLanguageUI()
        {
            // Section titles
            LblSettings.Text = _loc["Settings"];
            LblTuning.Text = _loc["Tuning"];
            LblExceptions.Text = _loc["Exceptions"];

            // Settings
            LblEnableSmooth.Text = _loc["EnableSmooth"];
            LblEnableSmoothDesc.Text = _loc["EnableSmoothDesc"];
            LblStartWindows.Text = _loc["StartWithWindows"];
            LblStartWindowsDesc.Text = _loc["StartWithWindowsDesc"];
            LblAcceleration.Text = _loc["ScrollAcceleration"];
            LblAccelerationDesc.Text = _loc["ScrollAccelerationDesc"];

            // Tuning
            LblSmoothness.Text = _loc["Smoothness"];
            LblSpeed.Text = _loc["ScrollSpeed"];
            LblMomentum.Text = _loc["Momentum"];
            LblGlide.Text = _loc["Glide"];
            LblAnimation.Text = _loc["Animation"];

            // Exceptions
            LblExcludedApps.Text = _loc["ExcludedApps"];
            BrowseAppsButton.Content = _loc["Manage"];

            // Footer
            LblMadeBy.Text = _loc["MadeBy"];

            // Update dynamic values
            RefreshExcludedAppsList();
            UpdateSliderLabels();
            UpdateFlagSelection();
        }

        private void UpdateFlagSelection()
        {
            var activeColor = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B5CF6"));
            var inactiveColor = new SolidColorBrush(Colors.Transparent);
            
            FlagVN.BorderBrush = _loc.CurrentLanguage == "vi" ? activeColor : inactiveColor;
            FlagUS.BorderBrush = _loc.CurrentLanguage == "en" ? activeColor : inactiveColor;
        }

        private void UpdateSliderLabels()
        {
            // Force update slider labels with current values
            SmoothnessSlider_ValueChanged(SmoothnessSlider, new RoutedPropertyChangedEventArgs<double>(0, SmoothnessSlider.Value));
            FrictionSlider_ValueChanged(FrictionSlider, new RoutedPropertyChangedEventArgs<double>(0, FrictionSlider.Value));
            GlideSlider_ValueChanged(GlideSlider, new RoutedPropertyChangedEventArgs<double>(0, GlideSlider.Value));
        }

        #endregion

        #region Social Links

        private void SocialFacebook_Click(object sender, MouseButtonEventArgs e)
        {
            OpenUrl("https://www.facebook.com/rain.107/");
        }

        private void SocialGithub_Click(object sender, MouseButtonEventArgs e)
        {
            OpenUrl("https://github.com/rainaku/Scroll-V");
        }

        private void SocialWebsite_Click(object sender, MouseButtonEventArgs e)
        {
            OpenUrl("https://rainaku.id.vn");
        }

        private void OpenUrl(string url)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch { }
        }

        #endregion

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
            if (value < 0.08) SmoothnessValue.Text = _loc["VerySmooth"];
            else if (value < 0.12) SmoothnessValue.Text = _loc["Smooth"];
            else if (value < 0.18) SmoothnessValue.Text = _loc["Medium"];
            else if (value < 0.25) SmoothnessValue.Text = _loc["Fast"];
            else SmoothnessValue.Text = _loc["Instant"];

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

            if (value < 0.85) FrictionValue.Text = _loc["Low"];
            else if (value < 0.90) FrictionValue.Text = _loc["Medium"];
            else if (value < 0.95) FrictionValue.Text = _loc["High"];
            else FrictionValue.Text = _loc["VeryHigh"];

            SaveSettings();
        }

        private void GlideSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_manager == null || GlideValue == null) return;

            double value = GlideSlider.Value / 100.0; // 1.0 - 5.0
            _manager.Settings.MomentumFactor = value;
            _manager.ApplySettings();

            if (value < 1.5) GlideValue.Text = _loc["Subtle"];
            else if (value < 2.5) GlideValue.Text = _loc["Medium"];
            else if (value < 3.5) GlideValue.Text = _loc["Strong"];
            else if (value < 4.5) GlideValue.Text = _loc["VeryStrong"];
            else GlideValue.Text = _loc["Maximum"];

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
            
            // Optimize memory when hiding
            App.OptimizeMemory();
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