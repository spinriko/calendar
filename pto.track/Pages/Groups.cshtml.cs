using Microsoft.AspNetCore.Mvc.RazorPages;

namespace pto.track.Pages
{
    public class GroupsModel : BasePageModel
    {
        private readonly ILogger<GroupsModel> _logger;

        public GroupsModel(IConfiguration configuration, ILogger<GroupsModel> logger)
            : base(configuration)
        {
            _logger = logger;
        }

        public void OnGet()
        {
        }
    }
}
