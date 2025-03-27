using Microsoft.Win32.TaskScheduler;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ipvcr.Scheduling.Windows
{
    [ExcludeFromCodeCoverage]
    public class TaskSchedulerRecordingScheduler : ITaskScheduler
    {
        public TaskSchedulerRecordingScheduler()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException("TaskSchedulerRecordingScheduler can only be run on Windows.");
            }
        }

        public void ScheduleTask(ScheduledTask task)
        {
            // Schedule the recording using the TaskScheduler nuget package
            using var ts = new TaskService();
            var td = ts.NewTask();
            td.RegistrationInfo.Author = "IPTV Recorder";
            td.RegistrationInfo.Source = "IPTV Recorder";
            td.RegistrationInfo.Description = "IPTV Recording: " + task.Name;
            td.Triggers.Add(new TimeTrigger(task.StartTime));
            td.Actions.Add(new ExecAction(task.Command, null, null));
            td.Data = "Task:" + System.Text.Json.JsonSerializer.Serialize(task);
            ts.RootFolder.RegisterTaskDefinition(task.Name, td);
        }

        public IEnumerable<ScheduledTask> FetchScheduledTasks()
        {
            // Get the list of scheduled recordings using the TaskScheduler nuget package
            return FetchScheduledTasksFull().Select(r => r.Item1);
        }

        private static IEnumerable<(ScheduledTask, Microsoft.Win32.TaskScheduler.Task)> FetchScheduledTasksFull()
        {
            // Get the list of scheduled recordings using the TaskScheduler nuget package
            using var ts = new TaskService();
            foreach (Microsoft.Win32.TaskScheduler.Task task in ts.RootFolder.Tasks)
            {
                if (!string.IsNullOrWhiteSpace(task.Definition.Data) && task.Definition.Data.StartsWith("Task:"))
                {
                    var json = task.Definition.Data[5..];
                    var stask = System.Text.Json.JsonSerializer.Deserialize<ScheduledTask>(json);
                    if (stask != null) 
                    {
                        yield return (stask, task);
                    }
                }
            }
        }

        public void CancelTask(Guid recordingId)
        {
            // Cancel the recording using the TaskScheduler nuget package
            var tasks = FetchScheduledTasksFull();
            var item = tasks.FirstOrDefault(r => r.Item1.Id == recordingId);
            var exists = item != default;
            if (!exists) throw new InvalidOperationException($"Task with id {recordingId} is not scheduled.");
            using var ts = new TaskService();
            ts.RootFolder.DeleteTask(item.Item2.Name);
        }
    }
}