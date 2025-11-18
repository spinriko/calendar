using Microsoft.AspNetCore.Mvc;
using pto.track.services;
using pto.track.services.DTOs;

namespace pto.track.Controllers;

[Route("api/[controller]")]
[ApiController]
public class EventsController(IEventService eventService) : ControllerBase
{
    private readonly IEventService _eventService = eventService;

    // GET: api/Events
    [HttpGet]
    public async Task<ActionResult<IEnumerable<EventDto>>> GetSchedulerEvents([FromQuery] DateTime start, [FromQuery] DateTime end)
    {
        var events = await _eventService.GetEventsAsync(start, end);
        return Ok(events);
    }

    // GET: api/Events/5
    [HttpGet("{id}")]
    public async Task<ActionResult<EventDto>> GetSchedulerEvent(Guid id)
    {
        var evt = await _eventService.GetEventByIdAsync(id);
        if (evt == null)
        {
            return NotFound();
        }

        return Ok(evt);
    }

    // PUT: api/Events/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutSchedulerEvent(Guid id, UpdateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var success = await _eventService.UpdateEventAsync(id, dto);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST: api/Events
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<EventDto>> PostSchedulerEvent(CreateEventDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var created = await _eventService.CreateEventAsync(dto);
        return CreatedAtAction(nameof(GetSchedulerEvent), new { id = created.Id }, created);
    }

    // DELETE: api/Events/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSchedulerEvent(Guid id)
    {
        var success = await _eventService.DeleteEventAsync(id);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }
}
