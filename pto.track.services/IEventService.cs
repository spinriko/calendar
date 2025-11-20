using pto.track.services.DTOs;

namespace pto.track.services;

/// <summary>
/// Service for managing calendar events.
/// </summary>
public interface IEventService
{
    /// <summary>
    /// Gets all calendar events within a date range.
    /// </summary>
    /// <param name="start">Start date of the range.</param>
    /// <param name="end">End date of the range.</param>
    /// <returns>A collection of events.</returns>
    Task<IEnumerable<EventDto>> GetEventsAsync(DateTime start, DateTime end);

    /// <summary>
    /// Gets a specific event by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    Task<EventDto?> GetEventByIdAsync(Guid id);

    /// <summary>
    /// Creates a new calendar event.
    /// </summary>
    /// <param name="dto">The event creation data.</param>
    /// <returns>The created event.</returns>
    Task<EventDto> CreateEventAsync(CreateEventDto dto);

    /// <summary>
    /// Updates an existing calendar event.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="dto">The updated event data.</param>
    /// <returns>True if the update was successful; otherwise, false.</returns>
    Task<bool> UpdateEventAsync(Guid id, UpdateEventDto dto);

    /// <summary>
    /// Deletes a calendar event.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <returns>True if the deletion was successful; otherwise, false.</returns>
    Task<bool> DeleteEventAsync(Guid id);
}
