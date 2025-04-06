using System.IO.Abstractions;

namespace ipvcr.Scheduling;

public class SchedulerSettings
{
    public string OutputPath { get; set; } = "/media";
    public string LoggingPath { get; set; } = "/var/log/iptvscheduler";
    public string M3uPlaylistPath { get; set; } = "/var/lib/iptvscheduler/m3u-playlist.m3u";
}

public interface ISettingsManager
{
    SchedulerSettings Settings { get; set; }

    event EventHandler<SettingsManager.SettingsChangedEventArgs>? SettingsChanged;
}

public class SettingsManager : ISettingsManager
{
    public event EventHandler<SettingsChangedEventArgs>? SettingsChanged;

    public class SettingsChangedEventArgs : EventArgs
    {
        public SchedulerSettings NewSettings { get; }

        public SettingsChangedEventArgs(SchedulerSettings newSettings)
        {
            NewSettings = newSettings;
        }
    }

    const string SETTINGS_FILENAME = "/etc/iptvscheduler/settings.json";


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
            return _settings;
        }
        set
        {
            SaveSettings(value);
            SettingsChanged?.Invoke(this, new SettingsChangedEventArgs(value));
        }
    }
    private SchedulerSettings LoadSettings()
    {
        // deserialize SchedulerSettings from json file SETTINGS_FILENAME
        lock (_lock)
        {
            if (!_filesystem.File.Exists(SETTINGS_FILENAME))
            {
                return new SchedulerSettings();
            }

            try
            {
                var json = _filesystem.File.ReadAllText(SETTINGS_FILENAME);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return new SchedulerSettings();
                }

                return System.Text.Json.JsonSerializer.Deserialize<SchedulerSettings>(json)
                       ?? new SchedulerSettings();
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
            _settings = settings;
            SaveSettingsToFile(System.Text.Json.JsonSerializer.Serialize(settings));
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
        try
        {
            if (!_filesystem.File.Exists(SETTINGS_FILENAME))
            {
                using var fileStream = _filesystem.File.Create(SETTINGS_FILENAME);
            }

            _filesystem.File.WriteAllText(SETTINGS_FILENAME, jsonSettings);
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
