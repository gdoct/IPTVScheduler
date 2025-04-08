namespace ipvcr.Scheduling.Shared;
public class SchedulerSettings
{
    public string MediaPath { get; set; } = "/media";
    public string DataPath { get; set; } = "/data";
    public string M3uPlaylistPath { get; set; } = "/data/m3u-playlist.m3u";
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

    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}