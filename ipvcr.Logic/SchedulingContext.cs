using ipvcr.Scheduling.Shared.Settings;

namespace ipvcr.Scheduling;

public interface IRecordingSchedulingContext
{
    IEnumerable<ScheduledRecording> Recordings { get; }

    void AddRecording(ScheduledRecording recording);
    string GetTaskDefinition(Guid id);
    void UpdateTaskDefinition(Guid taskId, string newDefinition);
    void RemoveRecording(Guid recordingId);
}

public class RecordingSchedulingContext(ITaskScheduler taskScheduler, ISettingsService settingsService) : IRecordingSchedulingContext
{
    private readonly ISettingsService SettingsService = settingsService;
    private ITaskScheduler Scheduler { get; init; } = taskScheduler;

    public IEnumerable<ScheduledRecording> Recordings => Scheduler
                            .FetchScheduledTasks()
                            .Select(ScheduledRecording.FromScheduledTask);

    public void AddRecording(ScheduledRecording recording) => Scheduler.ScheduleTask(recording.ToScheduledTask(SettingsService.FfmpegSettings));

    public void RemoveRecording(Guid recordingId) => Scheduler.CancelTask(recordingId);

    public string GetTaskDefinition(Guid id)
    {
        return Scheduler.GetTaskDefinition(id);
    }

    public void UpdateTaskDefinition(Guid taskId, string newDefinition)
    {
        Scheduler.UpdateTaskDefinition(taskId, newDefinition);
    }
}
