using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace pto.track.Pages
{
    public class SchedulingModel : BasePageModel
    {
        private readonly ILogger<SchedulingModel> _logger;

        public SchedulingModel(IConfiguration configuration, ILogger<SchedulingModel> logger)
            : base(configuration)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}