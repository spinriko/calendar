using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.services;

public class ResourceService : IResourceService
{
    private readonly SchedulerDbContext _context;

    public ResourceService(SchedulerDbContext context)
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
