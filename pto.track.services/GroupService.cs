using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.data.Models;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;

namespace pto.track.services;

public class GroupService : IGroupService
{
    private readonly PtoTrackDbContext _context;
    private readonly ILogger<GroupService> _logger;
    private readonly IMapper _mapper;

    /// <inheritdoc />
    public async Task<IEnumerable<ResourceDto>> GetResourcesByGroupAsync(int groupId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GroupService.GetResourcesByGroupAsync: Fetching resources for group {GroupId}", groupId);
        var resources = await _context.Resources
            .AsNoTracking()
            .Where(r => r.GroupId == groupId)
            .ProjectTo<ResourceDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        _logger.LogDebug("GroupService.GetResourcesByGroupAsync: Found {Count} resources for group {GroupId}", resources.Count, groupId);
        return resources;
    }

    public GroupService(PtoTrackDbContext context, ILogger<GroupService> logger, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<GroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GroupService.GetGroupsAsync: Fetching all groups");
        List<GroupDto> groups;

        if (_context.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            groups = await _context.Groups
                .AsNoTracking()
                .Select(g => new GroupDto(g.GroupId, g.Name))
                .ToListAsync(cancellationToken);
        }
        else
        {
            groups = await _context.Groups
                .AsNoTracking()
                .ProjectTo<GroupDto>(_mapper.ConfigurationProvider)
                .ToListAsync(cancellationToken);
        }

        _logger.LogDebug("GroupService.GetGroupsAsync: Found {Count} groups", groups.Count);
        return groups;
    }

    /// <inheritdoc />
    public async Task<GroupDto?> GetGroupByIdAsync(int groupId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GroupService.GetGroupByIdAsync: Fetching group {GroupId}", groupId);

        var group = await _context.Groups
            .AsNoTracking()
            .Where(g => g.GroupId == groupId)
            .ProjectTo<GroupDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        if (group == null)
        {
            _logger.LogDebug("GroupService.GetGroupByIdAsync: Group {GroupId} not found", groupId);
            throw new GroupNotFoundException(groupId);
        }

        return group;
    }

    /// <inheritdoc />
    public async Task<GroupDto> CreateGroupAsync(CreateGroupDto createDto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GroupService.CreateGroupAsync: Creating group with name {Name}", createDto.Name);

        var group = new Group
        {
            Name = createDto.Name
        };

        _context.Groups.Add(group);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("GroupService.CreateGroupAsync: Created group with ID {GroupId}", group.GroupId);

        return new GroupDto(group.GroupId, group.Name);
    }

    /// <inheritdoc />
    public async Task<bool> UpdateGroupAsync(int groupId, UpdateGroupDto updateDto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GroupService.UpdateGroupAsync: Updating group {GroupId}", groupId);

        var group = await _context.Groups.FindAsync(new object[] { groupId }, cancellationToken);
        if (group == null)
        {
            _logger.LogDebug("GroupService.UpdateGroupAsync: Group {GroupId} not found", groupId);
            throw new GroupNotFoundException(groupId);
        }

        group.Name = updateDto.Name;
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("GroupService.UpdateGroupAsync: Updated group {GroupId}", groupId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteGroupAsync(int groupId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("GroupService.DeleteGroupAsync: Deleting group {GroupId}", groupId);

        var group = await _context.Groups.FindAsync(new object[] { groupId }, cancellationToken);
        if (group == null)
        {
            _logger.LogDebug("GroupService.DeleteGroupAsync: Group {GroupId} not found", groupId);
            throw new GroupNotFoundException(groupId);
        }

        // Check if group has resources
        var hasResources = await _context.Resources.AnyAsync(r => r.GroupId == groupId, cancellationToken);
        if (hasResources)
        {
            _logger.LogWarning("GroupService.DeleteGroupAsync: Cannot delete group {GroupId} - has associated resources", groupId);
            throw new InvalidOperationException($"Cannot delete group {groupId} because it has associated resources.");
        }

        _context.Groups.Remove(group);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug("GroupService.DeleteGroupAsync: Deleted group {GroupId}", groupId);
        return true;
    }
}
