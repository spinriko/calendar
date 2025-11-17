using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Project.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure DB provider based on connection string.
// Priority: SQLite if detected -> default to SQLite if no/empty/user-secrets connection string -> SQL Server for valid SQL Server strings
var connStr = builder.Configuration.GetConnectionString("SchedulerDbContext");

if (!builder.Environment.IsEnvironment("Testing"))
{
    // Determine if we should use SQLite or SQL Server
    var useSqlite = !string.IsNullOrEmpty(connStr) && !string.Equals(connStr, "user-secrets", StringComparison.OrdinalIgnoreCase) &&
        (connStr.Contains("Data Source=", StringComparison.OrdinalIgnoreCase)
         || connStr.Contains("Filename=", StringComparison.OrdinalIgnoreCase)
         || connStr.EndsWith(".db", StringComparison.OrdinalIgnoreCase));

    if (useSqlite)
    {
        // SQLite - ensure containing directory exists if using a file-based Data Source
        try
        {
            var ds = connStr!;
            var marker = "Data Source=";
            var start = ds.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
            if (start >= 0)
            {
                start += marker.Length;
                var path = ds[start..].Split(';', StringSplitOptions.RemoveEmptyEntries)[0].Trim();
                if (!string.IsNullOrEmpty(path) && !Path.IsPathRooted(path))
                {
                    // normalize relative paths to content root
                    path = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, path));
                }

                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir!);
            }
        }
        catch
        {
            // best-effort directory creation; failure is non-fatal here
        }

        builder.Services.AddDbContext<SchedulerDbContext>(options =>
        {
            options.UseSqlite(connStr);
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });
    }
    else if (string.IsNullOrEmpty(connStr) || string.Equals(connStr, "user-secrets", StringComparison.OrdinalIgnoreCase))
    {
        // No connection string provided or placeholder; default to SQLite at a safe location
        var defaultSqlitePath = Path.Combine(builder.Environment.ContentRootPath, "App_Data", "scheduler.db");
        var dir = Path.GetDirectoryName(defaultSqlitePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        var defaultConnStr = $"Data Source={defaultSqlitePath}";
        builder.Services.AddDbContext<SchedulerDbContext>(options =>
        {
            options.UseSqlite(defaultConnStr);
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });
    }
    else
    {
        // SQL Server connection string provided
        builder.Services.AddDbContext<SchedulerDbContext>(options =>
        {
            options.UseSqlServer(connStr);
            options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
        });
    }
}
else
{
    // Testing environment: register with a placeholder, tests will override via WebApplicationFactory
    builder.Services.AddDbContext<SchedulerDbContext>(options =>
        options.UseSqlServer("Server=.;Database=TestDb;Trusted_Connection=true;"));
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();
app.MapControllers();

// Skip database initialization in Testing environment (WebApplicationFactory handles it)
if (!app.Environment.IsEnvironment("Testing"))
{
    using (var serviceScope = app.Services.CreateScope())
    {
        var services = serviceScope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<SchedulerDbContext>();
            // Prefer migrations when available so schema upgrades work on deployment
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred migrating or creating the DB.");
        }
    }
}

app.Run();

// Expose a Program class for integration testing (WebApplicationFactory uses this)
public partial class Program { }
