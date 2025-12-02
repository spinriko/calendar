using pto.track.services.DTOs;

namespace pto.track.services;

/// <summary>
/// Service for managing groups.
/// </summary>
public interface IGroupService
{
    /// <summary>
    /// Gets all groups in the system.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all groups.</returns>
    Task<IEnumerable<GroupDto>> GetGroupsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific group by ID.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The group if found.</returns>
    Task<GroupDto?> GetGroupByIdAsync(int groupId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new group.
    /// </summary>
    /// <param name="createDto">The group data for creation.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created group.</returns>
    Task<GroupDto> CreateGroupAsync(CreateGroupDto createDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group to update.</param>
    /// <param name="updateDto">The updated group data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the update was successful.</returns>
    Task<bool> UpdateGroupAsync(int groupId, UpdateGroupDto updateDto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a group.
    /// </summary>
    /// <param name="groupId">The unique identifier of the group to delete.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the deletion was successful.</returns>
    Task<bool> DeleteGroupAsync(int groupId, CancellationToken cancellationToken = default);
}
