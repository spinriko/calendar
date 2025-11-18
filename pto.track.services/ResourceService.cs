using Microsoft.EntityFrameworkCore;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services;

public class ResourceService : IResourceService
{
    private readonly PtoTrackDbContext _context;

    public ResourceService(PtoTrackDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<ResourceDto>> GetResourcesAsync()
    {
        return await _context.Resources
            .AsNoTracking()
            .Select(r => new ResourceDto(r.Id, r.Name))
            .ToListAsync();
    }
}
