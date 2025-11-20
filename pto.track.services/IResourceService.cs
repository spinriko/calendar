using pto.track.services.DTOs;

namespace pto.track.services;

/// <summary>
/// Service for managing resources (employees/users).
/// </summary>
public interface IResourceService
{
    /// <summary>
    /// Gets all resources in the system.
    /// </summary>
    /// <returns>A collection of all resources.</returns>
    Task<IEnumerable<ResourceDto>> GetResourcesAsync();

    /// <summary>
    /// Gets only active resources.
    /// </summary>
    /// <returns>A collection of active resources.</returns>
    Task<IEnumerable<ResourceDto>> GetActiveResourcesAsync();

    /// <summary>
    /// Gets all resources that have approver privileges.
    /// </summary>
    /// <returns>A collection of approvers.</returns>
    Task<IEnumerable<ResourceDto>> GetApproversAsync();

    /// <summary>
    /// Gets a specific resource by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the resource.</param>
    /// <returns>The resource if found; otherwise, null.</returns>
    Task<ResourceDto?> GetResourceByIdAsync(int id);
}
