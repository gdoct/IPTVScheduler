using System.IO.Abstractions;
using ipvcr.Auth;

namespace ipvcr.Scheduling.Shared.Settings;

public class AdminPasswordManager(IFileSystem fileSystem, ITokenManager tokenManager) : BaseSettingsManager<AdminPasswordSettings>(fileSystem, SETTINGS_FILENAME, "/data"),
                    IAdminSettingsManager, ISettingsManager<AdminPasswordSettings>
{
    private readonly ITokenManager _tokenManager = tokenManager;
    private const string DEFAULT_PASSWORD = "default_password";
    private const string DEFAULT_USERNAME = "admin";
    const string SETTINGS_FILENAME = "adminpassword.json";

    public string AdminUsername { get; set; } = DEFAULT_USERNAME;
    public string AdminPassword { get; set; } = DEFAULT_PASSWORD;
    public override AdminPasswordSettings Settings
    {
        get 
        {
            // create a copy of the current adminpasswordsettings and set admin password to empty
            // this is to prevent the password from being exposed when returning the settings
            var settings = new AdminPasswordSettings
            {
                AdminUsername = _settings.AdminUsername,
                AdminPassword = string.Empty
            };
            return settings;

        }
        set
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value), "Settings cannot be null.");
            }
            if (string.IsNullOrWhiteSpace(value.AdminUsername))
            {
                throw new ArgumentException("Admin username cannot be null or empty.", nameof(value.AdminUsername));
            }
            var current = _settings;
            if (current.AdminUsername != value.AdminUsername)
            {
                _settings.AdminUsername = value.AdminUsername;
                SaveSettings(_settings);
            }
        }
    }
    protected override AdminPasswordSettings LoadSettings()
    {
        var settings = base.LoadSettings();

        if (string.IsNullOrWhiteSpace(settings.AdminUsername))
        {
            settings.AdminUsername = DEFAULT_USERNAME;
        }
        if (string.IsNullOrWhiteSpace(settings.AdminPassword))
        {
            settings.AdminPassword = DEFAULT_PASSWORD;
        }

        return settings;
    }

    public string GetAdminPassword()
    {
        return Settings.AdminPassword;
    }

    public bool ValidateAdminPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty.", nameof(password));
        }
        var passwordHash = _tokenManager.CreateHash(password);
        var currentpasswordhash = _settings.AdminPassword;
        return passwordHash == currentpasswordhash;
    }

    public void UpdateAdminPassword(string newPassword)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password cannot be null or empty.", nameof(newPassword));
        }

        _settings.AdminPassword = _tokenManager.CreateHash(newPassword);
        SaveSettings(_settings);
    }
}
