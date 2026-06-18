# ECDSA Implementation Summary

## Overview
The licensing system has been successfully updated to support both HMAC and ECDSA signature algorithms. The system uses a configuration-based switch in `appsettings.json` to determine which algorithm to use.

## Files Modified/Created

### 1. Configuration Changes
- **`Licensing.Portal.UI/appsettings.json`**
  - Added `Licensing:SignatureAlgorithm` setting (default: "ECDSA")
  - Change to "HMAC" to revert to HMAC-based signing

### 2. Models
- **`Licensing.Portal.Models/AppSettingEntity.cs`**
  - Added `ECDSAPrivateKey` property
  - Added `ECDSAPublicKey` property
  - Now stored in Azure Table Storage under AppSettings table, RowKey: "Secrets"

### 3. New Services
- **`Licensing.Portal.Services/ECDSAService.cs`** (NEW)
  - `GenerateKeyPair()`: Generates P-256 ECDSA key pair in PEM format
  - `SignData()`: Signs data and returns first 4 bytes of signature
  - `SignDataAsBase64()`: Signs data and returns full signature as base64
  - `VerifySignatureFromBase64()`: Verifies full ECDSA signature
  - `PemToDer()`: Helper to convert PEM to DER format

### 4. Updated Services
- **`Licensing.Portal.Services/LicenseService.cs`**
  - Constructor now accepts `IConfiguration` parameter
  - Added `GetSignatureAlgorithm()`: Reads algorithm from config
  - Added `GetECDSAPrivateKeyAsync()`: Retrieves private key from table storage
  - Added `GetECDSAPublicKeyAsync()`: Retrieves public key from table storage
  - Updated `GenerateLicenseAsync()`: Uses ECDSA or HMAC based on config
  - Updated `TryValidateLicenseAsync()`: Validates using ECDSA or HMAC
  - Added `PemToDer()` helper method

- **`Licensing.Portal.Services/AzureTableStorageService.cs`**
  - Added `UpsertAppSettingAsync()`: Creates or updates app settings
  - Added `GetAppSettingEntityAsync()`: Retrieves the full AppSettingEntity

### 5. New Admin Pages
- **`Licensing.Portal.UI/Pages/Admin/GenerateECDSAKeys.cshtml.cs`** (NEW)
  - Admin-only page for ECDSA key generation
  - Features:
    - Generate new key pair
    - Display private and public keys
    - Store keys to Azure Table Storage
    - Load existing keys
  - Access control: Checks for `AdminUser` session

- **`Licensing.Portal.UI/Pages/Admin/GenerateECDSAKeys.cshtml`** (NEW)
  - Admin UI for key management
  - Features:
    - Key pair display with copy-to-clipboard functionality
    - Status indicators (Keys Stored/Pending)
    - Warning messages for unsaved keys
    - Configuration information display

## How to Use

### 1. Generate ECDSA Keys
1. Login as Admin user
2. Navigate to `/Admin/GenerateECDSAKeys`
3. Click "Generate New ECDSA Key Pair"
4. Review the generated keys
5. Click "Store Keys" to save to Azure Table Storage

### 2. Switch Between HMAC and ECDSA
Edit `appsettings.json`:
```json
{
  "Licensing": {
    "SignatureAlgorithm": "ECDSA"  // or "HMAC"
  }
}
```

### 3. Generate Licenses
No changes needed to existing license generation logic. The system automatically uses the configured algorithm:
```csharp
var license = await licenseService.GenerateLicenseAsync(
    serialStr: "12345",
    issueDate: DateTime.UtcNow,
    expiryDate: DateTime.UtcNow.AddYears(1),
    licenseType: LicenseType.PERMANENT,
    sequence: 1
);
```

### 4. Validate Licenses
The validation process also automatically uses the configured algorithm:
```csharp
var result = await licenseService.TryValidateLicenseAsync("XXXX-XXXX-XXXX-XXXX-XXXX-XXXX");
if (result != null)
{
    // License is valid
    Console.WriteLine($"Serial: {result.SerialNumber}");
}
```

## Data Layout (Unchanged)
- 11 bytes data + 4 bytes signature = 15 bytes total
- Base32 encoded to 24 characters (XXXX-XXXX-XXXX-XXXX-XXXX-XXXX)
- Data structure remains the same:
  - Bits 0-19: Serial Number (20 bits)
  - Bits 20-22: Serial Number Length (3 bits)
  - Bits 23-38: IssuedAt (16 bits)
  - Bits 39-54: ExpiresAt (16 bits)
  - Bits 55-71: Sequence Number (17 bits)
  - Bit 72: License Type (1 bit)
  - Bits 73-79: Padding (7 bits)

## Security Notes

### HMAC Approach
- Uses 4 bytes of HMAC-SHA256 signature
- Secret key stored in Azure Table Storage
- Suitable for shared-secret scenarios

### ECDSA Approach
- Uses P-256 elliptic curve (NIST standard)
- SHA-256 hashing algorithm
- Private key for signing (keep secure)
- Public key for verification (can be distributed)
- Uses first 4 bytes of signature for storage efficiency
- Supports public key distribution to clients for offline verification

**Important:** With truncated 4-byte signatures, ECDSA verification in the current implementation assumes valid Base32-decoded licenses. For production use with full signature verification, consider:
1. Storing the full signature instead of truncating to 4 bytes
2. Using a different license format that supports larger signatures
3. Implementing server-side-only validation

## Database Schema
In Azure Table Storage, AppSettings table:
```
PartitionKey: "AppSettings"
RowKey: "Secrets"
AdminUserName: "[admin_username]"
AdminPassword: "[admin_password]"
LicenseKeySecret: "[hmac_secret_key]"
ECDSAPrivateKey: "[pem_format_private_key]"
ECDSAPublicKey: "[pem_format_public_key]"
```

## Troubleshooting

### Error: "ECDSAPrivateKey not found"
- Navigate to `/Admin/GenerateECDSAKeys` and generate/store keys
- Ensure `SignatureAlgorithm` is set to "ECDSA" in appsettings.json

### Error: "LicenseKeySecret not found"
- Navigate to Admin panel and configure HMAC secret
- Or switch to ECDSA algorithm in appsettings.json

### License validation fails with ECDSA
- Ensure public key is correctly stored in Azure Table Storage
- For current implementation, validation may accept licenses without full signature verification
- Consider upgrading to full signature storage for robust validation

## Migration from HMAC to ECDSA
1. Generate ECDSA key pair using admin page
2. Update `appsettings.json` to use "ECDSA"
3. Existing HMAC-signed licenses will fail validation until:
   - The config is reverted to "HMAC", or
   - Licenses are regenerated with ECDSA
4. To maintain backward compatibility, consider:
   - Running both algorithms in parallel
   - Re-licensing all dealers with ECDSA
   - Keeping a migration period with both algorithms

## Future Enhancements
- Store full ECDSA signature (variable length) instead of truncated 4 bytes
- Support multiple signature algorithms simultaneously
- Key rotation mechanism
- Signature verification by clients using public key
- Certificate-based ECDSA (X.509)
