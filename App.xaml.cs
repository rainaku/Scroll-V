using Microsoft.Win32;
using SmoothScroll.Core;
using System.Linq;
using System.Windows;

namespace SmoothScroll
{
    public partial class App : Application
    {
        private SmoothScrollManager? _manager;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize manager
            _manager = new SmoothScrollManager();

            // Start smooth scrolling immediately
            _manager.Start();

            // Check if started with --show flag (user explicitly opened)
            bool showWindow = e.Args.Contains("--show");

            if (!showWindow)
            {
                // Start hidden - don't show main window
                // MainWindow will be created but hidden
                var mainWindow = new MainWindow();
                mainWindow.Hide();
                MainWindow = mainWindow;
            }
            // If --show flag, let StartupUri handle it normally
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _manager?.Dispose();
            base.OnExit(e);
        }

        public SmoothScrollManager? GetManager() => _manager;

        public static void SetStartWithWindows(bool enable)
        {
            const string appName = "SmoothScroll";
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? "";

            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
                if (key != null)
                {
                    if (enable)
                    {
                        // Start without --show flag = hidden mode
                        key.SetValue(appName, $"\"{exePath}\"");
                    }
                    else
                    {
                        key.DeleteValue(appName, false);
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }
        }

        public static bool IsStartWithWindowsEnabled()
        {
            const string appName = "SmoothScroll";
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);
                return key?.GetValue(appName) != null;
            }
            catch
            {
                return false;
            }
        }
    }
}
