using System.IO.Abstractions;
using ipvcr.Scheduling.Shared;
using Moq;

namespace ipvcr.Scheduling.Linux.Tests;

public class AtRecordingSchedulerTests
{
    [Fact]
    public void AtRecordingScheduler_CtorChecksIfInstalled()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());
        Assert.NotNull(scheduler);
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CtorThrowsIfNotInstalled()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns((string.Empty, string.Empty, 0));
        Assert.Throws<MissingDependencyException>(() => AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>()));
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_ScheduleTask()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var settings = new SchedulerSettings { MediaPath = "/new/path" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);
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

        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        // Act
        processrunner.Setup(m => m.RunProcess("chmod", It.IsAny<string>())).Returns(("", "", 0));
        processrunner.Setup(m => m.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("", "", 0));
        fs.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        fs.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        fs.Setup(fs => fs.File.Delete(It.IsAny<string>()));
#if WINDOWS
        Assert.Throws<DirectoryNotFoundException>(() => scheduler.ScheduleTask(task));
#else
        scheduler.ScheduleTask(task);
        // Assert
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // "export TASK_JOB_ID='{task.Id}';\r\nexport TASK_DEFINITION='{{\"Id\":\"{task.Id}\",\"Name\":\"whatever\",\"Command\":\"\",\"StartTime\":\"2026-03-27T15:01:12.0384718+01:00\",\"TaskType\":1}}'\r\necho \"ffmpeg -i http://whatever -t 60 -c copy -f mp4 whatever.mp4\" | at 15:01 03-27-2026\"", "", 0
        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        Assert.Single(scheduler.FetchScheduledTasks());

        processrunner.Setup(m => m.RunProcess("atrm", It.IsAny<string>())).Returns(("", "", 0));
        scheduler.CancelTask(task.Id);
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        processrunner.VerifyAll();
#endif
    }

    [Fact]
    public void AtRecordingScheduler_ScheduleTask_ThrowsWhenStartDateInThePast()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());
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
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var settings = new SchedulerSettings { MediaPath = "/new/path" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);

        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);
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

        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        // Act
        processrunner.Setup(m => m.RunProcess("chmod", It.IsAny<string>())).Returns(("", "error", 2));
        fs.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        fs.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
#if WINDOWS
        Assert.Throws<DirectoryNotFoundException>(() => scheduler.ScheduleTask(task));
#else
        Assert.Throws<InvalidOperationException>(() => scheduler.ScheduleTask(task));
        processrunner.VerifyAll();
#endif
    }

    [Fact]
    public void AtRecordingScheduler_ScheduleTask_ThrowsWhenUnableToCallAtCommand()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var settings = new SchedulerSettings { MediaPath = "/new/path" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);
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

        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "", 0));
        Assert.Empty(scheduler.FetchScheduledTasks());
        // Act
        processrunner.Setup(m => m.RunProcess("chmod", It.IsAny<string>())).Returns(("", "", 0));
        processrunner.Setup(m => m.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("", "error", 2));
        fs.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        fs.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);

#if WINDOWS
        Assert.Throws<DirectoryNotFoundException>(() => scheduler.ScheduleTask(task));
#else
        Assert.Throws<InvalidOperationException>(() => scheduler.ScheduleTask(task));
        processrunner.VerifyAll();
#endif
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());

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
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // "export TASK_JOB_ID='{task.Id}';\r\nexport TASK_DEFINITION='{{\"Id\":\"{task.Id}\",\"Name\":\"whatever\",\"Command\":\"\",\"StartTime\":\"2026-03-27T15:01:12.0384718+01:00\",\"TaskType\":1}}'\r\necho \"ffmpeg -i http://whatever -t 60 -c copy -f mp4 whatever.mp4\" | at 15:01 03-27-2026\"", "", 0
        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        var coll = scheduler.FetchScheduledTasks();
        Assert.Single(coll);
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_IgnoresIfUnableToParse()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("", "error", 2));
        // Act & Assert
        var coll = scheduler.FetchScheduledTasks();
        Assert.Throws<InvalidOperationException>(() => coll.ToList());
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_IgnoresIfAtqHasIncorrectOutput()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());

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
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("\tkjd", "", 0));
        var coll = scheduler.FetchScheduledTasks();
        Assert.Empty(coll.ToList());
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_FailsIfAtqHasIncorrectFormatting()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());

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
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("33 15", "", 0));
        var coll = scheduler.FetchScheduledTasks();
        Assert.Empty(coll.ToList());
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_FailsIfAtHasErrorCode()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());

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

        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // "export TASK_JOB_ID='{task.Id}';\r\nexport TASK_DEFINITION='{{\"Id\":\"{task.Id}\",\"Name\":\"whatever\",\"Command\":\"\",\"StartTime\":\"2026-03-27T15:01:12.0384718+01:00\",\"TaskType\":1}}'\r\necho \"ffmpeg -i http://whatever -t 60 -c copy -f mp4 whatever.mp4\" | at 15:01 03-27-2026\"", "", 0
        // var cmd = task.Command;
        // var taskid = task.Id.ToString();
        // var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        // var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns(("", "error", 2));
        // Act & Assert
        var coll = scheduler.FetchScheduledTasks();
        Assert.Throws<InvalidOperationException>(() => coll.ToList());
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_SkipsIfSerializedTaskIsUndefined()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());

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

        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var expected = $"TASK_ID=\"{taskid}\"\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        // Act & Assert
        Assert.Empty(scheduler.FetchScheduledTasks().ToList());
        processrunner.VerifyAll();
    }


    [Fact]
    public void AtRecordingScheduler_FetchScheduledTasks_SkipsIfSerializedTaskIsNull()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());

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

        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var expected = $"TASK_ID=\"{taskid}\"\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        // Act & Assert
        Assert.Empty(scheduler.FetchScheduledTasks().ToList());
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CancelTask()
    {
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var settings = new SchedulerSettings { DataPath = "/new/path" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);

        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);

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
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        processrunner.Setup(m => m.RunProcess("atrm", It.IsAny<string>())).Returns(("", "", 0));

        fs.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        fs.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        fs.Setup(fs => fs.File.Delete(It.IsAny<string>()));

        scheduler.CancelTask(task.Id);
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CancelTask_ThrowsIfNotScheduled()
    {
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());

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
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        Assert.Throws<InvalidOperationException>(() => scheduler.CancelTask(Guid.NewGuid()));
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CancelTask_ThrowsIfCancelFails()
    {
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, Mock.Of<ISettingsManager>());

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
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='null'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        Assert.Throws<InvalidOperationException>(() => scheduler.CancelTask(task.Id));
        processrunner.VerifyAll();
    }

    [Fact]
    public void AtRecordingScheduler_CancelTask_ThrowsIfAtrmFails()
    {
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var settings = new SchedulerSettings { MediaPath = "/new/path" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);

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
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));
        fs.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        fs.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        fs.Setup(fs => fs.File.Delete(It.IsAny<string>()));

        var cmd = task.Command;
        var taskid = task.Id.ToString();
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        var expected = $"TASK_ID=\"{taskid}\"\nTASK_DEFINITION='{taskdefinition}'\necho \"{cmd}\" | at {task.StartTime:HH:mm MM/dd/yyyy}";

        processrunner.Setup(m => m.RunProcess("at", $"-c 3")).Returns((expected, "", 0));
        processrunner.Setup(m => m.RunProcess("atrm", It.IsAny<string>())).Returns(("", "error", 2));
        Assert.Throws<InvalidOperationException>(() => scheduler.CancelTask(task.Id));
        processrunner.VerifyAll();
    }
}