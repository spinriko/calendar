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
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of events.</returns>
    Task<IEnumerable<EventDto>> GetEventsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific event by ID.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The event if found; otherwise, null.</returns>
    Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new calendar event.
    /// </summary>
    /// <param name="dto">The event creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created event.</returns>
    Task<EventDto> CreateEventAsync(CreateEventDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing calendar event.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="dto">The updated event data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> UpdateEventAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a calendar event.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A result indicating success or failure.</returns>
    Task<Result> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default);
}
