using Microsoft.Extensions.Logging;
using pto.track.services.Authentication;

namespace pto.track.services.Identity;

/// <summary>
/// Identity enricher that syncs the current user from external sources (AD + ADP)
/// and adds group/role claims along with a refresh timestamp.
/// Runs as part of the claims transformation pipeline during authentication.
/// </summary>
public class UserSyncIdentityEnricher : IIdentityEnricher
{
    private readonly IUserSyncService _userSyncService;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly ILogger<UserSyncIdentityEnricher> _logger;

    public UserSyncIdentityEnricher(
        IUserSyncService userSyncService,
        IUserClaimsProvider claimsProvider,
        ILogger<UserSyncIdentityEnricher> logger)
    {
        _userSyncService = userSyncService;
        _claimsProvider = claimsProvider;
        _logger = logger;
    }

    /// <summary>
    /// Syncs the user from external identity sources and returns enriched attributes.
    /// </summary>
    /// <param name="normalizedIdentity">User identifier (typically email or UPN).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Dictionary of claim values to add to the principal.</returns>
    public async Task<IDictionary<string, string?>> EnrichAsync(string normalizedIdentity, CancellationToken cancellationToken = default)
    {
        var claims = new Dictionary<string, string?>();

        try
        {
            // Extract identifiers from current claims
            var immutableId = _claimsProvider.GetActiveDirectoryId();
            var email = _claimsProvider.GetEmail();
            var employeeNumber = _claimsProvider.GetEmployeeNumber();

            if (string.IsNullOrEmpty(immutableId) && string.IsNullOrEmpty(email) && string.IsNullOrEmpty(employeeNumber))
            {
                _logger.LogWarning("UserSyncIdentityEnricher: No usable identifier found in claims for {NormalizedIdentity}. Skipping sync.", normalizedIdentity);
                return claims;
            }

            _logger.LogInformation("UserSyncIdentityEnricher: Syncing user {Email} (AD ID: {ImmutableId}).", email, immutableId);

            // Call on-demand sync to get fresh groups/roles from source of truth
            var syncResult = await _userSyncService.SyncUserOnDemandAsync(immutableId, email, employeeNumber, cancellationToken);

            if (!syncResult.Success)
            {
                _logger.LogWarning("UserSyncIdentityEnricher: Sync failed for {Email}: {Message}", email, syncResult.Message);
                // Note: Still add the refresh timestamp even on failure, so we can track attempts
                claims["membership_refresh"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                return claims;
            }

            // Add membership refresh timestamp as unix seconds (UTC)
            var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            claims["membership_refresh"] = unixTimestamp;

            // TODO: Add groups and roles as claims once the structure is known
            // For now, these are stubbed in the sync result; when API contracts are defined,
            // populate additional claims like:
            // claims["user_groups"] = string.Join(";", syncResult.Groups);
            // claims["user_roles"] = string.Join(";", syncResult.Roles);

            _logger.LogInformation("UserSyncIdentityEnricher: Successfully synced {Email}. Groups={GroupCount}, Roles={RoleCount}, Timestamp={UnixTimestamp}",
                email, syncResult.Groups.Count, syncResult.Roles.Count, unixTimestamp);

            return claims;
        }
        catch (Exception ex)
        {
            // Log but don't throw; allow auth to proceed even if enrichment fails
            _logger.LogError(ex, "UserSyncIdentityEnricher: Unexpected error enriching identity for {NormalizedIdentity}", normalizedIdentity);
            // Still add refresh timestamp to indicate an attempt was made
            claims["membership_refresh"] = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            return claims;
        }
    }
}
