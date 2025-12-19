using pto.track.Middleware;
using pto.track.services;
using pto.track;
using System.Diagnostics;
using System.Security.Principal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting.WindowsServices;
using Microsoft.Extensions.Hosting;
using System.IO;
using System.Reflection;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.EventLog;

var options = new WebApplicationOptions
{
    Args = args,
    ContentRootPath = WindowsServiceHelpers.IsWindowsService() ? AppContext.BaseDirectory : default
};
var builder = WebApplication.CreateBuilder(options);

// File-based diagnostics: write startup and lifecycle traces to a local file
// so administrators can inspect startup/SCM interactions even if EventLog
// pipeline isn't wired. We use AppContext.BaseDirectory so logs land next
// to the deployed binaries when running as a Windows Service.
var _diagPath = Path.Combine(AppContext.BaseDirectory, "service-startup.log");
void _appendDiag(string m)
{
    try
    {
        File.AppendAllText(_diagPath, DateTime.UtcNow.ToString("o") + " " + m + Environment.NewLine);
    }
    catch { }
}

// When running as a Windows Service, redirect Console output to the diag file
if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
{
    try
    {
        var _sw = new StreamWriter(_diagPath, append: true) { AutoFlush = true };
        Console.SetOut(_sw);
        Console.SetError(_sw);
        _appendDiag("Console redirected to service-startup.log");
    }
    catch { }
}

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
        // Configure logging on the Host so the Host's ILoggerFactory includes EventLog
        builder.Host.ConfigureLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddEventLog(new EventLogSettings
            {
                LogName = "Application",
                SourceName = "PTO Track"
            });
            logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Information);
        });
        // Also register EventLog on the builder-level logging so application
        // `ILogger` instances (resolved from the app's ILoggerFactory) have
        // the EventLog provider when running as a Windows Service.
        builder.Logging.AddEventLog(new EventLogSettings
        {
            LogName = "Application",
            SourceName = "PTO Track"
        });
        // Explicitly add an EventLog provider instance to the logging builder
        // so the concrete provider is available on the built ILoggerFactory.
        try
        {
            builder.Logging.AddProvider(new Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider(new EventLogSettings
            {
                LogName = "Application",
                SourceName = "PTO Track"
            }));
        }
        catch
        {
            // non-fatal if provider construction fails for any reason
        }
    }
}

_appendDiag($"Builder configured. IsWindowsService={WindowsServiceHelpers.IsWindowsService()}; PID={Environment.ProcessId}; User={WindowsIdentity.GetCurrent()?.Name}");

// Diagnostic: attempt a raw Event Log write during startup when running as a service.
// This bypasses the logging pipeline and proves whether the process can write
// to the Application channel early in startup. Keep failures silent to avoid
// affecting normal startup.
if (OperatingSystem.IsWindows() && WindowsServiceHelpers.IsWindowsService())
{
    try
    {
        var identity = WindowsIdentity.GetCurrent()?.Name ?? "(unknown)";
        EventLog.WriteEntry("PTO Track", $"DIAGNOSTIC: service startup. Account={identity}; PID={Environment.ProcessId}", EventLogEntryType.Information);
        _appendDiag($"Raw EventLog write attempted. Account={identity}; PID={Environment.ProcessId}");
    }
    catch
    {
        // ignore errors — this is only for diagnostics
        _appendDiag("Raw EventLog write failed (ignored)");
    }
}


var app = builder.Build();
_appendDiag("Host built");

// Ensure the built ILoggerFactory has an EventLog provider attached so
// application `ILogger` calls will reach Event Log when running as a
// Windows Service. This attaches a concrete provider instance to the
// factory at runtime (non-fatal on failure).
try
{
    var lfAttach = app.Services.GetRequiredService<ILoggerFactory>();
    lfAttach.AddProvider(new Microsoft.Extensions.Logging.EventLog.EventLogLoggerProvider(new EventLogSettings
    {
        LogName = "Application",
        SourceName = "PTO Track"
    }));
}
catch
{
    // ignore
}

