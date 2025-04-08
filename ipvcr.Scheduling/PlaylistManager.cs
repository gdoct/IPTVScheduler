namespace ipvcr.Scheduling;

using System.Collections.Generic;
using System.IO.Abstractions;
using ipvcr.Scheduling.Shared;

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
    private object _lock = new object();

    public PlaylistManager(ISettingsManager settingsManager, IFileSystem fileSystem)
    {
        _playlistItems = new List<ChannelInfo>();
        _filesystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        if (string.IsNullOrEmpty(settingsManager.Settings.M3uPlaylistPath))
        {
            throw new ArgumentException("M3uPlaylistPath cannot be null or empty.", nameof(settingsManager));
        }
        if (_filesystem.File.Exists(settingsManager.Settings.M3uPlaylistPath))
        {
            LoadPlaylist(settingsManager.Settings.M3uPlaylistPath);
        }
        settingsManager.SettingsChanged += async (sender, args) =>
        {
            if (string.Compare(args.NewSettings.M3uPlaylistPath, _m3uPlaylistPath, StringComparison.OrdinalIgnoreCase) != 0)
            {
                await LoadPlaylistAsync(args.NewSettings.M3uPlaylistPath);
            }
        };
    }

    public Task LoadFromFileAsync(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));
        }
        if (!_filesystem.File.Exists(filePath))
        {
            throw new FileNotFoundException($"Playlist file not found: {filePath}");
        }

        return LoadPlaylistAsync(filePath);
    }

    private void LoadPlaylist(string playlistPath)
    {
        // call LoadPlaylistAsync and block until it completes
        try
        {
            var task = LoadPlaylistAsync(playlistPath);
            task.Wait(); // Block synchronously
        }
        catch (AggregateException ae)
        {
            // Handle the AggregateException and throw the inner exception
            throw new InvalidOperationException("Failed to load playlist", ae.InnerException);
        }
    }

    private async Task LoadPlaylistAsync(string playlistPath)
    {
        var parser = new M3uParser(_filesystem, playlistPath);
        var channels = new List<ChannelInfo>();
        await foreach (var channel in parser.ParsePlaylistAsync())
        {
            channels.Add(channel);
        }

        lock (_lock)
        {
            _playlistItems.Clear();
            _playlistItems.AddRange(channels);
        }
        _m3uPlaylistPath = playlistPath;
        _m3uPlaylistPath = playlistPath;
    }
    public List<ChannelInfo> GetPlaylistItems()
    {
        // return a copy of the collection
        lock (_lock)
        {
            return new(_playlistItems);
        }
    }
}