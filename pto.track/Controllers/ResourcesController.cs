using Microsoft.AspNetCore.Mvc;
using pto.track.services;
using pto.track.services.DTOs;

namespace pto.track.Controllers;

/// <summary>
/// API controller for managing resources (employees).
/// </summary>
[Produces("application/json")]
[Route("api/resources")]
public class ResourcesController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<ResourcesController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResourcesController"/> class.
    /// </summary>
    /// <param name="resourceService">The resource service.</param>
    /// <param name="logger">The logger.</param>
    public ResourcesController(IResourceService resourceService, ILogger<ResourcesController> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all resources in the system.
    /// </summary>
    /// <returns>A collection of all resources.</returns>
    // GET: api/resources
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> GetResources()
    {
        _logger.LogDebug("GetResources called");
        var resources = await _resourceService.GetResourcesAsync();
        _logger.LogDebug("Returning {Count} resources", resources.Count());
        return Ok(resources);
    }
}
