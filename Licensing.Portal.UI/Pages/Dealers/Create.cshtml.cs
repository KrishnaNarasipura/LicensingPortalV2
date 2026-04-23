using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Models;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages.Dealers
{
    public class CreateModel : PageModel
    {
        private readonly DealerService _dealerService;

        public CreateModel(DealerService dealerService)
        {
            _dealerService = dealerService;
        }

        [BindProperty]
        public Dealer Dealer { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string? GeneratedPassword { get; set; }

        public IActionResult OnGet()
        {
            // Check if admin is logged in
            var adminUser = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(adminUser))
            {
                return RedirectToPage("/Login");
            }

            return Page();
        }

        public IActionResult OnPost()
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
                // Use DealerService to create dealer with generated code and password
                var (success, message, createdDealer, tempPassword) = _dealerService.CreateDealer(Dealer);

                if (!success)
                {
                    ErrorMessage = message;
                    return Page();
                }

                // Store in TempData to display in modal
                TempData["DealerCode"] = createdDealer!.DealerCode;
                TempData["DealerName"] = createdDealer.DealerName;
                TempData["GeneratedPassword"] = tempPassword;
                TempData["ShowPasswordModal"] = true;

                return RedirectToPage();
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

