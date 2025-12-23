using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.WindowsServices;
using pto.track.Middleware;
using pto.track.services;
using pto.track.services.DbContextStrategies;

namespace pto.track;

public static class AppServiceExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure logging early so services can use it. When running as a
        // Windows Service we avoid clearing providers so host-level logging
        // configured via `builder.Host.ConfigureLogging(...)` (EventLog) is
        // preserved and not accidentally removed.
        if (!WindowsServiceHelpers.IsWindowsService())
        {
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
        }

        // Configure CORS - allow localhost defaults for development/local if not configured
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Default", policy =>
            {
                if (allowedOrigins != null && allowedOrigins.Length > 0)
                {
                    policy.WithOrigins(allowedOrigins)
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("local"))
                {
                    policy.WithOrigins("https://localhost:7241", "http://localhost:5139")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
                else
                {
                    // By default in non-dev environments, allow the corp server host
                    // to call APIs when no explicit Cors:AllowedOrigins are configured.
                    // This eases deployment to the corporate webappsdev host.
                    policy.WithOrigins("http://webappsdev")
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials();
                }
            });
        });

        // Hosted service support
        builder.Services.AddWindowsService();

        // Add framework services
        builder.Services.AddRazorPages();
        builder.Services.AddControllers(); // Required for API controllers

        // Add HttpContextAccessor for claims access
        builder.Services.AddHttpContextAccessor();

        // Configure authentication based on configured mode
        var authMode = builder.Configuration["Authentication:Mode"] ?? "Mock";
        if (authMode.Equals("Windows", StringComparison.OrdinalIgnoreCase)
            || authMode.Equals("ActiveDirectory", StringComparison.OrdinalIgnoreCase))
        {
            // Enable Windows/Negotiate authentication when requested.
            builder.Services.AddAuthentication(NegotiateDefaults.AuthenticationScheme)
                .AddNegotiate();
        }
        else
        {
            // Default to cookie-based mock authentication
            builder.Services.AddAuthentication("Cookies")
                .AddCookie("Cookies", options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.AccessDeniedPath = "/Account/AccessDenied";
                    options.Events.OnRedirectToLogin = context =>
                    {
                        // For API requests, return 401 instead of redirecting
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                            return Task.CompletedTask;
                        }
                        context.Response.Redirect(context.RedirectUri);
                        return Task.CompletedTask;
                    };
                });
        }

        builder.Services.AddAuthorization();

        // Add Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add global exception handler and problem details
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        // Choose and register the DbContext strategy based on environment/config
        var connStr = builder.Configuration.GetConnectionString("PtoTrackDbContext");
        // Treat the literal placeholder value 'user-secrets' as absent so local/test runs
        // that don't populate secrets don't accidentally try to use SQL Server.
        if (string.Equals(connStr, "user-secrets", StringComparison.OrdinalIgnoreCase))
        {
            connStr = string.Empty;
        }

        IDbContextStrategy strategy;
        if (builder.Environment.IsEnvironment("Testing") || string.IsNullOrWhiteSpace(connStr))
        {
            strategy = new pto.track.services.DbContextStrategies.InMemoryDbContextStrategy();
        }
        else
        {
            strategy = new pto.track.services.DbContextStrategies.SqlServerDbContextStrategy(connStr);
        }
        // Register the strategy so other components may resolve it if needed
        builder.Services.AddSingleton(typeof(pto.track.services.DbContextStrategies.IDbContextStrategy), strategy);

        // Configure database and register application services using the chosen strategy
        builder.Services.AddSchedulerServices(builder.Configuration, builder.Environment, strategy);

        return builder;
    }
}
