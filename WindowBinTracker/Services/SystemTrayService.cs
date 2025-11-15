using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowBinTracker.Interfaces;
using WindowBinTracker.Services;
using WindowBinTracker.UI;

namespace WindowBinTracker.Services
{
    public interface ISystemTrayService
    {
        Task InitializeAsync();
        void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info);
        void Dispose();
    }

    public class SystemTrayService : ISystemTrayService, IDisposable
    {
        private readonly ILogger<SystemTrayService> _logger;
        private readonly IRecycleBinService _recycleBinService;
        private readonly ISettingsService _settingsService;
        private NotifyIcon? _notifyIcon;
        private ContextMenuStrip? _contextMenu;
        private bool _isDisposed = false;

        public SystemTrayService(
            ILogger<SystemTrayService> logger,
            IRecycleBinService recycleBinService,
            ISettingsService settingsService)
        {
            _logger = logger;
            _recycleBinService = recycleBinService;
            _settingsService = settingsService;
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Create context menu
                    _contextMenu = new ContextMenuStrip();
                    _contextMenu.Items.Add("Show Recycle Bin Size", null, OnShowSize);
                    _contextMenu.Items.Add("Settings", null, OnSettings);
                    _contextMenu.Items.Add(new ToolStripSeparator());
                    _contextMenu.Items.Add("Exit", null, OnExit);

                    // Create notify icon
                    _notifyIcon = new NotifyIcon()
                    {
                        Icon = CreateRecycleBinIcon(),
                        ContextMenuStrip = _contextMenu,
                        Text = "Recycle Bin Tracker",
                        Visible = true
                    };

                    // Handle double click
                    _notifyIcon.DoubleClick += OnNotifyIconDoubleClick;

                    _logger.LogInformation("System tray initialized successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to initialize system tray");
                }
            });
        }

        private Icon CreateRecycleBinIcon()
        {
            try
            {
                // Try to get the system recycle bin icon
                return SystemIcons.WinLogo; // Fallback to Windows logo
            }
            catch
            {
                return SystemIcons.Application; // Ultimate fallback
            }
        }

        private void OnNotifyIconDoubleClick(object? sender, EventArgs e)
        {
            OnShowSize(sender, e);
        }

        private async void OnShowSize(object? sender, EventArgs e)
        {
            try
            {
                var size = await _recycleBinService.GetRecycleBinSizeAsync();
                string sizeText = FormatBytes(size);
                
                MessageBox.Show(
                    $"Current Recycle Bin Size: {sizeText}",
                    "Recycle Bin Size",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get recycle bin size");
                MessageBox.Show("Failed to get recycle bin size", "Error", MessageBoxButtons.OK, 
                    MessageBoxIcon.Error);
            }
        }

        private void OnSettings(object? sender, EventArgs e)
        {
            try
            {
                // Create settings form with required dependencies
                var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<SettingsForm>();
                var settingsForm = new SettingsForm(logger, _settingsService, _recycleBinService);
                
                var result = settingsForm.ShowDialog();
                
                if (result == DialogResult.OK)
                {
                    _logger.LogInformation("Settings updated successfully");
                    ShowNotification("Settings Updated", "Your settings have been saved successfully.", ToolTipIcon.Info);
                }
                
                settingsForm.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to open settings");
                MessageBox.Show("Failed to open settings", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnExit(object? sender, EventArgs e)
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
            }
            // Use Environment.Exit instead of Application.Exit to avoid conflicts
            Environment.Exit(0);
        }

        public void ShowNotification(string title, string message, ToolTipIcon icon = ToolTipIcon.Info)
        {
            try
            {
                _notifyIcon?.ShowBalloonTip(5000, title, message, icon);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to show balloon notification");
            }
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _contextMenu?.Dispose();
                _notifyIcon?.Dispose();
                _isDisposed = true;
            }
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
