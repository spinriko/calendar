using Microsoft.AspNetCore.Mvc;
using pto.track.services;
using pto.track.services.Authentication;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;

namespace pto.track.Controllers;

/// <summary>
/// API controller for managing groups. Administrator access only.
/// </summary>
[Produces("application/json")]
[Route("api/groups")]
public class GroupsController : Controller
{
    private readonly IGroupService _groupService;
    private readonly IUserClaimsProvider _claimsProvider;
    private readonly ILogger<GroupsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GroupsController"/> class.
    /// </summary>
    /// <param name="groupService">The group service.</param>
    /// <param name="claimsProvider">The user claims provider for authorization.</param>
    /// <param name="logger">The logger.</param>
    public GroupsController(
        IGroupService groupService,
        IUserClaimsProvider claimsProvider,
        ILogger<GroupsController> logger)
    {
        _groupService = groupService;
        _claimsProvider = claimsProvider;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all groups in the system. Administrator access only.
    /// </summary>
    /// <returns>A collection of all groups.</returns>
    // GET: api/groups
    [HttpGet]
    public async Task<ActionResult<IEnumerable<GroupDto>>> GetGroups()
    {
        if (!_claimsProvider.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized access attempt to GetGroups by non-admin user");
            return Forbid();
        }

        _logger.LogDebug("GetGroups called");
        var groups = await _groupService.GetGroupsAsync();
        _logger.LogDebug("Returning {Count} groups", groups.Count());
        return Ok(groups);
    }

    /// <summary>
    /// Retrieves a specific group by ID. Administrator access only.
    /// </summary>
    /// <param name="groupId">The group ID.</param>
    /// <returns>The requested group.</returns>
    // GET: api/groups/{groupId}
    [HttpGet("{groupId}")]
    public async Task<ActionResult<GroupDto>> GetGroupById(int groupId)
    {
        if (!_claimsProvider.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized access attempt to GetGroupById by non-admin user");
            return Forbid();
        }

        try
        {
            _logger.LogDebug("GetGroupById called for group {GroupId}", groupId);
            var group = await _groupService.GetGroupByIdAsync(groupId);
            return Ok(group);
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning("Group {GroupId} not found", groupId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Creates a new group. Administrator access only.
    /// </summary>
    /// <param name="createDto">The group data for creation.</param>
    /// <returns>The created group.</returns>
    // POST: api/groups
    [HttpPost]
    public async Task<ActionResult<GroupDto>> CreateGroup([FromBody] CreateGroupDto createDto)
    {
        if (!_claimsProvider.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized access attempt to CreateGroup by non-admin user");
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(createDto.Name))
        {
            return BadRequest(new { message = "Group name is required" });
        }

        _logger.LogDebug("CreateGroup called with name {Name}", createDto.Name);
        var group = await _groupService.CreateGroupAsync(createDto);
        _logger.LogDebug("Created group with ID {GroupId}", group.GroupId);
        return CreatedAtAction(nameof(GetGroupById), new { groupId = group.GroupId }, group);
    }

    /// <summary>
    /// Updates an existing group. Administrator access only.
    /// </summary>
    /// <param name="groupId">The group ID to update.</param>
    /// <param name="updateDto">The updated group data.</param>
    /// <returns>No content on success.</returns>
    // PUT: api/groups/{groupId}
    [HttpPut("{groupId}")]
    public async Task<ActionResult> UpdateGroup(int groupId, [FromBody] UpdateGroupDto updateDto)
    {
        if (!_claimsProvider.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized access attempt to UpdateGroup by non-admin user");
            return Forbid();
        }

        if (string.IsNullOrWhiteSpace(updateDto.Name))
        {
            return BadRequest(new { message = "Group name is required" });
        }

        try
        {
            _logger.LogDebug("UpdateGroup called for group {GroupId}", groupId);
            await _groupService.UpdateGroupAsync(groupId, updateDto);
            _logger.LogDebug("Updated group {GroupId}", groupId);
            return NoContent();
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning("Group {GroupId} not found for update", groupId);
            return NotFound(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes a group. Administrator access only.
    /// </summary>
    /// <param name="groupId">The group ID to delete.</param>
    /// <returns>No content on success.</returns>
    // DELETE: api/groups/{groupId}
    [HttpDelete("{groupId}")]
    public async Task<ActionResult> DeleteGroup(int groupId)
    {
        if (!_claimsProvider.IsInRole("Admin"))
        {
            _logger.LogWarning("Unauthorized access attempt to DeleteGroup by non-admin user");
            return Forbid();
        }

        try
        {
            _logger.LogDebug("DeleteGroup called for group {GroupId}", groupId);
            await _groupService.DeleteGroupAsync(groupId);
            _logger.LogDebug("Deleted group {GroupId}", groupId);
            return NoContent();
        }
        catch (GroupNotFoundException ex)
        {
            _logger.LogWarning("Group {GroupId} not found for deletion", groupId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning("Cannot delete group {GroupId}: {Message}", groupId, ex.Message);
            return BadRequest(new { message = ex.Message });
        }
    }
}
