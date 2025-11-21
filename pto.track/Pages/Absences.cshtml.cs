using Microsoft.AspNetCore.Mvc.RazorPages;

namespace pto.track.Pages;

public class AbsencesModel : PageModel
{
    private readonly ILogger<AbsencesModel> _logger;

    public AbsencesModel(ILogger<AbsencesModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }
}
