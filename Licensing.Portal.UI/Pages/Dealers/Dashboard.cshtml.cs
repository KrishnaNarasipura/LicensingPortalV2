using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages.Dealers
{
    public class DashboardModel : PageModel
    {
        private readonly DealerService _dealerService;

        public DashboardModel(DealerService dealerService)
        {
            _dealerService = dealerService;
        }

        public string? DealerName { get; set; }
        public string? DealerCode { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if dealer is logged in
            var dealerCode = HttpContext.Session.GetString("DealerCode");
            if (string.IsNullOrEmpty(dealerCode))
            {
                return RedirectToPage("/Login");
            }

            var dealer = await _dealerService.GetDealerAsync(dealerCode);
            if (dealer == null)
            {
                return RedirectToPage("/Login");
            }

            DealerName = dealer.DealerName;
            DealerCode = dealer.DealerCode;
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Remove("DealerCode");
            HttpContext.Session.Remove("DealerUser");
            return RedirectToPage("/Login");
        }
    }
}
