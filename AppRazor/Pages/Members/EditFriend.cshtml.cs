using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Models.DTO;
using Models.Interfaces;
using WebAppStudies.SeidoHelpers;
using Models;


namespace AppRazor.Pages
{
    public class EditFriendModel : PageModel
    {
        readonly IFriendsService _friendsService;
        readonly IPetsService _petsService;
        readonly IQuotesService _quotesService;
        readonly IAddressesService _addressesService;
        readonly ILogger<EditFriendModel> _logger;

        [BindProperty]
        public FriendIM FriendInput { get; set; } = null!;

        [BindProperty]
        public string PageHeader { get; set; } = null!;

        public List<SelectListItem> AnimalKindItems { set; get; } = new List<SelectListItem>().PopulateSelectList<AnimalKind>();

        public ModelValidationResult ValidationResult { get; set; } = new ModelValidationResult(false, Enumerable.Empty<string>(), Enumerable.Empty<KeyValuePair<string, ModelStateEntry>>());

        public async Task<IActionResult> OnGet()
        {
            if (Guid.TryParse(Request.Query["id"], out Guid friendId))
            {
                var friend = await _friendsService.ReadFriendAsync(friendId, false);

                if (friend == null || friend.Item == null)
                    return NotFound();

                FriendInput = new FriendIM(friend.Item);
                PageHeader = "Edit Friend";
            }
            else
            {
                return RedirectToPage("/Friends/Overview");
            }

            return Page();
        }

        public IActionResult OnPostDeletePet(Guid petId)
        {
            FriendInput.Pets.First(p => p.PetId == petId).StatusIM = StatusIM.Deleted;
            return Page();
        }

        public IActionResult OnPostEditPet(Guid petId)
        {
            int petIndex = FriendInput.Pets.FindIndex(p => p.PetId == petId);
            string[] keys = { $"FriendInput.Pets[{petIndex}].editName",
                            $"FriendInput.Pets[{petIndex}].editKind"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            var petToUpdate = FriendInput.Pets.First(p => p.PetId == petId);
            if (petToUpdate.StatusIM != StatusIM.Inserted)
            {
                petToUpdate.StatusIM = StatusIM.Modified;
            }

            petToUpdate.Name = petToUpdate.editName;
            petToUpdate.Kind = petToUpdate.editKind;

            return Page();
        }

        public IActionResult OnPostDeleteQuote(Guid quoteId)
        {
            FriendInput.Quotes.First(q => q.QuoteId == quoteId).StatusIM = StatusIM.Deleted;
            return Page();
        }

        public IActionResult OnPostEditQuote(Guid quoteId)
        {
            int quoteIndex = FriendInput.Quotes.FindIndex(q => q.QuoteId == quoteId);
            string[] keys = { $"FriendInput.Quotes[{quoteIndex}].editQuoteText",
                            $"FriendInput.Quotes[{quoteIndex}].editAuthor"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            var quoteToUpdate = FriendInput.Quotes.First(q => q.QuoteId == quoteId);
            if (quoteToUpdate.StatusIM != StatusIM.Inserted)
            {
                quoteToUpdate.StatusIM = StatusIM.Modified;
            }

            quoteToUpdate.QuoteText = quoteToUpdate.editQuoteText;
            quoteToUpdate.Author = quoteToUpdate.editAuthor;

            return Page();
        }

        public async Task<IActionResult> OnPostUndo()
        {
            var friend = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);

            FriendInput = new FriendIM(friend.Item);
            return Page();
        }

        public async Task<IActionResult> OnPostSave()
        {
            string[] keys = { "FriendInput.FirstName",
                              "FriendInput.LastName",
                              "FriendInput.Address.StreetAddress",
                              "FriendInput.Address.City",
                              "FriendInput.Address.ZipCode",
                              "FriendInput.Address.Country"};

            if (!ModelState.IsValidPartially(out ModelValidationResult validationResult, keys))
            {
                ValidationResult = validationResult;
                return Page();
            }

            await SaveAddress();

            var friend = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);

            await SavePets(friend.Item);

            await SaveQuotes(friend.Item);

            friend = await _friendsService.ReadFriendAsync(FriendInput.FriendId, false);

            var updatedFriend = FriendInput.UpdateModel(friend.Item);
            updatedFriend.Address = null;
            var friendDto = new FriendCuDto(updatedFriend);
            friendDto.AddressId = FriendInput.Address.AddressId;
            await _friendsService.UpdateFriendAsync(friendDto);

            return Redirect($"~/Friends/ViewFriend?id={FriendInput.FriendId}");
        }

