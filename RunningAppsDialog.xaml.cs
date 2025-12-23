using ScrollV.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScrollV
{
    public partial class RunningAppsDialog : Window
    {
        private readonly AppSettings _settings;
        private readonly LocalizationManager _loc = LocalizationManager.Instance;
        private List<RunningAppInfo> _allApps = new();
        private ObservableCollection<RunningAppInfo> _filteredApps = new();

        public RunningAppsDialog(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            UpdateLanguageUI();
            LoadRunningApps();
        }

        private void UpdateLanguageUI()
        {
            TitleText.Text = _loc["RunningApps"];
            SearchPlaceholder.Text = _loc["SearchApps"];
            HintText.Text = _loc["ToggleExcludeHint"];
        }

        private void LoadRunningApps()
        {
            _allApps.Clear();

            try
            {
                var processes = Process.GetProcesses()
                    .Where(p => !string.IsNullOrEmpty(p.MainWindowTitle) || IsCommonApp(p.ProcessName))
                    .GroupBy(p => p.ProcessName.ToLower())
                    .Select(g => g.First())
                    .OrderBy(p => p.ProcessName)
                    .ToList();

                foreach (var process in processes)
                {
                    try
                    {
                        var appInfo = new RunningAppInfo
                        {
                            ProcessName = process.ProcessName,
                            WindowTitle = string.IsNullOrEmpty(process.MainWindowTitle) 
                                ? "Background process" 
                                : process.MainWindowTitle,
                            IsEnabled = !_settings.IsAppExcluded(process.ProcessName)
                        };
                        _allApps.Add(appInfo);
                    }
                    catch
                    {
                        // Skip inaccessible processes
                    }
                }
            }
            catch
            {
                // Handle permission errors
            }

            UpdateFilteredList();
        }

        private bool IsCommonApp(string processName)
        {
            var commonApps = new[] { 
                "chrome", "firefox", "msedge", "opera", "brave",
                "explorer", "notepad", "code", "devenv", 
                "Discord", "Telegram", "Slack", "Teams",
                "spotify", "vlc", "mpc-hc64", "potplayer",
                "Word", "Excel", "PowerPoint", "Outlook"
            };
            return commonApps.Any(app => 
                processName.IndexOf(app, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private void UpdateFilteredList(string? searchText = null)
        {
            var filtered = _allApps.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(a => 
                    a.ProcessName.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    a.WindowTitle.Contains(searchText, StringComparison.OrdinalIgnoreCase));
            }

            _filteredApps = new ObservableCollection<RunningAppInfo>(filtered);
            AppsList.ItemsSource = _filteredApps;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string text = SearchBox.Text;
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(text) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
            UpdateFilteredList(text);
        }

        private void AppToggle_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.Tag is string processName)
            {
                // IsEnabled = smooth scroll works for this app
                // When OFF (unchecked), app is excluded from smooth scroll
                bool isEnabled = checkBox.IsChecked == true;
                
                if (isEnabled)
                {
                    // Remove from exclusion list - smooth scroll works
                    _settings.RemoveExcludedApp(processName);
                }
                else
                {
                    // Add to exclusion list - no smooth scroll
                    _settings.AddExcludedApp(processName);
                }

                // Update the app info
                var app = _allApps.FirstOrDefault(a => a.ProcessName == processName);
                if (app != null)
                {
                    app.IsEnabled = isEnabled;
                }
            }
        }

        private void Header_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }

    public class RunningAppInfo : INotifyPropertyChanged
    {
        private bool _isEnabled;

        public string ProcessName { get; set; } = string.Empty;
        public string WindowTitle { get; set; } = string.Empty;
        
        /// <summary>
        /// When true, smooth scroll is enabled for this app.
        /// When false, the app is excluded from smooth scrolling.
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
