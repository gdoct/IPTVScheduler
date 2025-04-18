using System.IO.Abstractions;

namespace ipvcr.Scheduling.Shared.Settings;

public class PlaylistSettingsManager : BaseSettingsManager<PlaylistSettings>, ISettingsManager<PlaylistSettings> 
{
    const string SETTINGS_FILENAME = "playlist-settings.json";

    public PlaylistSettingsManager(IFileSystem filesystem) 
        : base(filesystem, SETTINGS_FILENAME, "/data")
    {
    }
}