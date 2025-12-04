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
        try
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

            // Get roles from claims (these reflect impersonation if active)
            var claimRoles = _claimsProvider.GetRoles().ToList();

            // Determine effective role and isApprover based on claims when impersonating
            string effectiveRole = resource.Role;
            bool effectiveIsApprover = resource.IsApprover;

            // In mock mode with impersonation, use claim-based roles to determine effective role
            if (isMockMode && claimRoles.Any())
            {
                // Priority order: Admin > Manager > Approver > Employee
                if (claimRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
                {
                    effectiveRole = "Admin";
                    effectiveIsApprover = true;
                }
                else if (claimRoles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
                {
                    effectiveRole = "Manager";
                    effectiveIsApprover = true;
                }
                else if (claimRoles.Contains("Approver", StringComparer.OrdinalIgnoreCase))
                {
                    effectiveRole = "Approver";
                    effectiveIsApprover = true;
                }
                else if (claimRoles.Contains("Employee", StringComparer.OrdinalIgnoreCase))
                {
                    effectiveRole = "Employee";
                    effectiveIsApprover = false;
                }
            }

            return Ok(new
            {
                id = resource.Id,
                name = resource.Name,
                email = resource.Email,
                employeeNumber = resource.EmployeeNumber,
                role = effectiveRole,
                isApprover = effectiveIsApprover,
                isActive = resource.IsActive,
                department = resource.Department,
                roles = claimRoles,
                isMockMode = isMockMode
            });
        }
        catch (Exception ex)
        {
            // Log the error for diagnostics
            Console.Error.WriteLine($"GetCurrentUser error: {ex}");
            return StatusCode(500, new { message = "Internal server error", error = ex.Message, stack = ex.StackTrace });
        }
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
}
