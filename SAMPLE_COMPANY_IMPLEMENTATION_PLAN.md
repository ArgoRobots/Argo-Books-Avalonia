# Sample Company Implementation Plan

> **Status: IMPLEMENTED** - This feature has been fully implemented.

## Overview

This document outlines the implementation plan for the "Open Sample Company" feature in Argo Books. The feature allows users to explore the application with pre-populated data from `SampleCompanyData.xlsx`.

## Current State

### What Exists
- **Welcome Screen Button**: The UI button already exists in `WelcomeScreen.axaml` with the label "Explore Sample Company"
- **ViewModel Command**: `OpenSampleCompanyCommand` in `WelcomeScreenViewModel.cs` triggers the `OpenSampleCompanyRequested` event
- **Sample Data File**: `SampleCompanyData.xlsx` in the root directory contains comprehensive sample data across 17 sheets
- **Import Service**: `SpreadsheetImportService` can import Excel files with merge logic and auto-create missing references

### What's Missing
- **Event Handler**: No handler is wired up for `OpenSampleCompanyRequested` in `App.axaml.cs`
- **Implementation Logic**: No code to create and populate the sample company

## Sample Data Contents

The `SampleCompanyData.xlsx` file contains:

| Sheet Name | Record Count | Description |
|------------|--------------|-------------|
| Customers | 20 | Customer records with contact info |
| Suppliers | 10 | Vendor/supplier records |
| Categories | 15 | Product categories (Revenue, Expenses, Rental types) |
| Products | 30 | Products linked to categories and suppliers |
| Departments | 5 | Company departments |
| Employees | 12 | Employee records with salary info |
| Locations | 3 | Warehouse/inventory locations |
| Invoices | 30 | Customer invoices |
| Payments | 25 | Payment records |
| Revenue (Sales) | 50 | Sales transactions |
| Expenses (Purchases) | 35 | Purchase transactions |
| Inventory | 20 | Inventory items by location |
| Stock Adjustments | 15 | Inventory adjustments |
| Purchase Orders | 10 | Supplier purchase orders |
| Rental Inventory | 8 | Rental items |
| Rental Records | 12 | Rental transactions |
| Recurring Invoices | 5 | Recurring billing setup |

## Implementation Options

### Option A: Dynamic Import (Recommended)
Create a sample company in a temporary location and import data from the embedded Excel file.

**Pros:**
- Sample data can be easily updated by modifying the Excel file
- Uses existing import infrastructure
- No need to maintain pre-built binary files

**Cons:**
- Slightly slower initial load (import process)
- Requires embedding the Excel file as a resource

### Option B: Pre-Built Sample File
Ship a pre-built `.argo` file as an embedded resource.

**Pros:**
- Faster initial load
- No import process needed

**Cons:**
- Requires rebuilding the `.argo` file when sample data changes
- Additional maintenance burden
- Harder to keep in sync with data model changes

### Option C: In-Memory Sample Data
Build sample data programmatically at runtime.

**Pros:**
- No external files needed
- Always in sync with current models

**Cons:**
- Lots of code to maintain
- Harder to update sample data
- Risk of drift from Excel file

## Recommended Implementation (Option A)

### Step 1: Embed Sample Data as Resource

1. Move `SampleCompanyData.xlsx` to `ArgoBooks/Resources/` directory
2. Add to `.csproj` as EmbeddedResource:
   ```xml
   <ItemGroup>
     <EmbeddedResource Include="Resources\SampleCompanyData.xlsx" />
   </ItemGroup>
   ```

### Step 2: Create Sample Company Service

Create a new service `SampleCompanyService.cs` in `ArgoBooks.Core/Services/`:

```csharp
public class SampleCompanyService
{
    private readonly CompanyManager _companyManager;
    private readonly SpreadsheetImportService _importService;

    public async Task<string> CreateSampleCompanyAsync(
        Stream excelDataStream,
        CancellationToken cancellationToken = default);
}
```

**Responsibilities:**
1. Create a new company with name "Sample Company" in temp directory
2. Extract the Excel stream to a temp file
3. Import data using `SpreadsheetImportService` with `AutoCreateMissingReferences = true`
4. Set company settings (company name, business type, etc.)
5. Return the file path of the created sample company

### Step 3: Wire Up Event Handler in App.axaml.cs

Add handler in `WireWelcomeScreenEvents()`:

```csharp
_welcomeScreenViewModel.OpenSampleCompanyRequested += async (_, _) =>
{
    await OpenSampleCompanyAsync();
};
```

Create `OpenSampleCompanyAsync()` method:

