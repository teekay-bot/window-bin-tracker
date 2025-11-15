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
        private readonly INotificationService _notificationService;
        private readonly ISystemTrayService _systemTrayService;
        private readonly RecycleBinConfiguration _config;

        public RecycleBinMonitorService(
            ILogger<RecycleBinMonitorService> logger,
            IRecycleBinService recycleBinService,
            INotificationService notificationService,
            ISystemTrayService systemTrayService,
            IOptions<RecycleBinConfiguration> config)
        {
            _logger = logger;
            _recycleBinService = recycleBinService;
            _notificationService = notificationService;
            _systemTrayService = systemTrayService;
            _config = config.Value;

            // Subscribe to threshold reached events
            _recycleBinService.SizeThresholdReached += OnSizeThresholdReached;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Recycle Bin Monitor Service starting");

            try
            {
                await _recycleBinService.StartMonitoringAsync();

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
                await _recycleBinService.StopMonitoringAsync();
            }
        }

        private async void OnSizeThresholdReached(object? sender, RecycleBinSizeEventArgs e)
        {
            try
            {
                string title = "Recycle Bin Size Alert";
                string message = $"Recycle bin has reached {FormatBytes(e.CurrentSize)} " +
                               $"(threshold: {FormatBytes(e.Threshold)}). " +
                               "Consider emptying the recycle bin to free up disk space.";

                _logger.LogWarning($"Recycle bin threshold reached: {FormatBytes(e.CurrentSize)}");

                // Show both notification service and system tray notification
                await _notificationService.ShowNotificationAsync(title, message, NotificationType.Warning);
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
