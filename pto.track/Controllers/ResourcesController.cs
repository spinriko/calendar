using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using pto.track.data;

namespace pto.track.Controllers
{
    [Produces("application/json")]
    [Route("api/resources")]
    public class ResourcesController : Controller
    {

        private readonly SchedulerDbContext _context;

        public ResourcesController(SchedulerDbContext context)
        {
            _context = context;
        }

        // GET: api/resources
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SchedulerResource>>> GetResources()
        {
            var resources = await _context.Resources.AsNoTracking().ToListAsync();
            return resources;
        }

    }
}
