using AutoMapper;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services.Mapping;

/// <summary>
/// AutoMapper profile for resource-related entity to DTO mappings.
/// </summary>
public class ResourceMappingProfile : Profile
{
    public ResourceMappingProfile()
    {
        CreateMap<SchedulerResource, ResourceDto>();
    }
}
