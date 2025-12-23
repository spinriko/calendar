using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.Negotiate;
using pto.track.services;
using pto.track.services.Authentication;
using pto.track.services.Identity;

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
    private readonly IIdentityEnricher _identityEnricher;

    public CurrentUserController(
        IUserClaimsProvider claimsProvider,
        IUserSyncService userSync,
        IResourceService resourceService,
        IConfiguration configuration,
        IIdentityEnricher identityEnricher)
    {
        _claimsProvider = claimsProvider;
        _userSync = userSync;
        _resourceService = resourceService;
        _configuration = configuration;
        _identityEnricher = identityEnricher;
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

    /// <summary>
    /// DEBUG: Get all available claims for the current user (development only)
    /// </summary>
    [HttpGet("debug/claims")]
    public async Task<IActionResult> GetAllClaims()
    {
        // Check raw HTTP identity (not claims provider) for debugging
        if (User.Identity?.IsAuthenticated != true)
        {
            // Proactively trigger Windows/Negotiate authentication when configured
            var mode = _configuration["Authentication:Mode"] ?? "Mock";
            if (string.Equals(mode, "Windows", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mode, "ActiveDirectory", StringComparison.OrdinalIgnoreCase))
            {
                return Challenge(NegotiateDefaults.AuthenticationScheme);
            }

            return Unauthorized(new { message = "User is not authenticated", rawIdentityName = User.Identity?.Name });
        }

        var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        var identity = User.Identity;
        var normalized = NormalizeIdentityName(identity?.Name);
        var enriched = normalized != null
            ? await _identityEnricher.EnrichAsync(normalized, HttpContext.RequestAborted)
            : new Dictionary<string, string?>();

        return Ok(new
        {
            IdentityName = identity?.Name,
            NormalizedIdentity = normalized,
            AuthenticationType = identity?.AuthenticationType,
            IsAuthenticated = identity?.IsAuthenticated,
            Claims = claims,
            ClaimCount = claims.Count,
            Enriched = enriched
        });
    }

    private static string? NormalizeIdentityName(string? rawName)
    {
        if (string.IsNullOrWhiteSpace(rawName)) return rawName;

        // Common Windows formats: DOMAIN\\user or user@domain
        var name = rawName.Trim();

        // If domain\\user, drop the domain prefix
        var slashIdx = name.IndexOf('\\');
        if (slashIdx >= 0 && slashIdx < name.Length - 1)
        {
            name = name[(slashIdx + 1)..];
        }

        // Lowercase for stable lookups (UPN/user principal lookups are usually case-insensitive)
        return name.ToLowerInvariant();
    }
}
