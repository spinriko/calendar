

namespace pto.track.Controllers;

using Microsoft.AspNetCore.Mvc;
using pto.track.services;
using pto.track.services.DTOs;


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

    /// <summary>
    /// Retrieves resources for a specific group.
    /// </summary>
    /// <param name="groupId">The group ID to filter resources.</param>
    /// <returns>A collection of resources for the group.</returns>
    // GET: api/resources/group/{groupId}
    [HttpGet("group/{groupId}")]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> GetResourcesByGroup(int groupId)
    {
        _logger.LogDebug("GetResourcesByGroup called for group {GroupId}", groupId);
        var resources = await _resourceService.GetResourcesByGroupAsync(groupId);
        _logger.LogDebug("Returning {Count} resources for group {GroupId}", resources.Count(), groupId);
        return Ok(resources);
    }
}


