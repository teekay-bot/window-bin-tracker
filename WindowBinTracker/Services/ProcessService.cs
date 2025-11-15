using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using WindowNativeTemplate.Interfaces;

namespace WindowNativeTemplate.Services
{
    public class ProcessService : IProcessService
    {
        private readonly ILogger<ProcessService> _logger;

        public ProcessService(ILogger<ProcessService> logger)
        {
            _logger = logger;
        }

        public async Task<IEnumerable<ProcessInfo>> GetRunningProcessesAsync()
        {
            return await Task.Run(() =>
            {
                var processes = new List<ProcessInfo>();
                
                try
                {
                    var allProcesses = Process.GetProcesses();
                    
                    foreach (var process in allProcesses)
                    {
                        try
                        {
                            var processInfo = new ProcessInfo
                            {
                                Id = process.Id,
                                Name = process.ProcessName,
                                Path = GetProcessPath(process),
                                StartTime = process.StartTime,
                                WorkingSet = process.WorkingSet64,
                                MainWindowTitle = process.MainWindowTitle,
                                MainWindowHandle = process.MainWindowHandle
                            };
                            
                            processes.Add(processInfo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogDebug(ex, "Failed to get info for process {ProcessId}", process.Id);
                        }
                        finally
                        {
                            process.Dispose();
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enumerate processes");
                }

                return processes.OrderByDescending(p => p.WorkingSet).ToList();
            });
        }

        public async Task<ProcessInfo?> GetProcessByIdAsync(int processId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var process = Process.GetProcessById(processId);
                    
                    return new ProcessInfo
                    {
                        Id = process.Id,
                        Name = process.ProcessName,
                        Path = GetProcessPath(process),
                        StartTime = process.StartTime,
                        WorkingSet = process.WorkingSet64,
                        MainWindowTitle = process.MainWindowTitle,
                        MainWindowHandle = process.MainWindowHandle
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get process {ProcessId}", processId);
                    return null;
                }
            });
        }

        public async Task<bool> KillProcessAsync(int processId)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var process = Process.GetProcessById(processId);
                    process.Kill();
                    _logger.LogInformation("Successfully killed process {ProcessId}", processId);
                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to kill process {ProcessId}", processId);
                    return false;
                }
            });
        }

        public async Task<bool> StartProcessAsync(string path, string? arguments = null)
        {
            return await Task.Run(() =>
            {
                try
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = path,
                        Arguments = arguments ?? string.Empty,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using var process = Process.Start(startInfo);
                    
                    if (process != null)
                    {
                        _logger.LogInformation("Started process: {Path} {Arguments}", path, arguments ?? string.Empty);
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to start process: {Path} {Arguments}", path, arguments ?? string.Empty);
                    return false;
                }
            });
        }

        private static string GetProcessPath(Process process)
        {
            try
            {
                return process.MainModule?.FileName ?? string.Empty;
            }
            catch
            {
                // Some processes cannot be accessed due to permissions
                return string.Empty;
            }
        }
    }
}
