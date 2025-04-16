using System.IO.Abstractions;
using System.Text.Json;
using System.Text.RegularExpressions;
using ipvcr.Scheduling.Shared;

namespace ipvcr.Scheduling.Linux;

public interface ITaskScriptManager
{
    string ReadTaskScript(Guid id);
    void WriteTaskScript(ScheduledTask task, bool removeAfterCompletion);

    void MoveScriptToFailed(Guid id);
    void RemoveTaskScript(Guid id);
    string TaskScriptPath(Guid taskId);
}

public class TaskScriptManager(IFileSystem fileSystem, ISettingsManager settingsManager) : ITaskScriptManager
{
    private readonly IFileSystem _fileSystem = fileSystem;
    private readonly ISettingsManager _settingsManager = settingsManager;

    private string ScriptPath => Path.Combine(_settingsManager.Settings.DataPath, "tasks");
    private string ScriptPathFailed => Path.Combine(_settingsManager.Settings.DataPath, "tasks", "failed");
    public string TaskScriptPath(Guid taskId) => Path.Combine(ScriptPath, $"{taskId}.sh");

    public void WriteTaskScript(ScheduledTask task, bool removeAfterCompletion)
    {
        string scriptPath = TaskScriptPath(task.Id);
        string scriptContent = GenerateTaskScript(task, removeAfterCompletion);
        _fileSystem.File.WriteAllText(scriptPath, scriptContent);
        _fileSystem.File.SetAttributes(scriptPath, FileAttributes.Normal);
    }

    public string ReadTaskScript(Guid id)
    {
        string scriptPath = TaskScriptPath(id);
        if (_fileSystem.File.Exists(scriptPath))
        {
            return _fileSystem.File.ReadAllText(scriptPath);
        }
        throw new FileNotFoundException($"Task script not found for ID: {id}");
    }


    public void MoveScriptToFailed(Guid id)
    {
        string scriptPath = TaskScriptPath(id);
        string failedPath = Path.Combine(ScriptPathFailed, $"{id}.sh");
        if (_fileSystem.File.Exists(scriptPath))
        {
            if (!_fileSystem.Directory.Exists(ScriptPathFailed))
            {
                _fileSystem.Directory.CreateDirectory(ScriptPathFailed);
            }
            _fileSystem.File.Move(scriptPath, failedPath);
        }
    }
    public void RemoveTaskScript(Guid id)
    {
        string scriptPath = TaskScriptPath(id);
        if (_fileSystem.File.Exists(scriptPath))
        {
            _fileSystem.File.Delete(scriptPath);
        }
    }

    private static string GenerateTaskScript(ScheduledTask task, bool removeAfterCompletion)
    {
        if (removeAfterCompletion)
        {
            return @$"#!/bin/bash
# this script is generated by ipvcr

export TASK_JOB_ID='{task.Id}'
export TASK_DEFINITION='{task.InnerScheduledTask}'

{task.Command}

rm -f ""{task.Id}.sh""";
        }
        else
        {
            return @$"#!/bin/bash
# this script is generated by ipvcr

export TASK_JOB_ID='{task.Id}'
export TASK_DEFINITION='{task.InnerScheduledTask}'

{task.Command}

mkdir -p completed
mv ""{task.Id}.sh"" completed/";
        }
    }

}
