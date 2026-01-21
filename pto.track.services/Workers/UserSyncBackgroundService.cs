using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using pto.track.services.Identity;

namespace pto.track.services.Workers;

/// <summary>
/// Background service for periodic identity synchronization from external sources (AD + ADP).
/// Runs on a configurable interval to keep groups, roles, and user data in sync.
/// Optional; can be disabled via configuration for environments where sync is not needed.
/// </summary>
public class UserSyncBackgroundService : BackgroundService
{
    private readonly ILogger<UserSyncBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly UserSyncConfig _config;
    private readonly PeriodicTimer? _timer;

    public UserSyncBackgroundService(
        ILogger<UserSyncBackgroundService> logger,
        IServiceProvider serviceProvider,
        UserSyncConfig config)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _config = config;

        if (_config.Enabled && _config.IntervalMinutes > 0)
        {
            _timer = new PeriodicTimer(TimeSpan.FromMinutes(_config.IntervalMinutes));
        }
    }

    /// <summary>
    /// Starts the background sync worker if enabled.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_config.Enabled)
        {
            _logger.LogInformation("UserSyncBackgroundService is disabled (IdentitySync:Enabled=false). Skipping initialization.");
            return;
        }

        _logger.LogInformation("UserSyncBackgroundService started. Running sync every {IntervalMinutes} minutes.", _config.IntervalMinutes);

        try
        {
            // Run once at startup to initialize.
            await PerformSyncAsync(stoppingToken);

            // Continue periodic runs if timer is configured.
            if (_timer != null)
            {
                while (await _timer.WaitForNextTickAsync(stoppingToken))
                {
                    await PerformSyncAsync(stoppingToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("UserSyncBackgroundService cancelled.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UserSyncBackgroundService encountered an unhandled exception.");
            throw;
        }
    }

    /// <summary>
    /// Performs a single sync run.
    /// </summary>
    private async Task PerformSyncAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting identity sync batch operation at {Timestamp}.", DateTime.UtcNow);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var syncService = scope.ServiceProvider.GetRequiredService<IUserSyncService>();

            var result = await syncService.SyncAllUsersAsync(cancellationToken);

            _logger.LogInformation(
                "Identity sync completed at {Timestamp}. Processed={Processed}, Created={Created}, Updated={Updated}, Failed={Failed}, Success={Success}",
                result.CompletedAt, result.TotalProcessed, result.Created, result.Updated, result.Failed, result.Success);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Identity sync failed at {Timestamp}.", DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Cleans up timer resources on service shutdown.
    /// </summary>
    public override void Dispose()
    {
        _timer?.Dispose();
        base.Dispose();
    }
}
