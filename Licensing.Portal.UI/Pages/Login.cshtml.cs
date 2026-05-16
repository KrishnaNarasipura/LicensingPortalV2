using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages
{
    public class LoginModel : PageModel
    {
        private readonly DealerService _dealerService;
        private readonly IConfiguration _configuration;
        private readonly AzureTableStorageService _azureTableStorageService;

        public LoginModel(DealerService dealerService, IConfiguration configuration, AzureTableStorageService azureTableStorageService)
        {
            _dealerService = dealerService;
            _configuration = configuration;
            _azureTableStorageService = azureTableStorageService;
        }

        [BindProperty]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        public string Password { get; set; } = string.Empty;

        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Get admin credentials from Azure Table Storage
            string? adminUsername = null;
            string? adminPassword = null;   

            try
            {
                // Get admin password from Azure Table Storage
                adminUsername = await _azureTableStorageService.GetAppSettingAsync("AdminUserName");
                adminPassword = await _azureTableStorageService.GetAppSettingAsync("AdminPassword");
                
                if (string.IsNullOrEmpty(adminPassword))
                {
                    ErrorMessage = "Unable to retrieve admin credentials. Please contact support.";
                    return Page();
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Unable to retrieve admin credentials. Please contact support.";
                return Page();
            }

            // Check for admin login
            if (Username == adminUsername && Password == adminPassword)
            {
                HttpContext.Session.SetString("AdminUser", Username);
                return RedirectToPage("/Dealers/Index");
            }

            try
            {
                // Check for dealer login using DealerCode as username
                var dealer = await _dealerService.GetDealerAsync(Username);
                
                if (dealer != null && _dealerService.VerifyPassword(Password, dealer.TemporaryPassword))
                {
                    // Temporary password is correct
                    HttpContext.Session.SetString("DealerCode", dealer.DealerCode);
                    
                    if (dealer.PasswordChangeRequired)
                    {
                        return RedirectToPage("/Dealers/ChangePassword");
                    }

                    HttpContext.Session.SetString("DealerUser", Username);
                    return RedirectToPage("/Dealers/Dashboard");
                }

                // Check if dealer exists with permanent password
                if (dealer != null && !string.IsNullOrEmpty(dealer.Password) && _dealerService.VerifyPassword(Password, dealer.Password))
                {
                    HttpContext.Session.SetString("DealerUser", Username);
                    HttpContext.Session.SetString("DealerCode", dealer.DealerCode);
                    return RedirectToPage("/Dealers/Dashboard");
                }
            }
            catch (Exception)
            {
                ErrorMessage = "Unable to process login. Please try again.";
                return Page();
            }

            ErrorMessage = "Invalid username or password";
            return Page();
        }
    }
}


