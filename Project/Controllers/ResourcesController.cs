using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Project.Models;

namespace Project.Controllers
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
            return await _context.Resources.ToListAsync();
        }

    }
}
