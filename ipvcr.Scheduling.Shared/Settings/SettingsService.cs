using System.IO.Abstractions;
using ipvcr.Auth;

namespace ipvcr.Scheduling.Shared.Settings;

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
}