        private async Task SaveAddress()
        {
            if (FriendInput.Address.AddressId.HasValue && FriendInput.Address.AddressId != Guid.Empty)
            {
                await _addressesService.UpdateAddressAsync(FriendInput.Address.ToCUdto());
            }
            else
            {
                var addressResp = await _addressesService.CreateAddressAsync(FriendInput.Address.ToCUdto());
                FriendInput.Address.AddressId = addressResp.Item?.AddressId;
            }
        }

        private async Task SavePets(IFriend currentFriend)
        {
            var petsToDelete = FriendInput.Pets.Where(p => p.StatusIM == StatusIM.Deleted).ToList();
            foreach (var pet in petsToDelete)
            {
                if (currentFriend.Pets.Any(p => p.PetId == pet.PetId))
                {
                    await _petsService.DeletePetAsync(pet.PetId);
                }
            }

            var petsToUpdate = FriendInput.Pets.Where(p => p.StatusIM == StatusIM.Modified).ToList();
            foreach (var petInput in petsToUpdate)
            {
                var existingPet = currentFriend.Pets.FirstOrDefault(p => p.PetId == petInput.PetId);
                if (existingPet != null)
                {
                    existingPet = petInput.UpdateModel(existingPet);
                    await _petsService.UpdatePetAsync(new PetCuDto(existingPet));
                }
            }
        }

        private async Task SaveQuotes(IFriend currentFriend)
        {
            var quotesToDelete = FriendInput.Quotes.Where(q => q.StatusIM == StatusIM.Deleted).ToList();
            foreach (var quote in quotesToDelete)
            {
                if (currentFriend.Quotes.Any(q => q.QuoteId == quote.QuoteId))
                {
                    await _quotesService.DeleteQuoteAsync(quote.QuoteId);
                }
            }

            var quotesToUpdate = FriendInput.Quotes.Where(q => q.StatusIM == StatusIM.Modified).ToList();
            foreach (var quoteInput in quotesToUpdate)
            {
                var existingQuote = currentFriend.Quotes.FirstOrDefault(q => q.QuoteId == quoteInput.QuoteId);
                if (existingQuote != null)
                {
                    existingQuote = quoteInput.UpdateModel(existingQuote);
                    await _quotesService.UpdateQuoteAsync(new QuoteCuDto(existingQuote));
                }
            }
        }

        public EditFriendModel(IFriendsService friendsService, IPetsService petsService,
                              IQuotesService quotesService, IAddressesService addressesService,
                              ILogger<EditFriendModel> logger)
        {
            _friendsService = friendsService;
            _petsService = petsService;
            _quotesService = quotesService;
            _addressesService = addressesService;
            _logger = logger;
        }

        public enum StatusIM { Unknown, Unchanged, Inserted, Modified, Deleted }

        public class PetIM
        {
            public StatusIM StatusIM { get; set; }

            public Guid PetId { get; set; }

            [Required(ErrorMessage = "You must provide a pet name")]
            public string Name { get; set; } = string.Empty;

            [Required(ErrorMessage = "You must select an animal kind")]
            public AnimalKind Kind { get; set; }

            [Required(ErrorMessage = "You must provide a pet name")]
            public string editName { get; set; } = string.Empty;

            [Required(ErrorMessage = "You must select an animal kind")]
            public AnimalKind editKind { get; set; }

            public PetIM() { }

