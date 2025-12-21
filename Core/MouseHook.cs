using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScrollV.Core
{
    /// <summary>
    /// Low-level mouse hook to intercept scroll events system-wide
    /// </summary>
    public class MouseHook : IDisposable
    {
        #region Win32 API

        private const int WH_MOUSE_LL = 14;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_MOUSEHWHEEL = 0x020E;

        // Flag to identify injected events
        private static readonly UIntPtr SMOOTH_SCROLL_SIGNATURE = new UIntPtr(0x53534D53); // "SSMS"

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public POINT pt;
            public uint mouseData;
            public uint flags;
            public uint time;
            public IntPtr dwExtraInfo;
        }

        private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, System.Text.StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr WindowFromPoint(POINT Point);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private const int VK_CONTROL = 0x11;

        #endregion

        private IntPtr _hookId = IntPtr.Zero;
        private readonly LowLevelMouseProc _proc;
        private bool _isHooked = false;
        private bool _disposed = false;
        
        // Cache for process names to avoid expensive lookups
        private static readonly ConcurrentDictionary<uint, (string Name, DateTime CachedAt)> _processCache = new();
        private static readonly TimeSpan CacheExpiry = TimeSpan.FromSeconds(30);

        public event EventHandler<MouseScrollEventArgs>? MouseScroll;

        public bool IsEnabled { get; set; } = true;
        public bool IsPaused { get; set; } = false;

        // Signature to identify our own injected scroll events
        public static UIntPtr ScrollSignature => SMOOTH_SCROLL_SIGNATURE;

        public MouseHook()
        {
            _proc = HookCallback;
        }

        public void Start()
        {
            if (_isHooked) return;

            using (var curProcess = Process.GetCurrentProcess())
            using (var curModule = curProcess.MainModule!)
            {
                _hookId = SetWindowsHookEx(WH_MOUSE_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }

            if (_hookId == IntPtr.Zero)
            {
                throw new InvalidOperationException("Failed to install mouse hook");
            }

            _isHooked = true;
            IsPaused = false;
        }

        public void Stop()
        {
            if (!_isHooked) return;

            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _isHooked = false;
        }

        public void Pause()
        {
            IsPaused = true;
        }

        public void Resume()
        {
            IsPaused = false;
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // Fast path: if paused or disabled, pass through immediately
            if (IsPaused || !IsEnabled || nCode < 0)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            int msg = wParam.ToInt32();
            
            // Only process scroll events
            if (msg != WM_MOUSEWHEEL && msg != WM_MOUSEHWHEEL)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

            // Fast path: check if this is our own injected scroll event
            if (hookStruct.dwExtraInfo == (IntPtr)SMOOTH_SCROLL_SIGNATURE)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // Skip smooth scroll when Ctrl is held (zoom functionality)
            if ((GetAsyncKeyState(VK_CONTROL) & 0x8000) != 0)
            {
                return CallNextHookEx(_hookId, nCode, wParam, lParam);
            }

            // Extract scroll delta (hiword of mouseData)
            short delta = (short)(hookStruct.mouseData >> 16);
            bool isHorizontal = msg == WM_MOUSEHWHEEL;

            // Get the window under cursor
            IntPtr windowUnderCursor = WindowFromPoint(hookStruct.pt);

            // Get process info with caching
            GetWindowThreadProcessId(windowUnderCursor, out uint processId);
            string processName = GetProcessNameCached(processId);

            var args = new MouseScrollEventArgs
            {
                Delta = delta,
                IsHorizontal = isHorizontal,
                X = hookStruct.pt.x,
                Y = hookStruct.pt.y,
                WindowHandle = windowUnderCursor,
                ProcessId = processId,
                ProcessName = processName
            };

            // Raise event
            MouseScroll?.Invoke(this, args);

            // If handled, suppress the original scroll
            if (args.Handled)
            {
                return (IntPtr)1;
            }

            return CallNextHookEx(_hookId, nCode, wParam, lParam);
        }

        private static string GetProcessNameCached(uint processId)
        {
            if (processId == 0) return "Idle";

            // Check cache first
            if (_processCache.TryGetValue(processId, out var cached))
            {
                if (DateTime.Now - cached.CachedAt < CacheExpiry)
                {
                    return cached.Name;
                }
            }

            // Fast Win32 lookup instead of Process.GetProcessById
            string name = GetProcessNameWin32(processId);

            // Cache the result
            _processCache[processId] = (name, DateTime.Now);
            
            // Clean old cache entries periodically
            if (_processCache.Count > 100)
            {
                CleanCache();
            }

            return name;
        }

        private static string GetProcessNameWin32(uint processId)
        {
            IntPtr hProcess = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess != IntPtr.Zero)
            {
                try
                {
                    var sb = new System.Text.StringBuilder(1024);
                    int size = sb.Capacity;
                    if (QueryFullProcessImageName(hProcess, 0, sb, ref size))
                    {
                        string fullPath = sb.ToString();
                        return System.IO.Path.GetFileNameWithoutExtension(fullPath);
                    }
                }
                finally
                {
                    CloseHandle(hProcess);
                }
            }
            return "Unknown";
        }

        private static void CleanCache()
        {
            var now = DateTime.Now;
            foreach (var kvp in _processCache)
            {
                if (now - kvp.Value.CachedAt > CacheExpiry)
                {
                    _processCache.TryRemove(kvp.Key, out _);
                }
            }
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
                    Stop();
                }
                _disposed = true;
            }
        }

        ~MouseHook()
        {
            Dispose(false);
        }
    }

    public class MouseScrollEventArgs : EventArgs
    {
        public short Delta { get; set; }
        public bool IsHorizontal { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public IntPtr WindowHandle { get; set; }
        public uint ProcessId { get; set; }
        public string ProcessName { get; set; } = string.Empty;
        public bool Handled { get; set; }
    }
}
