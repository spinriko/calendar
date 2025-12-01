using pto.track.data;

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
}
