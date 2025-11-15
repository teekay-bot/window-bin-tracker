using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using WindowBinTracker.Models;

namespace WindowBinTracker.Services
{
    public interface ISettingsService
    {
        Task<RecycleBinSettings> GetSettingsAsync();
        Task SaveSettingsAsync(RecycleBinSettings settings);
        Task ResetToDefaultsAsync();
    }

    public class SettingsService : ISettingsService
    {
        private readonly ILogger<SettingsService> _logger;
        private readonly string _settingsFilePath;

        public SettingsService(ILogger<SettingsService> logger)
        {
            _logger = logger;
            _settingsFilePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "WindowBinTracker",
                "settings.json");
            
            EnsureSettingsDirectoryExists();
        }

        private void EnsureSettingsDirectoryExists()
        {
            var directory = Path.GetDirectoryName(_settingsFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                _logger.LogInformation($"Created settings directory: {directory}");
            }
        }

        public async Task<RecycleBinSettings> GetSettingsAsync()
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    _logger.LogInformation("Settings file not found, creating default settings");
                    var defaultSettings = new RecycleBinSettings();
                    await SaveSettingsAsync(defaultSettings);
                    return defaultSettings;
                }

                var json = await File.ReadAllTextAsync(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<RecycleBinSettings>(json);
                
                if (settings == null)
                {
                    _logger.LogWarning("Failed to deserialize settings, using defaults");
                    return new RecycleBinSettings();
                }

                _logger.LogDebug("Settings loaded successfully");
                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load settings, using defaults");
                return new RecycleBinSettings();
            }
        }

        public async Task SaveSettingsAsync(RecycleBinSettings settings)
        {
            try
            {
                var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                
                await File.WriteAllTextAsync(_settingsFilePath, json);
                _logger.LogInformation("Settings saved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
                throw;
            }
        }

        public async Task ResetToDefaultsAsync()
        {
            _logger.LogInformation("Resetting settings to defaults");
            var defaultSettings = new RecycleBinSettings();
            await SaveSettingsAsync(defaultSettings);
        }
    }
}
