using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
using WindowBinTracker.Interfaces;
using WindowBinTracker.Services;

namespace WindowBinTracker.Services
{
    public class RecycleBinMonitorService : BackgroundService
    {
        private readonly ILogger<RecycleBinMonitorService> _logger;
        private readonly IRecycleBinService _recycleBinService;
        private readonly ISystemTrayService _systemTrayService;
        private readonly ISettingsService _settingsService;
        private System.Threading.Timer? _settingsTimer;
        private long _currentThreshold;
        private int _currentInterval;
        private bool _notificationsEnabled = true;

        public RecycleBinMonitorService(
            ILogger<RecycleBinMonitorService> logger,
            IRecycleBinService recycleBinService,
            ISystemTrayService systemTrayService,
            ISettingsService settingsService)
        {
            _logger = logger;
            _recycleBinService = recycleBinService;
            _systemTrayService = systemTrayService;
            _settingsService = settingsService;

            // Subscribe to threshold reached events
            _recycleBinService.SizeThresholdReached += OnSizeThresholdReached;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Recycle Bin Monitor Service starting");

            try
            {
                // Load initial settings
                await LoadSettingsAndUpdateThreshold();
                
                // Monitor for settings changes every 5 seconds
                _settingsTimer = new System.Threading.Timer(async _ => await LoadSettingsAndUpdateThreshold(),
                    null, TimeSpan.Zero, TimeSpan.FromSeconds(5));

                _logger.LogInformation("Starting RecycleBinService monitoring...");
                await _recycleBinService.StartMonitoringAsync();
                _logger.LogInformation("RecycleBinService monitoring started");

                // Keep the service running
                while (!stoppingToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Recycle Bin Monitor Service stopping");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Recycle Bin Monitor Service");
            }
            finally
            {
                _settingsTimer?.Dispose();
                await _recycleBinService.StopMonitoringAsync();
            }
        }

        private async Task LoadSettingsAndUpdateThreshold()
        {
            try
            {
                var settings = await _settingsService.GetSettingsAsync();
                
                _logger.LogInformation($"Loaded settings: Threshold={settings.SizeThresholdBytes} bytes, Interval={settings.CheckIntervalMs}ms, Notifications={settings.NotificationsEnabled}");
                
                // Check if threshold or interval changed
                if (_currentThreshold != settings.SizeThresholdBytes || _currentInterval != settings.CheckIntervalMs)
                {
                    _currentThreshold = settings.SizeThresholdBytes;
                    _currentInterval = settings.CheckIntervalMs;
                    
                    _logger.LogInformation($"Updating threshold to {FormatBytes(_currentThreshold)} and interval to {_currentInterval}ms");
                    
                    // Update the recycle bin service threshold
                    await _recycleBinService.UpdateThresholdAsync(_currentThreshold, TimeSpan.FromMilliseconds(_currentInterval));
                    
                    _logger.LogInformation($"Updated monitoring threshold to {FormatBytes(_currentThreshold)} and interval to {_currentInterval}ms");
                }
                
                // Store notifications enabled flag
                _notificationsEnabled = settings.NotificationsEnabled;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
            }
        }

        private async void OnSizeThresholdReached(object? sender, RecycleBinSizeEventArgs e)
        {
            try
            {
                // Check if notifications are enabled
                if (!_notificationsEnabled)
                {
                    _logger.LogInformation("Notifications are disabled, skipping notification");
                    return;
                }

                string title = "Recycle Bin Size Alert";
                string message = $"Recycle bin has reached {FormatBytes(e.CurrentSize)} " +
                               $"(threshold: {FormatBytes(e.Threshold)}). " +
                               "Consider emptying the recycle bin to free up disk space.";

                _logger.LogWarning($"Recycle bin threshold reached: {FormatBytes(e.CurrentSize)}");

                // Show system tray notification
                _systemTrayService.ShowNotification(title, message, System.Windows.Forms.ToolTipIcon.Warning);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling recycle bin threshold event");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Recycle Bin Monitor Service is stopping");
            
            await _recycleBinService.StopMonitoringAsync();
            
            await base.StopAsync(cancellationToken);
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
    }
}
