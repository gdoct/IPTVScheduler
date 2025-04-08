namespace ipvcr.Scheduling;

public interface IRecordingSchedulingContext
{
    IEnumerable<ScheduledRecording> Recordings { get; }

    void AddRecording(ScheduledRecording recording);
    void RemoveRecording(Guid recordingId);
}

public class RecordingSchedulingContext : IRecordingSchedulingContext
{
    public RecordingSchedulingContext(ITaskScheduler taskScheduler) => Scheduler = taskScheduler ?? throw new ArgumentNullException(nameof(taskScheduler));

    private ITaskScheduler Scheduler { get; init; } 

    public IEnumerable<ScheduledRecording> Recordings => Scheduler
                            .FetchScheduledTasks()
                            .Select(ScheduledRecording.FromScheduledTask);

    public void AddRecording(ScheduledRecording recording) => Scheduler.ScheduleTask(recording.ToScheduledTask());

    public void RemoveRecording(Guid recordingId) => Scheduler.CancelTask(recordingId);
}
