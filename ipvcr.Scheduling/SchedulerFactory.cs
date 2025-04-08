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
            // // the Windows assembly auto-loads to the Windows TaskManager dlls and should not be loaded on Linux
            // var assemblypath = Path.Combine(rootpath, "windows/ipvcr.Scheduling.Windows.dll");
            // var assembly = Assembly.LoadFrom(assemblypath);
            // var type = assembly.GetType("ipvcr.Scheduling.Windows.TaskSchedulerRecordingScheduler") ?? throw new Exception("Failed to load Windows scheduler");
            // var result = Activator.CreateInstance(type) as ITaskScheduler;
            // return result ?? throw new Exception("Failed to create Windows scheduler");
            throw new PlatformNotSupportedException("Windows Task Scheduler is not supported in this version. Please use the Linux version.");
        }
        // if on linux, load the Linux scheduler assembly
        else if (platform == PlatformID.Unix)
        {
            return Linux.AtRecordingScheduler.Create() ?? throw new Exception("Failed to create Linux scheduler");
        }
        // otherwise, throw an exception
        throw new NotImplementedException();
    }
}