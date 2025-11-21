using Microsoft.AspNetCore.Mvc.RazorPages;

namespace pto.track.Pages;

public class AbsencesSchedulerModel : PageModel
{
    private readonly ILogger<AbsencesSchedulerModel> _logger;

    public AbsencesSchedulerModel(ILogger<AbsencesSchedulerModel> logger)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }
}
