using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using WindowBinTracker.Interfaces;

namespace WindowBinTracker.Services
{
    public class RecycleBinService : IRecycleBinService, IDisposable
    {
        private readonly ILogger<RecycleBinService> _logger;
        private readonly RecycleBinConfiguration _config;
        private System.Threading.Timer? _monitorTimer;
        private bool _isMonitoring;
        private bool _disposed;

        public event EventHandler<RecycleBinSizeEventArgs>? SizeThresholdReached;

        public bool IsMonitoring => _isMonitoring;

        public RecycleBinService(
            ILogger<RecycleBinService> logger,
            IOptions<RecycleBinConfiguration> config)
        {
            _logger = logger;
            _config = config.Value;
        }

        public async Task<long> GetRecycleBinSizeAsync()
        {
            long totalSize = 0;

            try
            {
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (drive.IsReady && drive.DriveType == DriveType.Fixed)
                    {
                        long size = await GetRecycleBinSizeForDriveAsync(drive.Name);
                        totalSize += size;
                        _logger.LogInformation($"Recycle bin size for {drive.Name}: {FormatBytes(size)}");
                    }
                }

                _logger.LogInformation($"Total recycle bin size: {FormatBytes(totalSize)}");
                return totalSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recycle bin size");
                return 0;
            }
        }

        private async Task<long> GetRecycleBinSizeForDriveAsync(string drivePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Try the modern Windows 10/11 path first
                    string recycleBinPath = Path.Combine(drivePath, "$Recycle.Bin");
                    if (!Directory.Exists(recycleBinPath))
                    {
                        // Fallback to older Windows versions
                        recycleBinPath = Path.Combine(drivePath, "RECYCLER");
                        if (!Directory.Exists(recycleBinPath))
                        {
                            _logger.LogInformation($"No recycle bin found on drive {drivePath}");
                            return 0;
                        }
                    }

                    long size = 0;
                    
                    // Get all user-specific recycle bin directories
                    var directories = Directory.GetDirectories(recycleBinPath);
                    _logger.LogInformation($"Found {directories.Length} recycle bin directories in {recycleBinPath}");
                    
                    foreach (string dir in directories)
                    {
                        try
                        {
                            // Skip system directories that require elevated access
                            string dirName = Path.GetFileName(dir);
                            if (dirName == "S-1-5-18")
                            {
                                _logger.LogInformation($"Skipping system recycle bin directory: {dirName}");
                                continue;
                            }

                            if (Directory.Exists(dir))
                            {
                                _logger.LogInformation($"Processing user recycle bin directory: {dirName}");
                                long dirSize = GetDirectorySize(dir);
                                if (dirSize > 0)
                                {
                                    size += dirSize;
                                    _logger.LogInformation($"User recycle bin {dirName}: {FormatBytes(dirSize)}");
                                }
                                else
                                {
                                    _logger.LogInformation($"User recycle bin {dirName} is empty");
                                }
                            }
                        }
                        catch (UnauthorizedAccessException uaex)
                        {
                            _logger.LogInformation($"Access denied to recycle bin directory: {Path.GetFileName(dir)} - {uaex.Message}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"Error processing recycle bin directory: {dir}");
                        }
                    }

                    return size;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error getting recycle bin size for drive {drivePath}");
                    return 0;
                }
            });
        }

        private long GetDirectorySize(string path)
        {
            long size = 0;
            try
            {
                // Get files in current directory
                string[] files;
                try
                {
                    files = Directory.GetFiles(path);
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogDebug($"Access denied to files in directory: {path}");
                    return 0;
                }

                foreach (string file in files)
                {
                    try
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        size += fileInfo.Length;
                    }
                    catch (FileNotFoundException)
                    {
                        // File might have been deleted during enumeration
                        continue;
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _logger.LogDebug($"Access denied to file: {file}");
                        continue;
                    }
                }

                // Get subdirectories
                string[] subDirs;
                try
                {
                    subDirs = Directory.GetDirectories(path);
                }
                catch (UnauthorizedAccessException)
                {
                    _logger.LogDebug($"Access denied to subdirectories in: {path}");
                    return size; // Return size of accessible files only
                }

                foreach (string subDir in subDirs)
                {
                    try
                    {
                        size += GetDirectorySize(subDir);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _logger.LogDebug($"Access denied to subdirectory: {subDir}");
                        continue;
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                _logger.LogDebug($"Access denied when calculating size for: {path}");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Error calculating directory size for: {path}");
            }
            return size;
        }

        public async Task StartMonitoringAsync()
        {
            if (_isMonitoring)
            {
                _logger.LogWarning("Recycle bin monitoring is already running");
                return;
            }

            _isMonitoring = true;
            _logger.LogInformation($"Starting recycle bin monitoring with threshold: {FormatBytes(_config.SizeThresholdBytes)}");

            _monitorTimer = new System.Threading.Timer(async _ => await CheckRecycleBinSize(), 
                null, TimeSpan.Zero, TimeSpan.FromMilliseconds(_config.CheckIntervalMs));

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
            _monitorTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _monitorTimer?.Dispose();
            _monitorTimer = null;

            _logger.LogInformation("Recycle bin monitoring stopped");
            await Task.CompletedTask;
        }

        private async Task CheckRecycleBinSize()
        {
            try
            {
                long currentSize = await GetRecycleBinSizeAsync();
                
                // Log current size for monitoring
                _logger.LogInformation($"Recycle bin size: {FormatBytes(currentSize)} (threshold: {FormatBytes(_config.SizeThresholdBytes)})");

                if (currentSize >= _config.SizeThresholdBytes)
                {
                    _logger.LogWarning($"Recycle bin size threshold reached: {FormatBytes(currentSize)} >= {FormatBytes(_config.SizeThresholdBytes)}");

                    var args = new RecycleBinSizeEventArgs
                    {
                        CurrentSize = currentSize,
                        Threshold = _config.SizeThresholdBytes,
                        DrivePath = "All Drives"
                    };

                    OnSizeThresholdReached(args);
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

    public class RecycleBinConfiguration
    {
        public long SizeThresholdBytes { get; set; } = 1073741824; // 1 GB default
        public int CheckIntervalMs { get; set; } = 30000; // 30 seconds default
    }
}
