using Moq;

namespace ipvcr.Scheduling.Linux.Tests;

public class AtRecordingSchedulerTests
{
    [Fact]
    public void AtRecordingScheduler_CtorChecksIfInstalled()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);
        Assert.NotNull(scheduler);
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CtorThrowsIfNotInstalled()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns((string.Empty, string.Empty, 0));
        Assert.Throws<MissingDependencyException>(() => AtRecordingScheduler.CreateWithProcessRunner(mock.Object));
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_ScheduleTask()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);
        var task = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        }.ToScheduledTask();

        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        // Act
        mock.Setup(m => m.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("", "", 0));
        scheduler.ScheduleTask(task);
        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3       Thu Apr  3 15:30:00 2025 a guido", "", 0));

        // "export TASK_JOB_ID='{task.Id}';\r\nexport TASK_DEFINITION='{{\"Id\":\"{task.Id}\",\"Name\":\"whatever\",\"Command\":\"\",\"StartTime\":\"2026-03-27T15:01:12.0384718+01:00\",\"TaskType\":1}}'\r\necho \"ffmpeg -i http://whatever -t 60 -c copy -f mp4 whatever.mp4\" | at 15:01 03-27-2026\"", "", 0
        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(task);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";
        
        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        Assert.Single(scheduler.FetchScheduledTasks());

        mock.Setup(m => m.RunProcess("atrm", It.IsAny<string>())).Returns(("", "", 0));
        scheduler.CancelTask(task.Id);
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        mock.VerifyAll();
    }
}