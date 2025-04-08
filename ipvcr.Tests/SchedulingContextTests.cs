// Purpose: This file contains the tests for the SchedulingContext class.
using ipvcr.Scheduling;
using Moq;

namespace ipvcr.Tests;

public class SchedulingContextTests
{
    [Fact]
    // test that the enumeration only offers recordings, even if more are present in the task manager's list
    public void Recordings_OnlyReturnsRecordings()
    {
        // Arrange
        var taskScheduler = new Mock<ITaskScheduler>();
        var json = System.Text.Json.JsonSerializer.Serialize(new ScheduledRecording(Guid.NewGuid(), "Task 1", "", "filename", "http://whatevah", "", DateTime.Now, DateTime.Now.AddHours(1)));
        taskScheduler.Setup(s => s.FetchScheduledTasks()).Returns(
        [
            new ScheduledTask(Guid.NewGuid(), "Task 1", "ffmpeg -i http://example.com/stream -t 3600 -c copy -f mp4 output.mp4", DateTime.Now, json),
            new ScheduledTask(Guid.NewGuid(), "Task 2", "ffmpeg -i http://example.com/stream -t 3600 -c copy -f mp4 output.mp4", DateTime.Now, json),
        ]);

        var context = new RecordingSchedulingContext(taskScheduler.Object);

        // Act
        var recordings = context.Recordings;

        // Assert
        Assert.Equal(2, recordings.Count());
    }

    [Fact]
    public void AddRecording_SchedulesTask()
    {
        // Arrange
        var taskScheduler = new Mock<ITaskScheduler>();
        var context = new RecordingSchedulingContext(taskScheduler.Object);

        // Act
        var recording = new ScheduledRecording(Guid.NewGuid(), "Task 1", "", "filename", "http://whatevah", "", DateTime.Now, DateTime.Now.AddHours(1));
        context.AddRecording(recording);

        // Assert
        taskScheduler.Verify(s => s.ScheduleTask(It.IsAny<ScheduledTask>()));
    }

    [Fact]
    public void RemoveRecording_UnschedulesTask()
    {
        // Arrange
        var taskScheduler = new Mock<ITaskScheduler>();
        var context = new RecordingSchedulingContext(taskScheduler.Object);
        var recording = new ScheduledRecording(Guid.NewGuid(), "Task 1", "", "filename", "http://whatevah", "", DateTime.Now, DateTime.Now.AddHours(1));

        // Act
        context.AddRecording(recording);
        context.RemoveRecording(recording.Id);

        // Assert
        taskScheduler.Verify(s => s.CancelTask(recording.Id));
    }
}