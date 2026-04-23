using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages
{
    public class LoginModel : PageModel
    {
        private readonly DealerService _dealerService;
        private readonly IConfiguration _configuration;

        public LoginModel(DealerService dealerService, IConfiguration configuration)
        {
            _dealerService = dealerService;
            _configuration = configuration;
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
            // Get admin credentials from appsettings
            var adminUsername = _configuration["Admin:Username"];
            var adminPassword = _configuration["Admin:Password"];

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


