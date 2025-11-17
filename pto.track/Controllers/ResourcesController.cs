using Microsoft.AspNetCore.Mvc;
using pto.track.services;

namespace pto.track.Controllers;

[Produces("application/json")]
[Route("api/resources")]
public class ResourcesController : Controller
{
    private readonly IResourceService _resourceService;

    public ResourcesController(IResourceService resourceService)
    {
        _resourceService = resourceService;
    }

    // GET: api/resources
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> GetResources()
    {
        var resources = await _resourceService.GetResourcesAsync();
        return Ok(resources);
    }
}
