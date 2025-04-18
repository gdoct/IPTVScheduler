using System.IO.Abstractions;
using System.Text;

namespace ipvcr.Scheduling.Shared.Settings;

public class SchedulerSettingsManager : BaseSettingsManager<SchedulerSettings>, ISettingsManager<SchedulerSettings> 
{
    const string SETTINGS_FILENAME = "settings.json";

    public SchedulerSettingsManager(IFileSystem filesystem) 
        : base(filesystem, SETTINGS_FILENAME, "/data")
    {
    }
}
