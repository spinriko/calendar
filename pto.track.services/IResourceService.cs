using pto.track.services.DTOs;

namespace pto.track.services;

public interface IResourceService
{
    Task<IEnumerable<ResourceDto>> GetResourcesAsync();
    Task<IEnumerable<ResourceDto>> GetActiveResourcesAsync();
    Task<IEnumerable<ResourceDto>> GetApproversAsync();
    Task<ResourceDto?> GetResourceByIdAsync(int id);
}
