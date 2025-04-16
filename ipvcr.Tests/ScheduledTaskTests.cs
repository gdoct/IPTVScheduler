using ipvcr.Scheduling;

namespace ipvcr.Tests;

public class ScheduledTaskTests
{
    [Fact]
    public void ScheduledTask_FromScheduledTask_ShouldSucceedConversion()
    {
        var id = Guid.NewGuid();
        var name = "name";
        var filename = "filename";
        var channelUri = "http://channel.uri.org/abc/def/1234";
        var startTime = DateTime.Now;
        var endTime = DateTime.Now.AddSeconds(10);
        var command = $"ffmpeg -i {channelUri} -t {Convert.ToInt32((endTime - startTime).TotalSeconds)} -c copy -f mp4 {filename}";
        var schedrec = new ScheduledRecording(id, name, "", filename, channelUri, "", startTime, endTime);
        var json = System.Text.Json.JsonSerializer.Serialize(schedrec);
        var scheduledTask = new ScheduledTask(id, name, command, startTime, json);
        var result = ScheduledRecording.FromScheduledTask(scheduledTask);
        Assert.Equal(id, result.Id);
        Assert.Equal(name, result.Name);
        Assert.Equal(filename, result.Filename);
        Assert.Equal(channelUri, result.ChannelUri);
        Assert.Equal(startTime.ToString("yyyy-MM-dd HH:mm:ss"), result.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(endTime.ToString("yyyy-MM-dd HH:mm:ss"), result.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.NotEqual(schedrec.ChannelUri, schedrec.Obfuscate());
        Assert.Equal("http://chan.../1234", schedrec.Obfuscate());
    }

    [Theory]
    [InlineData("{\"Id\":\"855e1099-8046-4811-80c0-b64501ed8251\",\"TaskId\":0,\"Name\":\"wa\",\"Command\":\"ffmpeg -i http://mag.diamondtv.top/crS7aWh0/p47Taxa/2998 -t 3600 -c copy -f mp4 -metadata title=\\u0022wa\\u0022 -metadata description=\\u0022wa - recorded from RTL 4 at Apr 17 2025, 04:04 PM\\u0022 /media/wa_20250417_16041604.mp4\",\"StartTime\":\"2025-04-17T16:04:00+02:00\",\"InnerScheduledTask\":\"\"}"
)]
    public void ScheduledTaskDeserialize_Valid(string json)
    {
        var task = System.Text.Json.JsonSerializer.Deserialize<ScheduledTask>(json);
        Assert.NotNull(task);
        Assert.NotEmpty(task.Name);
        Assert.NotEmpty(task.Command);
        Assert.Empty(task.InnerScheduledTask);
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
    public void ScheduledTask_Conversion_ThrowsWhenInnerScheduledTaskIsEmptyOrInvalid()
    {
        var task = new ScheduledTask(Guid.NewGuid(), "name", "command", DateTime.Now, string.Empty);
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(task));
        var task2 = new ScheduledTask(Guid.NewGuid(), "name", "command", DateTime.Now, "invalid_json");
        Assert.Throws<Exception>(() => ScheduledRecording.FromScheduledTask(task2));
    }

    [Fact]
    public void ScheduledRecording_SerializeToJsonInTask_ShouldBeCorrect()
    {
        var id = Guid.NewGuid();
        var name = "name";
        var description = "blabla";
        var filename = "filename";
        var channelUri = "http://channelUri";
        var channelName = "name";
        var startTime = DateTime.Now;
        var endTime = DateTime.Now.AddSeconds(10);
        var scheduledRecording = new ScheduledRecording(id, name, description, filename, channelUri, channelName, startTime, endTime);


        var task = scheduledRecording.ToScheduledTask();
        var innerjson = task.InnerScheduledTask;
        Assert.NotNull(innerjson);
        var innerScheduledRecording = System.Text.Json.JsonSerializer.Deserialize<ScheduledRecording>(innerjson);
        Assert.NotNull(innerScheduledRecording);
        Assert.Equal(id, innerScheduledRecording.Id);
        Assert.Equal(name, innerScheduledRecording.Name);
        Assert.Equal(description, innerScheduledRecording.Description);
        Assert.Equal(filename, innerScheduledRecording.Filename);
        Assert.Equal(channelUri, innerScheduledRecording.ChannelUri);
        Assert.Equal(channelName, innerScheduledRecording.ChannelName);
        Assert.Equal(startTime.ToString("yyyy-MM-dd HH:mm:ss"), innerScheduledRecording.StartTime.ToString("yyyy-MM-dd HH:mm:ss"));
        Assert.Equal(endTime.ToString("yyyy-MM-dd HH:mm:ss"), innerScheduledRecording.EndTime.ToString("yyyy-MM-dd HH:mm:ss"));
    }

    // [Fact]
    // public void TimeTest()
    // {
    //     var now = DateTime.Now;
    //     // these are correct converted to utc
    //     var localnow3 = DateTime.SpecifyKind(now, DateTimeKind.Local).ToUniversalTime();
    //     var localnow4 = DateTime.SpecifyKind(now, DateTimeKind.Unspecified).ToUniversalTime();
    //     var x = "";
    // }
}