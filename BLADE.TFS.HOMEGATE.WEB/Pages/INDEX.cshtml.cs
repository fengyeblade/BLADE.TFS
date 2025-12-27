using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace BLADE.SERVICEWEB.RAZORBODY9.Pages
{
    public class INDEXModel : PageModel
    {
        private readonly ILogger<INDEXModel> _logger;

        public INDEXModel(ILogger<INDEXModel> logger)
        {
            _logger = logger;
        }

        public void OnGet()
        {

        }
    }
}
