using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.IO;
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
                    _contextMenu.Items.Add("Clean Up", null, OnCleanUp);
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
                // Try to load from embedded resource first
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var resourceName = "WindowBinTracker.Resources.Icons.recyclebin.ico";
                
                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return new Icon(stream);
                }
                
                // Try to load from file path (for development)
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Icons", "recyclebin.ico");
                if (File.Exists(iconPath))
                {
                    return new Icon(iconPath);
                }
                
                // Fallback to system icons
                return SystemIcons.WinLogo;
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

        private async void OnCleanUp(object? sender, EventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to empty the recycle bin? This action cannot be undone.",
                    "Empty Recycle Bin",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    await EmptyRecycleBinAsync();
                    ShowNotification("Recycle Bin Emptied", "The recycle bin has been successfully emptied.", ToolTipIcon.Info);
                    _logger.LogInformation("Recycle bin emptied by user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to empty recycle bin");
                MessageBox.Show("Failed to empty recycle bin", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task EmptyRecycleBinAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    // Use Shell API to empty recycle bin
                    dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                    if (shell != null)
                    {
                        // Get recycle bin folder
                        dynamic folder = shell.NameSpace(10); // 10 = Recycle Bin
                        if (folder != null)
                        {
                            // Get all items in recycle bin
                            dynamic items = folder.Items();
                            if (items != null && items.Count > 0)
                            {
                                // Select all items and delete them
                                foreach (dynamic item in items)
                                {
                                    try
                                    {
                                        folder.InvokeVerb("delete"); // Use delete verb to permanently delete
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogWarning(ex, $"Failed to delete item: {item.Name}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Shell API failed to empty recycle bin, trying alternative method");
                    
                    // Alternative method using PowerShell
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "powershell",
                        Arguments = "-Command \"Clear-RecycleBin -Force\"",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true
                    };

                    using (var process = System.Diagnostics.Process.Start(psi))
                    {
                        process?.WaitForExit(30000); // Wait max 30 seconds
                    }
                }
            });
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
