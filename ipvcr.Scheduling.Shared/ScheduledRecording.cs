namespace ipvcr.Scheduling;

public class ScheduledRecording
{
    public string Description { get; set; } = string.Empty;
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Filename { get; set; } = string.Empty;
    public string ChannelUri { get; set; } = string.Empty;
    public string ChannelName { get; set; } = string.Empty;
    public DateTimeOffset StartTime { get; set; } = DateTimeOffset.Now.AddDays(1);
    public DateTimeOffset EndTime { get; set; } = DateTimeOffset.Now.AddDays(1).AddHours(1);
    public ScheduledRecording()
    {

    }

    public ScheduledRecording(Guid id, string name, string description, string filename, string channelUri, string channelName, DateTimeOffset startTime, DateTime endTime)
    {
        Id = id;
        Name = name;
        Description = description;
        Filename = filename;
        ChannelUri = channelUri;
        ChannelName = channelName;
        StartTime = startTime;
        EndTime = endTime;
    }

    private string GenerateFfMpegCommandString()
    {
        return $"ffmpeg -i {ChannelUri} -t {Convert.ToInt32((EndTime - StartTime).TotalSeconds)} -c copy -f mp4 -metadata title=\"{Name}\" -metadata description=\"{Description}\" {Filename}";
    }

    public ScheduledTask ToScheduledTask()
    {
        // we have the user's StartTime and we have the user's TimezoneOffset
        // e.g. the user schedules at 16.00 localtime with a 2 hr offset. we should create a task at 14.00 UTC
        // so we need to convert the local time to UTC by subtracting the offset
        return new(Id,
            Name,
            GenerateFfMpegCommandString(),
            StartTime,
            System.Text.Json.JsonSerializer.Serialize(this)
            );
    }

    public static ScheduledRecording FromScheduledTask(ScheduledTask scheduledTask)
    {
        var json = scheduledTask.InnerScheduledTask;
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new Exception("ScheduledTask.InnerScheduledTask is null or empty");
        }
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<ScheduledRecording>(json)!;
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new Exception($"Failed to deserialize ScheduledRecording from ScheduledTask: {ex.Message}", ex);
        }
    }

    public string Obfuscate()
    {
        // input uri is "http://secret.host.tv/username/password/219885"
        // transform to "http://secr.../219885"
        var parts = ChannelUri.Split('/');
        var first4letterersofhostname = parts[2].Substring(0, 4);
        var lastpart = parts[parts.Length - 1];
        var obfuscatedUri = parts[0] + "//" + first4letterersofhostname + "..." + "/" + lastpart;
        return obfuscatedUri;
    }
}
