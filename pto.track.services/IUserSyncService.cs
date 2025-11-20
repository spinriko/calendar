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
    Task<SchedulerResource?> EnsureCurrentUserExistsAsync();

    /// <summary>
    /// Gets the current user's resource ID from the database.
    /// </summary>
    Task<int?> GetCurrentUserResourceIdAsync();
}
