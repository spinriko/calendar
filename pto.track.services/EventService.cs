using Microsoft.EntityFrameworkCore;
using pto.track.data;
using pto.track.services.DTOs;

namespace pto.track.services;

public class EventService : IEventService
{
    private readonly PtoTrackDbContext _context;

    public EventService(PtoTrackDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<EventDto>> GetEventsAsync(DateTime start, DateTime end)
    {
        return await _context.Events
            .AsNoTracking()
            .Where(e => !((e.End <= start) || (e.Start >= end)))
            .Select(e => new EventDto(e.Id, e.Start, e.End, e.Text, e.Color, e.ResourceId))
            .ToListAsync();
    }

    public async Task<EventDto?> GetEventByIdAsync(int id)
    {
        var evt = await _context.Events.FindAsync(id);
        return evt == null ? null : new EventDto(evt.Id, evt.Start, evt.End, evt.Text, evt.Color, evt.ResourceId);
    }

    public async Task<EventDto> CreateEventAsync(CreateEventDto dto)
    {
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

        return new EventDto(entity.Id, entity.Start, entity.End, entity.Text, entity.Color, entity.ResourceId);
    }

    public async Task<bool> UpdateEventAsync(int id, UpdateEventDto dto)
    {
        var existing = await _context.Events.FindAsync(id);
        if (existing == null) return false;

        existing.Start = dto.Start;
        existing.End = dto.End;
        existing.Text = dto.Text;
        existing.Color = dto.Color;
        existing.ResourceId = dto.ResourceId;

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Events.AnyAsync(e => e.Id == id))
            {
                return false;
            }
            throw;
        }
    }

    public async Task<bool> DeleteEventAsync(int id)
    {
        var entity = await _context.Events.FindAsync(id);
        if (entity == null) return false;

        _context.Events.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}
