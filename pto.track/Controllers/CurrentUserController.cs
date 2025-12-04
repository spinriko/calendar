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

            var (effectiveRole, effectiveIsApprover) = DetermineEffectiveRoleAndApprover(resource);
            var claimRoles = _claimsProvider.GetRoles().ToList();
            var isMockMode = IsMockAuthenticationMode();

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

    private (string role, bool isApprover) DetermineEffectiveRoleAndApprover(pto.track.data.Resource resource)
    {
        var claimRoles = _claimsProvider.GetRoles().ToList();

        // In mock mode with impersonation, use claim-based roles to determine effective role
        if (IsMockAuthenticationMode() && claimRoles.Any())
        {
            return GetRoleFromClaims(claimRoles);
        }

        return (resource.Role, resource.IsApprover);
    }

    private (string role, bool isApprover) GetRoleFromClaims(List<string> claimRoles)
    {
        // Priority order: Admin > Manager > Approver > Employee
        if (claimRoles.Contains("Admin", StringComparer.OrdinalIgnoreCase))
            return ("Admin", true);

        if (claimRoles.Contains("Manager", StringComparer.OrdinalIgnoreCase))
            return ("Manager", true);

        if (claimRoles.Contains("Approver", StringComparer.OrdinalIgnoreCase))
            return ("Approver", true);

        if (claimRoles.Contains("Employee", StringComparer.OrdinalIgnoreCase))
            return ("Employee", false);

        return ("Employee", false); // Default
    }

    private bool IsMockAuthenticationMode()
    {
        var authMode = _configuration["Authentication:Mode"];
        return string.Equals(authMode, "Mock", StringComparison.OrdinalIgnoreCase);
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
