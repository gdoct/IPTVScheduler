using ipvcr.Scheduling;

namespace ipvcr.Tests;

public class ScheduledTaskTests
{
    [Fact]
    public void ScheduledTask_FromScheduledTask_ShouldThrowIfTaskIfTypeIsNotTask()
    {
        var scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "command", DateTime.Now, ScheduledTaskType.Command);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));
    }

    [Fact]
    public void ScheduledTask_FromScheduledTask_ShouldCOnvertTimeFromUtcToLocal30041973()
    {
        var localnow = DateTime.Now;
        var scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "ffmpeg -i http://channelUri -t 10 -c copy -f mp4 filename", localnow, ScheduledTaskType.Recording);
        var t = ScheduledRecording.FromScheduledTask(scheduledTask);
        Assert.Equal(localnow.ToString("yyyy-MM-dd HH:mm:ss"), t.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public void ScheduledTask_FromScheduledTask_ShouldSucceedConversion()
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

    [Fact]
    public void ScheduledTask_ToAndFromScheduledRecording_ShouldSucceedConversion()
    {
        var id = Guid.NewGuid();
        var name = "name";
        var filename = "filename";
        var channelUri = "http://channelUri";
        var startTime = DateTime.Now;
        var endTime = DateTime.Now.AddSeconds(10);
        var scheduledRecording = new ScheduledRecording(id, name, "", filename, channelUri, "", startTime, endTime);
        var scheduledTask = scheduledRecording.ToScheduledTask();
        var result = ScheduledRecording.FromScheduledTask(scheduledTask);
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(filename, result.Filename);
        Assert.Equal(channelUri, result.ChannelUri);
        Assert.Equal(startTime.ToString("yyyy-MM-dd HH:mm:ss"), result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(endTime.ToString("yyyy-MM-dd HH:mm:ss"), result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    [Fact]
    public void ScheduledTask_Conversion_ShouldThrowOnInvalidInput()
    {
        // test for these invalid inputs:

        //if (scheduledTask.TaskType != ScheduledTaskType.Recording) throw new Exception("Invalid task type");
        var scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "ffmpeg -i http://example.com/stream -t 3600 -c copy -f mp4 output.mp4", DateTime.Now
                    , ScheduledTaskType.Command);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));

        //var commandParts = scheduledTask.Command.Split(' ');
        // Ensure the command starts with "ffmpeg"
        scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "command", DateTime.Now, ScheduledTaskType.Recording);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));

        //if (commandParts.Length < 10 || commandParts[0] != "ffmpeg") throw new Exception("Invalid command format");
        scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "ffmpeg", DateTime.Now, ScheduledTaskType.Recording);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));

        //if (commandParts[1] != "-i" || commandParts[3] != "-t" || commandParts[5] != "-c" || commandParts[6] != "copy" || commandParts[7] != "-f" || commandParts[8] != "mp4")
        //    throw new Exception("Invalid command structure");
        scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "ffmpeg -t 3600 -c copy -f mp4 -i http://example.com/stream output.mp4", DateTime.Now, ScheduledTaskType.Recording);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));

        //if (!Uri.IsWellFormedUriString(commandParts[2], UriKind.Absolute)) throw new Exception("Invalid channel URI");
        scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "ffmpeg -i http://example.com:80:90/stream -t 3600 -c copy -f mp4 output.mp4", DateTime.Now, ScheduledTaskType.Recording);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));

        //if (!int.TryParse(commandParts[4], out int duration)) throw new Exception("Invalid duration");
        scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "ffmpeg -i http://example.com/stream -t bla -c copy -f mp4 output.mp4", DateTime.Now, ScheduledTaskType.Recording);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));

        //if (string.IsNullOrWhiteSpace(commandParts[9])) throw new Exception("Invalid filename"); */
        scheduledTask = new ScheduledTask(Guid.NewGuid(), "name", "ffmpeg -i http://example.com/stream -t 3600 -c copy -f mp4 ", DateTime.Now, ScheduledTaskType.Recording);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(scheduledTask));
    }

    [Fact]
    public void TimeTest()
    {
        var now = DateTime.Now;
        // these are correct converted to utc
        var localnow3 = DateTime.SpecifyKind(now, DateTimeKind.Local).ToUniversalTime();
        var localnow4 = DateTime.SpecifyKind(now, DateTimeKind.Unspecified).ToUniversalTime();
        var x = "";
    }
}