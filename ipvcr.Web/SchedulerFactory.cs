using ipvcr.Scheduling;
using System.Reflection;

namespace ipvcr.Web;

public class SchedulerFactory
{
    public static ITaskScheduler GetScheduler()
    {
        // if on windows, load the Windows scheduler assembly
        if (Environment.OSVersion.Platform == PlatformID.Win32NT)
        {
            var assembly = Assembly.Load("ipvcr.Scheduling.Windows");
            var type = assembly.GetType("Scheduling.Windows.TaskSchedulerRecordingScheduler");
            if (type == null)
            {
                throw new Exception("Failed to load Windows scheduler");
            }
            var result = Activator.CreateInstance(type) as ITaskScheduler;
            return result ?? throw new Exception("Failed to create Windows scheduler");
        }
        // if on linux, load the Linux scheduler assembly
        else if (Environment.OSVersion.Platform == PlatformID.Unix)
        {
            var assembly = Assembly.Load("ipvcr.Scheduling.Linux");
            var type = assembly.GetType("Scheduling.Linux.AtRecordingScheduler") ?? throw new Exception("Failed to load Linux scheduler");
            var method = type.GetMethod("Create") ?? throw new Exception("Failed to load Linux scheduler");
            var result = method.Invoke(null, null) as ITaskScheduler;
            return result ?? throw new Exception("Failed to create Windows scheduler");
        }
        // otherwise, throw an exception
        throw new NotImplementedException();
    }
}