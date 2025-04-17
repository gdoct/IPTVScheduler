namespace ipvcr.Scheduling.Shared;

public interface ISettingsManager
{
    public class SettingsChangedEventArgs : EventArgs
    {
        public SchedulerSettings NewSettings { get; }

        public SettingsChangedEventArgs(SchedulerSettings newSettings)
        {
            NewSettings = newSettings;
        }
    }
    SchedulerSettings Settings { get; set; }
    string GetAdminPassword();
    void UpdateAdminPassword(string newPassword);
    event EventHandler<SettingsChangedEventArgs>? SettingsChanged;
}