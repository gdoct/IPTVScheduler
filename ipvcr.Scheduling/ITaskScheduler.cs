namespace ipvcr.Scheduling;

public interface ITaskScheduler
{
    void ScheduleTask(ScheduledTask task);
    IEnumerable<ScheduledTask> FetchScheduledTasks();
    void CancelTask(Guid taskId);
}