            public PetIM(IPet model)
            {
                StatusIM = StatusIM.Unchanged;
                PetId = model.PetId;
                Name = editName = model.Name;
                Kind = editKind = model.Kind;
            }

            public IPet UpdateModel(IPet model)
            {
                model.PetId = this.PetId;
                model.Name = this.Name;
                model.Kind = this.Kind;
                return model;
            }
        }

        public class QuoteIM
        {
            public StatusIM StatusIM { get; set; }

            public Guid QuoteId { get; set; }

            [Required(ErrorMessage = "You must enter a quote text")]
            public string QuoteText { get; set; } = string.Empty;

            [Required(ErrorMessage = "You must enter an author")]
            public string Author { get; set; } = string.Empty;

            [Required(ErrorMessage = "You must enter a quote text")]
            public string editQuoteText { get; set; } = string.Empty;

            [Required(ErrorMessage = "You must enter an author")]
            public string editAuthor { get; set; } = string.Empty;

            public QuoteIM() { }

            public QuoteIM(IQuote model)
            {
                StatusIM = StatusIM.Unchanged;
                QuoteId = model.QuoteId;
                QuoteText = editQuoteText = model.QuoteText;
                Author = editAuthor = model.Author;
            }

            public IQuote UpdateModel(IQuote model)
            {
                model.QuoteId = this.QuoteId;
                model.QuoteText = this.QuoteText;
                model.Author = this.Author;
                return model;
            }
        }

        public class AddressIM
        {
            public Guid? AddressId { get; set; }

            [Required(ErrorMessage = "Street is required")]
            public string StreetAddress { get; set; } = string.Empty;

            [Required(ErrorMessage = "City is required")]
            public string City { get; set; } = string.Empty;

            [Required(ErrorMessage = "Zip code is required")]
            public int ZipCode { get; set; }

            [Required(ErrorMessage = "Country is required")]
            public string Country { get; set; } = string.Empty;

            public AddressIM() { }
            public AddressIM(IAddress model)
            {
                if (model == null)
                {
                    StreetAddress = string.Empty;
                    City = string.Empty;
                    ZipCode = 0;
                    Country = string.Empty;
                    return;
                }
                AddressId = model.AddressId;
                StreetAddress = model.StreetAddress;
                City = model.City;
                ZipCode = model.ZipCode;
                Country = model.Country ?? string.Empty;
            }

            public AddressCuDto ToCUdto() => new AddressCuDto
            {
                AddressId = this.AddressId,
                StreetAddress = this.StreetAddress,
                City = this.City,
                ZipCode = this.ZipCode,
                Country = this.Country
            };
        }

        public class FriendIM
        {
            public StatusIM StatusIM { get; set; }

            public Guid FriendId { get; set; }

            [Required(ErrorMessage = "You must provide a first name")]
            public string FirstName { get; set; } = string.Empty;

            [Required(ErrorMessage = "You must provide a last name")]
            public string LastName { get; set; } = string.Empty;

            [DataType(DataType.Date)]
            public DateTime? Birthday { get; set; } = null;

            public AddressIM Address { get; set; } = new AddressIM();

            public List<PetIM> Pets { get; set; } = new List<PetIM>();
            public List<QuoteIM> Quotes { get; set; } = new List<QuoteIM>();

            public FriendIM() { }
            public FriendIM(IFriend model)
            {
                StatusIM = StatusIM.Unchanged;
                FriendId = model.FriendId;
                FirstName = model.FirstName ?? string.Empty;
                LastName = model.LastName ?? string.Empty;
                Birthday = model.Birthday;
                Address = new AddressIM(model.Address);

                Pets = model.Pets?.Select(m => new PetIM(m)).ToList() ?? new List<PetIM>();
                Quotes = model.Quotes?.Select(m => new QuoteIM(m)).ToList() ?? new List<QuoteIM>();
            }

            public IFriend UpdateModel(IFriend model)
            {
                model.FirstName = this.FirstName;
                model.LastName = this.LastName;
                model.Birthday = this.Birthday;
                return model;
            }
        }
    }
}