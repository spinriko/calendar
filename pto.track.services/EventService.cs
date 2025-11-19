using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services;

public class EventService : IEventService
{
    private readonly PtoTrackDbContext _context;
    private readonly ILogger<EventService> _logger;

    public EventService(PtoTrackDbContext context, ILogger<EventService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<EventDto>> GetEventsAsync(DateTime start, DateTime end)
    {
        _logger.LogDebug("EventService.GetEventsAsync: start={Start}, end={End}", start, end);
        var events = await _context.Events
            .AsNoTracking()
            .Where(e => !((e.End <= start) || (e.Start >= end)))
            .Select(e => new EventDto(e.Id, e.Start, e.End, e.Text, e.Color, e.ResourceId))
            .ToListAsync();
        _logger.LogDebug("EventService.GetEventsAsync: Found {Count} events", events.Count);
        return events;
    }

    public async Task<EventDto?> GetEventByIdAsync(Guid id)
    {
        _logger.LogDebug("EventService.GetEventByIdAsync: id={Id}", id);
        var evt = await _context.Events.FindAsync(id);
        if (evt == null)
        {
            _logger.LogDebug("EventService.GetEventByIdAsync: Event {Id} not found", id);
            return null;
        }
        return new EventDto(evt.Id, evt.Start, evt.End, evt.Text, evt.Color, evt.ResourceId);
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto dto)
    {
        _logger.LogDebug("EventService.CreateEventAsync: Creating event");
        var entity = new SchedulerEvent
        {
            Start = dto.Start,
            End = dto.End,
            Text = dto.Text,
            Color = dto.Color,
            ResourceId = dto.ResourceId
        };

        _context.Events.Add(entity);
        await _context.SaveChangesAsync();
        _logger.LogDebug("EventService.CreateEventAsync: Created event with id={Id}", entity.Id);

        return new EventDto(entity.Id, entity.Start, entity.End, entity.Text, entity.Color, entity.ResourceId);
    }

    public async Task<bool> UpdateEventAsync(Guid id, UpdateEventDto dto)
    {
        _logger.LogDebug("EventService.UpdateEventAsync: id={Id}", id);
        var existing = await _context.Events.FindAsync(id);
        if (existing == null)
        {
            _logger.LogDebug("EventService.UpdateEventAsync: Event {Id} not found", id);
            return false;
        }

        existing.Start = dto.Start;
        existing.End = dto.End;
        existing.Text = dto.Text;
        existing.Color = dto.Color;
        existing.ResourceId = dto.ResourceId;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogDebug("EventService.UpdateEventAsync: Event {Id} updated successfully", id);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Events.AnyAsync(e => e.Id == id))
            {
                _logger.LogDebug("EventService.UpdateEventAsync: Concurrency - Event {Id} not found", id);
                return false;
            }
            throw;
        }
    }

    public async Task<bool> DeleteEventAsync(Guid id)
    {
        _logger.LogDebug("EventService.DeleteEventAsync: id={Id}", id);
        var entity = await _context.Events.FindAsync(id);
        if (entity == null)
        {
            _logger.LogDebug("EventService.DeleteEventAsync: Event {Id} not found", id);
            return false;
        }

        _context.Events.Remove(entity);
        await _context.SaveChangesAsync();
        _logger.LogDebug("EventService.DeleteEventAsync: Event {Id} deleted successfully", id);
        return true;
    }
}
