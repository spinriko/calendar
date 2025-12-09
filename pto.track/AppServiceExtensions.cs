using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using pto.track.Middleware;
using pto.track.services;

namespace pto.track;

public static class AppServiceExtensions
{
    public static WebApplicationBuilder ConfigureServices(this WebApplicationBuilder builder)
    {
        // Configure logging early so services can use it
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.AddDebug();

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
                    // Disallow all origins by default in non-dev environments
                    policy.SetIsOriginAllowed(_ => false);
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

        // Add authentication - using cookie scheme for mock authentication in development
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

        builder.Services.AddAuthorization();

        // Add Swagger/OpenAPI
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add global exception handler and problem details
        builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
        builder.Services.AddProblemDetails();

        // Configure database and register application services
        builder.Services.AddSchedulerServices(builder.Configuration, builder.Environment);

        return builder;
    }
}
