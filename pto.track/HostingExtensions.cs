using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace pto.track;

using pto.track.Middleware;
using pto.track.services;

public static class HostingExtensions
{
    public static WebApplicationBuilder ConfigureAppConfiguration(this WebApplicationBuilder builder)
    {
        // Centralize the "local" environment logic here
        if (builder.Environment.IsEnvironment("local"))
        {
            builder.Configuration.AddUserSecrets(System.Reflection.Assembly.GetExecutingAssembly());
        }

        return builder;
    }

    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        // Compose smaller pipeline steps for clarity
        app.ConfigurePathBase()
           .ConfigureExceptionHandling()
           .ConfigureSecurity()
           .ConfigureRoutingAndAuth()
           .ConfigureDeveloperTools()
           .ConfigureStaticFiles()
           .ConfigureEndpoints()
           .ConfigureHealthEndpoints()
           .EnsureDatabaseMigrated();

        return app;
    }

    // PathBase
    public static WebApplication ConfigurePathBase(this WebApplication app)
    {
        var pathBase = app.Configuration.GetValue<string>("PathBase");
        if (!string.IsNullOrEmpty(pathBase))
        {
            app.UsePathBase(pathBase);
        }
        return app;
    }

    public static WebApplication ConfigureExceptionHandling(this WebApplication app)
    {
        app.UseExceptionHandler();
        return app;
    }

    public static WebApplication ConfigureSecurity(this WebApplication app)
    {
        if (!app.Environment.IsLocalOrDev())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }
        return app;
    }

    public static WebApplication ConfigureRoutingAndAuth(this WebApplication app)
    {
        app.UseRouting();
        app.UseCors("Default");
        app.UseAuthentication();
        app.UseMockAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static WebApplication ConfigureDeveloperTools(this WebApplication app)
    {
        if (app.Environment.IsLocalOrDev())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        return app;
    }

    public static WebApplication ConfigureStaticFiles(this WebApplication app)
    {
        // For local environment, use no-cache static files to ease development
        if (app.Environment.IsEnvironment("local"))
        {
            app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
            {
                OnPrepareResponse = ctx =>
                {
                    ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                    ctx.Context.Response.Headers["Pragma"] = "no-cache";
                    ctx.Context.Response.Headers["Expires"] = "-1";
                }
            });
        }

        // Always map application static assets
        app.MapStaticAssets();
        return app;
    }

    public static WebApplication ConfigureEndpoints(this WebApplication app)
    {
        app.MapRazorPages().WithStaticAssets();
        app.MapControllers();
        return app;
    }

    public static WebApplication ConfigureHealthEndpoints(this WebApplication app)
    {
        app.MapHealthChecks("/health");
        app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });
        app.MapHealthChecks("/health/live");
        return app;
    }

    public static WebApplication EnsureDatabaseMigrated(this WebApplication app)
    {
        if (!app.Environment.IsEnvironment("Testing"))
        {
            app.Services.MigrateDatabase();
        }
        return app;
    }

    private static bool IsLocalOrDev(this IHostEnvironment env)
        => env.IsDevelopment() || env.IsEnvironment("local");

    private static void ConfigureLocalStaticFiles(this WebApplication app)
    {
        app.UseStaticFiles(new Microsoft.AspNetCore.Builder.StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                ctx.Context.Response.Headers["Pragma"] = "no-cache";
                ctx.Context.Response.Headers["Expires"] = "-1";
            }
        });
    }
}
