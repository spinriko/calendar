namespace pto.track.services.Exceptions;

/// <summary>
/// Exception thrown when a requested event is not found.
/// </summary>
public class EventNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventNotFoundException"/> class.
    /// </summary>
    /// <param name="eventId">The ID of the event that was not found.</param>
    public EventNotFoundException(Guid eventId)
        : base($"Event with ID '{eventId}' was not found.")
    {
        EventId = eventId;
    }

    /// <summary>
    /// Gets the ID of the event that was not found.
    /// </summary>
    public Guid EventId { get; }
}
