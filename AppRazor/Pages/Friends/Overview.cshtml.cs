using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Models.Interfaces;
using Services;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class OverviewModel : PageModel
    {
        //Just like for WebApi
        readonly IFriendsService _service = null;
        readonly ILogger<OverviewModel> _logger = null;

        public List<IFriend> Friends { get; set; } = new List<IFriend>();

        public async Task<IActionResult> OnGet()
        {
            var result = await _service.ReadFriendAsync()

            return Page();
        }

        //Inject services just like in WebApi
        public OverviewModel(IFriendsService service, ILogger<OverviewModel> logger)
        {
            _service = service;
            _logger = logger;
        }
    }
}
