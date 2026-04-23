using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Models;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages.Admin
{
    public class ValidateLicenseModel : PageModel
    {
        private readonly LicenseService _licenseService;

        public ValidateLicenseModel(LicenseService licenseService)
        {
            _licenseService = licenseService;
        }

        [BindProperty]
        public string? LicenseKey { get; set; }

        public LicenseKeyData? ValidatedLicense { get; set;}
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public bool IsValidated { get; set; } = false;

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

            if (string.IsNullOrWhiteSpace(LicenseKey))
            {
                ErrorMessage = "Please enter a license key to validate.";
                return Page();
            }

            try
            {
                // Validate the license key
                if (_licenseService.TryValidateLicense(LicenseKey, out var licenseKeyData))
                {
                    ValidatedLicense = licenseKeyData;
                    IsValidated = true;

                    // Check if license is expired
                    bool isExpired = licenseKeyData!.LicenseType == LicenseType.METERED
                        && licenseKeyData.ExpiresAt.HasValue
                        && DateTime.UtcNow > licenseKeyData.ExpiresAt.Value;

                    if (isExpired)
                    {
                        ErrorMessage = "License has expired.";
                    }
                    else
                    {
                        SuccessMessage = licenseKeyData.LicenseType == LicenseType.PERMANENT
                            ? "✓ License is valid (PERMANENT)"
                            : "✓ License is valid (METERED)";
                    }
                }
                else
                {
                    ErrorMessage = "Invalid license key. The key format is incorrect or the signature is invalid.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error validating license: {ex.Message}";
            }

            return Page();
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Remove("AdminUser");
            return RedirectToPage("/Login");
        }
    }
}
