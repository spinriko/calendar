using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace pto.track.Pages;

/// <summary>
/// Base page model that provides common functionality for all pages.
/// Automatically enables impersonation panel in Mock authentication mode.
/// </summary>
public abstract class BasePageModel : PageModel
{
    protected readonly IConfiguration Configuration;

    protected BasePageModel(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    /// <summary>
    /// Enables the impersonation panel if running in Mock authentication mode.
    /// </summary>
    protected void EnableImpersonationIfMockMode()
    {
        var authMode = Configuration["Authentication:Mode"] ?? "Mock";
        ViewData["ShowImpersonation"] = authMode.Equals("Mock", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Called before the page handler executes. Automatically enables impersonation for Mock mode.
    /// </summary>
    public override void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        base.OnPageHandlerExecuting(context);
        EnableImpersonationIfMockMode();
    }
}
