using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Models;
using Licensing.Portal.Services;

namespace Licensing.Portal.Pages.Dealers
{
    public class GenerateLicenseModel : PageModel
    {
        private readonly LicenseService _licenseService;
        private readonly DealerService _dealerService;

        public GenerateLicenseModel(LicenseService licenseService, DealerService dealerService)
        {
            _licenseService = licenseService;
            _dealerService = dealerService;
        }

        [BindProperty]
        public License License { get; set; } = new();

        public string? DealerName { get; set; }
        public string? ErrorMessage { get; set; }
        public string? SuccessMessage { get; set; }
        public LicenseResponse? GeneratedLicense { get; set; }

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

            // Pre-populate form with dealer information
            License.DealerCode = dealer.DealerCode;
            License.InternalDealerId = dealer.InternalDealerId;
            License.IssueDate = DateTime.Now;
            DealerName = dealer.DealerName;

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

            // Validate serial number
            if (string.IsNullOrEmpty(License.SerialNumber))
            {
                ErrorMessage = "Please enter a serial number.";
                License.DealerCode = dealer.DealerCode;
                License.InternalDealerId = dealer.InternalDealerId;
                License.IssueDate = DateTime.Now;
                return Page();
            }

            // Validate serial number length (maximum 8 characters)
            if (License.SerialNumber.Length > 8)
            {
                ErrorMessage = "Serial number cannot exceed 8 characters.";
                License.DealerCode = dealer.DealerCode;
                License.InternalDealerId = dealer.InternalDealerId;
                License.IssueDate = DateTime.Now;
                return Page();
            }

            // Validate first 2 digits of serial number match first 2 digits of InternalDealerId
            string serialNumberPrefix = License.SerialNumber.Substring(0, Math.Min(2, License.SerialNumber.Length));

            if (serialNumberPrefix != dealer.InternalDealerId)
            {
                ErrorMessage = $"You are not authorized to generate license for this device";
                License.DealerCode = dealer.DealerCode;
                License.InternalDealerId = dealer.InternalDealerId;
                License.IssueDate = DateTime.Now;
                return Page();
            }

            // Validate required fields
            if (string.IsNullOrEmpty(License.LicenseType))
            {
                ErrorMessage = "Please select a license type.";
                License.DealerCode = dealer.DealerCode;
                License.InternalDealerId = dealer.InternalDealerId;
                License.IssueDate = DateTime.Now;
                return Page();
            }

            // Validate expiry days for Metered license
            DateTime? expiryDate = null;
            if (License.LicenseType == "Metered")
            {
                if (!License.ExpiryDays.HasValue || License.ExpiryDays <= 0)
                {
                    ErrorMessage = "Please enter a valid number of days for Metered license.";
                    License.DealerCode = dealer.DealerCode;
                    License.InternalDealerId = dealer.InternalDealerId;
                    License.IssueDate = DateTime.Now;
                    return Page();
                }

                // Calculate expiry date for Metered license
                expiryDate = License.IssueDate.AddDays(License.ExpiryDays.Value);
            }

            try
            {
                // Convert LicenseType string to enum
                LicenseType licenseType;
                if (License.LicenseType == "Metered")
                {
                    licenseType = LicenseType.METERED;
                }
                else if (License.LicenseType == "Permanent")
                {
                    licenseType = LicenseType.PERMANENT;
                }
                else
                {
                    ErrorMessage = "Invalid license type.";
                    License.DealerCode = dealer.DealerCode;
                    License.InternalDealerId = dealer.InternalDealerId;
                    License.IssueDate = DateTime.Now;
                    return Page();
                }

                // Increment license sequence for this dealer using DealerService
                var newSequence = await _dealerService.IncrementLicenseSequenceAsync(dealer.DealerCode);
                dealer.LicenseSequence = newSequence;

                // Create LicenseRequest for the controller
                var licenseRequest = new LicenseRequest
                {
                    SerialNumber = License.SerialNumber,
                    LicenseType = licenseType,
                    IssuedAt = License.IssueDate,
                    ExpiresAt = expiryDate
                };

                // Call the LicenseService to generate the activation key with sequence
                var licenseKey = _licenseService.GenerateLicense(
                    licenseRequest.SerialNumber,
                    licenseRequest.IssuedAt,
                    licenseRequest.ExpiresAt,
                    licenseRequest.LicenseType,
                    dealer.LicenseSequence
                );

                // Create the license response
                GeneratedLicense = new LicenseResponse
                {
                    LicenseKey = licenseKey,
                    SerialNumber = licenseRequest.SerialNumber,
                    LicenseType = licenseRequest.LicenseType,
                    IssuedAt = licenseRequest.IssuedAt,
                    ExpiresAt = licenseRequest.ExpiresAt
                };

                // License is generated but not saved to database
                SuccessMessage = "License generated successfully!";
                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error generating license: {ex.Message}";
                License.DealerCode = dealer.DealerCode;
                License.InternalDealerId = dealer.InternalDealerId;
                License.IssueDate = DateTime.Now;
                return Page();
            }
        }

        public IActionResult OnPostCancel()
        {
            return RedirectToPage("/Dealers/Dashboard");
        }

        public IActionResult OnPostLogout()
        {
            HttpContext.Session.Clear();
            return RedirectToPage("/Login");
        }
    }
}
