using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Models.Interfaces;
using Services.Interfaces;

namespace AppRazor.Pages.Friends
{
    public class ViewFriendModel : PageModel
    {
        readonly ILogger<ViewFriendModel> _logger;
        readonly IFriendsService _service;

        public IFriend? Friend { get; set; }

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            var resp = await _service.ReadFriendAsync(id, false);
            Friend = resp.Item;

            if (Friend == null)
                return NotFound();

            Friend.Pets ??= new List<IPet>();
            Friend.Quotes ??= new List<IQuote>();
            Friend.Address ??= new DbModels.AddressDbM
            {
                StreetAddress = "",
                ZipCode = 0,
                City = "",
                Country = ""
            };

            return Page();
        }

        public ViewFriendModel(IFriendsService service, ILogger<ViewFriendModel> logger)
        {
            _logger = logger;
            _service = service;
        }
    }
}
