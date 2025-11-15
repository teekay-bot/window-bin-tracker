# Recycle Bin Tracker

A Windows system tray application that monitors your Recycle Bin size and sends notifications when it exceeds a configured threshold.

## Features

- **System Tray Integration**: Runs in the background with a system tray icon
- **Configurable Threshold**: Set custom size thresholds for notifications
- **Flexible Check Intervals**: Configure how often to check the Recycle Bin
- **Notification Options**: 
  - Windows Toast notifications
  - System tray balloon tips
  - Mute notifications for specific durations (1 hour, 24 hours, 7 days)
- **Startup Options**: Option to start automatically with Windows
- **Settings UI**: Easy-to-use settings dialog to configure all options

## Installation

### Option 1: PowerShell Installation (Recommended)

1. **Build the application**:
   ```powershell
   cd "c:\Users\teeka\Git Projects\Personal\window-bin-tracker\WindowBinTracker"
   dotnet build --configuration Release
   ```

2. **Run the installation script** (as Administrator):
   ```powershell
   Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
   .\install.ps1
   ```
   
   To install with auto-start:
   ```powershell
   .\install.ps1 -AutoStart
   ```

### Option 2: Manual Installation

1. **Build the application**:
   ```powershell
   cd "c:\Users\teeka\Git Projects\Personal\window-bin-tracker\WindowBinTracker"
   dotnet build --configuration Release
   ```

2. **Create installation directory**:
   ```powershell
   New-Item -ItemType Directory -Path "C:\Program Files\RecycleBinTracker" -Force
   ```

3. **Copy application files**:
   ```powershell
   Copy-Item -Path "bin\Release\net8.0-windows\*" -Destination "C:\Program Files\RecycleBinTracker" -Recurse -Force
   ```

4. **Create shortcuts** (optional):
   - Desktop shortcut to `C:\Program Files\RecycleBinTracker\WindowBinTracker.exe`
   - Start Menu shortcut to the same executable
   - Startup folder shortcut for auto-start

## Usage

1. **Start the application**:
   - Double-click the desktop shortcut
   - Or run `WindowBinTracker.exe` from the installation directory

2. **System Tray Icon**:
   - The application will appear in the system tray
   - Right-click the icon for options:
     - Show current Recycle Bin size
     - Open Settings
     - Exit

3. **Settings Configuration**:
   - **Size Threshold**: Set the minimum size that triggers notifications
   - **Check Interval**: Choose how often to check (seconds, minutes, or hours)
   - **Notification Options**:
     - Enable/disable notifications
     - Choose between Toast notifications and balloon tips
     - Minimize to tray option
     - Start with Windows option
   - **Mute Notifications**:
     - Mute for 1 hour, 24 hours, or 7 days
     - Unmute manually when needed

## Configuration File

Settings are stored in `%APPDATA%\WindowBinTracker\settings.json`. You can manually edit this file if needed.

## Logs

Application logs are stored in the `logs` subdirectory of the installation folder.

## Uninstallation

### Automatic Uninstallation

1. Run the uninstall script (as Administrator):
   ```powershell
   powershell -ExecutionPolicy Bypass -File "C:\Program Files\RecycleBinTracker\uninstall.ps1"
   ```

2. Or use "Add or Remove Programs" in Windows Settings

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

## Requirements

- Windows 10 or later
- .NET 8.0 Runtime (installed automatically by the installer)
- Administrative privileges for installation

## Building from Source

1. Clone the repository
2. Install .NET 8.0 SDK
3. Run `dotnet build --configuration Release`
4. Follow the installation instructions above
