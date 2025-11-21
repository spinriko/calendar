using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace pto.track.Pages
{
    public class SchedulingModel : PageModel
    {
        private readonly ILogger<SchedulingModel> _logger;

        public SchedulingModel(ILogger<SchedulingModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}