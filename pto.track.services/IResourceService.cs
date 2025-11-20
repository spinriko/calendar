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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of all resources.</returns>
    Task<IEnumerable<ResourceDto>> GetResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets only active resources.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of active resources.</returns>
    Task<IEnumerable<ResourceDto>> GetActiveResourcesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all resources that have approver privileges.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of approvers.</returns>
    Task<IEnumerable<ResourceDto>> GetApproversAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific resource by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the resource.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The resource if found; otherwise, null.</returns>
    Task<ResourceDto?> GetResourceByIdAsync(int id, CancellationToken cancellationToken = default);
}
