using Microsoft.AspNetCore.Mvc.RazorPages;

namespace pto.track.Pages;

public class AbsencesSchedulerModel : BasePageModel
{
    private readonly ILogger<AbsencesSchedulerModel> _logger;

    public AbsencesSchedulerModel(IConfiguration configuration, ILogger<AbsencesSchedulerModel> logger)
        : base(configuration)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }
}
