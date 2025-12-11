using pto.track.Middleware;
using pto.track.services;
using pto.track;
using Microsoft.Extensions.Hosting.WindowsServices;

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

var app = builder.Build();

// Configure pipeline and map endpoints using centralized helpers
app.ConfigurePipeline();

app.Run();

// Expose a Program class for integration testing (WebApplicationFactory uses this)
public partial class Program { }
