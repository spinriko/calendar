using AutoMapper;
using pto.track.data.Models;
using pto.track.services.DTOs;

namespace pto.track.services.Mapping;

/// <summary>
/// AutoMapper profile for group-related entity to DTO mappings.
/// </summary>
public class GroupMappingProfile : Profile
{
    public GroupMappingProfile()
    {
        CreateMap<Group, GroupDto>();
    }
}
