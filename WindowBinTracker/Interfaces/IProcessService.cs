using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WindowNativeTemplate.Interfaces
{
    public interface IProcessService
    {
        Task<IEnumerable<ProcessInfo>> GetRunningProcessesAsync();
        Task<ProcessInfo?> GetProcessByIdAsync(int processId);
        Task<bool> KillProcessAsync(int processId);
        Task<bool> StartProcessAsync(string path, string? arguments = null);
    }

    public class ProcessInfo
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public long WorkingSet { get; set; }
        public string? MainWindowTitle { get; set; }
        public IntPtr MainWindowHandle { get; set; }
    }
}
