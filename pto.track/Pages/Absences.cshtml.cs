using Microsoft.AspNetCore.Mvc.RazorPages;

namespace pto.track.Pages;

public class AbsencesModel : BasePageModel
{
    private readonly ILogger<AbsencesModel> _logger;

    public AbsencesModel(IConfiguration configuration, ILogger<AbsencesModel> logger)
        : base(configuration)
    {
        _logger = logger;
    }

    public void OnGet()
    {

    }
}
