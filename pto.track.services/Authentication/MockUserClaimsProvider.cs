using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace pto.track.services.Authentication;

/// <summary>
/// Mock implementation of IUserClaimsProvider for development/testing.
/// Returns hardcoded user information to simulate an authenticated user.
/// Supports impersonation for testing different roles.
/// </summary>
public class MockUserClaimsProvider : IUserClaimsProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private const string ImpersonationCookieName = "MockImpersonation";

    // Default admin user
    private const string AdminEmployeeNumber = "ADMIN001";
    private const string AdminEmail = "admin@example.com";
    private const string AdminDisplayName = "Admin User";
    private const string AdminAdId = "mock-ad-guid-admin";

    public MockUserClaimsProvider(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private string GetImpersonationRole()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request.Cookies.TryGetValue(ImpersonationCookieName, out var role) == true)
        {
            return role;
        }
        return "Admin"; // Default to Admin
    }

    public string? GetEmployeeNumber()
    {
        var role = GetImpersonationRole();
        return role switch
        {
            "Employee" => "EMP001",
            "Manager" => "MGR001",
            "Approver" => "APR001",
            _ => AdminEmployeeNumber
        };
    }

    public string? GetEmail()
    {
        var role = GetImpersonationRole();
        return role switch
        {
            "Employee" => "employee@example.com",
            "Manager" => "manager@example.com",
            "Approver" => "approver@example.com",
            _ => AdminEmail
        };
    }

    public string? GetDisplayName()
    {
        var role = GetImpersonationRole();
        return role switch
        {
            "Employee" => "Test Employee",
            "Manager" => "Test Manager",
            "Approver" => "Test Approver",
            _ => AdminDisplayName
        };
    }

    public string? GetActiveDirectoryId()
    {
        var role = GetImpersonationRole();
        return role switch
        {
            "Employee" => "mock-ad-guid-employee",
            "Manager" => "mock-ad-guid-manager",
            "Approver" => "mock-ad-guid-approver",
            _ => AdminAdId
        };
    }

    public bool IsAuthenticated() => true;

    public IEnumerable<string> GetRoles()
    {
        var role = GetImpersonationRole();
        return role switch
        {
            "Employee" => new[] { "Employee" },
            "Manager" => new[] { "Employee", "Manager" },
            "Approver" => new[] { "Employee", "Approver" },
            _ => new[] { "Employee", "Manager", "Approver", "Admin" } // Admin has all roles
        };
    }

    public bool IsInRole(string role) => GetRoles().Contains(role, StringComparer.OrdinalIgnoreCase);
}
