using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Licensing.Portal.Services;
using Licensing.Portal.Models;

namespace Licensing.Portal.Pages.Admin;

public class GenerateECDSAKeysModel : PageModel
{
    private readonly AzureTableStorageService _azureTableStorageService;
    private readonly ILogger<GenerateECDSAKeysModel> _logger;

    public GenerateECDSAKeysModel(AzureTableStorageService azureTableStorageService, ILogger<GenerateECDSAKeysModel> logger)
    {
        _azureTableStorageService = azureTableStorageService;
        _logger = logger;
    }

    [BindProperty]
    public string? PrivateKey { get; set; }

    [BindProperty]
    public string? PublicKey { get; set; }

    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
    public bool KeysGenerated { get; set; } = false;
    public bool KeysStored { get; set; } = false;

    public async Task<IActionResult> OnGetAsync()
    {
        // Check if user is admin
        var adminUser = HttpContext.Session.GetString("AdminUser");
        if (string.IsNullOrEmpty(adminUser))
        {
            return RedirectToPage("/Login");
        }

        // Load existing keys if available
        var appSettings = await _azureTableStorageService.GetAppSettingForECDSAAsync();
        if (appSettings != null && !string.IsNullOrEmpty(appSettings.ECDSAPrivateKey))
        {
            PrivateKey = appSettings.ECDSAPrivateKey;
            PublicKey = appSettings.ECDSAPublicKey;
            KeysStored = true;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostGenerateAsync()
    {
        // Check if user is admin
        var adminUser = HttpContext.Session.GetString("AdminUser");
        if (string.IsNullOrEmpty(adminUser))
        {
            return RedirectToPage("/Login");
        }

        try
        {
            // Generate new ECDSA key pair
            (string privateKey, string publicKey) = ECDSAService.GenerateKeyPair();
            
            PrivateKey = privateKey;
            PublicKey = publicKey;
            KeysGenerated = true;
            Message = "ECDSA key pair generated successfully. Keys are displayed below. Click 'Store Keys' to save them to Azure Table Storage.";

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating ECDSA keys");
            ErrorMessage = "Error generating ECDSA keys: " + ex.Message;
            return Page();
        }
    }

    public async Task<IActionResult> OnPostStoreAsync()
    {
        // Check if user is admin
        var adminUser = HttpContext.Session.GetString("AdminUser");
        if (string.IsNullOrEmpty(adminUser))
        {
            return RedirectToPage("/Login");
        }

        if (string.IsNullOrEmpty(PrivateKey) || string.IsNullOrEmpty(PublicKey))
        {
            ErrorMessage = "Private key and Public key are required";
            return Page();
        }

        try
        {
            // Get existing settings or create new ones
            var appSettings = await _azureTableStorageService.GetAppSettingForECDSAAsync();
            
            if (appSettings == null)
            {
                appSettings = new AppSettingEntity
                {
                    PartitionKey = "AppSettings",
                    RowKey = "Secrets"
                };
            }

            // Update keys
            appSettings.ECDSAPrivateKey = PrivateKey;
            appSettings.ECDSAPublicKey = PublicKey;

            // Save to Azure Table Storage
            await _azureTableStorageService.UpsertAppSettingAsync(appSettings);

            Message = "ECDSA keys have been successfully stored in Azure Table Storage.";
            KeysStored = true;
            KeysGenerated = false;

            _logger.LogInformation("Admin {AdminUser} stored new ECDSA keys", adminUser);

            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error storing ECDSA keys");
            ErrorMessage = "Error storing ECDSA keys: " + ex.Message;
            return Page();
        }
    }
}
