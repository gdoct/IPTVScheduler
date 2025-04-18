using System.IO.Abstractions;
using ipvcr.Auth;

namespace ipvcr.Scheduling.Shared.Settings;

/// <summary>
/// Enum for identifying which type of settings has changed
/// </summary>
public enum SettingsType
{
    Scheduler,
    Playlist,
    Ssl,
    Ffmpeg,
    AdminPassword
}

/// <summary>
/// Event arguments for the SettingsChanged event
/// </summary>
public class SettingsServiceChangedEventArgs : EventArgs
{
    public SettingsType SettingsType { get; }
    public object NewSettings { get; }

    public SettingsServiceChangedEventArgs(SettingsType settingsType, object newSettings)
    {
        SettingsType = settingsType;
        NewSettings = newSettings;
    }
}

/// <summary>
/// A facade service that provides centralized access to all settings in the application.
/// </summary>
public interface ISettingsService
{
    // Properties for direct settings access with auto-save functionality
    SchedulerSettings SchedulerSettings { get; set; }
    PlaylistSettings PlaylistSettings { get; set; }
    SslSettings SslSettings { get; set; }
    FfmpegSettings FfmpegSettings { get; set; }
    AdminPasswordSettings AdminPasswordSettings { get; set; }
    // Admin password management
    bool ValidateAdminPassword(string passwordhash);
    void UpdateAdminPassword(string newPassword);
    void ResetFactoryDefaults();
    // Settings changed event
    event EventHandler<SettingsServiceChangedEventArgs>? SettingsChanged;
}

/// <summary>
/// Implementation of the settings service that provides access to all settings managers
/// and allows for easy manipulation of settings with automatic saving.
/// </summary>
public class SettingsService : ISettingsService
{
    private readonly ISettingsManager<SchedulerSettings> _schedulerSettingsManager;
    private readonly ISettingsManager<PlaylistSettings> _playlistSettingsManager;
    private readonly ISettingsManager<SslSettings> _sslSettingsManager;
    private readonly ISettingsManager<FfmpegSettings> _ffmpegSettingsManager;
    private readonly ISettingsManager<AdminPasswordSettings> _adminPasswordManager;

    /// <summary>
    /// Event that is raised when any of the underlying settings are changed
    /// </summary>
    public event EventHandler<SettingsServiceChangedEventArgs>? SettingsChanged;

    /// <summary>
    /// Initializes a new instance of the SettingsService class.
    /// </summary>
    /// <param name="fileSystem">The file system abstraction to use.</param>
    public SettingsService(IFileSystem fileSystem, ITokenManager tokenManager)
    {
        _schedulerSettingsManager = new SchedulerSettingsManager(fileSystem);
        _playlistSettingsManager = new PlaylistSettingsManager(fileSystem);
        _sslSettingsManager = new SslSettingsManager(fileSystem);
        _ffmpegSettingsManager = new FfmpegSettingsManager(fileSystem);
        _adminPasswordManager = new AdminPasswordManager(fileSystem, tokenManager);

        // Subscribe to the SettingsChanged event of each settings manager
        _schedulerSettingsManager.SettingsChanged += (sender, args) => OnSettingsChanged(SettingsType.Scheduler, args.NewSettings);
        _playlistSettingsManager.SettingsChanged += (sender, args) => OnSettingsChanged(SettingsType.Playlist, args.NewSettings);
        _sslSettingsManager.SettingsChanged += (sender, args) => OnSettingsChanged(SettingsType.Ssl, args.NewSettings);
        _ffmpegSettingsManager.SettingsChanged += (sender, args) => OnSettingsChanged(SettingsType.Ffmpeg, args.NewSettings);
        _adminPasswordManager.SettingsChanged += (sender, args) => OnSettingsChanged(SettingsType.AdminPassword, args.NewSettings);
    }

    // Settings properties that automatically load and save
    public SchedulerSettings SchedulerSettings
    {
        get => _schedulerSettingsManager.Settings;
        set => _schedulerSettingsManager.Settings = value;
    }

    public PlaylistSettings PlaylistSettings
    {
        get => _playlistSettingsManager.Settings;
        set => _playlistSettingsManager.Settings = value;
    }

    public SslSettings SslSettings
    {
        get => _sslSettingsManager.Settings;
        set => _sslSettingsManager.Settings = value;
    }

    public FfmpegSettings FfmpegSettings
    {
        get => _ffmpegSettingsManager.Settings;
        set => _ffmpegSettingsManager.Settings = value;
    }

    public AdminPasswordSettings AdminPasswordSettings
    {
        get => _adminPasswordManager.Settings;
        set => _adminPasswordManager.Settings = value;
    }

    // Admin password management methods
    public bool ValidateAdminPassword(string password)
    {
        // Assuming SchedulerSettingsManager has these methods
        if (_adminPasswordManager is AdminPasswordManager schedulerManager)
        {
            return schedulerManager.ValidateAdminPassword(password);
        }
        throw new InvalidOperationException("Cannot access admin password methods");
    }

    public void UpdateAdminPassword(string newPassword)
    {
        if (_adminPasswordManager is AdminPasswordManager schedulerManager)
        {
            schedulerManager.UpdateAdminPassword(newPassword);
        }
        else
        {
            throw new InvalidOperationException("Cannot access admin password methods");
        }
    }

    public void ResetFactoryDefaults()
    {
        _schedulerSettingsManager.Settings = new();
        _playlistSettingsManager.Settings = new();
        _sslSettingsManager.Settings = new();
        _ffmpegSettingsManager.Settings = new();
        _adminPasswordManager.Settings = new();
        if (_adminPasswordManager is AdminPasswordManager schedulerManager)
        {
            schedulerManager.SetDefaultAdminPassword();
        }
    }

    /// <summary>
    /// Raises the SettingsChanged event with the appropriate settings type and new settings
    /// </summary>
    private void OnSettingsChanged(SettingsType settingsType, object newSettings)
    {
        SettingsChanged?.Invoke(this, new SettingsServiceChangedEventArgs(settingsType, newSettings));
    }
}