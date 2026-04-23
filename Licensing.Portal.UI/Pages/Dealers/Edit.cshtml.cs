using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Models;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages.Dealers
{
    public class EditModel : PageModel
    {
        private readonly DealerService _dealerService;

        public EditModel(DealerService dealerService)
        {
            _dealerService = dealerService;
        }

        [BindProperty]
        public Dealer Dealer { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(string dealerCode)
        {
            // Check if admin is logged in
            var adminUser = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(adminUser))
            {
                return RedirectToPage("/Login");
            }

            if (string.IsNullOrEmpty(dealerCode))
            {
                return RedirectToPage("/Dealers/Index");
            }

            var dealer = await _dealerService.GetDealerAsync(dealerCode);

            if (dealer == null)
            {
                return RedirectToPage("/Dealers/Index");
            }

            Dealer = dealer;
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Check if admin is logged in
            var adminUser = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(adminUser))
            {
                return RedirectToPage("/Login");
            }

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Please fill in all required fields correctly.";
                return Page();
            }

            // Validate Internal Dealer ID is exactly 2 digits
            if (string.IsNullOrWhiteSpace(Dealer.InternalDealerId) || Dealer.InternalDealerId.Length != 2 || !Dealer.InternalDealerId.All(char.IsDigit))
            {
                ErrorMessage = "Internal Dealer ID must be exactly 2 digits (e.g., 01, 12, 99).";
                return Page();
            }

            try
            {
                // Update the dealer using the data already loaded (system-managed fields preserved from OnGetAsync)
                var success = await _dealerService.UpdateDealerAsync(Dealer);
                
                if (!success)
                {
                    ErrorMessage = "Failed to update dealer.";
                    return Page();
                }

                TempData["SuccessMessage"] = $"Dealer '{Dealer.DealerName}' ({Dealer.DealerCode}) updated successfully!";
                return RedirectToPage("/Dealers/Index");
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred while saving the dealer: {ex.Message}";
                return Page();
            }
        }

        public IActionResult OnPostCancel()
        {
            return RedirectToPage("/Dealers/Index");
        }
    }
}
