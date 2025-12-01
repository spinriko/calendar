using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.services.Authentication;
using pto.track.services.Mapping;

namespace pto.track.services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Configure DB provider - use SQL Server
        var connStr = configuration.GetConnectionString("PtoTrackDbContext");

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddDbContext<PtoTrackDbContext>(options => options.UseSqlServer(connStr));
        }

        // Register AutoMapper
        services.AddAutoMapper(typeof(ServiceCollectionExtensions).Assembly);

        // Add health checks (always register, but only add DbContext check when not testing)
        var healthChecksBuilder = services.AddHealthChecks();
        if (!environment.IsEnvironment("Testing"))
        {
            healthChecksBuilder.AddDbContextCheck<PtoTrackDbContext>("database", tags: new[] { "db", "ready" });
        }

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register application services
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IResourceService, ResourceService>();
        services.AddScoped<IAbsenceService, AbsenceService>();
        services.AddScoped<IUserSyncService, UserSyncService>();

        // Register authentication based on configuration
        var authMode = configuration["Authentication:Mode"] ?? "Mock";

        if (authMode.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IUserClaimsProvider, MockUserClaimsProvider>();
        }
        else if (authMode.Equals("ActiveDirectory", StringComparison.OrdinalIgnoreCase))
        {
            services.AddScoped<IUserClaimsProvider, ActiveDirectoryClaimsProvider>();
        }
        else
        {
            // Default to mock in development
            services.AddScoped<IUserClaimsProvider, MockUserClaimsProvider>();
        }

        return services;
    }

    public static void MigrateDatabase(this IServiceProvider serviceProvider)
    {
        using var serviceScope = serviceProvider.CreateScope();
        var services = serviceScope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<PtoTrackDbContext>();
            // Only run migrations if not using in-memory provider
            var providerName = context.Database.ProviderName;
            var logger = services.GetRequiredService<ILogger<PtoTrackDbContext>>();
            logger.LogDebug($"EF Core provider: {providerName}");
            if (providerName != null && !providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                context.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<PtoTrackDbContext>>();
            logger.LogError(ex, "An error occurred migrating or creating the DB.");
        }
    }
}
