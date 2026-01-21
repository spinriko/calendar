namespace pto.track.services.Identity;

/// <summary>
/// Configuration for identity synchronization from external sources (AD, ADP).
/// </summary>
public class UserSyncConfig
{
    /// <summary>
    /// Enable/disable the identity sync worker.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Interval in minutes for scheduled sync runs. Only applicable if background worker is enabled.
    /// </summary>
    public int IntervalMinutes { get; set; } = 60;

    /// <summary>
    /// Base URL for the identity service API (AD-connected, provides users/groups/roles).
    /// Example: https://api.example.com/identity
    /// </summary>
    public string? IdentityServiceUrl { get; set; }

    /// <summary>
    /// Base URL for the ADP data mart API (provides employee/org hierarchy).
    /// Example: https://api.example.com/adp
    /// </summary>
    public string? AdpDataMartUrl { get; set; }

    /// <summary>
    /// API key or token for the identity service (stub for now).
    /// </summary>
    public string? IdentityServiceApiKey { get; set; }

    /// <summary>
    /// API key or token for the ADP data mart (stub for now).
    /// </summary>
    public string? AdpDataMartApiKey { get; set; }

    /// <summary>
    /// Maximum number of concurrent sync operations.
    /// </summary>
    public int MaxConcurrency { get; set; } = 1;

    /// <summary>
    /// Enable on-demand sync when a user logs in for the first time.
    /// This is the primary sync path; background worker is optional.
    /// </summary>
    public bool EnableOnDemandSync { get; set; } = true;
}
