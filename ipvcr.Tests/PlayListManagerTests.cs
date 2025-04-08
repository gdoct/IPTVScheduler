namespace ipvcr.Tests;

using System;
using System.IO.Abstractions;
using System.Text;
using System.Threading.Tasks;
using ipvcr.Scheduling;
using Moq;
public class PlaylistManagerTests
{
    class MockedFileSystemStream : FileSystemStream
    {
        public MockedFileSystemStream(Stream stream, string path, bool isAsync)
            : base(stream, path, isAsync)
        {
        }
    }

    private readonly Mock<ISettingsManager> _settingsManagerMock;
    private readonly Mock<IFileSystem> _fileSystemMock;

    public PlaylistManagerTests()
    {
        _settingsManagerMock = new Mock<ISettingsManager>();
        _fileSystemMock = new Mock<IFileSystem>();
    }


    [Fact]
    public void PlaylistManager_Constructor_ValidSettings()
    {
        var settingsManager = new Mock<ISettingsManager>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { M3uPlaylistPath = playlistPath };
        settingsManager.SetupGet(s => s.Settings).Returns(s);
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        var playlistManager = new PlaylistManager(settingsManager.Object, fileSystem.Object);

        // Assert
        Assert.NotNull(playlistManager);
    }

    [Fact]
    public void PlaylistManager_Constructor_TaskFaults()
    {
        var settingsManager = new Mock<ISettingsManager>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Throws<IOException>();
//            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { M3uPlaylistPath = playlistPath };
        settingsManager.SetupGet(s => s.Settings).Returns(s);
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        Assert.Throws<InvalidOperationException>(() => new PlaylistManager(settingsManager.Object, fileSystem.Object));
    }

    [Fact]
    public async Task PlaylistManager_LoadFromFileAsync_ValidPath()
    {
        // Arrange
        var settingsManager = new Mock<ISettingsManager>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.Exists(playlistPath)).Returns(true);
        file.Setup(x => x.OpenRead(playlistPath))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { M3uPlaylistPath = playlistPath };
        settingsManager.SetupGet(s => s.Settings).Returns(s);
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        var playlistManager = new PlaylistManager(settingsManager.Object, fileSystem.Object);

        var newPlaylistPath = "new_playlist.m3u";
        var m3uContent2 = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 7\" tvg-name=\"4K | RTL 7\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL7.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";

