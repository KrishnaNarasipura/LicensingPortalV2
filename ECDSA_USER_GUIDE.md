# ECDSA Key Management - User Guide

## Quick Start

### 1. Access the ECDSA Key Management Page
- Login as Admin user
- URL: `https://localhost:7000/Admin/GenerateECDSAKeys` (or your application URL)
- Alternatively, navigate from the Admin Dashboard

### 2. Generate New ECDSA Keys
#### Option A: First Time Setup
1. Click the button: **"Generate New ECDSA Key Pair"**
2. The system will generate a P-256 ECDSA key pair
3. You'll see two text areas with the keys:
   - **Private Key** (PEM format, starts with `-----BEGIN PRIVATE KEY-----`)
   - **Public Key** (PEM format, starts with `-----BEGIN PUBLIC KEY-----`)
4. Click **"Store Keys"** to save them to Azure Table Storage
5. You'll see a success message: "Keys have been successfully stored..."

#### Option B: Update Existing Keys
1. The page will show your current stored keys
2. Click **"Generate New ECDSA Key Pair"** to create new ones
3. Review the new keys
4. Click **"Store Keys"** to replace the old keys
5. Confirm the action (this will overwrite existing keys)

### 3. Copy Keys to Clipboard
- Click the **"Copy to Clipboard"** button under each key
- The key text will be copied for use in other systems or documentation

### 4. Switch Signature Algorithm
Edit your `appsettings.json` file:

```json
{
  "Licensing": {
    "SignatureAlgorithm": "ECDSA"
  }
}
```

Options:
- `"ECDSA"` - Use ECDSA for license signing
- `"HMAC"` - Use HMAC-SHA256 for license signing (legacy)

### 5. Verify Settings
The page displays your current configuration:
- Check the "Configuration" section at the bottom
- Verify the current algorithm setting matches your `appsettings.json`

## What Each Key Is Used For

### Private Key
- **Purpose**: Used by the license generation service to sign/create licenses
- **Who has it**: Only stored on the server
- **Security**: KEEP SECRET - do not share or expose
- **Format**: PKCS8 PEM format
- **Usage**: 
  - Server generates new licenses
  - Server signs license data with this key

### Public Key
- **Purpose**: Used to verify that a license was genuinely signed by the private key
- **Who has it**: Can be distributed to clients
- **Security**: Safe to share publicly
- **Format**: SubjectPublicKeyInfo PEM format
- **Usage**:
  - Clients can verify licenses offline
  - Third parties can validate licenses
  - No security risk if exposed

## Display Information

### Status Badges
- **? Keys Stored**: Keys are currently stored in Azure Table Storage and active
- **? Keys Not Yet Stored**: Generated keys are displayed but not yet saved

### Sections

#### Generate New Keys
Click to generate a fresh ECDSA key pair. Current keys (if any) are not affected.

#### Key Display
Shows the PEM-formatted keys. Keys are read-only in this display.

#### Configuration
Shows the current signature algorithm setting from your configuration file.

## Troubleshooting

### I don't see the page
- **Solution**: Ensure you're logged in as an Admin user
- Check your session hasn't expired
- Verify the URL: `/Admin/GenerateECDSAKeys`

### Keys aren't saving
- **Error**: "Error storing ECDSA keys: ..."
- **Solutions**:
  - Check Azure Table Storage connection string
  - Verify "AppSettings" table exists
  - Check table permissions
  - Review application logs for details

### Can't generate keys
- **Solutions**:
  - Check if admin session is valid
  - Refresh the page
  - Check browser console for errors
  - Verify server is running and responsive

### I lost my keys
- **Recovery**: If keys are stored in Azure Table Storage, they can be retrieved from the AppSettings table
- **New Generation**: Generate a new key pair using this page
- **Important**: All previously issued licenses with old keys will become invalid if you generate new keys

## Security Best Practices

1. **Backup Keys**
   - Store a backup of your private key in a secure location
   - Backup should be encrypted
   - Keep it separate from the application

2. **Key Rotation**
   - Periodically generate new keys (e.g., yearly)
   - Plan migration of existing licenses
   - Update clients with new public keys

3. **Access Control**
   - Only admin users can access this page
   - Monitor who generates new keys
   - Keep audit logs of key generation events

4. **Distribution**
   - Share public keys through secure channels
   - Embed public keys in client applications or configuration
   - Never share private keys

5. **Storage**
   - Private keys are stored in Azure Table Storage
   - Use Azure Key Vault for production environments
   - Enable encryption at rest and in transit

## Common Scenarios

### Scenario 1: Setting Up License System for First Time
1. Navigate to `/Admin/GenerateECDSAKeys`
2. Click "Generate New ECDSA Key Pair"
3. Review the keys
4. Click "Store Keys"
5. Verify "Keys Stored" badge appears
6. Start generating licenses

### Scenario 2: Migrate from HMAC to ECDSA
1. Generate ECDSA keys using this page
2. Update `appsettings.json` setting to "ECDSA"
3. Regenerate licenses for all dealers
4. Update client applications if needed
5. Test license validation

### Scenario 3: Emergency Key Rotation
1. Generate new key pair
2. Store the new keys
3. Update configuration if needed
4. Notify stakeholders about the change
5. Plan re-licensing of all dealers
6. Update clients with new public keys

### Scenario 4: Backup Keys for Disaster Recovery
1. Navigate to the key management page
2. View the current stored keys
3. Copy both private and public keys
4. Paste into a secure backup file
5. Encrypt and store the backup securely
6. Document the backup location and access method

## API Integration

### For Developers
The ECDSA keys can be used programmatically:

```csharp
// Generate license with ECDSA
var license = await licenseService.GenerateLicenseAsync(
    serialStr: "12345",
    issueDate: DateTime.UtcNow,
    expiryDate: DateTime.UtcNow.AddYears(1),
    licenseType: LicenseType.PERMANENT,
    sequence: 1
);

// Validate license
var result = await licenseService.TryValidateLicenseAsync(license);
if (result != null)
{
    Console.WriteLine($"Valid license: {result.SerialNumber}");
}
```

## Support

For technical support or issues:
1. Check the application logs
2. Verify Azure Table Storage connectivity
3. Ensure admin credentials are correct
4. Consult the ECDSA_IMPLEMENTATION.md documentation
5. Review error messages for specific guidance
