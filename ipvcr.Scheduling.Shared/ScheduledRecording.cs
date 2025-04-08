namespace ipvcr.Scheduling;

public class ScheduledRecording
{
    public string Description { get; init; } = string.Empty;
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Filename { get; init; } = string.Empty;
    public string ChannelUri { get; init; } = string.Empty;
    public string ChannelName { get; init; } = string.Empty;
    public DateTime StartTime { get; init; } = DateTime.Now.AddDays(1);
    public DateTime EndTime { get; init; } = DateTime.Now.AddDays(1).AddHours(1);
    public ScheduledRecording()
    {

    }

    public ScheduledRecording(Guid id, string name, string description, string filename, string channelUri, string channelName, DateTime startTime, DateTime endTime)
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
}
