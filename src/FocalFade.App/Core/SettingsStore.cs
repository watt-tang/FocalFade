using FocalFade.Models;
using FocalFade.Services;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;

namespace FocalFade.Core;

public sealed class SettingsStore : ISettingsStore
{
    private readonly ILogger<SettingsStore> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private AppSettings _settings;
    private readonly object _lock = new();
    private System.Threading.Timer? _saveTimer;

    public SettingsStore(ILogger<SettingsStore> logger)
    {
        _logger = logger;
        _settings = new AppSettings();
        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        SettingsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "FocalFade", "settings.json");
    }

    public string SettingsFilePath { get; }

    public AppSettings Settings
    {
        get { lock (_lock) return _settings; }
    }

    public event EventHandler<AppSettings>? SettingsChanged;

    public void Load()
    {
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath)!;
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(SettingsFilePath))
            {
                _logger.LogInformation("No settings file found, using defaults");
                _settings = new AppSettings();
                Save();
                return;
            }

            var json = File.ReadAllText(SettingsFilePath);
            var loaded = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions);

            if (loaded == null)
            {
                _logger.LogWarning("Settings file was null after deserialization, using defaults");
                _settings = new AppSettings();
                return;
            }

            // Schema migration
            if (loaded.SchemaVersion < AppSettings.CurrentSchemaVersion)
            {
                _logger.LogInformation("Migrating settings from schema {Old} to {New}",
                    loaded.SchemaVersion, AppSettings.CurrentSchemaVersion);

                // v1 -> v2: new fields get defaults from AppSettings record
                // The deserialized object already has defaults for missing JSON fields
                // Just bump the schema version
                loaded = loaded with { SchemaVersion = AppSettings.CurrentSchemaVersion };
            }

            _settings = loaded;
            _logger.LogInformation("Settings loaded from {Path}", SettingsFilePath);
        }
        catch (JsonException ex)
        {
            BackupCorruptSettings();
            _logger.LogError(ex, "Corrupt settings file, backing up and using defaults");
            _settings = new AppSettings();
            Save();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load settings, using defaults");
            _settings = new AppSettings();
        }
    }

    public void Save()
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsFilePath)!;
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var tempPath = SettingsFilePath + ".tmp";
                var json = JsonSerializer.Serialize(_settings, _jsonOptions);
                File.WriteAllText(tempPath, json);

                // Atomic replace
                if (File.Exists(SettingsFilePath))
                    File.Replace(tempPath, SettingsFilePath, SettingsFilePath + ".bak");
                else
                    File.Move(tempPath, SettingsFilePath);

                _logger.LogDebug("Settings saved");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save settings");
            }
        }
    }

    public void Update(Func<AppSettings, AppSettings> modifier)
    {
        lock (_lock)
        {
            _settings = modifier(_settings);
        }
        SettingsChanged?.Invoke(this, Settings);
        // Debounced save
        _saveTimer?.Dispose();
        _saveTimer = new System.Threading.Timer(_ => Save(), null, 500, Timeout.Infinite);
    }

    public void ResetToDefaults()
    {
        lock (_lock)
        {
            _settings = new AppSettings();
        }
        Save();
        SettingsChanged?.Invoke(this, Settings);
        _logger.LogInformation("Settings reset to defaults");
    }

    private void BackupCorruptSettings()
    {
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                var backupPath = SettingsFilePath + $".corrupt.{DateTime.Now:yyyyMMddHHmmss}.json";
                File.Copy(SettingsFilePath, backupPath);
                _logger.LogInformation("Corrupt settings backed up to {Path}", backupPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to backup corrupt settings");
        }
    }
}
