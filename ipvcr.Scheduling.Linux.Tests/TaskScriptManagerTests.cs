namespace ipvcr.Scheduling.Linux.Tests;

using System.IO.Abstractions;
using ipvcr.Scheduling.Linux;
using ipvcr.Scheduling.Shared;
using Moq;

public class TaskScriptManagerTests
{
    private readonly MockRepository _mocks;

    public TaskScriptManagerTests()
    {
        _mocks = new MockRepository(MockBehavior.Strict);    
    }

    [Fact]
    public void TaskScriptManager_Ctor_Valid()
    {
        // Arrange
        var fileSystem = _mocks.Create<IFileSystem>();
        var settingsManager = _mocks.Create<ISettingsManager>();
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsManager.Object);

        // Act & Assert
        Assert.NotNull(taskScriptManager);
        _mocks.VerifyAll();
    }

    [Fact]
    public void TaskScriptManager_TaskScriptPath_Valid()
    {
        // Arrange
        var fileSystem = _mocks.Create<IFileSystem>();
        var settingsManager = _mocks.Create<ISettingsManager>();
        settingsManager.SetupGet(s => s.Settings).Returns(new SchedulerSettings { DataPath = "/data/path" });
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsManager.Object);
        var taskId = Guid.NewGuid();

        // Act
        var result = taskScriptManager.TaskScriptPath(taskId);

        // Assert
        Assert.Equal(Path.Combine("/data/path", "tasks", $"{taskId}.sh"), result);
        _mocks.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TaskScriptManager_WriteTaskScript_Valid(bool moveAfterCompletion)
    {
        // Arrange
        var fileSystem = _mocks.Create<IFileSystem>();
        var settingsManager = _mocks.Create<ISettingsManager>();
        settingsManager.SetupGet(s => s.Settings).Returns(new SchedulerSettings { DataPath = "/data/path" });
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsManager.Object);
        var taskId = Guid.NewGuid();
        var task = new ScheduledTask(taskId, "name", "TestTask", DateTimeOffset.Now, "{}");
        var file = _mocks.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
        file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Callback<string, string>((path, content) =>
        {
            Assert.Equal(Path.Combine("/data/path", "tasks", $"{taskId}.sh"), path);
            Assert.False(string.IsNullOrWhiteSpace(content));
        });
        file.Setup(f => f.SetAttributes(It.IsAny<string>(), It.IsAny<FileAttributes>())).Verifiable();
        // Act
        taskScriptManager.WriteTaskScript(task, moveAfterCompletion);
        _mocks.VerifyAll();
    }

    [Fact]
    public void TaskScriptManager_WriteTaskScript_ThrowsFileNotFound()
    {
        // Arrange
        var fileSystem = _mocks.Create<IFileSystem>();
        var settingsManager = _mocks.Create<ISettingsManager>();
        settingsManager.SetupGet(s => s.Settings).Returns(new SchedulerSettings { DataPath = "/data/path" });
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsManager.Object);
        var taskId = Guid.NewGuid();
        var task = new ScheduledTask(taskId, "name", "TestTask", DateTimeOffset.Now, "{}");
        var file = _mocks.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
        file.Setup(f => f.WriteAllText(It.IsAny<string>(), It.IsAny<string>())).Throws(new FileNotFoundException());
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => taskScriptManager.WriteTaskScript(task, false));
        _mocks.VerifyAll();
    }

    [Fact]
    public void TaskScriptManager_ReadTaskScript_Valid()
    {
        // Arrange
        var fileSystem = _mocks.Create<IFileSystem>();
        var settingsManager = _mocks.Create<ISettingsManager>();
        settingsManager.SetupGet(s => s.Settings).Returns(new SchedulerSettings { DataPath = "/data/path" });
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsManager.Object);
        var taskId = Guid.NewGuid();
        var task = new ScheduledTask(taskId, "name", "TestTask", DateTimeOffset.Now, "{}");
        var file = _mocks.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
        file.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(f => f.ReadAllText(It.IsAny<string>())).Returns("#!/bin/bash");
        // Act
        var result = taskScriptManager.ReadTaskScript(taskId);
        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("#!/bin/bash", result);
        _mocks.VerifyAll();
    }

    [Fact]
    public void TaskScriptManager_ReadTaskScript_ThrowsIfFileNotFound()
    {
        // Arrange
        var fileSystem = _mocks.Create<IFileSystem>();
        var settingsManager = _mocks.Create<ISettingsManager>();
        settingsManager.SetupGet(s => s.Settings).Returns(new SchedulerSettings { DataPath = "/data/path" });
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsManager.Object);
        var taskId = Guid.NewGuid();
        var file = _mocks.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
        file.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => taskScriptManager.ReadTaskScript(taskId));
        _mocks.VerifyAll();
    }

    [Fact]
    public void TaskScriptManager_MoveScriptToFailed_Valid()
    {
        // Arrange
        var fileSystem = _mocks.Create<IFileSystem>();
        var settingsManager = _mocks.Create<ISettingsManager>();
        settingsManager.SetupGet(s => s.Settings).Returns(new SchedulerSettings { DataPath = "/data/path" });
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsManager.Object);
        var taskId = Guid.NewGuid();
        var file = _mocks.Create<IFile>();
        var directory = _mocks.Create<IDirectory>();
        fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
        fileSystem.SetupGet(fs => fs.Directory).Returns(directory.Object);
        directory.Setup(f => f.Exists(It.IsAny<string>())).Returns(false);
        directory.Setup(f => f.CreateDirectory(It.IsAny<string>())).Returns(Mock.Of<IDirectoryInfo>());
        file.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(f => f.Move(It.IsAny<string>(), It.IsAny<string>())).Verifiable();
        // Act
        taskScriptManager.MoveScriptToFailed(taskId);
        // Assert
        _mocks.VerifyAll();
    }

    [Fact]
    public void TaskScriptManager_RemoveTaskScript_Valid()
    {
        // Arrange
        var fileSystem = _mocks.Create<IFileSystem>();
        var settingsManager = _mocks.Create<ISettingsManager>();
        settingsManager.SetupGet(s => s.Settings).Returns(new SchedulerSettings { DataPath = "/data/path" });
        var taskScriptManager = new TaskScriptManager(fileSystem.Object, settingsManager.Object);
        var taskId = Guid.NewGuid();
        var file = _mocks.Create<IFile>();
        fileSystem.SetupGet(fs => fs.File).Returns(file.Object);
        file.Setup(f => f.Exists(It.IsAny<string>())).Returns(true);
        file.Setup(f => f.Delete(It.IsAny<string>())).Verifiable();
        // Act
        taskScriptManager.RemoveTaskScript(taskId);
        // Assert
        _mocks.VerifyAll();
    }

}