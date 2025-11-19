using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services;

public class ResourceService : IResourceService
{
    private readonly PtoTrackDbContext _context;
    private readonly ILogger<ResourceService> _logger;

    public ResourceService(PtoTrackDbContext context, ILogger<ResourceService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<ResourceDto>> GetResourcesAsync()
    {
        _logger.LogDebug("ResourceService.GetResourcesAsync: Fetching all resources");
        var resources = await _context.Resources
            .AsNoTracking()
            .Select(r => new ResourceDto(r.Id, r.Name))
            .ToListAsync();
        _logger.LogDebug("ResourceService.GetResourcesAsync: Found {Count} resources", resources.Count);
        return resources;
    }
}
