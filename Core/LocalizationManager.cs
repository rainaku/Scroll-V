using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace SmoothScroll.Core
{
    public class LocalizationManager : INotifyPropertyChanged
    {
        private static LocalizationManager? _instance;
        public static LocalizationManager Instance => _instance ??= new LocalizationManager();

        private string _currentLanguage = "vi";
        public string CurrentLanguage
        {
            get => _currentLanguage;
            set
            {
                if (_currentLanguage != value)
                {
                    _currentLanguage = value;
                    OnPropertyChanged(nameof(CurrentLanguage));
                    LanguageChanged?.Invoke(this, value);
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event EventHandler<string>? LanguageChanged;

        private readonly Dictionary<string, Dictionary<string, string>> _strings = new()
        {
            ["vi"] = new Dictionary<string, string>
            {
                // Section titles
                ["Settings"] = "Cài đặt",
                ["Tuning"] = "Tinh chỉnh",
                ["Exceptions"] = "Ngoại lệ",

                // Settings
                ["EnableSmooth"] = "Bật cuộn mượt",
                ["EnableSmoothDesc"] = "Áp dụng cho tất cả ứng dụng",
                ["StartWithWindows"] = "Khởi động cùng Windows",
                ["StartWithWindowsDesc"] = "Tự động chạy khi bật máy",
                ["ScrollAcceleration"] = "Tăng tốc cuộn",
                ["ScrollAccelerationDesc"] = "Cuộn nhanh hơn khi lướt liên tục",

                // Tuning
                ["Smoothness"] = "Độ mượt",
                ["ScrollSpeed"] = "Tốc độ cuộn",
                ["Momentum"] = "Quán tính",
                ["Glide"] = "Độ trôi",
                ["Animation"] = "Hiệu ứng",

                // Values
                ["VerySmooth"] = "Rất mượt",
                ["Smooth"] = "Mượt",
                ["Medium"] = "Trung bình",
                ["Fast"] = "Nhanh",
                ["Instant"] = "Tức thì",
                ["Low"] = "Thấp",
                ["High"] = "Cao",
                ["VeryHigh"] = "Rất cao",
                ["Subtle"] = "Nhẹ",
                ["Strong"] = "Mạnh",
                ["VeryStrong"] = "Rất mạnh",
                ["Maximum"] = "Tối đa",

                // Exceptions
                ["ExcludedApps"] = "Ứng dụng loại trừ",
                ["NoAppsExcluded"] = "Không có ứng dụng nào",
                ["AppsExcluded"] = "{0} ứng dụng được loại trừ",
                ["Manage"] = "Quản lý",

                // Running Apps Dialog
                ["RunningApps"] = "Ứng dụng đang chạy",
                ["SearchApps"] = "Tìm kiếm ứng dụng...",
                ["ToggleExcludeHint"] = "Bật/tắt để loại trừ ứng dụng khỏi cuộn mượt",

                // Footer
                ["MadeBy"] = "Thực hiện bởi rainaku",
            },
            ["en"] = new Dictionary<string, string>
            {
                // Section titles
                ["Settings"] = "Settings",
                ["Tuning"] = "Tuning",
                ["Exceptions"] = "Exceptions",

                // Settings
                ["EnableSmooth"] = "Enable Smooth Scroll",
                ["EnableSmoothDesc"] = "Apply to all applications",
                ["StartWithWindows"] = "Start with Windows",
                ["StartWithWindowsDesc"] = "Auto-start on boot",
                ["ScrollAcceleration"] = "Scroll Acceleration",
                ["ScrollAccelerationDesc"] = "Scroll faster on continuous input",

                // Tuning
                ["Smoothness"] = "Smoothness",
                ["ScrollSpeed"] = "Scroll Speed",
                ["Momentum"] = "Momentum",
                ["Glide"] = "Glide",
                ["Animation"] = "Animation",

                // Values
                ["VerySmooth"] = "Very Smooth",
                ["Smooth"] = "Smooth",
                ["Medium"] = "Medium",
                ["Fast"] = "Fast",
                ["Instant"] = "Instant",
                ["Low"] = "Low",
                ["High"] = "High",
                ["VeryHigh"] = "Very High",
                ["Subtle"] = "Subtle",
                ["Strong"] = "Strong",
                ["VeryStrong"] = "Very Strong",
                ["Maximum"] = "Maximum",

                // Exceptions
                ["ExcludedApps"] = "Excluded Apps",
                ["NoAppsExcluded"] = "No apps excluded",
                ["AppsExcluded"] = "{0} app(s) excluded",
                ["Manage"] = "Manage",

                // Running Apps Dialog
                ["RunningApps"] = "Running Apps",
                ["SearchApps"] = "Search apps...",
                ["ToggleExcludeHint"] = "Toggle to exclude apps from smooth scrolling",

                // Footer
                ["MadeBy"] = "Made by rainaku",
            }
        };

        public string Get(string key)
        {
            if (_strings.TryGetValue(_currentLanguage, out var langStrings))
            {
                if (langStrings.TryGetValue(key, out var value))
                    return value;
            }
            return key;
        }

        public string this[string key] => Get(key);

        public void ToggleLanguage()
        {
            CurrentLanguage = CurrentLanguage == "vi" ? "en" : "vi";
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
