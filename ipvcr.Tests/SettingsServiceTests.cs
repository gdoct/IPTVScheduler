using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using System.Text.Json;
using ipvcr.Auth;
using ipvcr.Scheduling.Shared.Settings;
using Moq;
using Xunit;

namespace ipvcr.Tests;

public class SettingsServiceTests
{
    [Fact]
    public void SettingsService_ShouldRaiseSettingsChangedEvent_WhenSchedulerSettingsChanged()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var tokenManager = new Mock<ITokenManager>();
        var service = new SettingsService(mockFileSystem, tokenManager.Object);

        bool eventRaised = false;
        SettingsType? changedSettingsType = null;
        object? newSettings = null;

        service.SettingsChanged += (sender, args) => 
        {
            eventRaised = true;
            changedSettingsType = args.SettingsType;
            newSettings = args.NewSettings;
        };

        // Act
        var settings = new SchedulerSettings { MediaPath = "/test/path" };
        service.SchedulerSettings = settings;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(SettingsType.Scheduler, changedSettingsType);
        Assert.NotNull(newSettings);
        var typedSettings = Assert.IsType<SchedulerSettings>(newSettings);
        Assert.Equal("/test/path", typedSettings.MediaPath);
    }

    [Fact]
    public void SettingsService_ShouldRaiseSettingsChangedEvent_WhenPlaylistSettingsChanged()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var tokenManager = new Mock<ITokenManager>();
        var service = new SettingsService(mockFileSystem, tokenManager.Object);

        bool eventRaised = false;
        SettingsType? changedSettingsType = null;
        object? newSettings = null;

        service.SettingsChanged += (sender, args) => 
        {
            eventRaised = true;
            changedSettingsType = args.SettingsType;
            newSettings = args.NewSettings;
        };

        // Act
        var settings = new PlaylistSettings { M3uPlaylistPath = "/test/playlist.m3u" };
        service.PlaylistSettings = settings;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(SettingsType.Playlist, changedSettingsType);
        Assert.NotNull(newSettings);
        var typedSettings = Assert.IsType<PlaylistSettings>(newSettings);
        Assert.Equal("/test/playlist.m3u", typedSettings.M3uPlaylistPath);
    }

    [Fact]
    public void SettingsService_ShouldRaiseSettingsChangedEvent_WhenFfmpegSettingsChanged()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var tokenManager = new Mock<ITokenManager>();
        var service = new SettingsService(mockFileSystem, tokenManager.Object);

        bool eventRaised = false;
        SettingsType? changedSettingsType = null;
        object? newSettings = null;

        service.SettingsChanged += (sender, args) => 
        {
            eventRaised = true;
            changedSettingsType = args.SettingsType;
            newSettings = args.NewSettings;
        };

        // Act
        var settings = new FfmpegSettings { Codec = "testcodec" };
        service.FfmpegSettings = settings;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(SettingsType.Ffmpeg, changedSettingsType);
        Assert.NotNull(newSettings);
        var typedSettings = Assert.IsType<FfmpegSettings>(newSettings);
        Assert.Equal("testcodec", typedSettings.Codec);
    }

    [Fact]
    public void SettingsService_ShouldRaiseSettingsChangedEvent_WhenSslSettingsChanged()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var tokenManager = new Mock<ITokenManager>();
        var service = new SettingsService(mockFileSystem, tokenManager.Object);

        bool eventRaised = false;
        SettingsType? changedSettingsType = null;
        object? newSettings = null;

        service.SettingsChanged += (sender, args) => 
        {
            eventRaised = true;
            changedSettingsType = args.SettingsType;
            newSettings = args.NewSettings;
        };

        // Act
        var settings = new SslSettings { UseSsl = true };
        service.SslSettings = settings;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(SettingsType.Ssl, changedSettingsType);
        Assert.NotNull(newSettings);
        var typedSettings = Assert.IsType<SslSettings>(newSettings);
        Assert.True(typedSettings.UseSsl);
    }

    [Fact]
    public void SettingsService_ShouldRaiseSettingsChangedEvent_WhenAdminPasswordSettingsChanged()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var tokenManager = new Mock<ITokenManager>();
        var service = new SettingsService(mockFileSystem, tokenManager.Object);

        bool eventRaised = false;
        SettingsType? changedSettingsType = null;
        object? newSettings = null;

        service.SettingsChanged += (sender, args) => 
        {
            eventRaised = true;
            changedSettingsType = args.SettingsType;
            newSettings = args.NewSettings;
        };

        // Act
        var settings = new AdminPasswordSettings { AdminUsername = "testadmin" };
        service.AdminPasswordSettings = settings;

        // Assert
        Assert.True(eventRaised);
        Assert.Equal(SettingsType.AdminPassword, changedSettingsType);
        Assert.NotNull(newSettings);
        var typedSettings = Assert.IsType<AdminPasswordSettings>(newSettings);
        Assert.Equal("testadmin", typedSettings.AdminUsername);
    }

    [Fact]
    public void SettingsService_ShouldNotThrowException_WhenNoEventHandlerRegistered()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var tokenManager = new Mock<ITokenManager>();
        var service = new SettingsService(mockFileSystem, tokenManager.Object);

        // Act & Assert (should not throw)
        var exception = Record.Exception(() => service.SchedulerSettings = new SchedulerSettings());
        Assert.Null(exception);
    }
}