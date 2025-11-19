using Microsoft.AspNetCore.Mvc;
using pto.track.services;
using pto.track.services.DTOs;

namespace pto.track.Controllers;

[Produces("application/json")]
[Route("api/resources")]
public class ResourcesController : Controller
{
    private readonly IResourceService _resourceService;
    private readonly ILogger<ResourcesController> _logger;

    public ResourcesController(IResourceService resourceService, ILogger<ResourcesController> logger)
    {
        _resourceService = resourceService;
        _logger = logger;
    }

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
