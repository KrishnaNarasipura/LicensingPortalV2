using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Models;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages.Dealers
{
    public class IndexModel : PageModel
    {
        private readonly DealerService _dealerService;

        public IndexModel(DealerService dealerService)
        {
            _dealerService = dealerService;
        }

        public List<Dealer> Dealers { get; set; } = new();
        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // Check if admin is logged in
            var adminUser = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(adminUser))
            {
                return RedirectToPage("/Login");
            }

            Dealers = await _dealerService.GetAllDealersAsync();
            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Remove("AdminUser");
            return RedirectToPage("/Login");
        }

        public async Task<IActionResult> OnPostResetPasswordAsync(string dealerCode)
        {
            // Check if admin is logged in
            var adminUser = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(adminUser))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var (success, newPassword) = await _dealerService.ResetDealerPasswordAsync(dealerCode);
                
                if (!success || newPassword == null)
                {
                    ErrorMessage = "Dealer not found.";
                    Dealers = await _dealerService.GetAllDealersAsync();
                    return Page();
                }

                var dealer = await _dealerService.GetDealerAsync(dealerCode);

                // Store in TempData to display in modal
                TempData["NewPassword"] = newPassword;
                TempData["DealerCode"] = dealer!.DealerCode;
                TempData["DealerName"] = dealer.DealerName;
                TempData["ShowResetModal"] = true;

                SuccessMessage = "Password reset successful!";
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error resetting password: {ex.Message}";
            }

            Dealers = await _dealerService.GetAllDealersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostDeleteAsync(string dealerCode)
        {
            // Check if admin is logged in
            var adminUser = HttpContext.Session.GetString("AdminUser");
            if (string.IsNullOrEmpty(adminUser))
            {
                return RedirectToPage("/Login");
            }

            try
            {
                var dealer = await _dealerService.GetDealerAsync(dealerCode);
                if (dealer == null)
                {
                    TempData["ErrorMessage"] = "Dealer not found.";
                    return RedirectToPage();
                }

                var success = await _dealerService.DeleteDealerAsync(dealerCode);
                
                if (!success)
                {
                    TempData["ErrorMessage"] = "Failed to delete dealer.";
                    return RedirectToPage();
                }

                TempData["SuccessMessage"] = $"Dealer '{dealer.DealerName}' ({dealer.DealerCode}) deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting dealer: {ex.Message}";
            }

            return RedirectToPage();
        }
    }
}