```csharp
private static async Task OpenSampleCompanyAsync()
{
    try
    {
        // Show loading indicator
        _appShellViewModel?.SetBusyState(true, "Creating sample company...");

        // Get embedded resource stream
        var assembly = typeof(App).Assembly;
        using var stream = assembly.GetManifestResourceStream(
            "ArgoBooks.Resources.SampleCompanyData.xlsx");

        if (stream == null)
        {
            ShowNotification("Error", "Sample data not found", NotificationType.Error);
            return;
        }

        // Create sample company
        var sampleService = new SampleCompanyService(CompanyManager!, _importService);
        var samplePath = await sampleService.CreateSampleCompanyAsync(stream);

        // Open the sample company
        await OpenCompanyWithRetryAsync(samplePath);

        // Show welcome notification
        ShowNotification("Welcome!",
            "You're exploring Sample Company. Feel free to experiment!",
            NotificationType.Information);
    }
    catch (Exception ex)
    {
        ShowNotification("Error",
            $"Failed to create sample company: {ex.Message}",
            NotificationType.Error);
    }
    finally
    {
        _appShellViewModel?.SetBusyState(false);
    }
}
```

### Step 4: Set Sample Company Metadata

When creating the sample company, configure meaningful defaults:

```csharp
companySettings.Company = new CompanyInfo
{
    Name = "Sample Company",
    BusinessType = "Retail",
    Industry = "Technology",
    Email = "info@samplecompany.com",
    Phone = "555-100-0000"
};
```

### Step 5: Add Visual Indicator for Sample Company

Consider adding a subtle indicator in the UI that shows the user is working with sample data:
- Banner at the top
- Badge in the company switcher
- Watermark in reports

This could be achieved by:
1. Adding an `IsSampleCompany` flag to `CompanySettings` or `FileFooter`
2. Checking this flag in the UI layer to show/hide indicators

### Step 6: Handle Sample Company Persistence

**Two options:**

**Option A - Ephemeral (Recommended for v1):**
- Sample company exists only in temp directory
- User is prompted to "Save As" if they make changes
- Sample data is recreated fresh each time

**Option B - Persistent:**
- First-time sample company is saved to a known location (e.g., Documents/ArgoBooks/SampleCompany.argo)
- Subsequent opens use the existing file
- User's changes are preserved

## Implementation Tasks

### Phase 1: Core Implementation
- [ ] Move `SampleCompanyData.xlsx` to `ArgoBooks/Resources/`
- [ ] Add as embedded resource in `.csproj`
- [ ] Create `SampleCompanyService` class
- [ ] Wire up `OpenSampleCompanyRequested` event handler
- [ ] Test basic flow end-to-end

### Phase 2: Polish
- [ ] Add loading indicator during creation
- [ ] Add welcome notification after opening
- [ ] Add error handling with user-friendly messages
- [ ] Consider visual indicator for sample company mode

### Phase 3: Enhancements (Optional)
- [ ] Add "Reset Sample Company" option to restore original data
- [ ] Add "Save As" prompt when closing with changes
- [ ] Consider periodic cleanup of old sample company temp files

## File Changes Summary

| File | Action | Description |
|------|--------|-------------|
| `SampleCompanyData.xlsx` | Move | Move from root to `ArgoBooks/Resources/` |
| `ArgoBooks.csproj` | Edit | Add EmbeddedResource entry |
| `ArgoBooks.Core/Services/SampleCompanyService.cs` | Create | New service class |
| `ArgoBooks/App.axaml.cs` | Edit | Add event handler and implementation |
| `ArgoBooks.Core/Models/CompanySettings.cs` | Edit (Optional) | Add `IsSampleCompany` flag |

## Testing Checklist

- [ ] Click "Explore Sample Company" creates and opens the company
- [ ] All 17 data types are properly imported
- [ ] ID counters are correctly updated after import
- [ ] App navigates to Dashboard after opening
- [ ] Sample company name appears in title bar
- [ ] No errors in console/logs
- [ ] Clicking button multiple times doesn't create duplicates
- [ ] Works on first run (no existing settings)
- [ ] Works when another company is already open

## Risks and Mitigations

| Risk | Mitigation |
|------|------------|
| Excel file gets out of sync with data model | Add validation tests that verify import works |
| Temp directory cleanup issues | Use proper temp file patterns, add cleanup on app exit |
| Import takes too long | Show progress indicator, optimize import service |
| Missing dependencies in sample data | Use `AutoCreateMissingReferences = true` |

## Timeline Estimate

This is a relatively straightforward feature that builds on existing infrastructure. The core implementation involves:
- Creating a new service (~100 lines)
- Modifying App.axaml.cs (~50 lines)
- Moving/embedding the Excel file
- Testing

## Conclusion

The recommended approach (Option A: Dynamic Import) provides the best balance of maintainability and user experience. It leverages existing infrastructure while keeping sample data easy to update.

The sample data in `SampleCompanyData.xlsx` is comprehensive and covers all major features of the application, making it an excellent resource for users to explore and learn the system.
