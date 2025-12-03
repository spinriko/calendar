using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

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
            // Check if user is already authenticated
            if (!context.User.Identity?.IsAuthenticated ?? true)
            {
                _logger.LogDebug("Auto-authenticating mock user");

                // Create claims for the mock user (matching MockUserClaimsProvider)
                var claims = new List<Claim>
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

                var identity = new ClaimsIdentity(claims, "MockAuth");
                var principal = new ClaimsPrincipal(identity);

                // Sign in the mock user
                await context.SignInAsync("Cookies", principal);

                _logger.LogDebug("Mock user authenticated successfully");
            }
        }

        await _next(context);
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
