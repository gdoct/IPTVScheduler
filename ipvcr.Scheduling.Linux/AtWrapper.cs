using System.Text.RegularExpressions;
using System.Threading.Tasks.Dataflow;
using ipvcr.Scheduling.Shared;

namespace ipvcr.Scheduling.Linux;

public class AtWrapper(IProcessRunner processRunner, ISettingsManager settingsManager) : CommandWrapperBase(processRunner, settingsManager, AT_COMMAND)
{
    private const string AT_DATE_FORMAT = "HH:mm MM/dd/yyyy";
    private const string AT_COMMAND = "at";

    public int ScheduleTask(ScheduledTask task)
    {
        Environment.SetEnvironmentVariable("TASK_ID", task.Id.ToString());
        var taskjson = System.Text.Json.JsonSerializer.Serialize(task);
        Environment.SetEnvironmentVariable("TASK_DEFINITION", taskjson);
        string startTimeFormatted = task.StartTime.ToLocalTime().ToString(AT_DATE_FORMAT);
        string atCommand = $"echo \"{base.GetScriptFilename(task)}\" | at {startTimeFormatted}";
        var (output, error, exitCode) = base.ExecuteShellCommand(atCommand);

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to schedule task: {error}");
        }
        if (Int32.TryParse(output, out int jobId))
        {
            return jobId;
        }
        else
        {
            throw new InvalidOperationException($"Failed to parse job ID from output: {output}");
        }
    }

    public (int id, ScheduledTask task) GetTaskDetails(int jobId)
    {
        var (output, error, exitCode) = base.ExecuteCommand($"-c {jobId}");

        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Failed to get job details for job {jobId}: {error}");
        }

        // The output of `at -c <job_id>` typically contains the job ID and the script content.
        // from the output read the value of the shell variable TASK_DEFINITION
        // TASK_DEFINITION is a json serialized ScheduledTask object
        // read this json and deserialize a ScheduledTask object
        var taskDefinitionRegex = new Regex("TASK_DEFINITION='({.*?})'", RegexOptions.Compiled | RegexOptions.Singleline);
        var match = taskDefinitionRegex.Match(output);
        if (match.Success)
        {
            string taskJson = match.Groups[1].Value.Replace("\\", string.Empty);
            try {
                var task = System.Text.Json.JsonSerializer.Deserialize<ScheduledTask>(taskJson);
// this will never return null. otherwise it's fine to throw.
#pragma warning disable CS8619 // Nullability of reference types in value doesn't match target type.
                return (jobId, task);
#pragma warning restore CS8619 // Nullability of reference types in value doesn't match target type.
            } catch {
                throw new InvalidOperationException($"Failed to deserialize task for job {jobId}");
            }
        }
        else
        {
            throw new InvalidOperationException($"Failed to parse task definition for job {jobId}");
        }
    }
}