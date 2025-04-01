using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace ipvcr.Scheduling.Linux;

public class AtRecordingScheduler : ITaskScheduler
{
    private readonly IProcessRunner _processRunner;
    private readonly ILogger<AtRecordingScheduler> _logger;
    private static readonly char[] separator = new[] { '\n' };
    private static readonly char[] separatorArray = new[] { ' ' };

    private AtRecordingScheduler(IProcessRunner processRunner) 
    {
        _processRunner = processRunner; 
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(); // Add console logging
        });

        // Create a logger for this class
        _logger = loggerFactory.CreateLogger<AtRecordingScheduler>();
    }

    [ExcludeFromCodeCoverage]
    public static AtRecordingScheduler Create()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD))
        {
            throw new PlatformNotSupportedException("AtRecordingScheduler can only be run on Linux or FreeBSD.");
        }
        return CreateWithProcessRunner(new ProcessRunner());
    }

    // unit tests can create this object with a mocked IProcessRunner
    public static AtRecordingScheduler CreateWithProcessRunner(IProcessRunner processRunner)
    {
        EnsureCommandIsInstalled(processRunner, "at");
        EnsureCommandIsInstalled(processRunner, "atq");
        EnsureCommandIsInstalled(processRunner, "atrm");
        return new AtRecordingScheduler(processRunner);
    }

    private static void EnsureCommandIsInstalled(IProcessRunner processRunner, string command)
    {
        var (output, _, _) = processRunner.RunProcess("which", "x" + command);

        if (string.IsNullOrWhiteSpace(output))
        {
            throw new MissingDependencyException(command);
        }
    }

    public void ScheduleTask(ScheduledTask task)
    {
        _logger.LogInformation("Scheduling task {taskid}..: {taskname} at {starttime} with command: {command}", task.Id.ToString()[..5], task.Name, task.StartTime, task.Command);
        if (task.StartTime < DateTime.Now)
        {
            throw new InvalidOperationException("Cannot schedule a task in the past.");
        }
        var startTime = task.StartTime.ToString("HH:mm MM/dd/yyyy");
        var command = $"export TASK_JOB_ID='{task.Id}';\nexport TASK_DEFINITION='{System.Text.Json.JsonSerializer.Serialize(task)}'\necho \"{task.Command}\" | at {startTime}";
        _logger.LogInformation("Command to be executed: {command}", command);
        // Run the command in a shell to schedule the task
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

        var lines = output.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            _logger.LogInformation("{line}", line);
            var parts = line.Split(separatorArray, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 1) continue;

            var jobId = parts[0];
            var (jobOutput, jobError, jobExitCode) = _processRunner.RunProcess("at", $"-c {jobId}");

            if (jobExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to get job details for job {jobId}: {jobError}");
            }

            var serializedTask = jobOutput.Split('\n').LastOrDefault(l => l.StartsWith("export TASK_DEFINITION="));
            if (serializedTask == null) 
            {
                _logger.LogWarning("No serialized task found for job {jobId}", jobId);
                continue;
            }

            var taskjson = serializedTask[23..].Trim('\'', '\r', '\n');
            // Clean up the taskjson string by removing extra characters
            _logger.LogDebug("Serialized task JSON: {taskjson}", taskjson);
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