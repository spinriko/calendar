using pto.track.Middleware;
using pto.track.services;
using pto.track;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Logging.EventLog;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
};
var builder = WebApplication.CreateBuilder(options);

// centralize minor configuration differences (local user-secrets, etc.)
builder.ConfigureAppConfiguration();

// centralize service registrations
builder.ConfigureServices();
// services are configured in AppServiceExtensions.ConfigureServices

// Ensure host integrates with the Windows Service lifecycle when appropriate.
// When the process is started by the Service Control Manager, UseWindowsService
// wires the Generic Host to report start/stop status back to SCM.
if (OperatingSystem.IsWindows())
{
    if (WindowsServiceHelpers.IsWindowsService())
    {
        builder.Host.UseWindowsService();
        // When running as a Windows Service, write important logs to Event Viewer
        // Use a stable SourceName so Windows admins can find entries easily.
        builder.Logging.AddEventLog(new EventLogSettings
        {
            LogName = "Application",
            SourceName = "PTO Track"
        });
    }
}

var app = builder.Build();

// Configure pipeline and map endpoints using centralized helpers
app.ConfigurePipeline();

app.Run();

// Expose a Program class for integration testing (WebApplicationFactory uses this)
public partial class Program { }
