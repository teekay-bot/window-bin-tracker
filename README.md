# Window Native Template

A comprehensive C# .NET template for building Windows-native applications with window management capabilities.

## Features

- **Window Management**: Monitor, control, and track active windows on Windows
- **Process Management**: Track and manage running processes
- **Real-time Monitoring**: Background service for window change events
- **Modern .NET**: Built with .NET 8.0 and modern C# features
- **Dependency Injection**: Uses Microsoft.Extensions.DependencyInjection
- **Structured Logging**: Serilog integration with console and file output
- **Configuration**: Flexible JSON-based configuration system

## Prerequisites

- .NET 8.0 SDK or later
- Windows operating system
- Visual Studio 2022 or VS Code with C# extension

## Quick Start

1. Clone or download this template
2. Navigate to the project directory
3. Restore dependencies:
   ```bash
   dotnet restore
   ```
4. Build the project:
   ```bash
   dotnet build
   ```
5. Run the application:
   ```bash
   dotnet run
   ```

## Project Structure

```
WindowNativeTemplate/
├── Interfaces/           # Service interfaces
│   ├── IWindowService.cs
│   └── IProcessService.cs
├── Services/            # Service implementations
│   ├── WindowService.cs
│   ├── ProcessService.cs
│   └── WindowMonitorService.cs
├── Program.cs           # Application entry point
├── WindowNativeTemplate.csproj  # Project file
├── appsettings.json     # Configuration
├── .gitignore          # Git ignore file
├── .editorconfig       # Editor configuration
└── README.md           # This file
```

## Usage

### Basic Window Operations

```csharp
// Get the window service
var windowService = serviceProvider.GetRequiredService<IWindowService>();

// Get all active windows
var windows = await windowService.GetActiveWindowsAsync();

// Get the foreground window
var foregroundWindow = await windowService.GetForegroundWindowAsync();

// Minimize a window
await windowService.MinimizeWindowAsync(windowHandle);
```

### Monitoring Window Changes

```csharp
// Subscribe to window change events
windowService.WindowChanged += (sender, e) =>
{
    Console.WriteLine($"Window changed from {e.PreviousWindow?.Title} to {e.CurrentWindow?.Title}");
};

// Start monitoring
await windowService.StartMonitoringAsync();
```

### Process Management

```csharp
// Get the process service
var processService = serviceProvider.GetRequiredService<IProcessService>();

// Get all running processes
var processes = await processService.GetRunningProcessesAsync();

// Get a specific process
var process = await processService.GetProcessByIdAsync(processId);

// Kill a process
await processService.KillProcessAsync(processId);
```

## Configuration

The application uses JSON configuration files:

- `appsettings.json` - Default configuration
- `appsettings.Development.json` - Development-specific settings
- `appsettings.Production.json` - Production-specific settings

### Configuration Options

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "WindowNativeTemplate": "Debug"
    }
  },
  "WindowMonitoring": {
    "CheckIntervalMs": 500,
    "LogWindowChanges": true,
    "TrackInactiveWindows": false
  },
  "ProcessTracking": {
    "RefreshIntervalMs": 5000,
    "IncludeSystemProcesses": false
  }
}
```

## API Reference

### IWindowService

- `GetActiveWindowsAsync()` - Returns all visible windows
- `GetForegroundWindowAsync()` - Returns the currently active window
- `SetForegroundWindowAsync(IntPtr hWnd)` - Sets a window as foreground
- `MinimizeWindowAsync(IntPtr hWnd)` - Minimizes a window
- `MaximizeWindowAsync(IntPtr hWnd)` - Maximizes a window
- `RestoreWindowAsync(IntPtr hWnd)` - Restores a window
- `CloseWindowAsync(IntPtr hWnd)` - Closes a window
- `StartMonitoringAsync()` - Starts monitoring window changes
- `StopMonitoringAsync()` - Stops monitoring window changes

### IProcessService

- `GetRunningProcessesAsync()` - Returns all running processes
- `GetProcessByIdAsync(int processId)` - Returns a specific process
- `KillProcessAsync(int processId)` - Terminates a process
- `StartProcessAsync(string path, string? arguments)` - Starts a new process

## Data Models

### WindowInfo

```csharp
public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; }
    public string ProcessName { get; set; }
    public int ProcessId { get; set; }
    public bool IsVisible { get; set; }
    public Rectangle Rectangle { get; set; }
    public DateTime LastActive { get; set; }
}
```

### ProcessInfo

```csharp
public class ProcessInfo
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Path { get; set; }
    public DateTime StartTime { get; set; }
    public long WorkingSet { get; set; }
    public string? MainWindowTitle { get; set; }
    public IntPtr MainWindowHandle { get; set; }
}
```

## Logging

The application uses Serilog for structured logging. Logs are written to:

- Console output
- Daily rolling files in the `logs/` directory

## Building and Publishing

### Debug Build
```bash
dotnet build -c Debug
```

### Release Build
```bash
dotnet build -c Release
```

### Publish as Self-Contained
```bash
dotnet publish -c Release -r win-x64 --self-contained true
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Security Considerations

This application uses Windows API calls that may require elevated privileges in some scenarios. Be aware of:

- Some windows may be protected and cannot be accessed
- System processes may require administrator privileges
- Always validate handles and process IDs before using them

## Troubleshooting

### Common Issues

1. **Access Denied**: Some windows and processes are protected by the OS
2. **Handle Invalid**: Window handles can become invalid if the window is closed
3. **Performance**: Monitoring too frequently can impact performance

### Debug Tips

- Enable debug logging in configuration
- Use Visual Studio's debugger to step through Windows API calls
- Check the logs directory for detailed error information

## Dependencies

- Microsoft.Extensions.Hosting
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Configuration
- Serilog
- System.Management

## Version History

- **1.0.0** - Initial release with basic window and process management
