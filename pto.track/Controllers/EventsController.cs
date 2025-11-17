using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models;

namespace Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly SchedulerDbContext _context;

        public EventsController(SchedulerDbContext context)
        {
            _context = context;
        }

        // GET: api/Events
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SchedulerEvent>>> GetSchedulerEvents([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            if (_context.Events == null)
            {
                return NotFound();
            }
            var events = await _context.Events
                .AsNoTracking()
                .Where(e => !((e.End <= start) || (e.Start >= end)))
                .ToListAsync();

            return events;
        }

        // GET: api/Events/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SchedulerEvent>> GetSchedulerEvent(int id)
        {
            if (_context.Events == null)
            {
                return NotFound();
            }
            var schedulerEvent = await _context.Events.FindAsync(id);

            if (schedulerEvent == null)
            {
                return NotFound();
            }

            return schedulerEvent;
        }

        // PUT: api/Events/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutSchedulerEvent(int id, SchedulerEvent schedulerEvent)
        {
            if (id != schedulerEvent.Id)
            {
                return BadRequest();
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            if (_context.Events == null)
            {
                return NotFound();
            }

            // Load existing entity and update fields to avoid tracking conflicts
            var existing = await _context.Events.FindAsync(id);
            if (existing == null)
            {
                return NotFound();
            }

            existing.Start = schedulerEvent.Start;
            existing.End = schedulerEvent.End;
            existing.Text = schedulerEvent.Text;
            existing.Color = schedulerEvent.Color;
            existing.ResourceId = schedulerEvent.ResourceId;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!SchedulerEventExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/Events
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<SchedulerEvent>> PostSchedulerEvent(SchedulerEvent schedulerEvent)
        {
            if (_context.Events == null)
            {
                return Problem("Entity set 'SchedulerDbContext.Events'  is null.");
            }
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            _context.Events.Add(schedulerEvent);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetSchedulerEvent", new { id = schedulerEvent.Id }, schedulerEvent);
        }

        // DELETE: api/Events/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSchedulerEvent(int id)
        {
            if (_context.Events == null)
            {
                return NotFound();
            }
            var schedulerEvent = await _context.Events.FindAsync(id);
            if (schedulerEvent == null)
            {
                return NotFound();
            }

            _context.Events.Remove(schedulerEvent);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool SchedulerEventExists(int id)
        {
            return (_context.Events?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
