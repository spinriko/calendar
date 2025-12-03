using Microsoft.AspNetCore.Mvc;
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

        public IActionResult OnGet()
        {
            // Check if user has Admin role
            if (!User.IsInRole("Admin"))
            {
                TempData["ErrorMessage"] = "Only administrators can view the Groups page.";
                return RedirectToPage("/Index");
            }

            return Page();
        }
    }
}
