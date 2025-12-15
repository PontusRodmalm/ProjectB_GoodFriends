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

        //ModelBinding for the form
        [BindProperty]
        public string? SearchFilter { get; set; }

        //will execute on a Get request
        public async Task<IActionResult> OnGet()
        {
            //Read a QueryParameters
            if (int.TryParse(Request.Query["pagenr"], out int pagenr))
            {
                ThisPageNr = pagenr;
            }

            SearchFilter = Request.Query["search"];
            CountryFilter = Request.Query["CountryFilter"];

            //Use the Service - get ALL friends (don't filter by name in service)
            var resp = await _service.ReadFriendsAsync(UseSeeds, false, null, 0, 10000);
            var allFriends = resp.PageItems;

            // Populate the country list from all friends
            Countries = allFriends
                .Where(f => !string.IsNullOrEmpty(f.Address?.Country))
                .Select(f => f.Address.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Filter by country if selected
            List<IFriend> filteredFriends = allFriends;

            if (!string.IsNullOrEmpty(CountryFilter))
            {
                filteredFriends = filteredFriends.Where(f => f.Address?.Country == CountryFilter).ToList();
            }

            // Also filter by city if search filter matches city name
            if (!string.IsNullOrEmpty(SearchFilter))
            {
                filteredFriends = filteredFriends.Where(f =>
                    f.Address?.City?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            NrOfFriends = filteredFriends.Count;

            //Pagination 
            Friends = filteredFriends
                .Skip(ThisPageNr * PageSize)
                .Take(PageSize)
                .ToList();

            UpdatePagination(NrOfFriends);

            return Page();
        }

        private void UpdatePagination(int nrOfItems)
        {
            //Pagination
            NrOfPages = (int)Math.Ceiling((double)nrOfItems / PageSize);
            PrevPageNr = Math.Max(0, ThisPageNr - 1);
            NextPageNr = Math.Min(NrOfPages - 1, ThisPageNr + 1);
            NrVisiblePages = Math.Min(10, NrOfPages);
        }

        public async Task<IActionResult> OnPostSearch()
        {
            //Use the Service - get all friends
            var resp = await _service.ReadFriendsAsync(UseSeeds, false, null, 0, 10000);
            var allFriends = resp.PageItems;

            // Populate the country list
            Countries = allFriends
                .Where(f => !string.IsNullOrEmpty(f.Address?.Country))
                .Select(f => f.Address.Country)
                .Distinct()
                .OrderBy(c => c)
                .ToList();

            // Filter by country if selected
            List<IFriend> filteredFriends = allFriends;

            if (!string.IsNullOrEmpty(CountryFilter))
            {
                filteredFriends = filteredFriends.Where(f => f.Address?.Country == CountryFilter).ToList();
            }

            // Filter by city if search filter is provided
            if (!string.IsNullOrEmpty(SearchFilter))
            {
                filteredFriends = filteredFriends.Where(f =>
                    f.Address?.City?.Contains(SearchFilter, StringComparison.OrdinalIgnoreCase) == true).ToList();
            }

            NrOfFriends = filteredFriends.Count;

            //Pagination
            Friends = filteredFriends
                .Skip(ThisPageNr * PageSize)
                .Take(PageSize)
                .ToList();

            UpdatePagination(NrOfFriends);

            //Page is rendered as the postback is part of the form tag
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteFriend(Guid friendId)
        {
            await _service.DeleteFriendAsync(friendId);

            //Use the Service
            var resp = await _service.ReadFriendsAsync(UseSeeds, false, SearchFilter, ThisPageNr, PageSize);
            Friends = resp.PageItems;
            NrOfFriends = resp.DbItemsCount;

            //Pagination
            UpdatePagination(resp.DbItemsCount);

            return Page();
        }

        //Inject services just like in WebApi
        public ListOfFriendsModel(IFriendsService service, ILogger<ListOfFriendsModel> logger)
        {
            _service = service;
            _logger = logger;
        }
    }
}
