using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services;
using Services.Interfaces;

namespace AppRazor.Pages
{
    public class SeedModel : PageModel
    {
        //Just like for WebApi
        readonly IAdminService _admin_service;
        readonly ILogger<SeedModel> _logger;

        public int NrOfFriends { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "You must enter nr of items to seed")]
        public int NrOfItemsToSeed { get; set; } = 100;

        [BindProperty]
        public bool RemoveSeeds { get; set; } = true;

        public async Task<IActionResult> OnGet()
        {
            var info = await _admin_service.GuestInfoAsync();
            NrOfFriends = info.Item.Db.NrSeededFriends + info.Item.Db.NrUnseededFriends;
            return Page();
        }
        public async Task<IActionResult> OnPost()
        {
            if (ModelState.IsValid)
            {
                if (RemoveSeeds)
                {
                    await _admin_service.RemoveSeedAsync(true);
                    await _admin_service.RemoveSeedAsync(false);
                }
                await _admin_service.SeedAsync(NrOfItemsToSeed);

                return Redirect($"~/Friends/Overview");
            }
            return Page();
        }

        //Inject services just like in WebApi
        public SeedModel(IAdminService admin_service, ILogger<SeedModel> logger)
        {
            _admin_service = admin_service;
            _logger = logger;
        }
    }
}
