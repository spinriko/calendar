using pto.track.services.DTOs;

namespace pto.track.services;

public interface IEventService
{
    Task<IEnumerable<EventDto>> GetEventsAsync(DateTime start, DateTime end);
    Task<EventDto?> GetEventByIdAsync(Guid id);
    Task<EventDto> CreateEventAsync(CreateEventDto dto);
    Task<bool> UpdateEventAsync(Guid id, UpdateEventDto dto);
    Task<bool> DeleteEventAsync(Guid id);
}