// DIAGNOSTIC: robustly enumerate registered ILoggerFactory providers and write to Event Log
try
{
    var lf = app.Services.GetRequiredService<ILoggerFactory>();
    var tf = lf.GetType();
    object? providersObj = null;
    // common private field
    var providersField = tf.GetField("_providers", BindingFlags.NonPublic | BindingFlags.Instance);
    if (providersField != null)
    {
        providersObj = providersField.GetValue(lf);
    }

    // fallback: scan non-public/public fields for an IEnumerable of providers
    if (providersObj == null)
    {
        foreach (var f in tf.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
        {
            try
            {
                var val = f.GetValue(lf);
                if (val is IEnumerable ie)
                {
                    foreach (var item in ie)
                    {
                        if (item == null) continue;
                        var tname = item.GetType().FullName ?? item.GetType().Name;
                        if (typeof(Microsoft.Extensions.Logging.ILoggerProvider).IsAssignableFrom(item.GetType()) || tname.IndexOf("LoggerProvider", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            providersObj = ie;
                            break;
                        }
                    }
                }
            }
            catch { }
            if (providersObj != null) break;
        }
    }

    // fallback: scan properties
    if (providersObj == null)
    {
        foreach (var p in tf.GetProperties(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
        {
            try
            {
                var val = p.GetValue(lf);
                if (val is IEnumerable ie)
                {
                    foreach (var item in ie)
                    {
                        if (item == null) continue;
                        var tname = item.GetType().FullName ?? item.GetType().Name;
                        if (typeof(Microsoft.Extensions.Logging.ILoggerProvider).IsAssignableFrom(item.GetType()) || tname.IndexOf("LoggerProvider", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            providersObj = ie;
                            break;
                        }
                    }
                }
            }
            catch { }
            if (providersObj != null) break;
        }
    }

    var names = new List<string>();
    if (providersObj is IEnumerable provEnum)
    {
        foreach (var p in provEnum)
        {
            names.Add(p?.GetType().FullName ?? "(null)");
        }
    }
    // Include factory type and provider count for better diagnostics
    try
    {
        var factoryType = lf.GetType().FullName ?? "(unknown)";
        var providerCount = 0;
        if (providersObj is IEnumerable provEnumCount)
        {
            foreach (var _ in provEnumCount) providerCount++;
        }
        names.Insert(0, $"FACTORY={factoryType};PROVIDER_COUNT={providerCount}");
    }
    catch { }
    if (names.Count == 0) names.Add("(no providers found)");
    EventLog.WriteEntry("PTO Track", "LOGGING PROVIDERS: " + string.Join(", ", names), EventLogEntryType.Information);
    try { _appendDiag("LOGGING PROVIDERS: " + string.Join(", ", names)); } catch { }
}
catch (Exception ex)
{
    try { EventLog.WriteEntry("PTO Track", "PROVIDER ENUM ERROR: " + ex.Message, EventLogEntryType.Error); } catch { }
}
// DIAGNOSTIC PIPELINE: attempt to write via the logging pipeline so configured
// providers (EventLog) will receive the message when running as a Windows Service.
try
{
    var logger = app.Services.GetService(typeof(ILogger<Program>)) as ILogger<Program> ??
                 app.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
    logger.LogInformation("DIAGNOSTIC PIPELINE: Host built and about to run. Account={Account}; PID={PID}",
        WindowsIdentity.GetCurrent()?.Name ?? "(unknown)", Environment.ProcessId);
    try { _appendDiag("DIAGNOSTIC PIPELINE: logged via ILogger"); } catch { }
}
catch
{
    // Keep diagnostics non-fatal
    try { _appendDiag("DIAGNOSTIC PIPELINE: logger call failed"); } catch { }
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

// DIAGNOSTIC: log and write a small file when the application signals it has started.
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

            // Also emit a host-like lifecycle message using the Microsoft.Hosting.Lifetime
            // category. If the logging pipeline is wired to EventLog this will flow to
            // Event Viewer; otherwise fall back to writing directly via EventLog so
            // administrators still see the lifecycle entry.
            try
            {
                var hostLifetimeLogger = loggerFactory.CreateLogger("Microsoft.Hosting.Lifetime");
                hostLifetimeLogger.LogInformation("Application started. Account={Account}; PID={PID}", WindowsIdentity.GetCurrent()?.Name ?? "(unknown)", Environment.ProcessId);
            }
            catch
            {
                try
                {
                    EventLog.WriteEntry("PTO Track", "Microsoft.Hosting.Lifetime: Application started. Account=" + (WindowsIdentity.GetCurrent()?.Name ?? "(unknown)") + "; PID=" + Environment.ProcessId, EventLogEntryType.Information);
                }
                catch { }
            }
        }
        catch
        {
            // non-fatal
        }
    });
}
catch
{
    // ignore diagnostics failure
}

app.Run();

// Expose a Program class for integration testing (WebApplicationFactory uses this)
public partial class Program { }
