using System;
using System.IO;
using Microsoft.Extensions.FileProviders;
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

    // Allow calling ConfigureAppConfiguration from a built WebApplication in test shims.
    public static WebApplication ConfigureAppConfiguration(this WebApplication app)
    {
        return app;
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
        // In development/local environments show full exception details to aid debugging.
        if (app.Environment.IsLocalOrDev())
        {
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler();
        }

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

            // Temporary guard: allow disabling Swagger UI via configuration when
            // investigating static web assets or running in constrained dev envs.
            // Default behavior: enabled unless explicitly disabled in config
            var disableSwaggerUi = app.Configuration.GetValue<bool>("DisableSwaggerUI", false);
            if (!disableSwaggerUi)
            {
                app.UseSwaggerUI();
            }
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

        // Always serve static files from wwwroot (for all environments)
        app.UseStaticFiles();
        // Serve bundled frontend assets from /dist with correct content-type mappings
        try
        {
            var distPath = Path.Combine(app.Environment.ContentRootPath ?? string.Empty, "wwwroot", "dist");
            if (Directory.Exists(distPath))
            {
                var provider = new FileExtensionContentTypeProvider();
                app.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = new PhysicalFileProvider(distPath),
                    RequestPath = "/dist",
                    ContentTypeProvider = provider,
                    OnPrepareResponse = ctx =>
                    {
                        try
                        {
                            var requestPath = ctx.Context.Request.Path.Value ?? string.Empty;
                            var fileName = ctx.File?.Name ?? string.Empty;

                            // Manifest should be no-cache so the app picks up new mappings quickly
                            if (fileName.Equals("asset-manifest.json", StringComparison.OrdinalIgnoreCase)
                                || requestPath.EndsWith("/asset-manifest.json", StringComparison.OrdinalIgnoreCase))
                            {
                                ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                                ctx.Context.Response.Headers["Pragma"] = "no-cache";
                                ctx.Context.Response.Headers["Expires"] = "-1";
                            }
                            else
                            {
                                // Hashed assets are immutable — allow long caching in browsers/CDNs
                                ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, immutable";
                            }
                        }
                        catch (Exception ex)
                        {
                            // Be conservative on error: don't set long cache headers
                            ctx.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                            ctx.Context.Response.Headers["Pragma"] = "no-cache";
                            ctx.Context.Response.Headers["Expires"] = "-1";
                            app.Logger.LogDebug(ex, "Error while preparing response headers for /dist assets");
                        }
                    }
                });
            }
        }
        catch (Exception ex)
        {
            app.Logger.LogDebug(ex, "Could not configure /dist static files mapping");
        }

        // Always map application static assets
        /*
        try
        {
            app.MapStaticAssets();
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Skipping static web assets mapping (disabled or manifest missing).");
        }
        */
        return app;
    }

    public static WebApplication ConfigureEndpoints(this WebApplication app)
    {
        app.MapRazorPages();
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
        // Always attempt to run the host-level migration/seeding helper.
        // The helper itself is defensive: it will skip SQL migrations when
        // running in the Testing environment or when no connection string
        // is configured, but it will populate in-memory databases with
        // baseline seed data so integration tests have a predictable state.
        app.Services.MigrateDatabase();
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
