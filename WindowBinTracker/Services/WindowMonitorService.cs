using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using WindowNativeTemplate.Interfaces;

namespace WindowNativeTemplate.Services
{
    public class WindowMonitorService : BackgroundService
    {
        private readonly ILogger<WindowMonitorService> _logger;
        private readonly IWindowService _windowService;

        public WindowMonitorService(
            ILogger<WindowMonitorService> logger,
            IWindowService windowService)
        {
            _logger = logger;
            _windowService = windowService;
            
            // Subscribe to window change events
            _windowService.WindowChanged += OnWindowChanged;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Window Monitor Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // The actual monitoring is handled by WindowService
                    // This service just ensures the monitoring stays alive
                    await Task.Delay(1000, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Window Monitor Service");
                    await Task.Delay(5000, stoppingToken); // Wait before retrying
                }
            }

            _logger.LogInformation("Window Monitor Service is stopping.");
        }

        private void OnWindowChanged(object? sender, WindowChangedEventArgs e)
        {
            try
            {
                if (e.CurrentWindow != null)
                {
                    _logger.LogInformation(
                        "Window changed: '{PreviousTitle}' -> '{CurrentTitle}' (Process: {ProcessName}, PID: {ProcessId})",
                        e.PreviousWindow?.Title ?? "[None]",
                        e.CurrentWindow.Title,
                        e.CurrentWindow.ProcessName,
                        e.CurrentWindow.ProcessId);
                }
                else if (e.PreviousWindow != null)
                {
                    _logger.LogInformation(
                        "Window closed: '{WindowTitle}' (Process: {ProcessName}, PID: {ProcessId})",
                        e.PreviousWindow.Title,
                        e.PreviousWindow.ProcessName,
                        e.PreviousWindow.ProcessId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error handling window change event");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Window Monitor Service is stopping.");
            
            // Unsubscribe from events
            _windowService.WindowChanged -= OnWindowChanged;
            
            // Stop window monitoring
            await _windowService.StopMonitoringAsync();
            
            await base.StopAsync(cancellationToken);
        }
    }
}
