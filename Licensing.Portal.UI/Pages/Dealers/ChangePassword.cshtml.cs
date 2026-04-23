using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages.Dealers
{
    public class ChangePasswordModel : PageModel
    {
        private readonly DealerService _dealerService;

        public ChangePasswordModel(DealerService dealerService)
        {
            _dealerService = dealerService;
        }

        [BindProperty]
        public string NewPassword { get; set; } = string.Empty;

        [BindProperty]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
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

        public async Task<IActionResult> OnPostAsync()
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

            // Validate passwords match
            if (NewPassword != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match. Please try again.";
                return Page();
            }

            // Validate password requirements
            if (!_dealerService.ValidatePassword(NewPassword))
            {
                ErrorMessage = "Password must be at least 8 characters and include one uppercase letter, one number, and one special character (!@#$%^&*).";
                return Page();
            }

            try
            {
                // Update dealer password
                var success = await _dealerService.UpdateDealerPasswordAsync(dealerCode, NewPassword, requireChange: false);
                
                if (!success)
                {
                    ErrorMessage = "Failed to update password. Please try again.";
                    return Page();
                }

                // Update session
                HttpContext.Session.SetString("DealerUser", dealer.DealerCode);

                SuccessMessage = "Password changed successfully! Redirecting to dashboard...";
                
                // Redirect to dashboard after brief delay (via client-side redirect in view)
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"An error occurred: {ex.Message}";
                return Page();
            }
        }
    }
}
