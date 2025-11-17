using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using pto.track.data;

namespace pto.track.services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddSchedulerServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Configure DB provider - use SQL Server
        var connStr = configuration.GetConnectionString("SchedulerDbContext");

        if (!environment.IsEnvironment("Testing"))
        {
            services.AddDbContext<SchedulerDbContext>(options => options.UseSqlServer(connStr));
        }

        // Register application services
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IResourceService, ResourceService>();

        return services;
    }

    public static void MigrateDatabase(this IServiceProvider serviceProvider)
    {
        using var serviceScope = serviceProvider.CreateScope();
        var services = serviceScope.ServiceProvider;
        try
        {
            var context = services.GetRequiredService<SchedulerDbContext>();
            // Prefer migrations when available so schema upgrades work on deployment
            context.Database.Migrate();
        }
        catch (Exception ex)
        {
            var logger = services.GetRequiredService<ILogger<SchedulerDbContext>>();
            logger.LogError(ex, "An error occurred migrating or creating the DB.");
        }
    }
}
