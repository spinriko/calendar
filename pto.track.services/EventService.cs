using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using pto.track.data;
using pto.track.services.DTOs;
using pto.track.services.Exceptions;

namespace pto.track.services;

public class EventService : IEventService
{
    private readonly PtoTrackDbContext _context;
    private readonly ILogger<EventService> _logger;
    private readonly IMapper _mapper;

    public EventService(PtoTrackDbContext context, ILogger<EventService> logger, IMapper mapper)
    {
        _context = context;
        _logger = logger;
        _mapper = mapper;
    }

    /// <inheritdoc />
    public async Task<IEnumerable<EventDto>> GetEventsAsync(DateTime start, DateTime end, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("EventService.GetEventsAsync: start={Start}, end={End}", start, end);
        var events = await _context.Events
            .AsNoTracking()
            .Where(e => !((e.End <= start) || (e.Start >= end)))
            .ProjectTo<EventDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);
        _logger.LogDebug("EventService.GetEventsAsync: Found {Count} events", events.Count);
        return events;
    }

    /// <inheritdoc />
    public async Task<EventDto?> GetEventByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("EventService.GetEventByIdAsync: id={Id}", id);
        var evt = await _context.Events.FindAsync(new object[] { id }, cancellationToken);
        if (evt == null)
        {
            _logger.LogDebug("EventService.GetEventByIdAsync: Event {Id} not found", id);
            throw new EventNotFoundException(id);
        }
        return _mapper.Map<EventDto>(evt);
    }

    /// <inheritdoc />
    public async Task<EventDto> CreateEventAsync(CreateEventDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("EventService.CreateEventAsync: Creating event");
        var entity = _mapper.Map<SchedulerEvent>(dto);

        _context.Events.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("EventService.CreateEventAsync: Created event with id={Id}", entity.Id);

        return _mapper.Map<EventDto>(entity);
    }

    /// <inheritdoc />
    public async Task<Result> UpdateEventAsync(Guid id, UpdateEventDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("EventService.UpdateEventAsync: id={Id}", id);
        var existing = await _context.Events.FindAsync(new object[] { id }, cancellationToken);
        if (existing == null)
        {
            _logger.LogDebug("EventService.UpdateEventAsync: Event {Id} not found", id);
            throw new EventNotFoundException(id);
        }

        _mapper.Map(dto, existing);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogDebug("EventService.UpdateEventAsync: Event {Id} updated successfully", id);
            return Result.SuccessResult();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.Events.AnyAsync(e => e.Id == id, cancellationToken))
            {
                _logger.LogDebug("EventService.UpdateEventAsync: Concurrency - Event {Id} not found", id);
                throw new EventNotFoundException(id);
            }
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<Result> DeleteEventAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("EventService.DeleteEventAsync: id={Id}", id);
        var entity = await _context.Events.FindAsync(new object[] { id }, cancellationToken);
        if (entity == null)
        {
            _logger.LogDebug("EventService.DeleteEventAsync: Event {Id} not found", id);
            throw new EventNotFoundException(id);
        }

        _context.Events.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogDebug("EventService.DeleteEventAsync: Event {Id} deleted successfully", id);
        return Result.SuccessResult();
    }
}
