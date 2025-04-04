using System.Text.Json;
using ipvcr.Scheduling;
using Moq;
using System.IO.Abstractions.TestingHelpers;
using System.IO.Abstractions;

namespace ipvcr.Tests;

public class SettingsManagerTests
{
    private const string SettingsFilePath = "/etc/iptvscheduler/settings.json";

    [Fact]
    public void LoadSettings_FileDoesNotExist_ReturnsDefaultSettings()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var settingsManager = CreateSettingsManager(mockFileSystem);

        // Act
        var settings = settingsManager.Settings;

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("/media", settings.OutputPath);
    }

    [Fact]
    public void LoadSettings_FileExistsWithValidJson_ReturnsDeserializedSettings()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        var validJson = JsonSerializer.Serialize(new SchedulerSettings { OutputPath = "/tmp/output" });
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData(validJson));
        var settingsManager = CreateSettingsManager(mockFileSystem);

        // Act
        var settings = settingsManager.Settings;

        // Assert
        Assert.NotNull(settings);
        Assert.Equal("/tmp/output", settings.OutputPath);
    }

    [Fact]
    public void LoadSettings_FileExistsWithInvalidJson_ThrowsInvalidOperationException()
    {
        // Arrange
        var mockFileSystem = new MockFileSystem();
        mockFileSystem.AddFile(SettingsFilePath, new MockFileData("Invalid JSON"));
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => _ = CreateSettingsManager(mockFileSystem));
    }

    private SettingsManager CreateSettingsManager(MockFileSystem mockFileSystem)
    {
        var mockFileSystemWrapper = new Mock<IFileSystem>();
        mockFileSystemWrapper.Setup(fs => fs.File).Returns(mockFileSystem.File);
        mockFileSystemWrapper.Setup(fs => fs.Directory).Returns(mockFileSystem.Directory);
        mockFileSystemWrapper.Setup(fs => fs.Path).Returns(mockFileSystem.Path);

        return new SettingsManager(mockFileSystemWrapper.Object); // Adjust constructor if dependency injection is needed
    }
}