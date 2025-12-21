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

            // Create the main window
            var mainWindow = new MainWindow();
            MainWindow = mainWindow;

            if (showWindow)
            {
                // User explicitly opened - show the window
                mainWindow.Show();
            }
            else
            {
                // Start hidden - run in system tray
                mainWindow.Hide();
            }
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
