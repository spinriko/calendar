using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace pto.track.services.Authentication;

/// <summary>
/// Active Directory implementation of IUserClaimsProvider.
/// Reads user information from Windows authentication claims.
/// </summary>
public class ActiveDirectoryClaimsProvider : IUserClaimsProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ActiveDirectoryClaimsProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    public string? GetEmployeeNumber()
    {
        // AD attribute: employeeNumber or employeeID
        return User?.FindFirst("employeeNumber")?.Value
            ?? User?.FindFirst("employeeID")?.Value
            ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetEmail()
    {
        // Standard email claim
        return User?.FindFirst(ClaimTypes.Email)?.Value
            ?? User?.FindFirst("email")?.Value
            ?? User?.FindFirst("mail")?.Value;
    }

    public string? GetDisplayName()
    {
        // Display name from AD
        return User?.FindFirst(ClaimTypes.Name)?.Value
            ?? User?.FindFirst("displayName")?.Value
            ?? User?.FindFirst("name")?.Value;
    }

    public string? GetActiveDirectoryId()
    {
        // Active Directory ObjectGUID or Azure AD objectId
        return User?.FindFirst("objectGUID")?.Value
            ?? User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
            ?? User?.FindFirst("oid")?.Value;
    }

    public bool IsAuthenticated()
    {
        return User?.Identity?.IsAuthenticated ?? false;
    }

    public IEnumerable<string> GetRoles()
    {
        if (User == null) return Enumerable.Empty<string>();

        return User.FindAll(ClaimTypes.Role)
            .Select(c => c.Value)
            .Union(User.FindAll("role").Select(c => c.Value))
            .Union(User.FindAll("roles").Select(c => c.Value));
    }

    public bool IsInRole(string role)
    {
        return User?.IsInRole(role) ?? false;
    }
}
