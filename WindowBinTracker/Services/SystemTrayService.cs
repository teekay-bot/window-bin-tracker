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
                MessageBox.Show($"Failed to get recycle bin size: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void OnCleanUp(object? sender, EventArgs e)
        {
            _logger.LogInformation("Clean Up menu item clicked");
            try
            {
                var result = MessageBox.Show(
                    "Are you sure you want to empty the recycle bin? This action cannot be undone.",
                    "Empty Recycle Bin",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    _logger.LogInformation("User confirmed empty operation");
                    await EmptyRecycleBinAsync();
                    _logger.LogInformation("EmptyRecycleBinAsync completed successfully");
                    ShowNotification("Recycle Bin Emptied", "The recycle bin has been successfully emptied.", ToolTipIcon.Info);
                    _logger.LogInformation("Recycle bin emptied by user");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to empty recycle bin");
                MessageBox.Show($"Failed to empty recycle bin: {ex.Message}\n\nType: {ex.GetType().Name}\n\nStack Trace:\n{ex.StackTrace}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async Task EmptyRecycleBinAsync()
        {
            try
            {
                await Task.Run(async () =>
                {
                // Get initial size for verification
                long initialSize = 0;
                try
                {
                    initialSize = await _recycleBinService.GetRecycleBinSizeAsync();
                    _logger.LogInformation($"Initial recycle bin size: {FormatBytes(initialSize)}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get initial recycle bin size, continuing with empty operation");
                }
                
                bool success = false;
                Exception lastException = null;

                // Method 1: Use PowerShell Clear-RecycleBin (most reliable)
                _logger.LogInformation("Attempting to empty recycle bin with PowerShell...");
                success = TryEmptyWithPowerShell();
                _logger.LogInformation($"PowerShell method result: {success}");
                
                if (!success)
                {
                    _logger.LogWarning("PowerShell method failed, trying Shell API");
                    lastException = new Exception("PowerShell method failed");
                    
                    // Method 2: Shell API as fallback
                    _logger.LogInformation("Attempting to empty recycle bin with Shell API...");
                    success = TryEmptyWithShellAPI();
                    _logger.LogInformation($"Shell API method result: {success}");
                    
                    if (!success)
                    {
                        lastException = new Exception("Shell API method failed");
                    }
                }

                // If empty operation reported success, trust it completely
                if (success)
                {
                    _logger.LogInformation("Empty operation reported success - operation completed successfully");
                    return; // Success!
                }
                else
                {
                    _logger.LogError($"Empty operation failed. Success flag: {success}");
                    _logger.LogError($"Last exception: {lastException?.Message ?? "null"}");
                    _logger.LogError($"Last exception type: {lastException?.GetType().Name ?? "null"}");
                    
                    if (lastException != null)
                    {
                        _logger.LogError($"Stack trace: {lastException.StackTrace}");
                        throw lastException;
                    }
                    else
                    {
                        throw new Exception("Unknown error occurred while emptying recycle bin - no exception details available");
                    }
                }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in EmptyRecycleBinAsync");
                throw;
            }
        }

        private bool TryEmptyWithPowerShell()
        {
            _logger.LogInformation("=== Starting PowerShell empty method ===");
            try
            {
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
                    if (process == null) return false;
                    
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    
                    process.WaitForExit(30000); // Wait max 30 seconds
                    
                    _logger.LogInformation($"PowerShell Clear-RecycleBin output: {output}");
                    if (!string.IsNullOrEmpty(error))
                    {
                        _logger.LogWarning($"PowerShell error: {error}");
                    }
                    
                    // PowerShell Clear-RecycleBin can return non-zero exit codes even when successful
                    // Check if there are any errors in stderr instead
                    if (string.IsNullOrEmpty(error) || !error.Contains("error", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation("PowerShell Clear-RecycleBin completed successfully");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"PowerShell Clear-RecycleBin failed with error: {error}");
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PowerShell Clear-RecycleBin failed with exception");
                return false;
            }
        }

        private bool TryEmptyWithShellAPI()
        {
            _logger.LogInformation("=== Starting Shell API empty method ===");
            try
            {
                dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                if (shell == null) return false;

                dynamic folder = shell.NameSpace(10); // 10 = Recycle Bin
                if (folder == null) return false;

                dynamic items = folder.Items();
                if (items == null || items.Count == 0) return true; // Already empty

                // Try to use "Empty Recycle Bin" verb if available
                foreach (dynamic verb in folder.Verbs())
                {
                    if (verb.Name.Equals("&Empty Recycle Bin", StringComparison.OrdinalIgnoreCase) ||
                        verb.Name.Equals("Empty Recycle Bin", StringComparison.OrdinalIgnoreCase))
                    {
                        verb.DoIt();
                        return true;
                    }
                }

                // Fallback: delete each item individually
                int deletedCount = 0;
                foreach (dynamic item in items)
                {
                    try
                    {
                        // Select the item first
                        folder.SelectItem(item, 8); // 8 = Select all
                        folder.InvokeVerb("delete");
                        deletedCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, $"Failed to delete item: {item.Name}");
                    }
                }

                _logger.LogInformation($"Deleted {deletedCount} items via Shell API");
                return deletedCount > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Shell API method failed with exception");
                return false;
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
                if (_notifyIcon != null)
                {
                    // Always refresh the custom icon before showing notification
                    var customIcon = CreateRecycleBinIcon();
                    _notifyIcon.Icon = customIcon;
                    
                    // Force the notify icon to refresh with a small delay
                    _notifyIcon.Visible = false;
                    System.Threading.Thread.Sleep(100);
                    _notifyIcon.Visible = true;
                    
                    // Wait a bit more before showing notification
                    System.Threading.Thread.Sleep(200);
                    
                    _notifyIcon.ShowBalloonTip(5000, title, message, icon);
                    _logger.LogInformation($"Notification shown: {title} - {message}");
                }
                else
                {
                    _logger.LogWarning("Cannot show notification - NotifyIcon is null");
                }
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
    }
}
