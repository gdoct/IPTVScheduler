namespace ipvcr.Scheduling;

using System.Collections.Generic;
using System.IO.Abstractions;


public interface IPlaylistManager
{
    List<ChannelInfo> GetPlaylistItems();
    Task LoadFromFileAsync(string filePath);
}

public class PlaylistManager : IPlaylistManager
{
    private readonly List<ChannelInfo> _playlistItems;
    private readonly IFileSystem _filesystem;
    private string? _m3uPlaylistPath;

    public PlaylistManager(ISettingsManager settingsManager) : this(settingsManager, new FileSystem())
    {

    }

    public PlaylistManager(ISettingsManager settingsManager, IFileSystem fileSystem)
    {
        _playlistItems = new List<ChannelInfo>();
        _filesystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

        if (_filesystem.File.Exists(settingsManager.Settings.M3uPlaylistPath))
        {
            Task.Run(() => LoadPlaylist(settingsManager.Settings.M3uPlaylistPath))
                .ContinueWith(t =>
                {
                    if (t.IsFaulted)
                    {
                        throw new Exception("Failed to load playlist", t.Exception);
                    }
                });
        }
        settingsManager.SettingsChanged += async (sender, args) =>
        {
            if (string.Compare(args.NewSettings.M3uPlaylistPath, _m3uPlaylistPath, StringComparison.OrdinalIgnoreCase) != 0)
            {
                await LoadPlaylist(args.NewSettings.M3uPlaylistPath);
            }
        };
    }

    public Task LoadFromFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }

        return LoadPlaylist(filePath);
    }

    private async Task LoadPlaylist(string playlistPath)
    {
        var parser = new M3uParser(playlistPath);
        _playlistItems.Clear();
        await foreach (var channel in parser.ParsePlaylistAsync())
        {
            _playlistItems.Add(channel);
        }
        _m3uPlaylistPath = playlistPath;
    }
    public List<ChannelInfo> GetPlaylistItems()
    {
        return _playlistItems;
    }
}