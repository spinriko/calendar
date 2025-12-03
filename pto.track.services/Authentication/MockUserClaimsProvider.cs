using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace pto.track.services.Authentication;

/// <summary>
/// Mock implementation of IUserClaimsProvider for development/testing.
/// Returns user information from the authenticated claims principal.
/// Note: Impersonation is now handled by MockAuthenticationMiddleware via ImpersonationData cookie.
/// </summary>
public class MockUserClaimsProvider : IUserClaimsProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public MockUserClaimsProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? GetUser() => _httpContextAccessor.HttpContext?.User;

    public string? GetEmployeeNumber()
    {
        return GetUser()?.FindFirst("employeeNumber")?.Value
            ?? GetUser()?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public string? GetEmail()
    {
        return GetUser()?.FindFirst(ClaimTypes.Email)?.Value;
    }

    public string? GetDisplayName()
    {
        return GetUser()?.FindFirst(ClaimTypes.Name)?.Value;
    }

    public string? GetActiveDirectoryId()
    {
        return GetUser()?.FindFirst("objectGUID")?.Value;
    }

    public bool IsAuthenticated() => GetUser()?.Identity?.IsAuthenticated ?? false;

    public IEnumerable<string> GetRoles()
    {
        return GetUser()?.FindAll(ClaimTypes.Role).Select(c => c.Value) ?? Enumerable.Empty<string>();
    }

    public bool IsInRole(string role) => GetRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
}