        var file2 = new Mock<IFile>();
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent2));
        fileSystem.SetupGet(x => x.File).Returns(file2.Object);
        file2.Setup(x => x.Exists(newPlaylistPath)).Returns(true);
        file2.Setup(x => x.OpenRead(newPlaylistPath))
            .Returns(new MockedFileSystemStream(stream2, newPlaylistPath, true));

        await playlistManager.LoadFromFileAsync(newPlaylistPath);
        var lst = new List<ChannelInfo>();
        Assert.Equal(3, playlistManager.GetPlaylistItems().Count);
    }

    [Fact]
    public void PlaylistManager_LoadFromFileAsync_EmptyPath_Throws()
    {
        // Arrange
        var settingsManager = new Mock<ISettingsManager>();
        var fileSystem = new Mock<IFileSystem>();

        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { M3uPlaylistPath = "playlist.m3u" };
        fileSystem.Setup(fs => fs.File.Exists(s.M3uPlaylistPath)).Returns(true);
        settingsManager.SetupGet(s => s.Settings).Returns(s);
        var playlistManager = new PlaylistManager(settingsManager.Object, fileSystem.Object);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(() => playlistManager.LoadFromFileAsync(string.Empty));
    }

    [Fact]
    public void PlaylistManager_LoadFromFileAsync_NotFoundThrows()
    {
        // Arrange
        var settingsManager = new Mock<ISettingsManager>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "not_found_playlist.m3u";
        var s = new SchedulerSettings
        {
            M3uPlaylistPath = playlistPath
        };
        settingsManager.SetupGet(s => s.Settings).Returns(s);
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(false);

        var playlistManager = new PlaylistManager(settingsManager.Object, fileSystem.Object);

        // Act & Assert
        Assert.ThrowsAsync<FileNotFoundException>(() => playlistManager.LoadFromFileAsync(playlistPath));
    }

    [Fact]
    public void PlaylistManager_Ctor_EmptyPathThrows()
    {
        // Arrange
        var settingsManager = new Mock<ISettingsManager>();
        
        var s = new SchedulerSettings
        {
            M3uPlaylistPath = string.Empty
        };
        settingsManager.SetupGet(s => s.Settings).Returns(s);

        // Act & Assert
        Assert.Throws<ArgumentException>(() => new PlaylistManager(settingsManager.Object, Mock.Of<IFileSystem>()));
    }

    [Fact]
    public void PlaylistManager_Ctor_EmptyFileSystemThrows()
    {
        // Arrange
        var settingsManager = new Mock<ISettingsManager>();
        var o = new object();
#pragma warning disable CS8604 // Possible null reference argument.
        Assert.Throws<ArgumentNullException>(() => new PlaylistManager(settingsManager.Object, o as IFileSystem));
#pragma warning restore CS8604 // Possible null reference argument.
    }

    [Fact]
    public void PlaylistManager_SettingsChanged_SamePlaylistFile()
    {
        var settingsManager = new Mock<ISettingsManager>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";

        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        //file.Setup(x => x.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { M3uPlaylistPath = playlistPath };
        settingsManager.SetupGet(s => s.Settings).Returns(s);
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        var playlistManager = new PlaylistManager(settingsManager.Object, fileSystem.Object);

        // Act
        settingsManager.Raise(x => x.SettingsChanged += null, new SettingsManager.SettingsChangedEventArgs(s));

        // Assert
        // No exception should be thrown
        fileSystem.VerifyAll();
        settingsManager.VerifyAll();
    }

     [Fact]
    public void PlaylistManager_SettingsChanged_ReloadPlaylist()
    {
        var settingsManager = new Mock<ISettingsManager>();
        var fileSystem = new Mock<IFileSystem>();
        var playlistPath = "valid_playlist.m3u";
        var newplaylistPath = "new_playlist.m3u";
        var m3uContent = "#EXTM3U\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 4\" tvg-name=\"4K | RTL 4\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL4.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 4\n" +
                             "http://example.com/stream1\n" +
                             "##### SUPERGROUP #####\n" +
                             "#EXTINF:-1 tvg-id=\"4K | RTL 5\" tvg-name=\"4K | RTL 5\" tvg-logo=\"https://logo/images/logos/NEDERLAND-NEW1/RTL5.png\" group-title=\"NL | 4K NEDERLAND\",4K | RTL 5\n" +
                             "http://example.com/stream2\n";
        var expectedChannels = new List<ChannelInfo>
            {
                new ("1", "4K | RTL 4", "https://logo/images/logos/NEDERLAND-NEW1/RTL4.png", new Uri("http://example.com/stream1"), "NL | 4K NEDERLAND"),
                new ("2", "4K | RTL 5", "https://logo/images/logos/NEDERLAND-NEW1/RTL5.png", new Uri("http://example.com/stream2"), "NL | 4K NEDERLAND")
            };

        var file = new Mock<IFile>();
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        var stream2 = new MemoryStream(Encoding.UTF8.GetBytes(m3uContent));
        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream, playlistPath, true));

        var s = new SchedulerSettings { M3uPlaylistPath = playlistPath };
        settingsManager.SetupGet(s => s.Settings).Returns(s);
        fileSystem.Setup(fs => fs.File.Exists(playlistPath)).Returns(true);

        var playlistManager = new PlaylistManager(settingsManager.Object, fileSystem.Object);

        // Act
        var newSettings = new SchedulerSettings { M3uPlaylistPath = newplaylistPath };

        fileSystem.SetupGet(x => x.File).Returns(file.Object);
        file.Setup(x => x.OpenRead(It.IsAny<string>()))
            .Returns(new MockedFileSystemStream(stream2, newplaylistPath, true));
        fileSystem.Setup(fs => fs.File.Exists(newplaylistPath)).Returns(true);

        settingsManager.Raise(x => x.SettingsChanged += null, new SettingsManager.SettingsChangedEventArgs(newSettings));

        // Assert
        // No exception should be thrown
        fileSystem.VerifyAll();
        settingsManager.VerifyAll();
    }
}