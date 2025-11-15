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

### Option 1: Direct Download (Easiest)

1. **Download the installer**:
   - [Download RecycleBinTrackerSetup-1.1.0.exe](installer/RecycleBinTrackerSetup-1.1.0.exe) *(~5MB)*
   - Or download from the latest [GitHub Release](https://github.com/teekay-bot/window-bin-tracker/releases)

2. **Run the installer** (as Administrator):
   - Double-click `RecycleBinTrackerSetup-1.1.0.exe`
   - Follow the installation wizard
   - Choose installation directory (default: `C:\Program Files\RecycleBinTracker`)

### Option 2: Build from Source

1. **Build the application**:
   ```powershell
   cd "c:\Users\teeka\Git Projects\Personal\window-bin-tracker\WindowBinTracker"
   dotnet build --configuration Release
   ```

2. **Create installer**:
   ```powershell
   # Use Inno Setup to create installer from setup.iss
   # Or run the pre-built installer from the installer/ directory
   ```

3. **Run the installer** (as Administrator):
   - Execute `RecycleBinTrackerSetup-1.1.0.exe` from the `installer/` directory
   - Follow the installation wizard

### Option 3: Manual Installation

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

## Building from Source

1. Clone the repository
2. Install .NET 8.0 SDK
3. Run `dotnet build --configuration Release`
4. Create installer using Inno Setup with `setup.iss`
5. Follow the installation instructions above

## Downloads

### Latest Version: 1.1.0
- **[Download Installer](installer/RecycleBinTrackerSetup-1.1.0.exe)** - Windows Installer (~5MB)
- **[View on GitHub](https://github.com/teekay-bot/window-bin-tracker)** - Source code and releases

### File Information
- **File**: `RecycleBinTrackerSetup-1.1.0.exe`
- **Size**: ~5MB
- **Requirements**: Windows 10+, .NET 8.0 Runtime (included)
- **Permissions**: Requires Administrator for installation

## Recent Updates

### Version 1.1.0
- ✨ Added **Clean Up** option to system tray context menu
- 🎨 Updated with custom recycle bin icon
- 🐛 Fixed UI text truncation issues
- 🧹 Cleaned up unused code and dependencies
- 📦 Improved installer configuration
- 🏷️ Standardized version numbers across all components
- 📝 Added version display in Settings form title
