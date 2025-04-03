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
            !RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD) &&
            !RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            throw new PlatformNotSupportedException("AtRecordingScheduler can only be run on Linux, MacOS or FreeBSD.");
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
        var (output, _, _) = processRunner.RunProcess("which", command);

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
        Environment.SetEnvironmentVariable("TASK_JOB_ID", task.Id.ToString());
        Environment.SetEnvironmentVariable("TASK_DEFINITION", System.Text.Json.JsonSerializer.Serialize(task));
        var command = $"echo '{task.Command}' | at {startTime}";
        _logger.LogInformation("Command to be executed: {command}", command);
        var (result, error, exitCode) = _processRunner.RunProcess("/bin/bash", $"-c \"{command}\"");

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
            // example line: 
            // 2       Wed Apr  2 13:08:00 2025 a guido
            // extract the first  number using a RegEx
            var jobIdMatch = System.Text.RegularExpressions.Regex.Match(line, @"^\d+");
            var jobId = jobIdMatch.Success ? jobIdMatch.Value : string.Empty;
            if (string.IsNullOrWhiteSpace(jobId)) continue;

            var (jobOutput, jobError, jobExitCode) = _processRunner.RunProcess("at", $"-c {jobId}");

            if (jobExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to get job details for job {jobId}: {jobError}");
            }

            var serializedTask = jobOutput.Split('\n').LastOrDefault(l => l.StartsWith("TASK_DEFINITION="));
            // ; export TASK_DEFINITION
            if (serializedTask == null) 
            {
                _logger.LogWarning("No serialized task found for job {jobId}", jobId);
                continue;
            }

            var taskjson = serializedTask[16..].Trim('\'', '\r', '\n').Replace("\\", string.Empty).Replace("; export TASK_DEFINITION", string.Empty);
            _logger.LogInformation("Serialized task JSON: {taskjson}", taskjson);
            var task = System.Text.Json.JsonSerializer.Deserialize<ScheduledTask>(taskjson);

            if (task == null) continue;
            task.TaskId = int.Parse(jobId);
            yield return task;
        }
    }


    public void CancelTask(Guid taskId)
    {
        var recording = FetchScheduledTasks().FirstOrDefault(r => r.Id == taskId) ?? throw new InvalidOperationException($"Task with id {taskId} is not scheduled.");
        var jobId = recording.TaskId.ToString();
        _logger.LogInformation("Cancelling task {taskid}..: {taskname} with job id: {jobId}", taskId.ToString()[..5], recording.Name, jobId);
        var (output, error, exitCode) = _processRunner.RunProcess("atrm", jobId);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to cancel recording: {error}");
        }
    }
}