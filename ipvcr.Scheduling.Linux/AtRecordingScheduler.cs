using ipvcr.Scheduling.Shared;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.InteropServices;

namespace ipvcr.Scheduling.Linux;

public class AtRecordingScheduler : ITaskScheduler
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _filesystem;
    private readonly ISettingsManager _settingsManager;
    private readonly ILogger<AtRecordingScheduler> _logger;
    private static readonly char[] separator = new[] { '\n' };
    private static readonly char[] separatorArray = new[] { ' ' };

    private AtRecordingScheduler(IProcessRunner processRunner, IFileSystem fileSystem, ISettingsManager settingsManager)
    {
        _processRunner = processRunner;
        _filesystem = fileSystem;
        _settingsManager = settingsManager;
        var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole(); 
        });
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
        return CreateWithProcessRunner(new ProcessRunner(), new FileSystem(), new SettingsManager(new FileSystem()));
    }

    // unit tests can create this object with a mocked IProcessRunner
    public static AtRecordingScheduler CreateWithProcessRunner(IProcessRunner processRunner, IFileSystem fileSystem, ISettingsManager settingsManager)
    {
        ArgumentNullException.ThrowIfNull(settingsManager);
        ArgumentNullException.ThrowIfNull(processRunner);
        ArgumentNullException.ThrowIfNull(fileSystem);
        EnsureCommandIsInstalled(processRunner, "at");
        EnsureCommandIsInstalled(processRunner, "atq");
        EnsureCommandIsInstalled(processRunner, "atrm");
        return new AtRecordingScheduler(processRunner, fileSystem, settingsManager);
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
        _logger.LogInformation("Scheduling task {taskid}", task.Id.ToString()[..5]);
        _logger.LogDebug("Scheduling task {taskid}..: {taskname} at {starttime} with command: {command}", task.Id.ToString()[..5], task.Name, task.StartTime, task.Command);
        if (task.StartTime < DateTime.Now)
        {
            throw new InvalidOperationException("Cannot schedule a task in the past.");
        }
        var startTime = task.StartTime.ToString("HH:mm MM/dd/yyyy");
        Environment.SetEnvironmentVariable("TASK_JOB_ID", task.Id.ToString());
        Environment.SetEnvironmentVariable("TASK_DEFINITION", task.InnerScheduledTask);
        // create a script at /var/lib/iptvscheduler/tasks named {id}.sh
        var scriptfilename = Path.Combine(_settingsManager.Settings.DataPath, $"tasks/{task.Id}.sh");
        _logger.LogDebug("Creating script file {scriptfilename}", scriptfilename);
        var scriptContent = @$"#!/bin/bash
        export TASK_JOB_ID={task.Id}
        export TASK_DEFINITION='{task.InnerScheduledTask}'
        {task.Command} && rm -f {scriptfilename}
        # remove the script after succesful execution";
        _filesystem.File.WriteAllText(scriptfilename, scriptContent);
        // make the script executable
        var (_, error, exitCode) = _processRunner.RunProcess("chmod", $"+x {scriptfilename}");
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to make script executable: {error}");
        }
        // schedule the script to be run at the specified time
        var command = $"echo \"{scriptfilename}\" | at {startTime}";
        _logger.LogDebug("Command to be executed through at: {command}", command);
        (_, error, exitCode) = _processRunner.RunProcess("/bin/bash", $"-c \"{command}\"");

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to schedule task: {error}");
        }
    }

    public IEnumerable<ScheduledTask> FetchScheduledTasks()
    {
        var (output, error, exitCode) = _processRunner.RunProcess("atq", string.Empty);
        _logger.LogInformation("Fetching scheduled tasks..");
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get scheduled tasks: {error}");
        }

        var lines = output.Split(separator, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            _logger.LogDebug("{line}", line);
            // example line: 
            // 2       Wed Apr  2 13:08:00 2025 a guido
            // extract the first  number using a RegEx
            var jobId = line.Split("\t")[0];
            if (string.IsNullOrWhiteSpace(jobId))
            {
                _logger.LogWarning("No job ID found in line: {line}", line);
                continue;
            };
            if (!Int32.TryParse(jobId, out var jobIdInt))
            {
                _logger.LogWarning("Failed to parse job ID: {jobId}", jobId);
                continue;
            }
            var (jobOutput, jobError, jobExitCode) = _processRunner.RunProcess("at", $"-c {jobId}");

            if (jobExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to get job details for job {jobId}: {jobError}");
            }

            var serializedTask = jobOutput.Split('\n').LastOrDefault(l => l.StartsWith("TASK_DEFINITION="));
            // ; export TASK_DEFINITION
            if (string.IsNullOrEmpty(serializedTask))
            {
                _logger.LogWarning("No serialized task found for job {jobId}", jobId);
                continue;
            }
            var taskjson = serializedTask[16..].Trim('\'', '\r', '\n').Replace("\\", string.Empty).Replace("; export TASK_DEFINITION", string.Empty);
            _logger.LogDebug("Serialized task JSON: {taskjson}", taskjson);

            var recordingtask = System.Text.Json.JsonSerializer.Deserialize<ScheduledRecording>(taskjson);

            if (recordingtask == null) continue;
            var task = recordingtask.ToScheduledTask();
            task.TaskId = jobIdInt;
            yield return task;
        }
    }

    public void CancelTask(Guid taskId)
    {
        var recording = FetchScheduledTasks().FirstOrDefault(r => r.Id == taskId) ?? throw new InvalidOperationException($"Task with id {taskId} is not scheduled.");
        var jobId = recording.TaskId.ToString();
        _logger.LogInformation("Cancelling task \"{taskname}\" with job id: {jobId}", recording.Name, jobId);
        var (output, error, exitCode) = _processRunner.RunProcess("atrm", jobId);
        var scriptfilename = Path.Combine(_settingsManager.Settings.DataPath, $"tasks/{taskId}.sh");

        if (_filesystem.File.Exists(scriptfilename))
        {
            _filesystem.File.Delete(scriptfilename);
        }
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to cancel recording: {error}");
        }
    }
}