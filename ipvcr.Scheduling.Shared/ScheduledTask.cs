namespace ipvcr.Scheduling
{
    public enum ScheduledTaskType
    {
        Command,
        Recording,
        Transcoding
    }

    public class ScheduledTask(Guid id, string name, string command, DateTime startTime, ScheduledTaskType taskType)
    {
        public Guid Id { get; init; } = id;
        public int TaskId { get; set; } = 0;
        public string Name { get; init; } = name;
        public string Command { get; init; } = command;
        public DateTime StartTime { get; init; } = startTime; // this is in server time
        public DateTime StartTimeUtc => DateTime.SpecifyKind(StartTime, DateTimeKind.Local).ToUniversalTime();
        public ScheduledTaskType TaskType { get; init; } = taskType;
    }
}
