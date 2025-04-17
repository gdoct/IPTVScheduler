namespace ipvcr.Scheduling.Shared;

public class SchedulerSettings
{
    public const string DEFAULT_USERNAME = "admin";
    public const string DEFAULT_PASSWORD = "MDAwMDAwMDAwMDBpcHZjcg=="; // "ipvcr" hashed
    public string MediaPath { get; set; } = "/media";
    public string DataPath { get; set; } = "/data";
    public string M3uPlaylistPath { get; set; } = "/data/m3u-playlist.m3u";
    public bool RemoveTaskAfterExecution { get; set; } = true;
    public string AdminUsername { get; set; } = DEFAULT_USERNAME;
    public string AdminPassword { get; set; } = DEFAULT_PASSWORD; // "ipvcr" hashed
    public string SslCertificatePath { get; set; } = "/data/ssl-certificates/certificate.pfx";
}
