using System;
using System.Threading.Tasks;

namespace WindowBinTracker.Interfaces
{
    public interface IRecycleBinService
    {
        event EventHandler<RecycleBinSizeEventArgs>? SizeThresholdReached;
        
        Task<long> GetRecycleBinSizeAsync();
        Task StartMonitoringAsync();
        Task StopMonitoringAsync();
        Task UpdateThresholdAsync(long thresholdBytes, TimeSpan checkInterval);
        bool IsMonitoring { get; }
    }

    public class RecycleBinSizeEventArgs : EventArgs
    {
        public long CurrentSize { get; set; }
        public long Threshold { get; set; }
        public string DrivePath { get; set; } = string.Empty;
    }
}
