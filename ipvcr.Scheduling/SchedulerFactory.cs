using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ipvcr.Scheduling;

public class SchedulerFactory
{
    [ExcludeFromCodeCoverage]
    public static ITaskScheduler GetScheduler(PlatformID platform)
    {
        // if on windows, load the Windows scheduler assembly
        if (platform == PlatformID.Win32NT)
        {
            var assembly = Assembly.LoadFrom("windows/ipvcr.Scheduling.Windows.dll");
            var type = assembly.GetType("ipvcr.Scheduling.Windows.TaskSchedulerRecordingScheduler") ?? throw new Exception("Failed to load Windows scheduler");
            var result = Activator.CreateInstance(type) as ITaskScheduler;
            return result ?? throw new Exception("Failed to create Windows scheduler");
        }
        // if on linux, load the Linux scheduler assembly
        else if (platform == PlatformID.Unix)
        {
            var assembly = Assembly.LoadFrom("linux/ipvcr.Scheduling.Linux.dll");
            var type = assembly.GetType("ipvcr.Scheduling.Linux.AtRecordingScheduler") ?? throw new Exception("Failed to load Linux scheduler");
            var method = type.GetMethod("Create") ?? throw new Exception("Failed to load Linux scheduler");
            try {
            var result = method.Invoke(null, null) as ITaskScheduler;
            return result ?? throw new Exception("Failed to create Windows scheduler");
            } catch (TargetInvocationException e) {
                if (e.InnerException is PlatformNotSupportedException pe)
                    throw pe;
                throw;
            }
        }
        // otherwise, throw an exception
        throw new NotImplementedException();
    }
}