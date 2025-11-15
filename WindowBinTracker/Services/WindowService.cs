using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WindowNativeTemplate.Interfaces;

namespace WindowNativeTemplate.Services
{
    public class WindowService : IWindowService, IDisposable
    {
        private readonly ILogger<WindowService> _logger;
        private readonly Timer? _monitorTimer;
        private IntPtr _lastForegroundWindow = IntPtr.Zero;
        private bool _isMonitoring;

        public event EventHandler<WindowChangedEventArgs>? WindowChanged;

        // Windows API imports
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

        [DllImport("user32.dll")]
        private static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        // Constants
        private const int SW_MINIMIZE = 6;
        private const int SW_MAXIMIZE = 3;
        private const int SW_RESTORE = 9;
        private const uint WM_CLOSE = 0x0010;

        public WindowService(ILogger<WindowService> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<WindowInfo>> GetActiveWindowsAsync()
        {
            return await Task.Run(() =>
            {
                var windows = new List<WindowInfo>();
                EnumWindows((hWnd, lParam) =>
                {
                    if (IsWindowVisible(hWnd))
                    {
                        var windowInfo = GetWindowInfo(hWnd);
                        if (windowInfo != null)
                        {
                            windows.Add(windowInfo);
                        }
                    }
                    return true;
                }, IntPtr.Zero);

                return windows;
            });
        }

        public async Task<WindowInfo?> GetForegroundWindowAsync()
        {
            return await Task.Run(() =>
            {
                var hWnd = GetForegroundWindow();
                return GetWindowInfo(hWnd);
            });
        }

        public async Task<bool> SetForegroundWindowAsync(IntPtr hWnd)
        {
            return await Task.Run(() => SetForegroundWindow(hWnd));
        }

        public async Task<bool> MinimizeWindowAsync(IntPtr hWnd)
        {
            return await Task.Run(() => ShowWindow(hWnd, SW_MINIMIZE));
        }

        public async Task<bool> MaximizeWindowAsync(IntPtr hWnd)
        {
            return await Task.Run(() => ShowWindow(hWnd, SW_MAXIMIZE));
        }

        public async Task<bool> RestoreWindowAsync(IntPtr hWnd)
        {
            return await Task.Run(() => ShowWindow(hWnd, SW_RESTORE));
        }

        public async Task<bool> CloseWindowAsync(IntPtr hWnd)
        {
            return await Task.Run(() => PostMessage(hWnd, WM_CLOSE, IntPtr.Zero, IntPtr.Zero));
        }

        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring) return;

            _isMonitoring = true;
            _logger.LogInformation("Starting window monitoring");

            _monitorTimer?.Dispose();
            _monitorTimer = new Timer(CheckWindowChange, null, 0, 500);

            await Task.CompletedTask;
        }

        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring) return;

            _isMonitoring = false;
            _logger.LogInformation("Stopping window monitoring");

            _monitorTimer?.Dispose();
            await Task.CompletedTask;
        }

        private void CheckWindowChange(object? state)
        {
            try
            {
                var currentWindow = GetForegroundWindow();
                if (currentWindow != _lastForegroundWindow)
                {
                    var previousInfo = GetWindowInfo(_lastForegroundWindow);
                    var currentInfo = GetWindowInfo(currentWindow);

                    _lastForegroundWindow = currentWindow;

                    WindowChanged?.Invoke(this, new WindowChangedEventArgs
                    {
                        PreviousWindow = previousInfo,
                        CurrentWindow = currentInfo,
                        ChangeTime = DateTime.UtcNow
                    });

                    if (currentInfo != null)
                    {
                        _logger.LogDebug("Window changed to: {Title} ({ProcessName})", 
                            currentInfo.Title, currentInfo.ProcessName);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking window change");
            }
        }

        private WindowInfo? GetWindowInfo(IntPtr hWnd)
        {
            try
            {
                if (hWnd == IntPtr.Zero || !IsWindowVisible(hWnd))
                    return null;

                var sb = new StringBuilder(256);
                GetWindowText(hWnd, sb, sb.Capacity);

                GetWindowThreadProcessId(hWnd, out uint processId);
                var process = Process.GetProcessById((int)processId);

                GetWindowRect(hWnd, out RECT rect);

                return new WindowInfo
                {
                    Handle = hWnd,
                    Title = sb.ToString(),
                    ProcessName = process.ProcessName,
                    ProcessId = (int)processId,
                    IsVisible = IsWindowVisible(hWnd),
                    Rectangle = new System.Drawing.Rectangle(rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top),
                    LastActive = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Failed to get window info for handle {Handle}", hWnd);
                return null;
            }
        }

        public void Dispose()
        {
            _monitorTimer?.Dispose();
        }
    }
}
