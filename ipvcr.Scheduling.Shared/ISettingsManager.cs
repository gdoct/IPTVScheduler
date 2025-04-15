namespace ipvcr.Scheduling.Shared;
public class SchedulerSettings
{
    public const string DEFAULT_USERNAME = "admin";
    public const string DEFAULT_PASSWORD = "aXB2Y3I="; // "ipvcr" hashed
    public string MediaPath { get; set; } = "/media";
    public string DataPath { get; set; } = "/data";
    public string M3uPlaylistPath { get; set; } = "/data/m3u-playlist.m3u";
    public bool RemoveTaskAfterExecution { get; set; } = true;
    public string AdminUsername { get; set; } = DEFAULT_USERNAME;
    public string AdminPasswordHash { get; set; } = DEFAULT_PASSWORD; // "ipvcr" hashed
}

public interface ISettingsManager
{
    public class SettingsChangedEventArgs : EventArgs
    {
        public SchedulerSettings NewSettings { get; }

        public SettingsChangedEventArgs(SchedulerSettings newSettings)
        {
            NewSettings = newSettings;
        }
    }
    SchedulerSettings Settings { get; set; }
    string GetAdminPasswordHash();
    void UpdateAdminPassword(string newPassword);
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}