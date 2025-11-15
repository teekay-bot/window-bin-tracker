using Microsoft.Extensions.Logging;
using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using WindowBinTracker.Interfaces;
using WindowBinTracker.Models;

namespace WindowBinTracker.Services
{
    public interface INotificationService
    {
        Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Info);
    }

    public enum NotificationType
    {
        Info,
        Warning,
        Error
    }

    public class WindowsNotificationService : INotificationService
    {
        private readonly ILogger<WindowsNotificationService> _logger;
        private readonly ISettingsService _settingsService;
        private RecycleBinSettings? _currentSettings;

        public WindowsNotificationService(ILogger<WindowsNotificationService> logger, ISettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;
        }

        public async Task ShowNotificationAsync(string title, string message, NotificationType type = NotificationType.Info)
        {
            try
            {
                // Load current settings
                _currentSettings = await _settingsService.GetSettingsAsync();

                // Check if notifications are enabled and not muted
                if (!_currentSettings.NotificationsEnabled || _currentSettings.IsMuted)
                {
                    _logger.LogInformation($"Notification skipped - Enabled: {_currentSettings.NotificationsEnabled}, Muted: {_currentSettings.IsMuted}");
                    return;
                }

                await Task.Run(() =>
                {
                    try
                    {
                        // Use Windows Toast notifications via Windows Runtime
                        ShowToastNotification(title, message, type);
                        _logger.LogInformation($"Notification sent: {title} - {message}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to show Windows notification");
                        // Fallback to console if notification fails
                        Console.WriteLine($"\n🔔 {title}\n{message}\n");
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check notification settings");
            }
        }

        private void ShowToastNotification(string title, string message, NotificationType type)
        {
            try
            {
                // For Windows 10/11, we can use PowerShell to create toast notifications
                string icon = type switch
                {
                    NotificationType.Warning => "⚠️",
                    NotificationType.Error => "❌",
                    _ => "ℹ️"
                };

                string powershellScript = $@"
Add-Type -AssemblyName System.Windows.Forms
$notification = New-Object System.Windows.Forms.NotifyIcon
$notification.Icon = [System.Drawing.SystemIcons]::{GetIconName(type)}
$notification.BalloonTipTitle = '{title}'
$notification.BalloonTipText = '{message}'
$notification.BalloonTipIcon = '{GetBalloonTipIcon(type)}'
$notification.Visible = $true
$notification.ShowBalloonTip(10000)
Start-Sleep -Seconds 10
$notification.Dispose()";

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "powershell",
                    Arguments = $"-Command \"{powershellScript}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var process = System.Diagnostics.Process.Start(psi))
                {
                    process?.WaitForExit(10000); // Wait max 10 seconds
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PowerShell toast notification failed");
                throw;
            }
        }

        private static string GetIconName(NotificationType type)
        {
            return type switch
            {
                NotificationType.Warning => "Warning",
                NotificationType.Error => "Error",
                _ => "Information"
            };
        }

        private static string GetBalloonTipIcon(NotificationType type)
        {
            return type switch
            {
                NotificationType.Warning => "Warning",
                NotificationType.Error => "Error",
                _ => "Info"
            };
        }
    }
}
