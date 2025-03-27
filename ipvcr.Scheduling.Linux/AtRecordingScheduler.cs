using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace ipvcr.Scheduling.Linux;

public class AtRecordingScheduler : ITaskScheduler
{
    private readonly IProcessRunner _processRunner;

    private AtRecordingScheduler(IProcessRunner processRunner) { _processRunner = processRunner; }

    [ExcludeFromCodeCoverage]
    public static AtRecordingScheduler Create()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            throw new PlatformNotSupportedException("AtRecordingScheduler can only be run on Linux or FreeBSD.");
        }
        return Create(new ProcessRunner());
    }

    // unit tests can create this object with a mocked IProcessRunner
    public static AtRecordingScheduler Create(IProcessRunner processRunner)
    {
        EnsureCommandIsInstalled(processRunner, "at");
        EnsureCommandIsInstalled(processRunner, "atq");
        EnsureCommandIsInstalled(processRunner, "atrm");
        return new AtRecordingScheduler(processRunner);
    }

    private static void EnsureCommandIsInstalled(IProcessRunner processRunner, string command)
    {
        var (output, _, _) = processRunner.RunProcess("which", command);

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new InvalidOperationException($"'{command}' command is not available on this system.");
        }
    }

    public void ScheduleTask(ScheduledTask task)
    {
        var startTime = task.StartTime.ToString("HH:mm MM/dd/yyyy");
        var command = $"export TASK_JOB_ID='{task.Id}';\nexport TASK_DEFINITION='{System.Text.Json.JsonSerializer.Serialize(task)}'\necho \"{task.Command}\" | at {startTime}";

        var (_, error, exitCode) = _processRunner.RunProcess("/bin/bash", $"-c \"{command}\"");

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to schedule task: {error}");
        }
    }

    public IEnumerable<ScheduledTask> FetchScheduledTasks()
    {
        var (output, error, exitCode) = _processRunner.RunProcess("atq", string.Empty);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get scheduled tasks: {error}");
        }

        var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1) continue;

            var jobId = parts[0];
            var (jobOutput, jobError, jobExitCode) = _processRunner.RunProcess("at", $"-c {jobId}");

            if (jobExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to get job details for job {jobId}: {jobError}");
            }

            var serializedTask = jobOutput.Split('\n').LastOrDefault(l => l.StartsWith("export TASK_DEFINITION="));
            if (serializedTask == null) continue;

            var taskjson = serializedTask[23..].Trim('\'', '\r', '\n');
            // Clean up the taskjson string by removing extra characters

            var task = System.Text.Json.JsonSerializer.Deserialize<ScheduledTask>(taskjson);
            if (task == null) continue;

            yield return task;
        }
    }


    public void CancelTask(Guid taskId)
    {
        var recording = FetchScheduledTasks().FirstOrDefault(r => r.Id == taskId) ?? throw new InvalidOperationException($"Task with id {taskId} is not scheduled.");
        var jobId = FetchScheduledTasks().First(r => r.Id == taskId).Id.ToString();
        var (output, error, exitCode) = _processRunner.RunProcess("atrm", jobId);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to cancel recording: {error}");
        }
    }
}