# Recycle Bin Tracker

A Windows system tray application that monitors your Recycle Bin size and sends notifications when it exceeds a configured threshold.

## Features

- **System Tray Integration**: Runs in the background with a custom recycle bin icon
- **Configurable Threshold**: Set custom size thresholds for notifications
- **Flexible Check Intervals**: Configure how often to check the Recycle Bin
- **System Tray Management**:
  - Show current Recycle Bin size
  - **Clean Up** - Empty recycle bin directly from system tray
  - Open Settings dialog
  - Exit application
- **Notification Options**: 
  - System tray balloon tips
  - Mute notifications for specific durations (1 hour, 24 hours, 7 days)
- **Startup Options**: Option to start automatically with Windows
- **Settings UI**: Easy-to-use settings dialog to configure all options
- **Custom Icons**: Modern recycle bin icon

## Installation

### Direct Download

1. **Download the installer**:
   - [Download RecycleBinTrackerSetup-1.1.0.exe](WindowBinTracker/installer/RecycleBinTrackerSetup-1.1.0.exe)
   - Or download from the latest [GitHub Release](https://github.com/teekay-bot/window-bin-tracker/releases)

2. **Run the installer** (as Administrator):
   - Double-click `RecycleBinTrackerSetup-1.1.0.exe`
   - Follow the installation wizard
   - Choose installation directory (default: `C:\Program Files\RecycleBinTracker`)

## Usage

1. **Start the application**:
   - Double-click the desktop shortcut
   - Or run `WindowBinTracker.exe` from the installation directory

2. **System Tray Icon**:
   - The application will appear in the system tray with a recycle bin icon
   - Right-click the icon for options:
     - **Show Recycle Bin Size** - Display current size in a dialog
     - **Clean Up** - Empty the recycle bin with confirmation
     - **Settings** - Open configuration dialog
     - **Exit** - Close the application

3. **Settings Configuration**:
   - **Size Threshold**: Set the minimum size that triggers notifications
   - **Check Interval**: Choose how often to check (seconds, minutes, or hours)
   - **Notification Options**:
     - Enable/disable notifications
     - Show balloon tips option
     - Minimize to tray option
     - Start with Windows option
   - **Mute Notifications**:
     - Mute for 1 hour, 24 hours, or 7 days
     - Unmute manually when needed

4. **Clean Up Feature**:
   - Right-click system tray icon → "Clean Up"
   - Confirmation dialog appears before emptying recycle bin
   - Success notification shown when completed

## Configuration File

Settings are stored in `%APPDATA%\WindowBinTracker\settings.json`. You can manually edit this file if needed.

## Logs

Application logs are stored in the `logs` subdirectory of the installation folder.

## Uninstallation

### Automatic Uninstallation

1. Use "Add or Remove Programs" in Windows Settings
2. Or run the uninstaller from the installation directory

### Manual Uninstallation

1. **Stop the application** if running
2. **Delete the installation directory**:
   ```powershell
   Remove-Item -Path "C:\Program Files\RecycleBinTracker" -Recurse -Force
   ```
3. **Remove shortcuts** from Desktop, Start Menu, and Startup folder
4. **Delete user settings**:
   ```powershell
   Remove-Item -Path "%APPDATA%\WindowBinTracker" -Recurse -Force
   ```

## Running as Windows Service

The application can also run as a Windows Service:

1. **Install as service**:
   ```powershell
   sc create "RecycleBinTracker" binPath="C:\Program Files\RecycleBinTracker\WindowBinTracker.exe --service"
   sc start "RecycleBinTracker"
   ```

2. **Remove service**:
   ```powershell
   sc stop "RecycleBinTracker"
   sc delete "RecycleBinTracker"
   ```

## Troubleshooting

- **Application won't start**: Check if .NET 8.0 Runtime is installed
- **No notifications**: Ensure notifications are enabled in settings and Windows notification settings
- **High CPU usage**: Increase the check interval in settings
- **Logs not created**: Check write permissions to the installation directory
- **Clean Up not working**: Check administrative privileges and Windows permissions

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime (included in installer)
- Administrative privileges for installation
