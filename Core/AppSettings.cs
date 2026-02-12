using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ScrollV.Core
{
    /// <summary>
    /// Application settings with per-app customization support
    /// </summary>
    public class AppSettings
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Scroll-V",
            "settings.json"
        );

        // Global settings
        public bool Enabled { get; set; } = true;
        public bool StartWithWindows { get; set; } = true;
        public bool StartMinimized { get; set; } = true;
        public bool ShowNotifications { get; set; } = true;
        public string Language { get; set; } = "vi"; // vi or en

        // Scroll settings
        public double SmoothnessFactor { get; set; } = 0.05; // Very Smooth
        public double AccelerationFactor { get; set; } = 1.2;
        public double FrictionFactor { get; set; } = 0.97; // Very High Momentum
        public double MomentumFactor { get; set; } = 3.2; // Strong Glide (1.0 - 5.0)
        public EasingType EasingFunction { get; set; } = EasingType.EaseOutQuad;
        public bool UseAcceleration { get; set; } = true;
        public double ScrollMultiplier { get; set; } = 1.4; // 1.4x Speed

        // Per-application settings
        public Dictionary<string, AppSpecificSettings> PerAppSettings { get; set; } = new();

        // Excluded applications
        // Note: IDM (Internet Download Manager) has its own mouse hook that conflicts with smooth scroll
        public List<string> ExcludedApps { get; set; } = new()
        {
            "vlc",
            "mpc-hc64",
            "mpc-hc",
            "PotPlayerMini64",
            "photoshop",
            "illustrator",
            "idman",           // IDM main process
            "IDMan",           // IDM alternate casing
            "idmsettings",     // IDM settings
            "IEMonitor"        // IDM IE Monitor
        };

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }

            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string? directory = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string json = JsonSerializer.Serialize(this, JsonOptions);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }

        public bool IsAppExcluded(string processName)
        {
            return ExcludedApps.Contains(processName.ToLowerInvariant());
        }

        public AppSpecificSettings? GetAppSettings(string processName)
        {
            if (PerAppSettings.TryGetValue(processName.ToLowerInvariant(), out var settings))
            {
                return settings;
            }
            return null;
        }

        public void SetAppSettings(string processName, AppSpecificSettings settings)
        {
            PerAppSettings[processName.ToLowerInvariant()] = settings;
        }

        public void AddExcludedApp(string processName)
        {
            string name = processName.ToLowerInvariant();
            if (!ExcludedApps.Contains(name))
            {
                ExcludedApps.Add(name);
            }
        }

        public void RemoveExcludedApp(string processName)
        {
            ExcludedApps.Remove(processName.ToLowerInvariant());
        }
    }

    public class AppSpecificSettings
    {
        public bool Enabled { get; set; } = true;
        public double? SmoothnessFactor { get; set; }
        public double? AccelerationFactor { get; set; }
        public double? ScrollMultiplier { get; set; }
        public EasingType? EasingFunction { get; set; }
    }
}
