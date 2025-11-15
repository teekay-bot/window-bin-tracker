using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WindowNativeTemplate.Interfaces
{
    public interface IWindowService
    {
        Task<IEnumerable<WindowInfo>> GetActiveWindowsAsync();
        Task<WindowInfo?> GetForegroundWindowAsync();
        Task<bool> SetForegroundWindowAsync(IntPtr hWnd);
        Task<bool> MinimizeWindowAsync(IntPtr hWnd);
        Task<bool> MaximizeWindowAsync(IntPtr hWnd);
        Task<bool> RestoreWindowAsync(IntPtr hWnd);
        Task<bool> CloseWindowAsync(IntPtr hWnd);
        Task StartMonitoringAsync();
        Task StopMonitoringAsync();
        event EventHandler<WindowChangedEventArgs>? WindowChanged;
    }

    public class WindowInfo
    {
        public IntPtr Handle { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProcessName { get; set; } = string.Empty;
        public int ProcessId { get; set; }
        public bool IsVisible { get; set; }
        public System.Drawing.Rectangle Rectangle { get; set; }
        public DateTime LastActive { get; set; }
    }

    public class WindowChangedEventArgs : EventArgs
    {
        public WindowInfo? PreviousWindow { get; set; }
        public WindowInfo? CurrentWindow { get; set; }
        public DateTime ChangeTime { get; set; }
    }
}
