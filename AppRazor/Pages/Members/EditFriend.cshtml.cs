using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Models.DTO;
using Models.Interfaces;
using WebAppStudies.SeidoHelpers;


namespace AppRazor.Pages
{
    public class EditFriendModel : PageModel
    {
        private readonly IFriendsService _service;
        private readonly ILogger<EditFriendModel> _logger;

        [BindProperty]
        public FriendIM? FriendInput { get; set; }

        [BindProperty]
        public string? PageHeader { get; set; }

        public ModelValidationResult ValidationResult { get; set; } = new ModelValidationResult(false, Enumerable.Empty<string>(), Enumerable.Empty<KeyValuePair<string, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>>());

        public EditFriendModel(IFriendsService service, ILogger<EditFriendModel> logger)
        {
            _service = service;
            _logger = logger;
        }

        public async Task<IActionResult> OnGet()
        {
            if (Guid.TryParse(Request.Query["id"], out Guid friendId))
            {
                var resp = await _service.ReadFriendAsync(friendId, false);
                if (resp == null || resp.Item == null)
                    return NotFound();

                FriendInput = new FriendIM(resp.Item);
                PageHeader = "Edit Friend";
            }
            else
            {
                FriendInput = new FriendIM();
                FriendInput.StatusIM = StatusIM.Inserted;
                PageHeader = "Create New Friend";
            }
            return Page();
        }

        public async Task<IActionResult> OnPostSave()
        {
            if (!ModelState.IsValid)
            {
                ValidationResult = new ModelValidationResult(
                    true,
                    new List<string> { "Please correct the errors." },
                    new List<KeyValuePair<string, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>>()
                );
                return Page();
            }

            if (FriendInput.StatusIM == StatusIM.Inserted)
            {
                await _service.CreateFriendAsync(FriendInput.ToCUdto());
            }
            else
            {
                await _service.UpdateFriendAsync(FriendInput.ToCUdto());
            }

            return RedirectToPage("/Members/ListOfFriends");
        }

        public enum StatusIM { Unknown, Unchanged, Inserted, Modified, Deleted }

        public class FriendIM
        {
            public StatusIM StatusIM { get; set; }
            public Guid FriendId { get; set; }

            [Required(ErrorMessage = "You must provide a first name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "You must provide a last name")]
            public string LastName { get; set; } = string.Empty;

            public AddressIM Address { get; set; } = new AddressIM();

            public FriendIM() { }

            public FriendIM(IFriend model)
            {
                StatusIM = StatusIM.Unchanged;
                FriendId = model.FriendId;
                FirstName = model.FirstName ?? string.Empty;
                LastName = model.LastName ?? string.Empty;
                Address = new AddressIM(model.Address);
            }

            public FriendCuDto ToCUdto() => new FriendCuDto
            {
                FriendId = this.FriendId,
                FirstName = this.FirstName,
                LastName = this.LastName,
                AddressId = null
            };
        }

        public class AddressIM
        {
            [Required(ErrorMessage = "Street is required")]
            public string StreetAddress { get; set; }

            [Required(ErrorMessage = "City is required")]
            public string City { get; set; }

            [Required(ErrorMessage = "Zip code is required")]
            public string ZipCode { get; set; }

            public AddressIM() { }
            public AddressIM(IAddress model)
            {
                if (model == null)
                {
                    StreetAddress = string.Empty;
                    City = string.Empty;
                    ZipCode = string.Empty;
                    return;
                }
                StreetAddress = model.StreetAddress;
                City = model.City;
                ZipCode = model.ZipCode.ToString();
            }

            public AddressCuDto ToCUdto() => new AddressCuDto
            {
                StreetAddress = this.StreetAddress,
                City = this.City,
                ZipCode = int.TryParse(this.ZipCode, out int zip) ? zip : 0
            };
        }
    }
}
