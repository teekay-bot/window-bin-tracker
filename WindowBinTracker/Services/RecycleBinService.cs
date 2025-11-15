using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading.Tasks;
using WindowBinTracker.Interfaces;

namespace WindowBinTracker.Services
{
    public class RecycleBinService : IRecycleBinService, IDisposable
    {
        private readonly ILogger<RecycleBinService> _logger;
        private long _sizeThresholdBytes = 1073741824; // 1 GB default
        private int _checkIntervalMs = 30000; // 30 seconds default
        private System.Threading.Timer? _monitorTimer;
        private bool _isMonitoring;
        private bool _disposed;

        public event EventHandler<RecycleBinSizeEventArgs>? SizeThresholdReached;

        public bool IsMonitoring => _isMonitoring;

        public RecycleBinService(
            ILogger<RecycleBinService> logger)
        {
            _logger = logger;
        }

        public async Task<long> GetRecycleBinSizeAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    long totalSize = 0;
                    
                    // Use Shell API to get recycle bin size
                    dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                    dynamic folder = shell.NameSpace(10); // 10 = Recycle Bin
                    
                    if (folder != null)
                    {
                        foreach (dynamic item in folder.Items())
                        {
                            try
                            {
                                totalSize += item.Size;
                                _logger.LogInformation($"Found item in recycle bin: {item.Name} - {FormatBytes(item.Size)}");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, $"Error getting size for item: {item.Name}");
                            }
                        }
                    }
                    
                    _logger.LogInformation($"Total recycle bin size: {FormatBytes(totalSize)}");
                    return totalSize;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error getting recycle bin size using Shell API");
                    return 0;
                }
            });
        }

        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Recycle bin monitoring is already running");
                return;
            }

            _isMonitoring = true;
            _logger.LogInformation($"Starting recycle bin monitoring with threshold: {FormatBytes(_sizeThresholdBytes)}");

            _monitorTimer = new System.Threading.Timer(async _ => await CheckRecycleBinSize(), 
                null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_checkIntervalMs));

            await Task.CompletedTask;
        }

        public async Task StopMonitoringAsync()
        {
            if (!_isMonitoring)
            {
                _logger.LogWarning("Recycle bin monitoring is not running");
                return;
            }

            _isMonitoring = false;
            _monitorTimer?.Dispose();
            _monitorTimer = null;
            _logger.LogInformation("Recycle bin monitoring stopped");

            await Task.CompletedTask;
        }

        public async Task UpdateThresholdAsync(long thresholdBytes, TimeSpan checkInterval)
        {
            _sizeThresholdBytes = thresholdBytes;
            _checkIntervalMs = (int)checkInterval.TotalMilliseconds;
            
            _logger.LogInformation($"Updated threshold to {FormatBytes(thresholdBytes)} and interval to {checkInterval.TotalSeconds}s");
            
            // If monitoring is active, restart with new settings
            if (_isMonitoring)
            {
                await StopMonitoringAsync();
                await StartMonitoringAsync();
            }
        }

        private async Task CheckRecycleBinSize()
        {
            try
            {
                _logger.LogInformation("CheckRecycleBinSize called");
                
                long currentSize = await GetRecycleBinSizeAsync();
                
                // Log current size for monitoring
                _logger.LogInformation($"Recycle bin size: {FormatBytes(currentSize)} (threshold: {FormatBytes(_sizeThresholdBytes)})");

                if (currentSize >= _sizeThresholdBytes)
                {
                    _logger.LogWarning($"Recycle bin size threshold reached: {FormatBytes(currentSize)} >= {FormatBytes(_sizeThresholdBytes)}");
                    _logger.LogInformation("Firing SizeThresholdReached event");

                    var args = new RecycleBinSizeEventArgs
                    {
                        CurrentSize = currentSize,
                        Threshold = _sizeThresholdBytes,
                        DrivePath = "All Drives"
                    };

                    OnSizeThresholdReached(args);
                    _logger.LogInformation("SizeThresholdReached event fired");
                }
                else
                {
                    _logger.LogInformation("Threshold not reached, no notification");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during recycle bin size check");
            }
        }

        protected virtual void OnSizeThresholdReached(RecycleBinSizeEventArgs e)
        {
            SizeThresholdReached?.Invoke(this, e);
        }
        
        private string FormatBytes(long bytes)
        {
            string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = bytes;

            while (Math.Round(number / 1024) >= 1)
            {
                number /= 1024;
                counter++;
            }

            return $"{number:n1} {suffixes[counter]}";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _monitorTimer?.Dispose();
                _disposed = true;
            }
        }
    }
}

