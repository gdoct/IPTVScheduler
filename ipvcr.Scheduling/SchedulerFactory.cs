using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime;

namespace ipvcr.Scheduling;

public class SchedulerFactory
{
    [ExcludeFromCodeCoverage]
    public static ITaskScheduler GetScheduler(PlatformID platform)
    {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
        var rootpath = new FileInfo(System.Reflection.Assembly.GetEntryAssembly().Location).Directory.FullName;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                              // if on windows, load the Windows scheduler assembly
        if (platform == PlatformID.Win32NT)
        {
            var assemblypath = Path.Combine(rootpath, "windows/ipvcr.Scheduling.Windows.dll");
            var assembly = Assembly.LoadFrom(assemblypath);
            var type = assembly.GetType("ipvcr.Scheduling.Windows.TaskSchedulerRecordingScheduler") ?? throw new Exception("Failed to load Windows scheduler");
            var result = Activator.CreateInstance(type) as ITaskScheduler;
            return result ?? throw new Exception("Failed to create Windows scheduler");
        }
        // if on linux, load the Linux scheduler assembly
        else if (platform == PlatformID.Unix)
        {
            var assemblypath = Path.Combine(rootpath, "linux/ipvcr.Scheduling.Linux.dll");
            var assembly = Assembly.LoadFrom(assemblypath);
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