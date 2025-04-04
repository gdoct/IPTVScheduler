namespace ipvcr.Scheduling;

public class ScheduledRecording
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public string Name { get; init; } = string.Empty;
    public string Filename { get; init; } = string.Empty;
    public string ChannelUri { get; init; } = string.Empty;
    public DateTime StartTime { get; init; } = DateTime.Now.AddDays(1);
    public DateTime EndTime { get; init; } = DateTime.Now.AddDays(1).AddHours(1);
    public ScheduledRecording()
    {

    }

    public ScheduledRecording(Guid id, string name, string filename, string channelUri, DateTime startTime, DateTime endTime)
    {
        Id = id;
        Name = name;
        Filename = filename;
        ChannelUri = channelUri;
        StartTime = startTime;
        EndTime = endTime;
    }

    public ScheduledTask ToScheduledTask()
    {
        // we have the user's StartTime and we have the user's TimezoneOffset
        // e.g. the user schedules at 16.00 localtime with a 2 hr offset. we should create a task at 14.00 UTC
        // so we need to convert the local time to UTC by subtracting the offset
        return new(Id,
            Name,
            $"ffmpeg -i {ChannelUri} -t {Convert.ToInt32((EndTime - StartTime).TotalSeconds)} -c copy -f mp4 {Filename}",
            StartTime,
            ScheduledTaskType.Recording
            );
    }

    public static ScheduledRecording FromScheduledTask(ScheduledTask scheduledTask)
    {
        if (scheduledTask.TaskType != ScheduledTaskType.Recording)
        {
            throw new Exception("Invalid task type");
        }

        // Example input: "ffmpeg -i http://example.com/stream -t 3600 -c copy -f mp4 output.mp4"
        var commandParts = scheduledTask.Command.Split(' ');

        // Ensure the command starts with "ffmpeg"
        if (commandParts.Length < 10 || commandParts[0] != "ffmpeg")
        {
            throw new Exception("Invalid command format");
        }

        // Validate the command structure
        if (commandParts[1] != "-i" || commandParts[3] != "-t" || commandParts[5] != "-c" || commandParts[6] != "copy" || commandParts[7] != "-f" || commandParts[8] != "mp4")
        {
            throw new Exception("Invalid command structure");
        }

        var channelUri = commandParts[2];
        if (!Uri.IsWellFormedUriString(channelUri, UriKind.Absolute))
        {
            throw new Exception("Invalid channel URI");
        }

        if (!int.TryParse(commandParts[4], out int duration))
        {
            throw new Exception("Invalid duration");
        }

        var filename = commandParts[9];
        if (string.IsNullOrWhiteSpace(filename))
        {
            throw new Exception("Invalid filename");
        }

        var startTime = scheduledTask.StartTime; //.AddHours(scheduledTask.TimezoneOffset);
        var endTime = startTime.AddSeconds(duration);

        return new ScheduledRecording(
            scheduledTask.Id, scheduledTask.Name, filename, channelUri, startTime, endTime);
    }
}
