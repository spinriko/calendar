
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;

namespace pto.track.services;

public class ResourceService : IResourceService
{
    private readonly PtoTrackDbContext _context;
    private readonly ILogger<ResourceService> _logger;
    private readonly IMapper _mapper;

    public ResourceService(PtoTrackDbContext context, ILogger<ResourceService> logger, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }


    /// <summary>
    /// Gets resources filtered by group using specification pattern.
    /// </summary>
    public async Task<IEnumerable<ResourceDto>> GetResourcesByGroupAsync(int groupId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ResourceService.GetResourcesByGroupAsync: Fetching resources for group {GroupId}", groupId);
        var spec = new Specifications.ResourceGroupSpecification(groupId);
        var query = Specifications.SpecificationEvaluator.GetQuery(_context.Resources, spec);
        List<ResourceDto> resources;
        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            resources = await query
                .Select(r => new ResourceDto(
                    r.Id,
                    r.Name,
                    r.Email,
                    r.EmployeeNumber,
                    r.Role,
                    r.IsApprover,
                    r.IsActive,
                    r.Department,
                    r.GroupId
                ))
                .ToListAsync(cancellationToken);
        }
        else
        {
            resources = await query
                .ProjectTo<ResourceDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
        _logger.LogDebug("ResourceService.GetResourcesByGroupAsync: Found {Count} resources for group {GroupId}", resources.Count, groupId);
        return resources;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourceDto>> GetResourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ResourceService.GetResourcesAsync: Fetching all resources");
        List<ResourceDto> resources;
        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            resources = await _context.Resources
                .AsNoTracking()
                .Select(r => new ResourceDto(
                    r.Id,
                    r.Name,
                    r.Email,
                    r.EmployeeNumber,
                    r.Role,
                    r.IsApprover,
                    r.IsActive,
                    r.Department,
                    r.GroupId
                ))
                .ToListAsync(cancellationToken);
        }
        else
        {
            resources = await _context.Resources
                .AsNoTracking()
                .ProjectTo<ResourceDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }
        _logger.LogDebug("ResourceService.GetResourcesAsync: Found {Count} resources", resources.Count);
        return resources;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourceDto>> GetActiveResourcesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ResourceService.GetActiveResourcesAsync: Fetching active resources");
        var resources = await _context.Resources
            .AsNoTracking()
            .Where(r => r.IsActive)
            .ProjectTo<ResourceDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        _logger.LogDebug("ResourceService.GetActiveResourcesAsync: Found {Count} active resources", resources.Count);
        return resources;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<ResourceDto>> GetApproversAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ResourceService.GetApproversAsync: Fetching approvers");
        var approvers = await _context.Resources
            .AsNoTracking()
            .Where(r => r.IsApprover && r.IsActive)
            .ProjectTo<ResourceDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        _logger.LogDebug("ResourceService.GetApproversAsync: Found {Count} approvers", approvers.Count);
        return approvers;
    }

    /// <inheritdoc />
    public async Task<ResourceDto?> GetResourceByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("ResourceService.GetResourceByIdAsync: Fetching resource {Id}", id);
        var resource = await _context.Resources
            .AsNoTracking()
            .Where(r => r.Id == id)
            .ProjectTo<ResourceDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (resource == null)
        {
            _logger.LogDebug("ResourceService.GetResourceByIdAsync: Resource {Id} not found", id);
            throw new ResourceNotFoundException(id);
        }

        return resource;
    }
}
