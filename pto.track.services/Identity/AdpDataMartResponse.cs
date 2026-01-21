namespace pto.track.services.Identity;

/// <summary>
/// Staging DTO for ADP data mart API response.
/// Represents employee/org hierarchy data from ADP.
/// Structure is a stub; will be refined once actual API contract is known.
/// </summary>
public class AdpDataMartResponse
{
    /// <summary>
    /// Employee number (unique identifier from ADP).
    /// </summary>
    public string? EmployeeNumber { get; set; }

    /// <summary>
    /// Employee name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Department or organization.
    /// </summary>
    public string? Department { get; set; }

    /// <summary>
    /// Manager's employee number.
    /// </summary>
    public string? ManagerEmployeeNumber { get; set; }

    /// <summary>
    /// ADP group memberships (group IDs or names).
    /// </summary>
    public List<string> Groups { get; set; } = new();

    /// <summary>
    /// Raw response object (for future expansion or debugging).
    /// </summary>
    public object? RawData { get; set; }
}
