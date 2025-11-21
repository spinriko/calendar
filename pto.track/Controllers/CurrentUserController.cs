using Microsoft.AspNetCore.Mvc;
using pto.track.services;
using pto.track.services.Authentication;

namespace pto.track.Controllers;

/// <summary>
/// API endpoint for current user information
/// </summary>
[Route("api/[controller]")]
[ApiController]
public class CurrentUserController : ControllerBase
{
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly IUserSyncService _userSync;
    private readonly IResourceService _resourceService;
    private readonly IConfiguration _configuration;

    public CurrentUserController(
        IUserClaimsProvider claimsProvider,
        IUserSyncService userSync,
        IResourceService resourceService,
        IConfiguration configuration)
    {
        _claimsProvider = claimsProvider;
        _userSync = userSync;
        _resourceService = resourceService;
        _configuration = configuration;
    }

    /// <summary>
    /// Get current authenticated user's information
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!_claimsProvider.IsAuthenticated())
        {
            return Unauthorized(new { message = "User is not authenticated" });
        }

        // Ensure user exists in database and get their resource record
        var resource = await _userSync.EnsureCurrentUserExistsAsync();

        if (resource == null)
        {
            return Problem("Unable to retrieve or create user record");
        }

        // Check if running in Mock mode
        var authMode = _configuration["Authentication:Mode"];
        var isMockMode = string.Equals(authMode, "Mock", StringComparison.OrdinalIgnoreCase);

        return Ok(new
        {
            id = resource.Id,
            name = resource.Name,
            email = resource.Email,
            employeeNumber = resource.EmployeeNumber,
            role = resource.Role,
            isApprover = resource.IsApprover,
            isActive = resource.IsActive,
            department = resource.Department,
            roles = _claimsProvider.GetRoles(),
            isMockMode = isMockMode
        });
    }

    /// <summary>
    /// Checks if the current authenticated user has a specific role.
    /// </summary>
    /// <param name="roleName">The name of the role to check.</param>
    /// <returns>An object indicating whether the user has the specified role.</returns>
    [HttpGet("role/{roleName}")]
    public IActionResult CheckRole(string roleName)
    {
        if (!_claimsProvider.IsAuthenticated())
        {
            return Unauthorized();
        }

        var hasRole = _claimsProvider.IsInRole(roleName);
        return Ok(new { role = roleName, hasRole });
    }

    /// <summary>
    /// Sets impersonation role for the current session. Only available in Mock authentication mode for testing purposes.
    /// </summary>
    /// <param name="request">The impersonation request containing the desired role.</param>
    /// <returns>Success message if impersonation is set, or error if not in Mock mode.</returns>
    [HttpPost("impersonate")]
    public IActionResult SetImpersonation([FromBody] ImpersonationRequest request)
    {
        var authMode = _configuration["Authentication:Mode"];
        if (!string.Equals(authMode, "Mock", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Impersonation is only available in Mock authentication mode" });
        }

        var validRoles = new[] { "Admin", "Manager", "Approver", "Employee", "Employee2" };
        if (!validRoles.Contains(request.Role, StringComparer.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = $"Invalid role. Must be one of: {string.Join(", ", validRoles)}" });
        }

        // Set cookie to persist impersonation
        Response.Cookies.Append("MockImpersonation", request.Role, new CookieOptions
        {
            HttpOnly = false, // Allow JavaScript access for easier testing
            SameSite = SameSiteMode.Strict,
            MaxAge = TimeSpan.FromDays(7)
        });

        return Ok(new { message = $"Impersonating {request.Role}", role = request.Role });
    }

    /// <summary>
    /// Clears the current impersonation settings. Only available in Mock authentication mode.
    /// </summary>
    /// <returns>Success message indicating impersonation has been cleared.</returns>
    [HttpPost("clearimpersonation")]
    public IActionResult ClearImpersonation()
    {
        Response.Cookies.Delete("MockImpersonation");
        return Ok(new { message = "Impersonation cleared" });
    }
}

/// <summary>
/// Request model for setting user impersonation in Mock authentication mode.
/// </summary>
/// <param name="Role">The role to impersonate (Admin, Manager, Approver, Employee, or Employee2).</param>
public record ImpersonationRequest(string Role);
