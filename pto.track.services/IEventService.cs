using pto.track.services.DTOs;

namespace pto.track.services;

public interface IEventService
{
    Task<IEnumerable<EventDto>> GetEventsAsync(DateTime start, DateTime end);
    Task<EventDto?> GetEventByIdAsync(int id);
    Task<EventDto> CreateEventAsync(CreateEventDto dto);
    Task<bool> UpdateEventAsync(int id, UpdateEventDto dto);
    Task<bool> DeleteEventAsync(int id);
}
