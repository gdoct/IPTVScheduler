using ipvcr.Scheduling.Shared;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ipvcr.Scheduling.Linux;

public class AtRecordingScheduler : ITaskScheduler
{
    private readonly IProcessRunner _processRunner;
    private readonly IFileSystem _filesystem;
    private readonly ISettingsManager _settingsManager;
    private readonly ILogger<AtRecordingScheduler> _logger;

    // Constants
    private const string AT_DATE_FORMAT = "HH:mm MM/dd/yyyy";
    private static readonly char[] _newlineSeparator = new[] { '\n' };
    // This regex now requires a tab character after the job ID which is the standard format from atq
    private static readonly Regex _atqJobIdRegex = new(@"^(\d+)\t", RegexOptions.Compiled);
    private static readonly Regex _taskDefinitionRegex = new(@"TASK_DEFINITION=({.*?})", RegexOptions.Compiled | RegexOptions.Singleline);

    private AtRecordingScheduler(IProcessRunner processRunner, IFileSystem fileSystem, ISettingsManager settingsManager, ILogger<AtRecordingScheduler>? logger = null)
    {
        _processRunner = processRunner;
        _filesystem = fileSystem;
        _settingsManager = settingsManager;

        if (logger == null)
        {
            var loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
            });
            _logger = loggerFactory.CreateLogger<AtRecordingScheduler>();
        }
        else
        {
            _logger = logger;
        }
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

    // Unit tests can create this object with a mocked IProcessRunner
    public static AtRecordingScheduler CreateWithProcessRunner(IProcessRunner processRunner, IFileSystem fileSystem, ISettingsManager settingsManager, ILogger<AtRecordingScheduler>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(settingsManager);
        ArgumentNullException.ThrowIfNull(processRunner);
        ArgumentNullException.ThrowIfNull(fileSystem);

        EnsureCommandIsInstalled(processRunner, "at");
        EnsureCommandIsInstalled(processRunner, "atq");
        EnsureCommandIsInstalled(processRunner, "atrm");

        return new AtRecordingScheduler(processRunner, fileSystem, settingsManager, logger);
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
        ArgumentNullException.ThrowIfNull(task, nameof(task));

        string taskIdShort = task.Id.ToString()[..5];
        _logger.LogDebug("Scheduling task {taskid}: {taskname} at {starttime} with command: {command}",
            taskIdShort, task.Name, task.StartTime, task.Command);

        if (task.StartTime <= DateTimeOffset.Now)
        {
            throw new InvalidOperationException("Cannot schedule a task in the past.");
        }

        // Create task script file
        string scriptPath = EnsureTaskDirectoryExists();
        string scriptFilename = GetTaskScriptPath(task.Id);
        string scriptContent = GenerateTaskScript(task, _settingsManager.Settings.RemoveTaskAfterExecution);

        try
        {
            // Write script content
            _filesystem.File.WriteAllText(scriptFilename, scriptContent);

            // Make the script executable
            var (_, chmodError, chmodExitCode) = _processRunner.RunProcess("chmod", $"+x {scriptFilename}");
            if (chmodExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to make script executable: {chmodError}");
            }

            // Schedule script with at command
            string startTimeFormatted = task.StartTime.ToLocalTime().ToString(AT_DATE_FORMAT);
            string atCommand = $"echo \"{scriptFilename}\" | at {startTimeFormatted}";

            _logger.LogDebug("Command to be executed through at: {command}", atCommand);

            Environment.SetEnvironmentVariable("TASK_JOB_ID", task.Id.ToString());
            Environment.SetEnvironmentVariable("TASK_DEFINITION", task.InnerScheduledTask);

            var (_, atError, atExitCode) = _processRunner.RunProcess("/bin/bash", $"-c \"{atCommand}\"");

            if (atExitCode != 0)
            {
                // Clean up script file if scheduling fails
                SafeDeleteFile(scriptFilename);
                throw new InvalidOperationException($"Failed to schedule task: {atError}");
            }
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            // Clean up script file if there was an error
            SafeDeleteFile(scriptFilename);
            throw new InvalidOperationException($"Error scheduling task: {ex.Message}", ex);
        }
    }

    public IEnumerable<ScheduledTask> FetchScheduledTasks()
    {
        _logger.LogDebug("Fetching scheduled tasks");

        var (output, error, exitCode) = _processRunner.RunProcess("atq", string.Empty);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get scheduled tasks: {error}");
        }

        var lines = output.Split(_newlineSeparator, StringSplitOptions.RemoveEmptyEntries);
        var tasks = new List<ScheduledTask>();

        foreach (var line in lines)
        {
            _logger.LogDebug("Processing at job: {line}", line);

            // Extract job ID using regex for reliability
            var jobIdMatch = _atqJobIdRegex.Match(line);
            if (!jobIdMatch.Success)
            {
                _logger.LogWarning("No job ID found in line: {line}", line);
                continue;
            }

            var jobId = jobIdMatch.Groups[1].Value;
            if (!int.TryParse(jobId, out var jobIdInt))
            {
                _logger.LogWarning("Failed to parse job ID: {jobId}", jobId);
                continue;
            }

            // Get the job details - allowing exceptions to propagate for critical errors
            var task = GetTaskFromAtJob(jobIdInt);
            if (task != null)
            {
                tasks.Add(task);
            }
        }

        return tasks;
    }

    private ScheduledTask? GetTaskFromAtJob(int jobId)
    {
        var (jobOutput, jobError, jobExitCode) = _processRunner.RunProcess("at", $"-c {jobId}");

        if (jobExitCode != 0)
        {
            // This change immediately throws an exception instead of logging and continuing
            throw new InvalidOperationException($"Failed to get job details for job {jobId}: {jobError}");
        }

        // Find the task definition using regex
        var taskDefinitionMatch = _taskDefinitionRegex.Match(jobOutput);
        if (!taskDefinitionMatch.Success)
        {
            _logger.LogError("No serialized task found for job {jobId}", jobId);
            return null;
        }

        string taskJson = taskDefinitionMatch.Groups[1].Value;
        taskJson = taskJson.Replace("\\", string.Empty);

        _logger.LogDebug("Serialized task JSON: {taskjson}", taskJson);

        try
        {
            var recordingTask = System.Text.Json.JsonSerializer.Deserialize<ScheduledRecording>(taskJson);
            if (recordingTask == null)
            {
                return null;
            }

            var task = recordingTask.ToScheduledTask();
            task.TaskId = jobId;
            return task;
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize task definition for job {jobId}", jobId);
            return null;
        }
    }

    public void CancelTask(Guid taskId)
    {
        _logger.LogDebug("Attempting to cancel task with ID {taskId}", taskId);

        var recording = FetchScheduledTasks().FirstOrDefault(r => r.Id == taskId);
        if (recording == null)
        {
            throw new InvalidOperationException($"Task with id {taskId} is not scheduled.");
        }

        var jobId = recording.TaskId.ToString();
        _logger.LogDebug("Cancelling task \"{taskname}\" with job id: {jobId}", recording.Name, jobId);

        // Delete the script file first to prevent it from running even if atrm fails
        var scriptFilename = GetTaskScriptPath(taskId);
        SafeDeleteFile(scriptFilename);

        // Remove the at job
        var (_, error, exitCode) = _processRunner.RunProcess("atrm", jobId);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to cancel recording: {error}");
        }
    }

    public string GetTaskDefinition(Guid taskId)
    {
        var scriptFilename = GetTaskScriptPath(taskId);

        if (_filesystem.File.Exists(scriptFilename))
        {
            try
            {
                return _filesystem.File.ReadAllText(scriptFilename);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading task script file {scriptFilename}", scriptFilename);
            }
        }

        return string.Empty;
    }

    public void UpdateTaskDefinition(Guid taskId, string newDefinition)
    {
        if (string.IsNullOrWhiteSpace(newDefinition))
        {
            _logger.LogWarning("Attempted to update task {taskId} with empty definition", taskId);
            return;
        }

        var scriptFilename = GetTaskScriptPath(taskId);

        if (_filesystem.File.Exists(scriptFilename))
        {
            try
            {
                _filesystem.File.WriteAllText(scriptFilename, newDefinition);
                _logger.LogDebug("Updated task definition for {taskId}", taskId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update task definition for {taskId}", taskId);
                throw new InvalidOperationException($"Failed to update task definition: {ex.Message}", ex);
            }
        }
        else
        {
            _logger.LogWarning("Attempted to update non-existent task file for {taskId}", taskId);
        }
    }

    #region Helper Methods

    private string EnsureTaskDirectoryExists()
    {
        string scriptPath = Path.Combine(_settingsManager.Settings.DataPath, "tasks");
        if (!_filesystem.Directory.Exists(scriptPath))
        {
            try
            {
                _filesystem.Directory.CreateDirectory(scriptPath);
                _logger.LogDebug("Created tasks directory: {scriptPath}", scriptPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to create tasks directory: {ex.Message}", ex);
            }
        }
        return scriptPath;
    }

    private string GetTaskScriptPath(Guid taskId)
    {
        return Path.Combine(_settingsManager.Settings.DataPath, "tasks", $"{taskId}.sh");
    }

    private string GenerateTaskScript(ScheduledTask task, bool removeAfterCompletion)
    {
        if (removeAfterCompletion)
        {
            return @$"#!/bin/bash
# this script is generated by ipvcr at {DateTimeOffset.UtcNow}
# it will be deleted after execution

# this task is scheduled to be executed at {task.StartTime} or {task.StartTime.UtcDateTime}
export TASK_JOB_ID='{task.Id}'
export TASK_DEFINITION='{task.InnerScheduledTask}'

{task.Command}

# Clean up the script file after execution
rm -f ""{GetTaskScriptPath(task.Id)}""
";
        }
        else
        {
            return @$"#!/bin/bash
# this script is generated by ipvcr at {DateTimeOffset.UtcNow}
# it will be moved to the folder 'completed' after execution

# this task is scheduled to be executed at {task.StartTime} or {task.StartTime.UtcDateTime}
export TASK_JOB_ID='{task.Id}'
export TASK_DEFINITION='{task.InnerScheduledTask}'

{task.Command}

# Move the script file to the completed folder after execution
mkdir -p ""{Path.Combine(_settingsManager.Settings.DataPath, "tasks", "completed")}""
mv ""{GetTaskScriptPath(task.Id)}"" ""{Path.Combine(_settingsManager.Settings.DataPath, "tasks", "completed", $"{task.Id}.sh")}""
";
        }
    }

    private void SafeDeleteFile(string filePath)
    {
        try
        {
            if (_filesystem.File.Exists(filePath))
            {
                _filesystem.File.Delete(filePath);
                _logger.LogDebug("Deleted file: {filePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete file {filePath}", filePath);
        }
    }

    #endregion
}