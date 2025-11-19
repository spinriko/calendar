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
            .Select(r => new ResourceDto(
                r.Id,
                r.Name,
                r.Email,
                r.EmployeeNumber,
                r.Role,
                r.IsApprover,
                r.IsActive,
                r.Department))
            .ToListAsync();
        _logger.LogDebug("ResourceService.GetResourcesAsync: Found {Count} resources", resources.Count);
        return resources;
    }

    public async Task<IEnumerable<ResourceDto>> GetActiveResourcesAsync()
    {
        _logger.LogDebug("ResourceService.GetActiveResourcesAsync: Fetching active resources");
        var resources = await _context.Resources
            .AsNoTracking()
            .Where(r => r.IsActive)
            .Select(r => new ResourceDto(
                r.Id,
                r.Name,
                r.Email,
                r.EmployeeNumber,
                r.Role,
                r.IsApprover,
                r.IsActive,
                r.Department))
            .ToListAsync();
        _logger.LogDebug("ResourceService.GetActiveResourcesAsync: Found {Count} active resources", resources.Count);
        return resources;
    }

    public async Task<IEnumerable<ResourceDto>> GetApproversAsync()
    {
        _logger.LogDebug("ResourceService.GetApproversAsync: Fetching approvers");
        var approvers = await _context.Resources
            .AsNoTracking()
            .Where(r => r.IsApprover && r.IsActive)
            .Select(r => new ResourceDto(
                r.Id,
                r.Name,
                r.Email,
                r.EmployeeNumber,
                r.Role,
                r.IsApprover,
                r.IsActive,
                r.Department))
            .ToListAsync();
        _logger.LogDebug("ResourceService.GetApproversAsync: Found {Count} approvers", approvers.Count);
        return approvers;
    }

    public async Task<ResourceDto?> GetResourceByIdAsync(int id)
    {
        _logger.LogDebug("ResourceService.GetResourceByIdAsync: Fetching resource {Id}", id);
        var resource = await _context.Resources
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new ResourceDto(
                r.Id,
                r.Name,
                r.Email,
                r.EmployeeNumber,
                r.Role,
                r.IsApprover,
                r.IsActive,
                r.Department))
            .FirstOrDefaultAsync();

        if (resource == null)
        {
            _logger.LogDebug("ResourceService.GetResourceByIdAsync: Resource {Id} not found", id);
        }

        return resource;
    }
}
