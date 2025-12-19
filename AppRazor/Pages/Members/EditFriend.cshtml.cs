using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Models.DTO;
using Models.Interfaces;
using WebAppStudies.SeidoHelpers;
using System.Runtime.CompilerServices;
using Models;


namespace AppRazor.Pages
{
    public class EditFriendModel : PageModel
    {
        private readonly IFriendsService _service;
        private readonly ILogger<EditFriendModel> _logger;
        private readonly IAddressesService _addressesService;

        [BindProperty]
        public FriendIM? FriendInput { get; set; }

        [BindProperty]
        public string? PageHeader { get; set; }

        public ModelValidationResult ValidationResult { get; set; } = new ModelValidationResult(false, Enumerable.Empty<string>(), Enumerable.Empty<KeyValuePair<string, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>>());

        public EditFriendModel(IFriendsService service, ILogger<EditFriendModel> logger, IAddressesService addressesService)
        {
            _service = service;
            _logger = logger;
            _addressesService = addressesService;
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
                // No id provided, redirect to overview
                return RedirectToPage("/Friends/Overview");
            }
            return Page();
        }


        public async Task<IActionResult> OnPostSave()
        {
            if (!ModelState.IsValid || FriendInput == null || FriendInput.Address == null)
            {
                ValidationResult = new ModelValidationResult(
                    true,
                    new List<string> { "Please correct the errors." },
                    new List<KeyValuePair<string, Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateEntry>>()
                );
                return Page();
            }

            // Save or update address first
            var addressDto = FriendInput.Address.ToCUdto();
            ResponseItemDto<IAddress> addressResp;
            if (string.IsNullOrEmpty(FriendInput.Address.AddressId) || FriendInput.Address.AddressId == Guid.Empty.ToString())
            {
                addressResp = await _addressesService.CreateAddressAsync(addressDto);
            }
            else
            {
                addressDto.AddressId = Guid.Parse(FriendInput.Address.AddressId);
                addressResp = await _addressesService.UpdateAddressAsync(addressDto);
            }

            // Set AddressId on FriendCuDto
            var friendDto = FriendInput.ToCUdto();
            friendDto.AddressId = addressResp.Item?.AddressId ?? Guid.Empty;

            // Only update existing friends
            await _service.UpdateFriendAsync(friendDto);

            return RedirectToPage("/Friends/Overview");
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

            [DataType(DataType.Date)]
            public DateTime? Birthday { get; set; } = null;

            public FriendIM() { }

            public FriendIM(IFriend model)
            {
                StatusIM = StatusIM.Unchanged;
                FriendId = model.FriendId;
                FirstName = model.FirstName ?? string.Empty;
                LastName = model.LastName ?? string.Empty;
                Address = new AddressIM(model.Address);
                Birthday = model.Birthday;
            }

            public FriendCuDto ToCUdto() => new FriendCuDto
            {
                FriendId = this.FriendId,
                FirstName = this.FirstName,
                LastName = this.LastName,
                Birthday = this.Birthday,
                AddressId = null
            };
        }

        public class AddressIM
        {
            public string AddressId { get; set; } = string.Empty;

            [Required(ErrorMessage = "Street is required")]
            public string StreetAddress { get; set; } = string.Empty;

            [Required(ErrorMessage = "City is required")]
            public string City { get; set; } = string.Empty;

            [Required(ErrorMessage = "Zip code is required")]
            public string ZipCode { get; set; } = string.Empty;

            [Required(ErrorMessage = "Country is required")]
            public string Country { get; set; } = string.Empty;

            public AddressIM() { }
            public AddressIM(IAddress model)
            {
                if (model == null)
                {
                    StreetAddress = string.Empty;
                    City = string.Empty;
                    ZipCode = string.Empty;
                    Country = string.Empty;
                    return;
                }
                AddressId = model.AddressId.ToString();
                StreetAddress = model.StreetAddress;
                City = model.City;
                ZipCode = model.ZipCode.ToString();
                Country = model.Country ?? string.Empty;
            }

            public AddressCuDto ToCUdto() => new AddressCuDto
            {
                StreetAddress = this.StreetAddress,
                City = this.City,
                ZipCode = int.TryParse(this.ZipCode, out int zip) ? zip : 0,
                Country = this.Country
            };
        }
    }
}
