using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using pto.track.Models;

namespace pto.track.Middleware;

/// <summary>
/// Middleware that automatically signs in a mock user for development purposes.
/// Only active when Authentication:Mode is set to "Mock".
/// </summary>
public class MockAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MockAuthenticationMiddleware> _logger;

    public MockAuthenticationMiddleware(
        RequestDelegate next,
        IConfiguration configuration,
        ILogger<MockAuthenticationMiddleware> logger)
    {
        _next = next;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var authMode = _configuration["Authentication:Mode"] ?? "Mock";

        // Only auto-authenticate in Mock mode
        if (authMode.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            // Always check for impersonation cookie, even if already authenticated
            // This allows impersonation changes to take effect
            var impersonationData = context.Request.Cookies["ImpersonationData"]; List<Claim> claims;
            bool shouldReauthenticate = false;

            if (!string.IsNullOrEmpty(impersonationData))
            {
                // Use impersonated user claims
                var impersonation = System.Text.Json.JsonSerializer.Deserialize<ImpersonationData>(impersonationData);
                if (impersonation != null)
                {
                    claims = CreateClaimsForImpersonation(impersonation);

                    // Check if current user is different from impersonated user
                    var currentEmployeeNumber = context.User?.FindFirst("employeeNumber")?.Value;
                    if (currentEmployeeNumber != impersonation.EmployeeNumber)
                    {
                        shouldReauthenticate = true;
                        _logger.LogDebug("Impersonating user: {EmployeeNumber} with roles: {Roles}",
                            impersonation.EmployeeNumber, string.Join(", ", impersonation.Roles));
                    }
                }
                else
                {
                    claims = CreateDefaultMockClaims();
                    shouldReauthenticate = !context.User.Identity?.IsAuthenticated ?? true;
                    _logger.LogDebug("Auto-authenticating default mock user");
                }
            }
            else
            {
                // No impersonation - use default mock user claims
                claims = CreateDefaultMockClaims();

                // Only authenticate if not already authenticated as default user
                var isAuthenticated = context.User.Identity?.IsAuthenticated ?? false;
                var currentEmployeeNumber = context.User?.FindFirst("employeeNumber")?.Value;
                shouldReauthenticate = !isAuthenticated || currentEmployeeNumber != "EMP001";

                if (shouldReauthenticate)
                {
                    _logger.LogDebug("Auto-authenticating default mock user");
                }
            }

            // Sign in if we need to (re)authenticate
            if (shouldReauthenticate)
            {
                var identity = new ClaimsIdentity(claims, "MockAuth");
                var principal = new ClaimsPrincipal(identity);

                // Update the current request's user context
                context.User = principal;

                // Also persist the authentication for subsequent requests
                await context.SignInAsync("Cookies", principal);
            }
        }

        await _next(context);
    }

    private List<Claim> CreateDefaultMockClaims()
    {
        return new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, "EMP001"),
            new Claim(ClaimTypes.Email, "developer@example.com"),
            new Claim(ClaimTypes.Name, "Development User"),
            new Claim("employeeNumber", "EMP001"),
            new Claim("objectGUID", "mock-ad-guid-12345"),
            new Claim(ClaimTypes.Role, "Employee"),
            new Claim(ClaimTypes.Role, "Manager"),
            new Claim(ClaimTypes.Role, "Approver"),
            new Claim(ClaimTypes.Role, "Admin")
        };
    }

    private List<Claim> CreateClaimsForImpersonation(ImpersonationData impersonation)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, impersonation.EmployeeNumber),
            new Claim(ClaimTypes.Email, $"{impersonation.EmployeeNumber.ToLower()}@example.com"),
            new Claim(ClaimTypes.Name, GetDisplayNameForEmployee(impersonation.EmployeeNumber)),
            new Claim("employeeNumber", impersonation.EmployeeNumber),
            new Claim("objectGUID", $"mock-guid-{impersonation.EmployeeNumber}")
        };

        foreach (var role in impersonation.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        return claims;
    }

    private string GetDisplayNameForEmployee(string employeeNumber)
    {
        return employeeNumber switch
        {
            "EMP001" => "Development User",
            "EMP002" => "Test Employee",
            "EMP003" => "Test Manager",
            "EMP004" => "Test Approver",
            "EMP005" => "Administrator",
            _ => $"User {employeeNumber}"
        };
    }
}

/// <summary>
/// Extension methods for registering the MockAuthenticationMiddleware.
/// </summary>
public static class MockAuthenticationMiddlewareExtensions
{
    public static IApplicationBuilder UseMockAuthentication(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<MockAuthenticationMiddleware>();
    }
}
