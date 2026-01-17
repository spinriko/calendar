using pto.track.Middleware;
using pto.track.services;
using pto.track;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using System.IO;

var builder = WebApplication.CreateBuilder(args);

// File-based diagnostics: write startup and lifecycle traces to a local file
var _diagPath = Path.Combine(AppContext.BaseDirectory, "service-startup.log");
void _appendDiag(string m)
{
    try
    {
        File.AppendAllText(_diagPath, DateTime.UtcNow.ToString("o") + " " + m + Environment.NewLine);
    }
    catch { }
}

_appendDiag($"Builder created. PID={Environment.ProcessId}; User={WindowsIdentity.GetCurrent()?.Name}");

// centralize minor configuration differences (local user-secrets, etc.)
builder.ConfigureAppConfiguration();

// centralize service registrations
builder.ConfigureServices();


// When hosted by IIS ANCM (inprocess or out-of-process), clear default URLs so
// Kestrel defers to ANCM for binding management. ASPNETCORE_APPL_PATH is set by IIS
// and is a stable indicator across ANCM versions. For local dev, this variable won't
// exist and Kestrel will use defaults (localhost:5000) or appsettings configuration.
var iisHosted = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_APPL_PATH"));
if (iisHosted)
{
    builder.WebHost.UseUrls();
    _appendDiag("IIS/ANCM detected (ASPNETCORE_APPL_PATH set); cleared explicit URLs for IIS binding management");
}

var app = builder.Build();
_appendDiag("Host built");

// DIAGNOSTIC PIPELINE: log via ILogger so configured providers receive startup messages
try
{
    var logger = app.Services.GetService(typeof(ILogger<Program>)) as ILogger<Program> ??
                 app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
    logger.LogInformation("DIAGNOSTIC PIPELINE: Host built and about to run. Account={Account}; PID={PID}",
        WindowsIdentity.GetCurrent()?.Name ?? "(unknown)", Environment.ProcessId);
    _appendDiag("DIAGNOSTIC PIPELINE: logged via ILogger");
}
catch
{
    _appendDiag("DIAGNOSTIC PIPELINE: logger call failed");
}

Console.WriteLine($"WebRootPaht: {app.Environment.WebRootPath}");

// Restore PathBase support for reverse proxy or subdirectory hosting
var pathBase = builder.Configuration.GetValue<string>("PathBase");
if (!string.IsNullOrEmpty(pathBase))
{
    app.UsePathBase(pathBase);
}

// Configure pipeline and map endpoints using centralized helpers
app.ConfigurePipeline();

// DIAGNOSTIC: log when the application signals it has started
try
{
    var lifetime = app.Services.GetService(typeof(IHostApplicationLifetime)) as IHostApplicationLifetime
                  ?? app.Services.GetRequiredService<IHostApplicationLifetime>();
    var loggerFactory = app.Services.GetRequiredService<ILoggerFactory>();
    var lifetimeLogger = loggerFactory.CreateLogger("StartupLifetime");
    lifetime.ApplicationStarted.Register(() =>
    {
        try
        {
            lifetimeLogger.LogInformation("DIAGNOSTIC LIFETIME: ApplicationStarted. Account={Account}; PID={PID}",
                WindowsIdentity.GetCurrent()?.Name ?? "(unknown)", Environment.ProcessId);
            var path = Path.Combine(AppContext.BaseDirectory, "startup-diagnostic.txt");
            File.WriteAllText(path, $"StartedUtc={DateTime.UtcNow:o}; PID={Environment.ProcessId}; Account={WindowsIdentity.GetCurrent()?.Name}\n");
        }
        catch { }
    });
}
catch { }

app.Run();

// Expose a Program class for integration testing (WebApplicationFactory uses this)
public partial class Program { }
