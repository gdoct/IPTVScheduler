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
        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        // Act
        mock.Setup(m => m.RunProcess("chmod", It.IsAny<string>())).Returns(("", "", 0));
        mock.Setup(m => m.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("", "", 0));
        scheduler.ScheduleTask(task);
        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // "export TASK_JOB_ID='{task.Id}';\r\nexport TASK_DEFINITION='{{\"Id\":\"{task.Id}\",\"Name\":\"whatever\",\"Command\":\"\",\"StartTime\":\"2026-03-27T15:01:12.0384718+01:00\",\"TaskType\":1}}'\r\necho \"ffmpeg -i http://whatever -t 60 -c copy -f mp4 whatever.mp4\" | at 15:01 03-27-2026\"", "", 0
        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        Assert.Single(scheduler.FetchScheduledTasks());

        mock.Setup(m => m.RunProcess("atrm", It.IsAny<string>())).Returns(("", "", 0));
        scheduler.CancelTask(task.Id);
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_ScheduleTask_ThrowsWhenStartDateInThePast()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);
        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(-1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(-1).AddMinutes(10),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => scheduler.ScheduleTask(task));
    }

    [Fact]
    public void AtRecordingScheduler_ScheduleTask_ThrowsWhenUnableToSetExecute()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);
        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        // Act
        mock.Setup(m => m.RunProcess("chmod", It.IsAny<string>())).Returns(("", "error", 2));
        Assert.Throws<InvalidOperationException>(() => scheduler.ScheduleTask(task));
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_ScheduleTask_ThrowsWhenUnableToCallAtCommand()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);
        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        // Act
        mock.Setup(m => m.RunProcess("chmod", It.IsAny<string>())).Returns(("", "", 0));
        mock.Setup(m => m.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("", "error", 2));
        Assert.Throws<InvalidOperationException>(() => scheduler.ScheduleTask(task));
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // "export TASK_JOB_ID='{task.Id}';\r\nexport TASK_DEFINITION='{{\"Id\":\"{task.Id}\",\"Name\":\"whatever\",\"Command\":\"\",\"StartTime\":\"2026-03-27T15:01:12.0384718+01:00\",\"TaskType\":1}}'\r\necho \"ffmpeg -i http://whatever -t 60 -c copy -f mp4 whatever.mp4\" | at 15:01 03-27-2026\"", "", 0
        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        var coll = scheduler.FetchScheduledTasks();
        Assert.Single(coll);
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_IgnoresIfUnableToParse()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "error", 2));
        // Act & Assert
        var coll = scheduler.FetchScheduledTasks();
        Assert.Throws<InvalidOperationException>(() => coll.ToList());
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_IgnoresIfAtqHasIncorrectOutput()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("\tkjd", "", 0));
        var coll = scheduler.FetchScheduledTasks();
        Assert.Empty(coll.ToList());
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_FailsIfAtqHasIncorrectFormatting()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("33 15", "", 0));
        var coll = scheduler.FetchScheduledTasks();
        Assert.Empty(coll.ToList());
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_FailsIfAtHasErrorCode()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // "export TASK_JOB_ID='{task.Id}';\r\nexport TASK_DEFINITION='{{\"Id\":\"{task.Id}\",\"Name\":\"whatever\",\"Command\":\"\",\"StartTime\":\"2026-03-27T15:01:12.0384718+01:00\",\"TaskType\":1}}'\r\necho \"ffmpeg -i http://whatever -t 60 -c copy -f mp4 whatever.mp4\" | at 15:01 03-27-2026\"", "", 0
        // var cmd = task.Command;
        // var taskid = task.Id.ToString();
        // var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        // var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns(("", "error", 2));
        // Act & Assert
        var coll = scheduler.FetchScheduledTasks();
        Assert.Throws<InvalidOperationException>(() => coll.ToList());
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_SkipsIfSerializedTaskIsUndefined()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var expected = $"TASK_ID=\"{taskid}\"\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        // Act & Assert
        Assert.Empty(scheduler.FetchScheduledTasks().ToList());
        mock.VerifyAll();
    }


[Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_SkipsIfSerializedTaskIsNull()
    {
        // Arrange
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var expected = $"TASK_ID=\"{taskid}\"\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        // Act & Assert
        Assert.Empty(scheduler.FetchScheduledTasks().ToList());
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CancelTask()
    {
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        mock.Setup(m => m.RunProcess("atrm", It.IsAny<string>())).Returns(("", "", 0));
        scheduler.CancelTask(task.Id);
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CancelTask_ThrowsIfNotScheduled()
    {
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        Assert.Throws<InvalidOperationException>(() => scheduler.CancelTask(Guid.NewGuid()));
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CancelTask_ThrowsIfCancelFails()
    {
         var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='null'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        Assert.Throws<InvalidOperationException>(() => scheduler.CancelTask(task.Id));
        mock.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CancelTask_ThrowsIfAtrmFails()
    {
        var mock = new Mock<IProcessRunner>(MockBehavior.Strict);
        mock.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(mock.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        var task = recording.ToScheduledTask();

        // Assert
        mock.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        mock.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        mock.Setup(m => m.RunProcess("atrm", It.IsAny<string>())).Returns(("", "error", 2));
        Assert.Throws<InvalidOperationException>(() => scheduler.CancelTask(task.Id));
        mock.VerifyAll();
    }
}