using Microsoft.AspNetCore.Mvc;
using pto.track.Models;

namespace pto.track.Controllers;

/// <summary>
/// API controller for managing user impersonation (development/mock mode only).
/// </summary>
[ApiController]
[Route("api/impersonation")]
public class ImpersonationController : Controller
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImpersonationController> _logger;

    public ImpersonationController(
        IConfiguration configuration,
        ILogger<ImpersonationController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Sets impersonation data for the current session.
    /// Only works when Authentication:Mode is "Mock".
    /// </summary>
    [HttpPost]
    public IActionResult SetImpersonation([FromBody] UserImpersonationRequest request)
    {
        var authMode = _configuration["Authentication:Mode"] ?? "Mock";

        if (!authMode.Equals("Mock", StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new { message = "Impersonation only available in Mock authentication mode" });
        }

        var impersonationData = System.Text.Json.JsonSerializer.Serialize(request);

        Response.Cookies.Append("ImpersonationData", impersonationData, new CookieOptions
        {
            HttpOnly = true,
            Secure = false, // Set to true in production with HTTPS
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromHours(8)
        });

        _logger.LogInformation("Impersonation set for {EmployeeNumber} with roles {Roles}",
            request.EmployeeNumber, string.Join(", ", request.Roles));

        return Ok(new { message = "Impersonation applied" });
    }

    /// <summary>
    /// Clears impersonation data, reverting to default mock user.
    /// </summary>
    [HttpDelete]
    public IActionResult ClearImpersonation()
    {
        Response.Cookies.Delete("ImpersonationData");

        _logger.LogInformation("Impersonation cleared");

        return Ok(new { message = "Impersonation cleared" });
    }
}
