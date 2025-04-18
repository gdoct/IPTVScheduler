using ipvcr.Auth;
using ipvcr.Scheduling;
using ipvcr.Scheduling.Linux;
using ipvcr.Scheduling.Shared.Settings;
using Microsoft.AspNetCore.Authentication;
using System.IO.Abstractions;

namespace ipvcr.Web;

public class Program
{

    public static async Task RestartAspNetAsync()
    {
        await Task.Delay(1000); // Delay to allow the response to be sent
        
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var configuration = builder.Configuration; // Access the configuration
        var port = configuration.GetValue<int?>("Port") ?? 5000;
        var sslport = configuration.GetValue<int?>("SslPort") ?? 5001;
        
        // Create a settings manager instance early to access certificate path
        var fileSystem = new FileSystem();
        var schedulerSettingsManager = new SchedulerSettingsManager(fileSystem);
        var tokenManager = new TokenManager();
        var settingsService = new SettingsService(fileSystem, tokenManager);

        var certpath = settingsService.SslSettings.CertificatePath ?? string.Empty;
        var useSsl = settingsService.SslSettings.UseSsl;

        // Check if the certificate file exists, if not, generate it
        if (useSsl && !string.IsNullOrEmpty(certpath) && !File.Exists(certpath))
        {
            var gen = new SelfSignedCertificateGenerator(new FileSystem());
            gen.GenerateSelfSignedTlsCertificate(certpath, settingsService.SslSettings.CertificatePassword);
            Console.WriteLine($"Generated self-signed certificate at: {certpath}");
        }
        // Register the pre-created instance in the DI container
        builder.Services.AddSingleton<ISettingsService>(settingsService);

        if (useSsl && !string.IsNullOrEmpty(certpath) && File.Exists(certpath))
        {
            // Configure both HTTP and HTTPS endpoints using ConfigureKestrel
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                // HTTP endpoint
                serverOptions.ListenAnyIP(port);
                
                // HTTPS endpoint with certificate
                serverOptions.ListenAnyIP(sslport, listenOptions =>
                {
                    listenOptions.UseHttps(certpath, settingsService.SslSettings.CertificatePassword);
                });
            });
        }
        else
        {
            // Configure only HTTP endpoint
            builder.WebHost.ConfigureKestrel(serverOptions =>
            {
                serverOptions.ListenAnyIP(port);
            });
            
            if (useSsl)
            {
                //throw new InvalidOperationException("SSL is enabled but no certificate path is provided.");
                Console.WriteLine("SSL is enabled but no certificate path is provided. SSL will not be used.");
            }
        }
        // Configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        // Add services to the container.
        builder.Services.AddControllersWithViews();
        var platform = Environment.OSVersion.Platform;

        builder.Services.AddSingleton<IFileSystem, FileSystem>();
        builder.Services.AddSingleton<IPlaylistManager, PlaylistManager>();
        builder.Services.AddSingleton<ITokenManager>(tokenManager);

        builder.Services.AddTransient<IProcessRunner, ProcessRunner>();
        builder.Services.AddTransient<IRecordingSchedulingContext, RecordingSchedulingContext>();
        builder.Services.AddTransient<ITaskScheduler, AtScheduler>();

        // Add authentication with a default scheme
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = "TokenAuth";
            options.DefaultChallengeScheme = "TokenAuth";
        })
        .AddScheme<AuthenticationSchemeOptions, TokenAuthenticationHandler>("TokenAuth", options => { });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
            // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }

        // Comment out or remove this line to prevent HTTP to HTTPS redirection
        // app.UseHttpsRedirection();

        // Serve static files from wwwroot with default documents
        app.UseDefaultFiles(); // Add this line before UseStaticFiles
        app.UseStaticFiles();

        // Development-only CORS policy
        if (app.Environment.IsDevelopment())
        {
            app.UseCors(policy =>
                policy.WithOrigins("http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials());
        }

        app.UseRouting();
        app.UseTokenAuthentication(); // Custom middleware for token authentication
        app.UseAuthentication(); // Ensure this is called after UseRouting and before UseAuthorization
        app.UseAuthorization();
        // API controller routes
        app.MapControllers();

        // Handle SPA fallback for all non-API routes
        app.MapFallbackToFile("index.html");

        app.Run();
    }
}