namespace pto.track.services.Identity;

/// <summary>
/// Result of a user sync operation from external identity sources.
/// </summary>
public class UserSyncResult
{
    /// <summary>
    /// Indicates if the sync operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// User ID from the synced record (immutable AD ID, email, or employee number).
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Reason for failure or additional context.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Groups the user belongs to (from synced data).
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Roles assigned to the user (from synced data).
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Timestamp of when the sync was performed.
    /// </summary>
    public DateTime SyncedAt { get; set; } = DateTime.UtcNow;
}
