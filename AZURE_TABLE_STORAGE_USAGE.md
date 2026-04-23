# Azure Table Storage Integration Guide

## Overview
This project now includes Azure Table Storage integration for storing and retrieving dealer information. The implementation uses the `Azure.Data.Tables` namespace and provides a complete CRUD interface.

## Configuration

### Local Development (Using Azurite Storage Emulator)

1. **Install Azurite** (Azure Storage Emulator):
   ```bash
   npm install -g azurite
   ```

2. **Start Azurite**:
   ```bash
   azurite --silent --location c:\azurite --debug c:\azurite\debug.log
   ```

3. **appsettings.json** (already configured):
   ```json
   {
     "ConnectionStrings": {
       "AzureTableStorage": "UseDevelopmentStorage=true"
     }
   }
   ```

### Azure Production Configuration

1. **Create Azure Storage Account** in Azure Portal

2. **Get Connection String**:
   - Navigate to Storage Account ? Access keys
   - Copy "Connection string"

3. **Update Azure App Service Configuration**:
   - Go to App Service ? Configuration ? Application settings
   - Add new setting:
     - **Name**: `ConnectionStrings__AzureTableStorage`
     - **Value**: `DefaultEndpointsProtocol=https;AccountName=<your-account>;AccountKey=<your-key>;EndpointSuffix=core.windows.net`

## Table Structure

**Table Name**: `Dealers`

**Partition Key**: `"Dealers"` (all dealers share the same partition)

**Row Key**: `DealerCode` (unique dealer code)

### Columns
- Id (int)
- DealerName (string)
- Address (string)
- City (string)
- State (string)
- Pincode (string)
- PhoneNumber (string)
- InternalDealerId (string)
- DealerCode (string)
- TemporaryPassword (string)
- Password (string?)
- PasswordChangeRequired (bool)
- CreatedDate (DateTime)
- LicenseSequence (int)

## Usage Examples

### 1. Inject the Service

```csharp
public class MyPageModel : PageModel
{
    private readonly AzureTableStorageService _tableStorageService;
    
    public MyPageModel(AzureTableStorageService tableStorageService)
    {
        _tableStorageService = tableStorageService;
    }
}
```

### 2. Add a New Dealer

```csharp
var dealer = new Dealer
{
    Id = 1,
    DealerName = "ABC Motors",
    Address = "123 Main St",
    City = "Mumbai",
    State = "Maharashtra",
    Pincode = "400001",
    PhoneNumber = "9876543210",
    InternalDealerId = "01",
    DealerCode = "LIVE01240416001",
    TemporaryPassword = "temp123",
    PasswordChangeRequired = true,
    CreatedDate = DateTime.UtcNow,
    LicenseSequence = 0
};

await _tableStorageService.AddDealerAsync(dealer);
```

### 3. Get a Dealer by Dealer Code

```csharp
var dealerEntity = await _tableStorageService.GetDealerAsync("LIVE01240416001");

if (dealerEntity != null)
{
    var dealer = dealerEntity.ToDealer();
    Console.WriteLine($"Dealer: {dealer.DealerName}");
}
```

### 4. Get All Dealers

```csharp
var allDealers = await _tableStorageService.GetAllDealersAsync();

foreach (var dealerEntity in allDealers)
{
    var dealer = dealerEntity.ToDealer();
    Console.WriteLine($"{dealer.DealerCode} - {dealer.DealerName}");
}
```

### 5. Update a Dealer

```csharp
var dealer = await _tableStorageService.GetDealerAsync("LIVE01240416001");

if (dealer != null)
{
    var dealerObj = dealer.ToDealer();
    dealerObj.PhoneNumber = "9999999999";
    
    await _tableStorageService.UpdateDealerAsync(dealerObj);
}
```

### 6. Upsert (Insert or Update)

```csharp
// This will insert if doesn't exist, or update if exists
await _tableStorageService.UpsertDealerAsync(dealer);
```

### 7. Delete a Dealer

```csharp
await _tableStorageService.DeleteDealerAsync("LIVE01240416001");
```

### 8. Check if Dealer Exists

