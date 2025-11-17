namespace pto.track.services;

public interface IResourceService
{
    Task<IEnumerable<ResourceDto>> GetResourcesAsync();
}
