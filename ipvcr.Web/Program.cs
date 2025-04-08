using System.IO.Abstractions;
using ipvcr.Scheduling;

namespace ipvcr.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration; // Access the configuration
        var port = configuration.GetValue<int?>("Port") ?? 5000; 
        var sslport = configuration.GetValue<int?>("SslPort") ?? 5001; 
        var useSsl = configuration.GetValue<bool?>("UseSsl") ?? false; // Default to false if not set
        var certpath = configuration.GetValue<string?>("CertPath") ?? string.Empty; // Default to empty if not set
        builder.WebHost.UseUrls($"http://*:{port}");
        if (useSsl && !string.IsNullOrEmpty(certpath) && File.Exists(certpath))
        {
            // Use the provided certificate path for SSL
            builder.WebHost.UseUrls($"https://*:{sslport}");
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(sslport, listenOptions =>
                {
                    listenOptions.UseHttps(certpath);
                });
            });
        }
        else if (useSsl)
        {
            //throw new InvalidOperationException("SSL is enabled but no certificate path is provided.");
            Console.WriteLine("SSL is enabled but no certificate path is provided. SSL will not be used.");
        }


        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        var platform = Environment.OSVersion.Platform;
        builder.Services.AddTransient<IRecordingSchedulingContext>((_) => new RecordingSchedulingContext(SchedulerFactory.GetScheduler(platform)));
        
        var settingsManager = new SettingsManager(new FileSystem());
        builder.Services.AddSingleton<ISettingsManager>(settingsManager);
        builder.Services.AddSingleton<IPlaylistManager>((_) => new PlaylistManager(settingsManager, new FileSystem()));
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();

        app.UseRouting();

        app.UseAuthorization();

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");

        app.Run();
    }
}