using ipvcr.Scheduling;

namespace ipvcr.Tests;

public class ScheduledRecordingTests
{
    [Fact]
    public void ScheduledRecording_FromScheduledTask_ShouldThrowIfTaskIfTypeIsNotRecording()
    {
        var scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "command", DateTime.Now, ScheduledTaskType.Command);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));
    }

    [Fact]
    public void ScheduledRecording_FromScheduledTask_ShouldSucceedConversion()
    {
        var id = Guid.NewGuid();
        var name = "name";
        var filename = "filename";
        var channelUri = "http://channelUri";
        var startTime = DateTime.Now;
        var endTime = DateTime.Now.AddSeconds(10);
        var scheduledTask = new ScheduledTask(id, name, $"ffmpeg -i {channelUri} -t 10 -c copy -f mp4 {filename}", startTime, ScheduledTaskType.Recording);
        var result = ScheduledRecording.FromScheduledTask(scheduledTask);
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(filename, result.Filename);
        Assert.Equal(channelUri, result.ChannelUri);
        Assert.Equal(startTime.ToString("yyyy-MM-dd HH:mm:ss"), result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(endTime.ToString("yyyy-MM-dd HH:mm:ss"), result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}