using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using pto.track.services;
using pto.track.services.DTOs;

namespace pto.track.Controllers;

/// <summary>
/// API controller for managing calendar events.
/// </summary>
[Authorize]
[Route("api/[controller]")]
[ApiController]
public class EventsController(IEventService eventService, ILogger<EventsController> logger) : ControllerBase
{
    private readonly IEventService _eventService = eventService;
    private readonly ILogger<EventsController> _logger = logger;

    /// <summary>
    /// Retrieves all events within a specified date range.
    /// </summary>
    /// <param name="start">The start date of the range.</param>
    /// <param name="end">The end date of the range.</param>
    /// <returns>A collection of events within the specified date range.</returns>
    // GET: api/Events
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetSchedulerEvents([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        _logger.LogDebug("GetSchedulerEvents called with start={Start}, end={End}", start, end);
        var events = await _eventService.GetEventsAsync(start, end);
        _logger.LogDebug("Returning {Count} events", events.Count());
        return Ok(events);
    }

    /// <summary>
    /// Retrieves a specific event by its ID.
    /// </summary>
    /// <param name="id">The unique identifier of the event.</param>
    /// <returns>The event with the specified ID, or NotFound if it doesn't exist.</returns>
    // GET: api/Events/5
    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetSchedulerEvent(Guid id)
    {
        _logger.LogDebug("GetSchedulerEvent called with id={Id}", id);
        var evt = await _eventService.GetEventByIdAsync(id);
        if (evt == null)
        {
            _logger.LogDebug("Event {Id} not found", id);
            return NotFound();
        }

        return Ok(evt);
    }

    /// <summary>
    /// Updates an existing event.
    /// </summary>
    /// <param name="id">The unique identifier of the event to update.</param>
    /// <param name="dto">The updated event data.</param>
    /// <returns>NoContent if successful, NotFound if the event doesn't exist.</returns>
    // PUT: api/Events/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSchedulerEvent(Guid id, UpdateEventDto dto)
    {
        _logger.LogDebug("PutSchedulerEvent called with id={Id}", id);
        if (!ModelState.IsValid)
        {
            _logger.LogDebug("Invalid ModelState for PutSchedulerEvent");
            return BadRequest(ModelState);
        }

        var result = await _eventService.UpdateEventAsync(id, dto);
        if (!result.Success)
        {
            _logger.LogDebug("Event {Id} not found or update failed", id);
            return NotFound();
        }

        return NoContent();
    }

    /// <summary>
    /// Creates a new event.
    /// </summary>
    /// <param name="dto">The event data to create.</param>
    /// <returns>The created event with a location header pointing to the new resource.</returns>
    // POST: api/Events
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<EventDto>> PostSchedulerEvent(CreateEventDto dto)
    {
        _logger.LogDebug("PostSchedulerEvent called");
        if (!ModelState.IsValid)
        {
            _logger.LogDebug("Invalid ModelState for PostSchedulerEvent");
            return BadRequest(ModelState);
        }

        var created = await _eventService.CreateEventAsync(dto);
        _logger.LogDebug("Event created with id={Id}", created.Id);
        return CreatedAtAction(nameof(GetSchedulerEvent), new { id = created.Id }, created);
    }

    /// <summary>
    /// Deletes an event.
    /// </summary>
    /// <param name="id">The unique identifier of the event to delete.</param>
    /// <returns>NoContent if successful, NotFound if the event doesn't exist.</returns>
    // DELETE: api/Events/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchedulerEvent(Guid id)
    {
        _logger.LogDebug("DeleteSchedulerEvent called with id={Id}", id);
        var result = await _eventService.DeleteEventAsync(id);
        if (!result.Success)
        {
            _logger.LogDebug("Event {Id} not found or delete failed", id);
            return NotFound();
        }

        _logger.LogDebug("Event {Id} deleted successfully", id);
        return NoContent();
    }
}
