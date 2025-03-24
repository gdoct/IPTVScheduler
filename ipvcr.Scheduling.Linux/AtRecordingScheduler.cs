namespace ipvcr.Scheduling.Linux
{
    public class AtRecordingScheduler : ITaskScheduler
    {
        private AtRecordingScheduler() {}

        public static AtRecordingScheduler Create()
        {
            if (!IsLinux())
            {
                throw new PlatformNotSupportedException("AtRecordingScheduler can only be used on Linux systems.");
            }

            EnsureCommandIsInstalled("at");
            EnsureCommandIsInstalled("atq");
            EnsureCommandIsInstalled("atrm");
            return new AtRecordingScheduler();
        }

        private static void EnsureCommandIsInstalled(string command)
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "which",
                    Arguments = command,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string result = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            if (string.IsNullOrWhiteSpace(result))
            {
                throw new InvalidOperationException("'at' command is not available on this system.");
            }
        }

        public void ScheduleTask(ScheduledTask task)
        {
            // Schedule the recording using the 'at' command
            var startTime = task.StartTime.ToString("HH:mm MM/dd/yyyy");
            var command = $"export TASK_JOB_ID='{task.Id}';\nexport TASK_DEFINITION='{System.Text.Json.JsonSerializer.Serialize(task)}'\necho \"{task.Command}\" | at {startTime}";

            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = $"-c \"{command}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to schedule task: {error}");
            }
        }

        public IEnumerable<ScheduledTask> FetchScheduledTasks()
        {
            // Get the list of scheduled recordings using the 'atq' command
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "atq",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to get scheduled tasks: {error}");
            }
            
            // the output of atq will be similar to
            // 3       Sun Mar 23 18:53:00 2025 a guido
            // 2       Sun Mar 23 18:52:00 2025 a guido
            // the first column is the job id

            // we can use each job id to query the job details with "at -c {id}"
            // that will show all the environment variables including the TASK_JOB_ID
            // so if we find the line "export TASK_JOB_ID='..'" we can parse the guid of the job
            var lines = output.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 1) continue;

                var jobId = parts[0];
                var jobProcess = new System.Diagnostics.Process
                {
                    StartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "at",
                        Arguments = $"-c {jobId}",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                jobProcess.Start();
                var jobOutput = jobProcess.StandardOutput.ReadToEnd();
                var jobError = jobProcess.StandardError.ReadToEnd();
                jobProcess.WaitForExit();

                if (jobProcess.ExitCode != 0)
                {
                    throw new InvalidOperationException($"Failed to get job details for job {jobId}: {jobError}");
                }

                var serializedtask = jobOutput.Split('\n').LastOrDefault(l => l.StartsWith("export TASK_DEFINITION="));
                if (serializedtask == null) continue;
                var task = System.Text.Json.JsonSerializer.Deserialize<ScheduledTask>(serializedtask[22..]);
                if (task == null) continue;

                yield return task;
            }

        }

        public void CancelTask(Guid taskId)
        {
            // Cancel the recording using the 'atrm -c jobid' command
            // first check the recording is scheduled and the id matches
            var recording = FetchScheduledTasks().FirstOrDefault(r => r.Id == taskId);
            if (recording == null)
            {
                throw new InvalidOperationException($"Task with id {taskId} is not scheduled.");
            }
            var jobId = FetchScheduledTasks().First(r => r.Id == taskId).Id.ToString();
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = "atrm",
                    Arguments = jobId,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException($"Failed to cancel recording: {error}");
            }
        }
       
        private static bool IsLinux()
        {
            return System.Runtime.InteropServices.RuntimeInformation
                .IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux);
        }
    }
}