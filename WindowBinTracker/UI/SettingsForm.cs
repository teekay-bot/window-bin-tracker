using Microsoft.Extensions.Logging;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowBinTracker.Interfaces;
using WindowBinTracker.Models;
using WindowBinTracker.Services;

namespace WindowBinTracker.UI
{
    public partial class SettingsForm : Form
    {
        private readonly ILogger<SettingsForm> _logger;
        private readonly ISettingsService _settingsService;
        private readonly IRecycleBinService _recycleBinService;
        private RecycleBinSettings _settings;

        private NumericUpDown thresholdNumeric;
        private ComboBox thresholdUnitCombo;
        private NumericUpDown intervalNumeric;
        private ComboBox intervalUnitCombo;
        private CheckBox notificationsCheckBox;
        private CheckBox balloonTipsCheckBox;
        private CheckBox minimizeToTrayCheckBox;
        private CheckBox startWithWindowsCheckBox;
        private GroupBox muteGroupBox;
        private Button mute1HourButton;
        private Button mute24HoursButton;
        private Button mute7DaysButton;
        private Button unmuteButton;
        private Label muteStatusLabel;
        private Button saveButton;
        private Button cancelButton;
        private Button resetButton;
        private Label currentSizeLabel;

        public SettingsForm(
            ILogger<SettingsForm> logger,
            ISettingsService settingsService,
            IRecycleBinService recycleBinService)
        {
            _logger = logger;
            _settingsService = settingsService;
            _recycleBinService = recycleBinService;
            _settings = new RecycleBinSettings();

            InitializeComponent();
            LoadSettingsAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Recycle Bin Tracker Settings v1.1.0";
            this.Size = new Size(500, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            
            // Set form icon
            this.Icon = CreateRecycleBinIcon();

            // Threshold settings
            var thresholdLabel = new Label
            {
                Text = "Size Threshold:",
                Location = new Point(20, 20),
                Size = new Size(100, 23)
            };

            thresholdNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 1000,
                Value = 10,
                Location = new Point(130, 18),
                Size = new Size(80, 23)
            };

            thresholdUnitCombo = new ComboBox
            {
                Location = new Point(220, 18),
                Size = new Size(60, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            thresholdUnitCombo.Items.AddRange(new object[] { "MB", "GB", "TB" });
            thresholdUnitCombo.SelectedIndex = 1;

            // Check interval
            var intervalLabel = new Label
            {
                Text = "Check Interval:",
                Location = new Point(20, 60),
                Size = new Size(100, 23)
            };

            intervalNumeric = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 1440,
                Value = 30,
                Location = new Point(130, 58),
                Size = new Size(80, 23)
            };

            intervalUnitCombo = new ComboBox
            {
                Location = new Point(220, 58),
                Size = new Size(80, 23),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            intervalUnitCombo.Items.AddRange(new object[] { "seconds", "minutes", "hours" });
            intervalUnitCombo.SelectedIndex = 0;

            // Notification settings
            notificationsCheckBox = new CheckBox
            {
                Text = "Enable notifications",
                Location = new Point(20, 100),
                Size = new Size(150, 23),
                Checked = true
            };

            balloonTipsCheckBox = new CheckBox
            {
                Text = "Show balloon tips",
                Location = new Point(20, 130),
                Size = new Size(150, 23),
                Checked = true
            };

            minimizeToTrayCheckBox = new CheckBox
            {
                Text = "Minimize to tray",
                Location = new Point(20, 160),
                Size = new Size(150, 23),
                Checked = true
            };

            startWithWindowsCheckBox = new CheckBox
            {
                Text = "Start with Windows",
                Location = new Point(20, 190),
                Size = new Size(150, 23),
                Checked = false
            };

            // Mute settings
            muteGroupBox = new GroupBox
            {
                Text = "Mute Notifications",
                Location = new Point(20, 230),
                Size = new Size(340, 120)
            };

            mute1HourButton = new Button
            {
                Text = "Mute 1 Hour",
                Location = new Point(10, 25),
                Size = new Size(100, 30)
            };

            mute24HoursButton = new Button
            {
                Text = "Mute 24 Hours",
                Location = new Point(120, 25),
                Size = new Size(100, 30)
            };

            mute7DaysButton = new Button
            {
                Text = "Mute 7 Days",
                Location = new Point(230, 25),
                Size = new Size(80, 30)
            };

            unmuteButton = new Button
            {
                Text = "Unmute",
                Location = new Point(320, 25),
                Size = new Size(70, 30)
            };

            muteStatusLabel = new Label
            {
                Text = "Notifications enabled",
                Location = new Point(10, 65),
                Size = new Size(310, 23),
                ForeColor = Color.Green
            };

            // Current size
            currentSizeLabel = new Label
            {
                Text = "Current Recycle Bin Size: Loading...",
                Location = new Point(20, 370),
                Size = new Size(340, 23),
                Font = new Font(this.Font, FontStyle.Bold)
            };

            // Buttons
            saveButton = new Button
            {
                Text = "Save",
                Location = new Point(200, 520),
                Size = new Size(80, 30),
                UseVisualStyleBackColor = true
            };

            cancelButton = new Button
            {
                Text = "Cancel",
                Location = new Point(290, 520),
                Size = new Size(80, 30),
                UseVisualStyleBackColor = true
            };

            resetButton = new Button
            {
                Text = "Reset to Defaults",
                Location = new Point(20, 520),
                Size = new Size(120, 30),
                UseVisualStyleBackColor = true
            };

            // Add controls to form
            this.Controls.AddRange(new Control[] {
                thresholdLabel, thresholdNumeric, thresholdUnitCombo,
                intervalLabel, intervalNumeric, intervalUnitCombo,
                notificationsCheckBox, balloonTipsCheckBox, minimizeToTrayCheckBox, startWithWindowsCheckBox,
                currentSizeLabel,
                saveButton, cancelButton, resetButton
            });

            muteGroupBox.Controls.AddRange(new Control[] {
                mute1HourButton, mute24HoursButton, mute7DaysButton, unmuteButton, muteStatusLabel
            });
            this.Controls.Add(muteGroupBox);

            // Event handlers
            saveButton.Click += SaveButton_Click;
            cancelButton.Click += CancelButton_Click;
            resetButton.Click += ResetButton_Click;
            mute1HourButton.Click += (s, e) => MuteForHours(1);
            mute24HoursButton.Click += (s, e) => MuteForHours(24);
            mute7DaysButton.Click += (s, e) => MuteForDays(7);
            unmuteButton.Click += (s, e) => Unmute();
        }

        private async void LoadSettingsAsync()
        {
            try
            {
                _settings = await _settingsService.GetSettingsAsync();
                
                // Load threshold
                var thresholdBytes = _settings.SizeThresholdBytes;
                if (thresholdBytes >= 1099511627776) // TB
                {
                    thresholdNumeric.Value = (decimal)(thresholdBytes / 1099511627776.0);
                    thresholdUnitCombo.SelectedIndex = 2;
                }
                else if (thresholdBytes >= 1073741824) // GB
                {
                    thresholdNumeric.Value = (decimal)(thresholdBytes / 1073741824.0);
                    thresholdUnitCombo.SelectedIndex = 1;
                }
                else // MB
                {
                    thresholdNumeric.Value = (decimal)(thresholdBytes / 1048576.0);
                    thresholdUnitCombo.SelectedIndex = 0;
                }

                // Load interval
                var intervalMs = _settings.CheckIntervalMs;
                if (intervalMs >= 3600000) // hours
                {
                    intervalNumeric.Value = intervalMs / 3600000;
                    intervalUnitCombo.SelectedIndex = 2;
                }
                else if (intervalMs >= 60000) // minutes
                {
                    intervalNumeric.Value = intervalMs / 60000;
                    intervalUnitCombo.SelectedIndex = 1;
                }
                else // seconds
                {
                    intervalNumeric.Value = intervalMs / 1000;
                    intervalUnitCombo.SelectedIndex = 0;
                }

                // Load checkboxes
                notificationsCheckBox.Checked = _settings.NotificationsEnabled;
                balloonTipsCheckBox.Checked = _settings.ShowBalloonTips;
                minimizeToTrayCheckBox.Checked = _settings.MinimizeToTray;
                startWithWindowsCheckBox.Checked = _settings.StartWithWindows;

                UpdateMuteStatus();
                UpdateCurrentSize();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings");
            }
        }

        private void UpdateCurrentSize()
        {
            Task.Run(async () =>
            {
                try
                {
                    var size = await _recycleBinService.GetRecycleBinSizeAsync();
                    this.Invoke(new Action(() =>
                    {
                        currentSizeLabel.Text = $"Current Recycle Bin Size: {FormatBytes(size)}";
                    }));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update current size");
                }
            });
        }

        private void UpdateMuteStatus()
        {
            var statusText = _settings.GetMuteStatusText();
            muteStatusLabel.Text = statusText;
            muteStatusLabel.ForeColor = _settings.IsMuted ? Color.Red : Color.Green;
        }

        private async void SaveButton_Click(object? sender, EventArgs e)
        {
            try
            {
                // Save threshold
                var unit = thresholdUnitCombo.SelectedItem?.ToString() ?? "GB";
                var value = thresholdNumeric.Value;
                _settings.SizeThresholdBytes = unit switch
                {
                    "MB" => (long)(value * 1048576),
                    "GB" => (long)(value * 1073741824),
                    "TB" => (long)(value * 1099511627776),
                    _ => (long)(value * 1073741824)
                };

                // Save interval
                var intervalUnit = intervalUnitCombo.SelectedItem?.ToString() ?? "seconds";
                var intervalValue = intervalNumeric.Value;
                _settings.CheckIntervalMs = intervalUnit switch
                {
                    "seconds" => (int)(intervalValue * 1000),
                    "minutes" => (int)(intervalValue * 60000),
                    "hours" => (int)(intervalValue * 3600000),
                    _ => (int)(intervalValue * 1000)
                };

                // Save checkboxes
                _settings.NotificationsEnabled = notificationsCheckBox.Checked;
                _settings.ShowBalloonTips = balloonTipsCheckBox.Checked;
                _settings.MinimizeToTray = minimizeToTrayCheckBox.Checked;
                _settings.StartWithWindows = startWithWindowsCheckBox.Checked;

                await _settingsService.SaveSettingsAsync(_settings);
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                MessageBox.Show("Failed to save settings", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CancelButton_Click(object? sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private async void ResetButton_Click(object? sender, EventArgs e)
        {
            if (MessageBox.Show("Reset all settings to defaults?", "Confirm Reset", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    await _settingsService.ResetToDefaultsAsync();
                    LoadSettingsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to reset settings");
                    MessageBox.Show("Failed to reset settings", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void MuteForHours(int hours)
        {
            _settings.MuteForHours(hours);
            await _settingsService.SaveSettingsAsync(_settings);
            UpdateMuteStatus();
        }

        private async void MuteForDays(int days)
        {
            _settings.MuteForDays(days);
            await _settingsService.SaveSettingsAsync(_settings);
            UpdateMuteStatus();
        }

        private async void Unmute()
        {
            _settings.Unmute();
            await _settingsService.SaveSettingsAsync(_settings);
            UpdateMuteStatus();
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
                
                // Fallback to default icon
                return SystemIcons.Application;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load recycle bin icon");
                return SystemIcons.Application;
            }
        }
    }
}
