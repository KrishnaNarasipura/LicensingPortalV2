# Azure Table Storage Only - Migration Guide

## Overview
This document outlines the complete refactoring to use **only Azure Table Storage** and remove all SQLite/PostgreSQL dependencies.

## Key Changes

### 1. **Model Changes**
- ? **Dealer.cs** - Removed `Id` property, `DealerCode` is now the primary key
- ? **DealerTableEntity.cs** - Removed `Id`, added `DealerCode` property
- ? **License.cs** - Removed `Id` and `DealerId`, keeping only `DealerCode`

### 2. **Service Changes**
- ? **DealerService.cs** - Completely refactored to use only `AzureTableStorageService`
  - Removed `DealerDbContext` dependency
  - All methods now use Azure Table Storage
  - Changed `IncrementLicenseSequence` to accept `dealerCode` (string) instead of `dealerId` (int)

### 3. **Razor Pages to Update**

#### Login.cshtml.cs
**Changes needed:**
- Remove `DealerDbContext` dependency
- Use `DealerService.GetDealerAsync(dealerCode)` instead of `_dbContext.Dealers.FirstOrDefault`
- Session: Store only `DealerCode` (remove `DealerId`)

#### Dealers/Index.cshtml.cs  
**Changes needed:**
- Remove `DealerDbContext` dependency
- Use `DealerService.GetAllDealersAsync()` to get dealers
- Use `DealerService.ResetDealerPasswordAsync(dealerCode)` for password reset
- Update `OnPostResetPassword` to accept `dealerCode` instead of `dealerId`

#### Dealers/Create.cshtml.cs
**Changes needed:**
- Already using `DealerService`, should work with minimal changes

#### Dealers/Dashboard.cshtml.cs
**Changes needed:**
- Remove `DealerDbContext` dependency
- Get `DealerCode` from session
- Use `DealerService.GetDealerAsync(dealerCode)`

#### Dealers/ChangePassword.cshtml.cs
**Changes needed:**
- Remove `DealerDbContext` dependency
- Get `DealerCode` from session
- Use `DealerService.GetDealerAsync(dealerCode)`
- Use `DealerService.UpdateDealerPasswordAsync()`

#### Dealers/GenerateLicense.cshtml.cs
**Changes needed:**
- Remove `DealerDbContext` dependency
- Get `DealerCode` from session
- Use `DealerService.GetDealerAsync(dealerCode)`
- Use `DealerService.IncrementLicenseSequenceAsync(dealerCode)`
- Update all `License.DealerId` references to use `DealerCode`

### 4. **Razor Views to Update**

#### Dealers/Index.cshtml
**Changes needed:**
- Change form input from `dealerId` to `dealerCode`:
```html
<!-- OLD -->
<input type="hidden" name="dealerId" value="@dealer.Id" />

<!-- NEW -->
<input type="hidden" name="dealerCode" value="@dealer.DealerCode" />
```

#### Dealers/GenerateLicense.cshtml
**Changes needed:**
- Remove any `DealerId` references
- Ensure only `DealerCode` is used

### 5. **Program.cs Changes**

**Remove:**
```csharp
// Remove database configuration
builder.Services.AddDbContext<DealerDbContext>(options => ...);
```

**Keep:**
```csharp
builder.Services.AddScoped<DealerService>();
builder.Services.AddScoped<LicenseService>();
builder.Services.AddScoped<AzureTableStorageService>();
```

**Remove:**
```csharp
// Remove database initialization
app.InitializeDatabase();
```

### 6. **appsettings.json Changes**

**Remove:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "..."  // Remove PostgreSQL connection
  },
  "DatabaseSettings": {  // Remove this entire section
    "DatabaseType": "SQLite",
    "SqliteDbPath": "..."
  }
}
```

**Keep only:**
```json
{
  "ConnectionStrings": {
    "AzureTableStorage": "UseDevelopmentStorage=true"
  },
  "Logging": { ... },
  "AllowedHosts": "*",
  "Licensing": { ... },
  "Admin": { ... }
}
```

### 7. **Files to Delete**

- `Licensing.Portal.Data/Data/DealerDbContext.cs`
- `Licensing.Portal.Data/Migrations/` (entire folder)
- `Licensing.Portal.UI/Data/DatabaseExtensions.cs`

### 8. **Project References to Remove**

From `Licensing.Portal.Services.csproj`:
- Remove: `Microsoft.EntityFrameworkCore`
- Remove: `Microsoft.EntityFrameworkCore.Sqlite`
- Remove: `Npgsql.EntityFrameworkCore.PostgreSQL`

From `Licensing.Portal.UI.csproj`:
- Remove reference to `Licensing.Portal.Data` project

### 9. **Session Management Changes**

**OLD (using ID):**
```csharp
HttpContext.Session.SetString("DealerId", dealer.Id.ToString());
var dealerId = HttpContext.Session.GetString("DealerId");
if (!int.TryParse(dealerId, out int id)) { ... }
```

**NEW (using Code):**
```csharp
HttpContext.Session.SetString("DealerCode", dealer.DealerCode);
var dealerCode = HttpContext.Session.GetString("DealerCode");
if (string.IsNullOrEmpty(dealerCode)) { ... }
```

## Migration Steps

###  Step 1: ? Update Models
- [x] Dealer.cs
- [x] DealerTableEntity.cs
- [x] License.cs

### Step 2: ? Update Services
- [x] DealerService.cs
- [ ] Update all calling code

### Step 3: Update Razor Pages (in order)
1. [ ] Login.cshtml.cs
2. [ ] Dealers/Index.cshtml.cs & Index.cshtml
3. [ ] Dealers/Create.cshtml.cs
4. [ ] Dealers/Dashboard.cshtml.cs
5. [ ] Dealers/ChangePassword.cshtml.cs
6. [ ] Dealers/GenerateLicense.cshtml.cs

### Step 4: Update Configuration
- [ ] Program.cs - Remove DbContext, keep only Azure services
- [ ] appsettings.json - Remove database settings
- [ ] Remove Database files and migrations

### Step 5: Testing
- [ ] Test dealer creation
- [ ] Test login with dealer code
- [ ] Test password change
- [ ] Test license generation
- [ ] Test password reset

## Important Notes

1. **DealerCode is now the primary key** - All lookups must use DealerCode
2. **No more auto-increment IDs** - DealerCode is generated via hash
3. **All database operations are async** - Prefer async methods
4. **Azure Table Storage only** - No fallback to SQLite
5. **Session uses DealerCode** - Update all session management

## Rollback Plan

If issues occur:
1. Keep old code in a separate branch
2. Azure Table Storage data persists - can query anytime
3. Can recreate SQLite from Azure if needed using migration script

## Next Steps

Would you like me to:
1. Continue with updating all Razor Pages?
2. Update Program.cs and configuration?
3. Create a complete working example of one page?

Let me know and I'll proceed with the implementation!
