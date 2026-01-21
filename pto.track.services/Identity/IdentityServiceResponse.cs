namespace pto.track.services.Identity;

/// <summary>
/// Staging DTO for identity service API response.
/// Represents user/group/role data from the identity service (AD-connected).
/// Structure is a stub; will be refined once actual API contract is known.
/// </summary>
public class IdentityServiceResponse
{
    /// <summary>
    /// Immutable user identifier (AD objectGuid or oid).
    /// </summary>
    public string? ImmutableId { get; set; }

    /// <summary>
    /// User email address.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Employee number (optional, from ADP integration upstream).
    /// </summary>
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// Groups the user belongs to (group IDs or names).
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Roles assigned to the user (role names or IDs).
    /// </summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>
    /// Raw response object (for future expansion or debugging).
    /// </summary>
    public object? RawData { get; set; }
}
