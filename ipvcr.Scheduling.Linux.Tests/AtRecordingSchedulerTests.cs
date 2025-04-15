using System.IO.Abstractions;
using ipvcr.Scheduling.Shared;
using Moq;
using Microsoft.Extensions.Logging;

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
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Loose);
        var fs = new Mock<IFileSystem>(MockBehavior.Loose);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        
        // Setup settings
        var settings = new SchedulerSettings { 
            MediaPath = "/media/recordings",
            DataPath = "/data"
        };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        // Setup which command to check for at utility
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("/usr/bin/at", string.Empty, 0));
        
        // Create scheduler
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(
            processrunner.Object, 
            fs.Object, 
            settingsmgr.Object,
            logger.Object
        );
        
        // Create test recording
        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddDays(1), // Future date
            ChannelUri = "http://example.com/stream",
            EndTime = DateTime.Now.AddDays(1).AddMinutes(30),
            Filename = "recording.mp4",
            Name = "Test Recording"
        };
        var task = recording.ToScheduledTask();

        // Setup directory creation
        var tasksDirectory = Path.Combine("/data", "tasks");
        fs.Setup(m => m.Directory.Exists(tasksDirectory)).Returns(false);
        var mockDirInfo = new Mock<IDirectoryInfo>();
        fs.Setup(m => m.Directory.CreateDirectory(tasksDirectory)).Returns(mockDirInfo.Object);
        
        // Setup file operations
        var scriptPath = Path.Combine(tasksDirectory, $"{recording.Id}.sh");
        fs.Setup(m => m.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
          .Callback<string, string>((path, content) => 
          {
              // Verify path and content are as expected
              Assert.Equal(scriptPath, path);
              Assert.Contains(recording.Id.ToString(), content);
              Assert.Contains("#!/bin/bash", content);
          });
        //fs.Setup(m => m.File.Exists(It.IsAny<string>())).Returns(true);
        
        // Setup script execution
        processrunner.Setup(m => m.RunProcess("chmod", $"+x {scriptPath}")).Returns(("", "", 0));
        processrunner.Setup(m => m.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("", "", 0));

#if WINDOWS
        Assert.Throws<DirectoryNotFoundException>(() => scheduler.ScheduleTask(task));
#else
        // Act
        scheduler.ScheduleTask(task);
        
        // Assert - All mocks should be verified
        processrunner.VerifyAll();
        fs.VerifyAll();
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
        fs.Setup(fs => fs.Directory.Exists(It.IsAny<string>())).Returns(true);
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
        var settings = new SchedulerSettings { DataPath = "/data" };
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
        fs.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        fs.Setup(fs => fs.Directory.Exists("/data/tasks")).Returns(false);

        // Mock the directory info returned by CreateDirectory
        var mockDirInfo = new Mock<IDirectoryInfo>();
        fs.Setup(fs => fs.Directory.CreateDirectory("/data/tasks")).Returns(mockDirInfo.Object);

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
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();

        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);

        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://example.com/stream",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(30),
            Filename = "recording.mp4",
            Name = "Test Recording"
        };

        // Set up atq command to return a job ID
        processrunner.Setup(m => m.RunProcess("atq", string.Empty))
            .Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // Set up at -c command to return a task definition with the recording serialized
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        processrunner.Setup(m => m.RunProcess("at", "-c 3"))
            .Returns((
                $"#!/bin/bash\nexport TASK_DEFINITION='{taskdefinition}'", 
                "", 
                0
            ));

        // Act
        var tasks = scheduler.FetchScheduledTasks().ToList();

        // Assert
        Assert.Single(tasks);
        var fetchedTask = tasks.First();
        
        // The task should have been deserialized correctly from the TASK_DEFINITION
        Assert.Equal(recording.Id, fetchedTask.Id);
        Assert.Equal(recording.Name, fetchedTask.Name);
        
        // Verify all mocks were called as expected
        processrunner.VerifyAll();
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
        Assert.Throws<InvalidOperationException>(() => scheduler.FetchScheduledTasks());
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

        // Assert - the input is formatted incorrectly so no job IDs are extracted
        processrunner.Setup(m => m.RunProcess("atq", string.Empty)).Returns(("33 15", "", 0));
        // This test just verifies no exceptions are thrown and that we get an empty collection
        var tasks = scheduler.FetchScheduledTasks().ToList();
        Assert.Empty(tasks);
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

        // Mock the atq command to return a job ID
        processrunner.Setup(m => m.RunProcess("atq", string.Empty))
                     .Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // Mock the 'at -c 3' command to return an error
        processrunner.Setup(m => m.RunProcess("at", "-c 3"))
                     .Returns(("", "error", 2));

        // When we try to access the tasks
        var exception = Assert.Throws<InvalidOperationException>(() => scheduler.FetchScheduledTasks().ToList());
        
        // Verify the exception message contains our expected error
        Assert.Contains("Failed to get job details for job 3", exception.Message);
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
        var settings = new SchedulerSettings { DataPath = "/data" };
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

        // Assert
        processrunner.Setup(m => m.RunProcess("atq", string.Empty))
            .Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        // Format the task definition exactly as expected by the regex in AtRecordingScheduler
        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        processrunner.Setup(m => m.RunProcess("at", $"-c 3"))
            .Returns((
                $"#!/bin/bash\nexport TASK_DEFINITION='{taskdefinition}'", 
                "", 
                0
            ));
        
        processrunner.Setup(m => m.RunProcess("atrm", "3")).Returns(("", "", 0));

        fs.Setup(fs => fs.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()));
        fs.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        fs.Setup(fs => fs.File.Delete(It.IsAny<string>()));

        scheduler.CancelTask(recording.Id);
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
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);

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
        processrunner.Setup(m => m.RunProcess("atq", string.Empty))
            .Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));

        var taskdefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        processrunner.Setup(m => m.RunProcess("at", $"-c 3"))
            .Returns((
                $"TASK_DEFINITION='{taskdefinition}'", 
                "", 
                0
            ));

        fs.Setup(fs => fs.File.Exists(It.IsAny<string>())).Returns(true);
        fs.Setup(fs => fs.File.Delete(It.IsAny<string>()));
        
        // This is the mock that should be verified - setting up atrm to fail
        processrunner.Setup(m => m.RunProcess("atrm", "3")).Returns(("", "error", 2));
        
        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => scheduler.CancelTask(recording.Id));
        processrunner.VerifyAll();
    }

    #region GetTaskDefinition Tests

    [Fact]
    public void AtRecordingScheduler_GetTaskDefinition_ReturnsScriptContent_WhenFileExists()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);
        
        var taskId = Guid.NewGuid();
        var expectedPath = Path.Combine("/data", "tasks", $"{taskId}.sh");
        var expectedContent = "#!/bin/bash\necho 'test script content'";
        
        // Setup the mock to return the file exists and content
        fs.Setup(f => f.File.Exists(expectedPath)).Returns(true);
        fs.Setup(f => f.File.ReadAllText(expectedPath)).Returns(expectedContent);

        // Act
        var result = scheduler.GetTaskDefinition(taskId);

        // Assert
        Assert.Equal(expectedContent, result);
        fs.Verify(f => f.File.Exists(expectedPath), Times.Once);
        fs.Verify(f => f.File.ReadAllText(expectedPath), Times.Once);
    }

    [Fact]
    public void AtRecordingScheduler_GetTaskDefinition_ReturnsEmptyString_WhenFileDoesNotExist()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);
        
        var taskId = Guid.NewGuid();
        var expectedPath = Path.Combine("/data", "tasks", $"{taskId}.sh");
        
        // Setup the mock to return file does not exist
        fs.Setup(f => f.File.Exists(expectedPath)).Returns(false);

        // Act
        var result = scheduler.GetTaskDefinition(taskId);

        // Assert
        Assert.Equal(string.Empty, result);
        fs.Verify(f => f.File.Exists(expectedPath), Times.Once);
    }

    [Fact]
    public void AtRecordingScheduler_GetTaskDefinition_ReturnsEmptyString_WhenFileReadThrowsException()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);
        
        var taskId = Guid.NewGuid();
        var expectedPath = Path.Combine("/data", "tasks", $"{taskId}.sh");
        
        // Setup the mock to throw an exception when reading the file
        fs.Setup(f => f.File.Exists(expectedPath)).Returns(true);
        fs.Setup(f => f.File.ReadAllText(expectedPath)).Throws(new IOException("Test exception"));

        // Act
        var result = scheduler.GetTaskDefinition(taskId);

        // Assert
        Assert.Equal(string.Empty, result);
        fs.Verify(f => f.File.Exists(expectedPath), Times.Once);
        fs.Verify(f => f.File.ReadAllText(expectedPath), Times.Once);
        
        // Verify log error was called
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<IOException>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    #endregion

    #region UpdateTaskDefinition Tests

    [Fact]
    public void AtRecordingScheduler_UpdateTaskDefinition_UpdatesFileContent_WhenFileExists()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);
        
        var taskId = Guid.NewGuid();
        var expectedPath = Path.Combine("/data", "tasks", $"{taskId}.sh");
        var newContent = "#!/bin/bash\necho 'updated script content'";
        
        // Setup the mock for file operations
        fs.Setup(f => f.File.Exists(expectedPath)).Returns(true);
        fs.Setup(f => f.File.WriteAllText(expectedPath, newContent));

        // Act
        scheduler.UpdateTaskDefinition(taskId, newContent);

        // Assert
        fs.Verify(f => f.File.Exists(expectedPath), Times.Once);
        fs.Verify(f => f.File.WriteAllText(expectedPath, newContent), Times.Once);
    }

    [Fact]
    public void AtRecordingScheduler_UpdateTaskDefinition_DoesNothing_WhenDefinitionIsEmpty()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);
        
        var taskId = Guid.NewGuid();
        string emptyDefinition = "";

        // Act
        scheduler.UpdateTaskDefinition(taskId, emptyDefinition);

        // Assert - verify file operations are never called
        fs.Verify(f => f.File.Exists(It.IsAny<string>()), Times.Never);
        fs.Verify(f => f.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        
        // Verify warning was logged
        logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void AtRecordingScheduler_UpdateTaskDefinition_DoesNothing_WhenFileDoesNotExist()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);
        
        var taskId = Guid.NewGuid();
        var expectedPath = Path.Combine("/data", "tasks", $"{taskId}.sh");
        var newContent = "#!/bin/bash\necho 'new script content'";
        
        // Setup the mock for file operations
        fs.Setup(f => f.File.Exists(expectedPath)).Returns(false);

        // Act
        scheduler.UpdateTaskDefinition(taskId, newContent);

        // Assert
        fs.Verify(f => f.File.Exists(expectedPath), Times.Once);
        fs.Verify(f => f.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        
        // Verify warning was logged
        logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<Exception>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public void AtRecordingScheduler_UpdateTaskDefinition_ThrowsInvalidOperationException_WhenWriteThrowsException()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);
        
        var taskId = Guid.NewGuid();
        var expectedPath = Path.Combine("/data", "tasks", $"{taskId}.sh");
        var newContent = "#!/bin/bash\necho 'test content'";
        var exceptionMessage = "Test write exception";
        
        // Setup the mock to throw an exception when writing the file
        fs.Setup(f => f.File.Exists(expectedPath)).Returns(true);
        fs.Setup(f => f.File.WriteAllText(expectedPath, newContent)).Throws(new IOException(exceptionMessage));

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => scheduler.UpdateTaskDefinition(taskId, newContent));
        
        // Verify the exception contains the original error message
        Assert.Contains(exceptionMessage, exception.Message);
        
        fs.Verify(f => f.File.Exists(expectedPath), Times.Once);
        fs.Verify(f => f.File.WriteAllText(expectedPath, newContent), Times.Once);
        
        // Verify log error was called
        logger.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<IOException>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    #endregion

    #region EnsureTaskDirectoryExists Test

    [Fact]
    public void AtRecordingScheduler_EnsureTaskDirectoryExists_ThrowsInvalidOperationException_WhenCreateDirectoryFails()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);
        
        // Setup directory doesn't exist
        fs.Setup(f => f.Directory.Exists(Path.Combine("/data", "tasks"))).Returns(false);
        
        // Setup CreateDirectory to throw an exception
        var exceptionMessage = "Access denied to create directory";
        fs.Setup(f => f.Directory.CreateDirectory(Path.Combine("/data", "tasks"))).Throws(new UnauthorizedAccessException(exceptionMessage));
        
        // Setup to test the method via ScheduleTask which calls EnsureTaskDirectoryExists internally
        var task = new ScheduledTask(
            Guid.NewGuid(), 
            "Test Task", 
            "echo 'test command'", 
            DateTimeOffset.Now.AddDays(1), 
            "test task serialized"
        );

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => scheduler.ScheduleTask(task));
        
        // Verify it wraps the original exception's message
        Assert.Contains(exceptionMessage, exception.Message);
        Assert.Contains("Failed to create tasks directory", exception.Message);
        
        // Verify the methods were called
        fs.Verify(f => f.Directory.Exists(Path.Combine("/data", "tasks")), Times.Once);
        fs.Verify(f => f.Directory.CreateDirectory(Path.Combine("/data", "tasks")), Times.Once);
    }

    #endregion

    #region GenerateTaskScript Tests

    [Fact]
    public void AtRecordingScheduler_GenerateTaskScript_CreatesScriptWithRemoveCommand_WhenRemoveAfterCompletionIsTrue()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { 
            DataPath = "/data",
            RemoveTaskAfterExecution = true
        };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        // Setup fs.Directory.Exists to return true to skip directory creation
        fs.Setup(f => f.Directory.Exists(It.IsAny<string>())).Returns(true);
        
        // Setup WriteAllText to capture the script content
        string capturedContent = null;
        fs.Setup(f => f.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
          .Callback<string, string>((_, content) => capturedContent = content);
          
        // Setup other mocks needed for ScheduleTask
        fs.Setup(f => f.File.Exists(It.IsAny<string>())).Returns(false);
        processrunner.Setup(p => p.RunProcess("chmod", It.IsAny<string>())).Returns(("", "", 0));
        processrunner.Setup(p => p.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("", "", 0));
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);
        
        var task = new ScheduledTask(
            Guid.NewGuid(), 
            "Test Task", 
            "echo 'test command'", 
            DateTimeOffset.Now.AddDays(1), 
            "test task serialized"
        );

        // Act
        scheduler.ScheduleTask(task);

        // Assert
        Assert.NotNull(capturedContent);
        Assert.Contains("# it will be deleted after execution", capturedContent);
        Assert.Contains("rm -f", capturedContent);
        Assert.DoesNotContain("mv", capturedContent);
    }

    [Fact]
    public void AtRecordingScheduler_GenerateTaskScript_CreatesScriptWithMoveCommand_WhenRemoveAfterCompletionIsFalse()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { 
            DataPath = "/data",
            RemoveTaskAfterExecution = false
        };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        // Setup fs.Directory.Exists to return true to skip directory creation
        fs.Setup(f => f.Directory.Exists(It.IsAny<string>())).Returns(true);
        
        // Setup WriteAllText to capture the script content
        string capturedContent = null;
        fs.Setup(f => f.File.WriteAllText(It.IsAny<string>(), It.IsAny<string>()))
          .Callback<string, string>((_, content) => capturedContent = content);
          
        // Setup other mocks needed for ScheduleTask
        fs.Setup(f => f.File.Exists(It.IsAny<string>())).Returns(false);
        processrunner.Setup(p => p.RunProcess("chmod", It.IsAny<string>())).Returns(("", "", 0));
        processrunner.Setup(p => p.RunProcess("/bin/bash", It.IsAny<string>())).Returns(("", "", 0));
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object);
        
        var task = new ScheduledTask(
            Guid.NewGuid(), 
            "Test Task", 
            "echo 'test command'", 
            DateTimeOffset.Now.AddDays(1), 
            "test task serialized"
        );

        // Act
        scheduler.ScheduleTask(task);

        // Assert
        Assert.NotNull(capturedContent);
        Assert.Contains("# it will be moved to the folder 'completed' after execution", capturedContent);
        Assert.Contains("mkdir -p", capturedContent);
        Assert.Contains("mv", capturedContent);
        Assert.Contains("completed", capturedContent);
        Assert.DoesNotContain("rm -f", capturedContent);
    }

    #endregion

    #region SafeDeleteFile Tests

    [Fact]
    public void AtRecordingScheduler_SafeDeleteFile_LogsWarning_WhenDeleteThrowsException()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);
        
        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        
        var taskId = recording.Id;
        var filePath = Path.Combine("/data", "tasks", $"{taskId}.sh");
        
        // Setup mocks to simulate failure during deletion
        fs.Setup(f => f.File.Exists(filePath)).Returns(true);
        fs.Setup(f => f.File.Delete(filePath)).Throws(new IOException("Permission denied while deleting file"));
        
        // Setup mocks for FetchScheduledTasks
        processrunner.Setup(m => m.RunProcess("atq", string.Empty))
            .Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));
            
        // Format the task definition exactly as the AtRecordingScheduler expects it
        var taskDefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        processrunner.Setup(m => m.RunProcess("at", $"-c 3"))
            .Returns((
                $"TASK_DEFINITION='{taskDefinition}'", 
                "", 
                0
            ));
            
        // Setup mock for atrm called by CancelTask
        processrunner.Setup(m => m.RunProcess("atrm", "3")).Returns(("", "", 0));

        // Act - We expect no exception to be thrown from SafeDeleteFile itself
        scheduler.CancelTask(taskId);

        // Assert - Verify the warning was logged
        logger.Verify(l => l.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => true),
            It.IsAny<IOException>(),
            It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
            
        fs.Verify(f => f.File.Exists(filePath), Times.Once);
        fs.Verify(f => f.File.Delete(filePath), Times.Once);
    }

    [Fact]
    public void AtRecordingScheduler_SafeDeleteFile_DoesNothing_WhenFileDoesNotExist()
    {
        // Arrange
        var processrunner = new Mock<IProcessRunner>(MockBehavior.Strict);
        var fs = new Mock<IFileSystem>(MockBehavior.Strict);
        var logger = new Mock<ILogger<AtRecordingScheduler>>();
        processrunner.Setup(mock => mock.RunProcess("which", It.IsAny<string>())).Returns(("path", string.Empty, 0));
        
        var settings = new SchedulerSettings { DataPath = "/data" };
        var settingsmgr = new Mock<ISettingsManager>(MockBehavior.Strict);
        settingsmgr.SetupGet(m => m.Settings).Returns(settings);
        
        var scheduler = AtRecordingScheduler.CreateWithProcessRunner(processrunner.Object, fs.Object, settingsmgr.Object, logger.Object);
        
        var recording = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        };
        
        var taskId = recording.Id;
        var filePath = Path.Combine("/data", "tasks", $"{taskId}.sh");
        
        // Setup mock to simulate file not existing
        fs.Setup(f => f.File.Exists(filePath)).Returns(false);
        
        // Setup mocks for FetchScheduledTasks
        processrunner.Setup(m => m.RunProcess("atq", string.Empty))
            .Returns(("3\tThu Apr  3 15:30:00 2025 a guido", "", 0));
            
        // Format the task definition exactly as the AtRecordingScheduler expects it
        var taskDefinition = System.Text.Json.JsonSerializer.Serialize(recording);
        processrunner.Setup(m => m.RunProcess("at", $"-c 3"))
            .Returns((
                $"TASK_DEFINITION='{taskDefinition}'", 
                "", 
                0
            ));
            
        // Setup mock for atrm called by CancelTask
        processrunner.Setup(m => m.RunProcess("atrm", "3")).Returns(("", "", 0));

        // Act
        scheduler.CancelTask(taskId);

        // Assert - Verify file existence was checked but delete was never called
        fs.Verify(f => f.File.Exists(filePath), Times.Once);
        fs.Verify(f => f.File.Delete(It.IsAny<string>()), Times.Never);
    }

    #endregion
}