using AutoMapper;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services.Mapping;

/// <summary>
/// AutoMapper profile for event-related entity to DTO mappings.
/// </summary>
public class EventMappingProfile : Profile
{
    public EventMappingProfile()
    {
        CreateMap<SchedulerEvent, EventDto>();

        CreateMap<CreateEventDto, SchedulerEvent>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());

        CreateMap<UpdateEventDto, SchedulerEvent>()
            .ForMember(dest => dest.Id, opt => opt.Ignore());
    }
}
