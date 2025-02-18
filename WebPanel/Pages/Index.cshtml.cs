using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebPanel.Pages
{
    public class IndexModel : PageModel
    {
        private readonly Bot _bot;

        public IndexModel(Bot bot)
        {
            _bot = bot;
        }

        public void OnGet()
        {
        }
    }
}
