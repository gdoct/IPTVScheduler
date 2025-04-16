using ipvcr.Scheduling.Shared;

namespace ipvcr.Scheduling.Linux
{
    public abstract class CommandWrapperBase
    {
        protected readonly IProcessRunner ProcessRunner;
        protected readonly ISettingsManager SettingsManager;

        protected CommandWrapperBase(IProcessRunner processRunner, ISettingsManager settingsManager, string command)
        {
            ProcessRunner = processRunner;
            SettingsManager = settingsManager;
            Command = command;
            EnsureCommandIsInstalled(command);
        }

        protected string Command { get; init;}

        public virtual (string output, string error, int exitCode) ExecuteCommand(string arguments) => 
            ProcessRunner.RunProcess(Command, arguments);

        public virtual (string output, string error, int exitCode) ExecuteShellCommand(string shellCommand) => 
            ProcessRunner.RunProcess("/bin/bash", $"-c \"{shellCommand}\"");

        protected void EnsureCommandIsInstalled(string command)
        {
            var (output, _, _) = ProcessRunner.RunProcess("which", command);

            if (string.IsNullOrWhiteSpace(output))
            {
                throw new MissingDependencyException(command);
            }
        }

        protected string GetScriptFilename(ScheduledTask task) => 
            Path.Combine(SettingsManager.Settings.DataPath, "tasks", $"{task.Id}.sh");
    }
}