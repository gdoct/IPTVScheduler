using System.IO.Abstractions;
using static ipvcr.Scheduling.Shared.ISettingsManager;

namespace ipvcr.Scheduling.Shared;


public class SettingsManager : ISettingsManager
{
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    const string SETTINGS_FILENAME = "settings.json";

    public SettingsManager(IFileSystem filesystem)
    {
        _filesystem = filesystem ?? throw new ArgumentNullException(nameof(filesystem));
        _settings = LoadSettings();
    }

    private readonly IFileSystem _filesystem;
    private SchedulerSettings _settings;
    private readonly object _lock = new();

    public SchedulerSettings Settings
    {
        get
        {
            return new SchedulerSettings
            {
                MediaPath = _settings.MediaPath,
                DataPath = _settings.DataPath,
                M3uPlaylistPath = _settings.M3uPlaylistPath,
                RemoveTaskAfterExecution = _settings.RemoveTaskAfterExecution,
                AdminUsername = _settings.AdminUsername,
                AdminPassword = string.Empty
            };;
        }
        set
        {
            var changedSettings = new SchedulerSettings
            {
                MediaPath = value.MediaPath,
                DataPath = value.DataPath,
                M3uPlaylistPath = value.M3uPlaylistPath,
                RemoveTaskAfterExecution = value.RemoveTaskAfterExecution,
                AdminUsername = value.AdminUsername,
                AdminPassword = _settings.AdminPassword
            };
            SaveSettings(changedSettings);
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(value));
        }
    }

    public string GetAdminPassword()
    {
        return _settings.AdminPassword;
    }

    public void UpdateAdminPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password cannot be null or empty.", nameof(newPassword));
        }

        _settings.AdminPassword = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(newPassword));
        var settings = _settings;
        SaveSettings(settings);
    }

    private SchedulerSettings LoadSettings()
    {
        // deserialize SchedulerSettings from json file SETTINGS_FILENAME
        lock (_lock)
        {
            var fullSettingsPath = Path.Combine("/data", SETTINGS_FILENAME);

            if (!_filesystem.File.Exists(fullSettingsPath))
            {
                var defaultSettings = new SchedulerSettings
                {
                    AdminUsername = SchedulerSettings.DEFAULT_USERNAME,
                    AdminPassword = SchedulerSettings.DEFAULT_PASSWORD
                };
                _settings = defaultSettings;
                SaveSettingsToFile(System.Text.Json.JsonSerializer.Serialize(defaultSettings));
                return defaultSettings;
            }

            try
            {
                var json = _filesystem.File.ReadAllText(fullSettingsPath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new SchedulerSettings();
                }

                var deserialized = System.Text.Json.JsonSerializer.Deserialize<SchedulerSettings>(json)
                       ?? new SchedulerSettings();
                if (string.IsNullOrWhiteSpace(deserialized.AdminUsername))
                {
                    deserialized.AdminUsername = SchedulerSettings.DEFAULT_USERNAME;
                }
                if (string.IsNullOrWhiteSpace(deserialized.AdminPassword))
                {
                    deserialized.AdminPassword = SchedulerSettings.DEFAULT_PASSWORD;
                }
                return deserialized;
            }
            catch (UnauthorizedAccessException)
            {
                throw new InvalidOperationException("The settings file is not readable.");
            }
            catch (System.Text.Json.JsonException)
            {
                throw new InvalidOperationException("The settings file contains invalid JSON.");
            }
            catch (IOException ex)
            {
                throw new InvalidOperationException("An error occurred while reading the settings file.", ex);
            }
        }
    }

    private void SaveSettings(SchedulerSettings settings)
    {
        lock (_lock)
        {
            var changedSettings = new SchedulerSettings
            {
                MediaPath = settings.MediaPath,
                DataPath = settings.DataPath,
                M3uPlaylistPath = settings.M3uPlaylistPath,
                RemoveTaskAfterExecution = settings.RemoveTaskAfterExecution,
                AdminUsername = settings.AdminUsername,
                AdminPassword = _settings.AdminPassword
            };
            _settings = changedSettings;
            SaveSettingsToFile(System.Text.Json.JsonSerializer.Serialize(changedSettings));
        }
    }
    private void SaveSettingsToFile(string jsonSettings)
    {
        // save jsonSettings to file SETTINGS_FILENAME
        // if file does not exist, create it
        // if file exists, overwrite it
        // if file is not writable, throw exception
        // if file is not readable, throw exception
        // if file is not a valid json, throw exception
        var fullSettingsPath = Path.Combine("/data", SETTINGS_FILENAME);
        try
        {
            if (!_filesystem.File.Exists(fullSettingsPath))
            {
                using var fileStream = _filesystem.File.Create(fullSettingsPath);
            }

            _filesystem.File.WriteAllText(fullSettingsPath, jsonSettings);
        }
        catch (UnauthorizedAccessException)
        {
            throw new InvalidOperationException("The settings file is not writable.");
        }
        catch (IOException ex)
        {
            throw new InvalidOperationException("An error occurred while writing to the settings file.", ex);
        }
    }
}