```csharp
bool exists = await _tableStorageService.DealerExistsAsync("LIVE01240416001");

if (exists)
{
    Console.WriteLine("Dealer found!");
}
```

### 9. Query by City

```csharp
var mumbaiDealers = await _tableStorageService.GetDealersByCityAsync("Mumbai");

foreach (var dealer in mumbaiDealers)
{
    Console.WriteLine($"{dealer.DealerName} - {dealer.City}");
}
```

### 10. Query by State

```csharp
var maharashtraDealers = await _tableStorageService.GetDealersByStateAsync("Maharashtra");
```

### 11. Get Dealer by Phone Number

```csharp
var dealer = await _tableStorageService.GetDealerByPhoneNumberAsync("9876543210");

if (dealer != null)
{
    Console.WriteLine($"Found: {dealer.DealerName}");
}
```

### 12. Get Next License Sequence

```csharp
// Increments and returns the next license sequence for a dealer
int nextSequence = await _tableStorageService.GetNextLicenseSequenceAsync("LIVE01240416001");

Console.WriteLine($"Next License Number: {nextSequence}");
```

## Complete Example: Create Dealer Page

```csharp
public class CreateModel : PageModel
{
    private readonly AzureTableStorageService _tableStorageService;
    
    public CreateModel(AzureTableStorageService tableStorageService)
    {
        _tableStorageService = tableStorageService;
    }
    
    [BindProperty]
    public Dealer Dealer { get; set; }
    
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }
        
        // Check if dealer already exists
        var exists = await _tableStorageService.DealerExistsAsync(Dealer.DealerCode);
        
        if (exists)
        {
            ModelState.AddModelError(string.Empty, "Dealer with this code already exists.");
            return Page();
        }
        
        // Add dealer to Azure Table Storage
        await _tableStorageService.AddDealerAsync(Dealer);
        
        return RedirectToPage("./Index");
    }
}
```

## Sync Between SQLite and Azure Table Storage

You can maintain both SQLite and Azure Table Storage in sync:

```csharp
public async Task<IActionResult> OnPostAsync()
{
    // Save to SQLite (existing code)
    _context.Dealers.Add(Dealer);
    await _context.SaveChangesAsync();
    
    // Also save to Azure Table Storage
    await _tableStorageService.AddDealerAsync(Dealer);
    
    return RedirectToPage("./Index");
}
```

## Error Handling

```csharp
try
{
    await _tableStorageService.AddDealerAsync(dealer);
}
catch (Azure.RequestFailedException ex) when (ex.Status == 409)
{
    // Conflict - entity already exists
    Console.WriteLine("Dealer already exists");
}
catch (Azure.RequestFailedException ex) when (ex.Status == 404)
{
    // Not found
    Console.WriteLine("Dealer not found");
}
catch (Exception ex)
{
    // Other errors
    Console.WriteLine($"Error: {ex.Message}");
}
```

## Performance Considerations

1. **Partition Key**: All dealers use `"Dealers"` as partition key. For better performance with large datasets, consider using region or state as partition key.

2. **Batch Operations**: For bulk inserts, consider using batch operations (not implemented in current version).

3. **Caching**: Implement caching for frequently accessed dealers.

## Migration Strategy

To migrate existing SQLite data to Azure Table Storage:

```csharp
public async Task MigrateToAzureTableStorageAsync()
{
    var dealers = await _context.Dealers.ToListAsync();
    
    foreach (var dealer in dealers)
    {
        await _tableStorageService.UpsertDealerAsync(dealer);
    }
    
    Console.WriteLine($"Migrated {dealers.Count} dealers to Azure Table Storage");
}
```

## Testing with Azurite

Start Azurite and use Azure Storage Explorer to view/manage data:

1. **Download**: [Azure Storage Explorer](https://azure.microsoft.com/features/storage-explorer/)
2. **Connect to**: Local ? Emulator
3. **Browse**: Tables ? Dealers

## Production Deployment Checklist

- [ ] Create Azure Storage Account
- [ ] Configure connection string in App Service
- [ ] Test CRUD operations
- [ ] Implement error handling
- [ ] Set up monitoring and logging
- [ ] Consider backup strategy
- [ ] Review partition key strategy for scale
