using Microsoft.Extensions.Logging;

namespace pto.track.services.Identity;

/// <summary>
/// No-op implementation of IActiveDirectoryService for local development and testing.
/// Returns null for all queries, allowing the application to run without domain connectivity.
/// Use this on non-domain-joined machines or in test environments.
/// </summary>
public class NoOpActiveDirectoryService : IActiveDirectoryService
{
    private readonly ILogger<NoOpActiveDirectoryService> _logger;

    public NoOpActiveDirectoryService(ILogger<NoOpActiveDirectoryService> logger)
    {
        _logger = logger;
    }

    public Task<AdUserAttributes?> GetUserAttributesAsync(string samAccountName)
    {
        _logger.LogDebug("NoOpActiveDirectoryService: Skipping AD query for {SamAccountName} (not domain-joined)", samAccountName);
        return Task.FromResult<AdUserAttributes?>(null);
    }
}
