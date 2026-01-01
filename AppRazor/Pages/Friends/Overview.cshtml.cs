using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

using Models.Interfaces;
using Services;
using Services.Interfaces;

namespace AppRazor.Pages
{

    public class ListOfFriendsModel : PageModel
    {
        readonly IFriendsService _service;
        readonly ILogger<ListOfFriendsModel> _logger;

        [BindProperty]
        public bool UseSeeds { get; set; } = true;

        public List<IFriend> Friends { get; set; } = new();
        public List<string> Countries { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? CountryFilter { get; set; }

        public int NrOfFriends { get; set; }

        //Pagination
        public int NrOfPages { get; set; }
        public int PageSize { get; } = 10;
        public int ThisPageNr { get; set; } = 0;
        public int PrevPageNr { get; set; } = 0;
        public int NextPageNr { get; set; } = 0;
        public int NrVisiblePages { get; set; } = 0;

        [BindProperty]
        public string? SearchFilter { get; set; }

        public async Task<IActionResult> OnGet()
        {
            if (int.TryParse(Request.Query["pagenr"], out int pagenr))
            {
                ThisPageNr = pagenr;
            }

            SearchFilter = Request.Query["search"];
            CountryFilter = Request.Query["CountryFilter"];

            var resp = await _service.ReadFriendsAsync(UseSeeds, false, null, 0, 10000);
            var allFriends = resp.PageItems;


            Countries = allFriends
                .Where(f => !string.IsNullOrEmpty(f.Address?.Country))
                .Select(f => f.Address.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            List<IFriend> filteredFriends = allFriends;

            if (!string.IsNullOrEmpty(CountryFilter))
            {
                filteredFriends = filteredFriends.Where(f => f.Address?.Country == CountryFilter).ToList();
            }


            if (!string.IsNullOrEmpty(SearchFilter))
            {
                filteredFriends = filteredFriends.Where(f =>
                    f.FirstName?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true ||
                    f.LastName?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true ||
                    f.Address?.City?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            NrOfFriends = filteredFriends.Count;

            Friends = filteredFriends
                .Skip(ThisPageNr * PageSize)
                .Take(PageSize)
                .ToList();

            UpdatePagination(NrOfFriends);

            return Page();
        }

        private void UpdatePagination(int nrOfItems)
        {
            NrOfPages = (int)Math.Ceiling((double)nrOfItems / PageSize);
            PrevPageNr = Math.Max(0, ThisPageNr - 1);
            NextPageNr = Math.Min(NrOfPages - 1, ThisPageNr + 1);
            NrVisiblePages = Math.Min(10, NrOfPages);
        }

        public async Task<IActionResult> OnPostSearch()
        {
            var resp = await _service.ReadFriendsAsync(UseSeeds, false, null, 0, 10000);
            var allFriends = resp.PageItems;


            Countries = allFriends
                .Where(f => !string.IsNullOrEmpty(f.Address?.Country))
                .Select(f => f.Address.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToList();


            List<IFriend> filteredFriends = allFriends;

            if (!string.IsNullOrEmpty(CountryFilter))
            {
                filteredFriends = filteredFriends.Where(f => f.Address?.Country == CountryFilter).ToList();
            }

            if (!string.IsNullOrEmpty(SearchFilter))
            {
                filteredFriends = filteredFriends.Where(f =>
                    f.FirstName?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true ||
                    f.LastName?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true ||
                    f.Address?.City?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            NrOfFriends = filteredFriends.Count;

            Friends = filteredFriends
                .Skip(ThisPageNr * PageSize)
                .Take(PageSize)
                .ToList();

            UpdatePagination(NrOfFriends);

            return Page();
        }

        public async Task<IActionResult> OnPostDeleteFriend(Guid friendId)
        {
            await _service.DeleteFriendAsync(friendId);

            var resp = await _service.ReadFriendsAsync(UseSeeds, false, SearchFilter, ThisPageNr, PageSize);
            Friends = resp.PageItems;
            NrOfFriends = resp.DbItemsCount;

            UpdatePagination(resp.DbItemsCount);

            return Page();
        }

        public ListOfFriendsModel(IFriendsService service, ILogger<ListOfFriendsModel> logger)
        {
            _service = service;
            _logger = logger;
        }
    }
}
