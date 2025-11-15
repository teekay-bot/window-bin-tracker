# Recycle Bin Tracker Installation Script
# Run this script as Administrator to install the application

param(
    [Parameter(Mandatory=$false)]
    [string]$InstallPath = "C:\Program Files\RecycleBinTracker",
    
    [Parameter(Mandatory=$false)]
    [switch]$AutoStart
)

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "Please run this script as Administrator"
    exit 1
}

# Create installation directory
New-Item -ItemType Directory -Path $InstallPath -Force

# Copy application files
$SourcePath = Split-Path -Parent $MyInvocation.MyCommand.Path
$AppPath = Join-Path $SourcePath "bin\Release\net8.0-windows"

Write-Host "Copying files to $InstallPath..."
Copy-Item -Path "$AppPath\*" -Destination $InstallPath -Recurse -Force

# Create shortcuts
$DesktopPath = [Environment]::GetFolderPath("Desktop")
$StartMenuPath = [Environment]::GetFolderPath("Programs")
$StartupPath = [Environment]::GetFolderPath("Startup")

# Desktop shortcut
$DesktopShortcut = Create-Object -ComObject WScript.Shell
$DesktopLink = $DesktopShortcut.CreateShortcut("$DesktopPath\Recycle Bin Tracker.lnk")
$DesktopLink.TargetPath = "$InstallPath\WindowBinTracker.exe"
$DesktopLink.WorkingDirectory = $InstallPath
$DesktopLink.Description = "Monitor your Recycle Bin size"
$DesktopLink.Save()

# Start Menu shortcut
$StartMenuShortcut = Create-Object -ComObject WScript.Shell
$StartMenuLink = $StartMenuShortcut.CreateShortcut("$StartMenuPath\Recycle Bin Tracker.lnk")
$StartMenuLink.TargetPath = "$InstallPath\WindowBinTracker.exe"
$StartMenuLink.WorkingDirectory = $InstallPath
$StartMenuLink.Description = "Monitor your Recycle Bin size"
$StartMenuLink.Save()

# Auto-start shortcut if requested
if ($AutoStart) {
    $StartupShortcut = Create-Object -ComObject WScript.Shell
    $StartupLink = $StartupShortcut.CreateShortcut("$StartupPath\Recycle Bin Tracker.lnk")
    $StartupLink.TargetPath = "$InstallPath\WindowBinTracker.exe"
    $StartupLink.WorkingDirectory = $InstallPath
    $StartupLink.Description = "Monitor your Recycle Bin size"
    $StartupLink.Save()
}

# Add to Windows Registry for uninstall
$RegPath = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RecycleBinTracker"
New-Item -Path $RegPath -Force
New-ItemProperty -Path $RegPath -Name "DisplayName" -Value "Recycle Bin Tracker" -PropertyType String -Force
New-ItemProperty -Path $RegPath -Name "InstallLocation" -Value $InstallPath -PropertyType String -Force
New-ItemProperty -Path $RegPath -Name "DisplayVersion" -Value "1.1.0" -PropertyType String -Force
New-ItemProperty -Path $RegPath -Name "Publisher" -Value "Your Company" -PropertyType String -Force
New-ItemProperty -Path $RegPath -Name "UninstallString" -Value "powershell -ExecutionPolicy Bypass -File `"$InstallPath\uninstall.ps1`"" -PropertyType String -Force

# Create uninstall script
$UninstallScript = @"
# Recycle Bin Tracker Uninstall Script
param()

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Error "Please run this script as Administrator"
    exit 1
}

# Stop running processes
Get-Process -Name "WindowBinTracker" -ErrorAction SilentlyContinue | Stop-Process -Force

# Remove shortcuts
Remove-Item -Path "`$([Environment]::GetFolderPath("Desktop"))\Recycle Bin Tracker.lnk" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "`$([Environment]::GetFolderPath("Programs"))\Recycle Bin Tracker.lnk" -Force -ErrorAction SilentlyContinue
Remove-Item -Path "`$([Environment]::GetFolderPath("Startup"))\Recycle Bin Tracker.lnk" -Force -ErrorAction SilentlyContinue

# Remove application files
Remove-Item -Path "$InstallPath" -Recurse -Force

# Remove registry entries
Remove-Item -Path "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\RecycleBinTracker" -Recurse -Force

# Remove user settings
Remove-Item -Path "`$([Environment]::GetFolderPath("ApplicationData"))\WindowBinTracker" -Recurse -Force -ErrorAction SilentlyContinue

Write-Host "Recycle Bin Tracker has been uninstalled successfully."
"@

$UninstallScript | Out-File -FilePath "$InstallPath\uninstall.ps1" -Encoding UTF8

Write-Host "Installation completed successfully!"
Write-Host "Application installed to: $InstallPath"
Write-Host "Desktop shortcut created"
Write-Host "Start Menu shortcut created"
if ($AutoStart) {
    Write-Host "Auto-start shortcut created"
}
Write-Host ""
Write-Host "To uninstall, go to Control Panel > Programs and Features or run:"
Write-Host "powershell -ExecutionPolicy Bypass -File `"$InstallPath\uninstall.ps1`""
