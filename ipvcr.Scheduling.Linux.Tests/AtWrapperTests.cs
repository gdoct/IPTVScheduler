namespace ipvcr.Scheduling.Linux.Tests;

using System.IO.Abstractions;
using System.Text.Json;
using ipvcr.Scheduling.Linux;
using ipvcr.Scheduling.Shared;
using Moq;

public class AtWrapperTests
{
    private readonly Mock<IFileSystem> _filesystemMock;
    private readonly Mock<IProcessRunner> _processRunnerMock;
    private readonly Mock<ISettingsManager> _settingsManagerMock;

    public AtWrapperTests()
    {
        _filesystemMock = new Mock<IFileSystem>();
        _processRunnerMock = new Mock<IProcessRunner>();
        _settingsManagerMock = new Mock<ISettingsManager>();

    }

    [Fact]
    public void AtWrapper_ScheduleTaskThrowsIfAtReturnsErrorCode()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsManagerMock.Setup(sm => sm.Settings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsManagerMock.Object);

        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        
        var expectedError = "Error occurred";
        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("", expectedError, 1));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.ScheduleTask(task));
        Assert.Equal($"Failed to schedule task: {expectedError}", exception.Message);
    }

    [Fact]
    public void AtWrapper_ScheduleTask_ReturnsJobId()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsManagerMock.Setup(sm => sm.Settings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsManagerMock.Object);

        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        
        var expectedJobId = 123;
        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((expectedJobId.ToString(), "", 0));

        // Act
        var jobId = atWrapper.ScheduleTask(task);

        // Assert
        Assert.Equal(expectedJobId, jobId);
    }

    [Fact]
    public void AtWrapper_ScheduleTask_ThrowsIfJobIdIsNotInteger()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsManagerMock.Setup(sm => sm.Settings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsManagerMock.Object);

        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        
        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("abc", "", 0));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.ScheduleTask(task));
        Assert.Equal("Failed to parse job ID from output: abc", exception.Message);
    }

  //  [Fact]
    public void AtWrapper_GetTaskDetails_ReturnsTaskDetails()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsManagerMock.Setup(sm => sm.Settings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsManagerMock.Object);

        var jobId = 123;
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var taskJson = JsonSerializer.Serialize(task);
        
        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(($"TASK_DEFINITION='{taskJson}'", "", 0));

        // Act
        var (returnedJobId, newtask) = atWrapper.GetTaskDetails(jobId);

        // Assert
        Assert.Equal(jobId, returnedJobId);
        Assert.NotNull(task);
        Assert.Equal(task.Id, newtask.Id);
        Assert.Equal(task.Name, newtask.Name);
        Assert.Equal(task.Command, newtask.Command);
        Assert.Equal(task.StartTime, newtask.StartTime);
        Assert.Equal(task.InnerScheduledTask, newtask.InnerScheduledTask);
    }

  //  [Fact]
    public void AtWrapper_GetTaskDetails_FailsIfEnvironmentNotSet()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsManagerMock.Setup(sm => sm.Settings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsManagerMock.Object);

        var jobId = 123;
        
        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns((jobId.ToString(), "TASK_DEFINITION=''", 0));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
        Assert.Equal($"Failed to parse task definition for job {jobId}", exception.Message);
    }

  //  [Fact]  
    public void AtWrapper_GetTaskDetails_FailsIfTaskNotDeserialized()
    {
        // Arrange
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsManagerMock.Setup(sm => sm.Settings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsManagerMock.Object);
        var incorrectJson = "{ null }";
        var task = new ScheduledTask(Guid.NewGuid(), "Test Task", "echo Hello World", DateTimeOffset.UtcNow.AddMinutes(10), "{}");
        var jobId = 123;
        
        _processRunnerMock.Setup(pr => pr.RunProcess("at", It.IsAny<string>()))

            .Returns(($"TASK_DEFINITION='{incorrectJson}'", string.Empty, 0));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
        Assert.Equal($"Failed to deserialize task for job {jobId}", exception.Message);
    } 

    [Fact]
    public void AtWrapper_GetTaskDetails_ThrowsIfAtqReturnsErrorCode()
    {
        // Arrange
        _processRunnerMock.Setup(pr => pr.RunProcess("which", "at")).Returns(("at", "", 0));
        _settingsManagerMock.Setup(sm => sm.Settings).Returns(new SchedulerSettings { DataPath = "/tmp" });
        var atWrapper = new AtWrapper(_filesystemMock.Object, _processRunnerMock.Object, _settingsManagerMock.Object);

        var jobId = 123;
        var expectedError = "Error occurred";
        
        _processRunnerMock.Setup(pr => pr.RunProcess(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(("", expectedError, 1));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => atWrapper.GetTaskDetails(jobId));
        Assert.Equal($"Failed to get job details for job {jobId}: {expectedError}", exception.Message);
    }
}