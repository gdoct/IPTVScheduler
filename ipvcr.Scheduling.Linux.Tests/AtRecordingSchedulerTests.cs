namespace ipvcr.Scheduling.Linux.Tests;

public class AtRecordingSchedulerTests
{
    [Fact]
    public void AtRecordingScheduler_CtorChecksIfInstalled()
    {
        // Arrange
        var scheduler = AtRecordingScheduler.Create();
        Assert.NotNull(scheduler);
        // Act
        // Assert
    }

    [Fact]
    public void AtRecordingScheduler_ScheduleTask()
    {
        // Arrange
        var scheduler = AtRecordingScheduler.Create();
        var task = new ScheduledRecording
        {
            Id = Guid.NewGuid(),
            StartTime = DateTime.Now.AddYears(1),
            ChannelUri = "http://whatever",
            EndTime = DateTime.Now.AddYears(1).AddMinutes(1),
            Filename = "whatever.mp4",
            Name = "whatever"
        }.ToScheduledTask();

        Assert.Empty(scheduler.FetchScheduledTasks());
        // Act
        scheduler.ScheduleTask(task);
        // Assert
        Assert.Single(scheduler.FetchScheduledTasks());

        scheduler.CancelTask(task.Id);
        Assert.Empty(scheduler.FetchScheduledTasks());
    }
}