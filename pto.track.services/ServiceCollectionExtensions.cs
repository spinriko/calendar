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
        IHostEnvironment environment,
        DbContextStrategies.IDbContextStrategy strategy)
    {
        // Configure DB provider via the provided strategy instance.
        var connStr = configuration.GetConnectionString("PtoTrackDbContext");
        // Treat the literal placeholder value 'user-secrets' as absent so local/test runs
        // that don't populate secrets don't accidentally try to use SQL Server.
        if (string.Equals(connStr, "user-secrets", StringComparison.OrdinalIgnoreCase))
        {
            connStr = string.Empty;
        }

        // Fail fast in local environment if connection string is missing to avoid accidentally
        // connecting to a real database when running locally without proper config.
        if (environment.IsEnvironment("local") && string.IsNullOrWhiteSpace(connStr))
        {
            throw new InvalidOperationException("Connection string 'PtoTrackDbContext' is missing. Add it to appsettings.local.json or user secrets before running locally.");
        }

        strategy.ConfigureServices(services, configuration);

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
        services.AddScoped<IGroupService, GroupService>();
        services.AddSingleton<Identity.IIdentityEnricher, Identity.NoOpIdentityEnricher>();

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
            var logger = services.GetRequiredService<ILogger<PtoTrackDbContext>>();

            // Defensive: if the host environment is explicitly "Testing", don't attempt migrations.
            var env = services.GetService<IHostEnvironment>();
            if (env != null && env.IsEnvironment("Testing"))
            {
                logger.LogDebug("Skipping database migration: running in Testing environment.");
                return;
            }

            // If no connection string is configured, assume tests intend to use InMemory DB and skip migrations.
            var config = services.GetService<IConfiguration>();
            var connStr = config?.GetConnectionString("PtoTrackDbContext");
            if (string.Equals(connStr, "user-secrets", StringComparison.OrdinalIgnoreCase))
            {
                connStr = string.Empty;
            }
            if (string.IsNullOrWhiteSpace(connStr))
            {
                logger.LogDebug("Skipping database migration: no connection string configured (likely Testing/InMemory run).");
                return;
            }

            var context = services.GetRequiredService<PtoTrackDbContext>();
            // Only run migrations if not using in-memory provider. Be defensive — provider resolution
            // or an invalid connection string should not cause the process to throw and kill the test run.
            string? providerName = null;
            try
            {
                providerName = context.Database.ProviderName;
                logger.LogDebug($"EF Core provider: {providerName}");
            }
            catch (Exception exProv)
            {
                logger.LogWarning(exProv, "Unable to determine EF Core provider. Skipping migrations to avoid unsafe database operations.");
                return;
            }

            // If provider indicates in-memory, ensure seed and skip migrations.
            if (providerName != null && providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    pto.track.data.SeedDefaults.EnsureSeedData(context);
                    logger.LogDebug("Ensured in-memory seed data via SeedDefaults.");
                }
                catch (Exception exSeed)
                {
                    logger.LogDebug(exSeed, "Error while ensuring in-memory seed data.");
                }
                return;
            }

            // Additional validation: ensure the configured connection string looks like a SQL Server connection string.
            bool LooksLikeSqlConnStr(string? s)
            {
                if (string.IsNullOrWhiteSpace(s)) return false;
                var lowered = s.ToLowerInvariant();
                // check for common SQL Server keys
                var containsKey = lowered.Contains("server=") || lowered.Contains("data source=") || lowered.Contains("initial catalog=") || lowered.Contains("trusted_connection=") || lowered.Contains("integrated security=") || lowered.Contains("user id=") || lowered.Contains("password=");
                // also require at least one '=' and a ';' (simple heuristic)
                var looksLikePairs = s.Contains('=') && s.Contains(';');
                return containsKey && looksLikePairs;
            }

            if (!LooksLikeSqlConnStr(connStr))
            {
                logger.LogWarning("Connection string for 'PtoTrackDbContext' does not look like a valid SQL connection string. Skipping migrations.");
                return;
            }

            try
            {
                context.Database.Migrate();
            }
            catch (Exception exMigrate)
            {
                // Migration failures in CI/dev should be logged but not crash the whole process.
                logger.LogError(exMigrate, "An error occurred while applying database migrations. Skipping further migration attempts.");
            }
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<PtoTrackDbContext>>();
            logger.LogError(ex, "An error occurred migrating or creating the DB.");
        }
    }

    private static void EnsureSeedData(PtoTrackDbContext context, ILogger logger)
    {
        // If resources already exist, assume the DB is seeded.
        if (context.Resources.Any())
        {
            logger.LogDebug("In-memory DB already contains resources; skipping seed.");
            return;
        }

        // Seed Group 1
        if (!context.Groups.Any(g => g.GroupId == 1))
        {
            context.Groups.Add(new pto.track.data.Models.Group { GroupId = 1, Name = "Group 1" });
        }

        // Create the standard initial resources matching the model-level seed
        var seedDate = new DateTime(2025, 11, 19, 0, 0, 0, DateTimeKind.Utc);

        var resources = new[]
        {
            new pto.track.data.Resource { Id = 1, Name = "Test Employee 1", Role = "Employee", IsActive = true, IsApprover = false, EmployeeNumber = "EMP001", Email = "employee@example.com", ActiveDirectoryId = "mock-ad-guid-employee", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 },
            new pto.track.data.Resource { Id = 2, Name = "Test Employee 2", Role = "Employee", IsActive = true, IsApprover = false, EmployeeNumber = "EMP002", Email = "employee2@example.com", ActiveDirectoryId = "mock-ad-guid-employee2", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 },
            new pto.track.data.Resource { Id = 3, Name = "Manager", Role = "Manager", IsActive = true, IsApprover = true, EmployeeNumber = "MGR001", Email = "manager@example.com", ActiveDirectoryId = "mock-ad-guid-manager", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 },
            new pto.track.data.Resource { Id = 4, Name = "Approver", Role = "Approver", IsActive = true, IsApprover = true, EmployeeNumber = "APR001", Email = "approver@example.com", ActiveDirectoryId = "mock-ad-guid-approver", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 },
            new pto.track.data.Resource { Id = 5, Name = "Administrator", Role = "Admin", IsActive = true, IsApprover = true, EmployeeNumber = "ADMIN001", Email = "admin@example.com", ActiveDirectoryId = "mock-ad-guid-admin", CreatedDate = seedDate, ModifiedDate = seedDate, GroupId = 1 }
        };

        // Only add those that don't already exist to avoid duplicates on repeated runs
        foreach (var r in resources)
        {
            if (!context.Resources.Any(e => e.Id == r.Id))
            {
                context.Resources.Add(r);
            }
        }

        context.SaveChanges();
        logger.LogDebug("In-memory DB seeded with baseline resources and groups.");
    }
}
