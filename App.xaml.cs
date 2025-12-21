using Microsoft.Win32;
using SmoothScroll.Core;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;

namespace SmoothScroll
{
    public partial class App : Application
    {
        private SmoothScrollManager? _manager;
        private DispatcherTimer? _memoryTimer;

        // Win32 API for memory management
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr hProcess, IntPtr dwMinimumWorkingSetSize, IntPtr dwMaximumWorkingSetSize);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Initialize manager
            _manager = new SmoothScrollManager();

            // Start smooth scrolling immediately
            _manager.Start();

            // Setup memory optimization timer (every 60 seconds)
            _memoryTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(60)
            };
            _memoryTimer.Tick += (s, args) => OptimizeMemory();
            _memoryTimer.Start();

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
                // Optimize memory when starting hidden
                OptimizeMemory();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _memoryTimer?.Stop();
            _manager?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// Optimizes memory usage by trimming working set and requesting GC
        /// </summary>
        public static void OptimizeMemory()
        {
            try
            {
                // Request garbage collection
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Optimized, false);
                GC.WaitForPendingFinalizers();
                
                // Trim working set (reduce physical memory usage)
                SetProcessWorkingSetSize(GetCurrentProcess(), (IntPtr)(-1), (IntPtr)(-1));
            }
            catch
            {
                // Ignore memory optimization errors
            }
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
