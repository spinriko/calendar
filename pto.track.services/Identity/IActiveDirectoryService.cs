namespace pto.track.services.Identity;

/// <summary>
/// Service for querying Active Directory user attributes.
/// </summary>
public interface IActiveDirectoryService
{
    /// <summary>
    /// Queries Active Directory for user attributes by sAMAccountName.
    /// </summary>
    /// <param name="samAccountName">The sAMAccountName (username) to search for</param>
    /// <returns>User attributes if found, null otherwise</returns>
    Task<AdUserAttributes?> GetUserAttributesAsync(string samAccountName);
}

/// <summary>
/// Active Directory user attributes returned from LDAP queries.
/// </summary>
public class AdUserAttributes
{
    public string? EmployeeId { get; set; }
    public string? UserPrincipalName { get; set; }
    public string? Mail { get; set; }
    public string? DisplayName { get; set; }
    public List<string> MemberOf { get; set; } = new();
}
