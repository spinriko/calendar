using pto.track.data;
using pto.track.services.Identity;

namespace pto.track.services;

/// <summary>
/// Service for synchronizing authenticated users with the Resources table.
/// </summary>
public interface IUserSyncService
{
    /// <summary>
    /// Ensures the current authenticated user exists in the Resources table.
    /// Creates or updates the user record based on claims.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<Resource?> EnsureCurrentUserExistsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current user's resource ID from the database.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<int?> GetCurrentUserResourceIdAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Syncs a user from external identity sources (AD + ADP) on-demand.
    /// Called when a user logs in for the first time or needs a refresh.
    /// </summary>
    /// <param name="immutableId">Immutable user identifier (AD objectGuid or oid).</param>
    /// <param name="email">User email address.</param>
    /// <param name="employeeNumber">ADP employee number (optional).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sync result with groups/roles applied from external source.</returns>
    Task<UserSyncResult> SyncUserOnDemandAsync(string? immutableId, string? email, string? employeeNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs a full sync of all users from external identity sources.
    /// Used by the background worker for periodic batch synchronization.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Summary of sync operation (updated/created/failed counts).</returns>
    Task<UserSyncBatchResult> SyncAllUsersAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Batch sync result for full identity synchronization.
/// </summary>
public class UserSyncBatchResult
{
    /// <summary>
    /// Total users processed.
    /// </summary>
    public int TotalProcessed { get; set; }

    /// <summary>
    /// Users newly created.
    /// </summary>
    public int Created { get; set; }

    /// <summary>
    /// Users updated.
    /// </summary>
    public int Updated { get; set; }

    /// <summary>
    /// Users with sync errors.
    /// </summary>
    public int Failed { get; set; }

    /// <summary>
    /// Overall success indicator.
    /// </summary>
    public bool Success => Failed == 0;

    /// <summary>
    /// Timestamp of when the sync completed.
    /// </summary>
    public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
}
