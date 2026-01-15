# AI-Powered Supplier & Category Selection Plan

## Simple Explanation

```
┌─────────────────────────────────────────────────────────────────┐
│                    YOU UPLOAD A RECEIPT                          │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│         STEP 1: AZURE DOCUMENT INTELLIGENCE (Eyes)               │
│                                                                  │
│   Reads the image and extracts text:                            │
│   • Vendor: "WALMART SUPERCENTER"                               │
│   • Date: January 15, 2026                                      │
│   • Total: $47.82                                               │
│   • Items: Milk, Bread, Eggs...                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              STEP 2: CHATGPT (Brain)                             │
│                                                                  │
│   Receives:                                                      │
│   • Vendor name from Azure                                      │
│   • Your existing suppliers list                                │
│   • Your existing categories list                               │
│                                                                  │
│   Thinks and returns:                                            │
│   • Supplier: "Walmart Inc." (95% confident)                    │
│   • Category: "Food & Beverages" (87% confident)                │
│   • OR suggests creating new ones if no match                   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                STEP 3: YOU REVIEW                                │
│                                                                  │
│   ┌─────────────────────────────────────────┐                   │
│   │  Supplier: [Walmart Inc. ▼]  95% match  │                   │
│   │  Category: [Food & Beverages ▼] 87%     │                   │
│   │                                         │                   │
│   │  [Confirm]  [Edit]                      │                   │
│   └─────────────────────────────────────────┘                   │
└─────────────────────────────────────────────────────────────────┘
```

### In Simple Terms

| Step | Service | Job |
|------|---------|-----|
| 1 | **Azure** | Reads the receipt image (OCR) |
| 2 | **ChatGPT** | Matches to your suppliers/categories |
| 3 | **You** | Review and confirm |

**Azure = Eyes** (reads the receipt)
**ChatGPT = Brain** (figures out which supplier/category)

---

## Reference Implementation

This follows the same pattern as `AIQueryTranslator.cs` in **Argo-Books-WinForms**:
- Location: `Argo Books/AISearch/AIQueryTranslator.cs`
- Uses `HttpClient` with Bearer token auth
- Calls `https://api.openai.com/v1/chat/completions`
- Model: `gpt-3.5-turbo` (we'll use `gpt-4o-mini` for better results)
- Builds a prompt, sends request, parses JSON response

---

## Overview

This document outlines the implementation plan for using AI (ChatGPT/OpenAI) to automatically select the best matching supplier and category, or create new ones when no good match exists, during the receipt scanning process.

## Current State

### Receipt Scanner Flow
1. User uploads receipt image
2. **Azure Document Intelligence** extracts OCR data:
   - Vendor name, date, amounts, line items, raw text
3. Basic supplier matching (simple string contains):
   ```csharp
   // Current logic in ReceiptsModalsViewModel.cs:377-388
   var matchedSupplier = SupplierOptions.FirstOrDefault(s =>
       s.Name.Contains(result.VendorName, StringComparison.OrdinalIgnoreCase) ||
       result.VendorName.Contains(s.Name, StringComparison.OrdinalIgnoreCase));
   ```
4. User manually selects category (no auto-matching)
5. User reviews and confirms expense creation

### Limitations
- Supplier matching is naive (exact substring only)
- No category matching at all
- User must navigate away to create new suppliers/categories
- No fuzzy matching or context awareness

---

## Proposed Solution

### Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Receipt Scanner Flow                         │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  1. User uploads receipt                                        │
│           ↓                                                      │
│  2. Azure Document Intelligence extracts data                   │
│           ↓                                                      │
│  3. NEW: AI Suggestion Service analyzes:                        │
│      • Vendor name + raw text                                   │
│      • Existing suppliers list                                  │
│      • Existing categories list                                 │
│      • Line item descriptions                                   │
│           ↓                                                      │
│  4. AI returns recommendations:                                 │
│      • Best matching supplier (or suggestion for new)           │
│      • Best matching category (or suggestion for new)           │
│      • Confidence scores                                        │
│           ↓                                                      │
│  5. UI shows AI suggestions with options to:                    │
│      • Accept suggestion                                        │
│      • Pick different option                                    │
│      • Auto-create new supplier/category                        │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

---

## Implementation Steps

### Phase 1: OpenAI Service Infrastructure

#### 1.1 Create OpenAI Service Interface

**File**: `ArgoBooks.Core/Services/IOpenAiService.cs`

```csharp
public interface IOpenAiService
{
    bool IsConfigured { get; }
    Task<bool> ValidateConfigurationAsync();
    Task<SupplierCategorySuggestion> GetSupplierCategorySuggestionAsync(
        ReceiptAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
```

#### 1.2 Create Request/Response Models

**File**: `ArgoBooks.Core/Models/AI/ReceiptAnalysisRequest.cs`

```csharp
public class ReceiptAnalysisRequest
{
    public string VendorName { get; set; } = string.Empty;
    public string? RawText { get; set; }
    public List<string> LineItemDescriptions { get; set; } = [];
    public decimal TotalAmount { get; set; }
    public List<ExistingSupplierInfo> ExistingSuppliers { get; set; } = [];
    public List<ExistingCategoryInfo> ExistingCategories { get; set; } = [];
}

public class ExistingSupplierInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ExistingCategoryInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ItemType { get; set; } = "Product";
}
```

**File**: `ArgoBooks.Core/Models/AI/SupplierCategorySuggestion.cs`

```csharp
public class SupplierCategorySuggestion
{
    // Supplier suggestion
    public string? MatchedSupplierId { get; set; }
    public string? MatchedSupplierName { get; set; }
    public double SupplierConfidence { get; set; }
    public bool ShouldCreateNewSupplier { get; set; }
    public NewSupplierSuggestion? NewSupplier { get; set; }

    // Category suggestion
    public string? MatchedCategoryId { get; set; }
    public string? MatchedCategoryName { get; set; }
    public double CategoryConfidence { get; set; }
    public bool ShouldCreateNewCategory { get; set; }
    public NewCategorySuggestion? NewCategory { get; set; }

    // Reasoning (for debugging/display)
    public string? SupplierReasoning { get; set; }
    public string? CategoryReasoning { get; set; }
}

public class NewSupplierSuggestion
{
    public string Name { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class NewCategorySuggestion
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string ItemType { get; set; } = "Product";
    public string Icon { get; set; } = "📦";
    public string Color { get; set; } = "#4A90D9";
}
```

#### 1.3 Implement OpenAI Service

**File**: `ArgoBooks.Core/Services/OpenAiService.cs`

```csharp
public class OpenAiService : IOpenAiService
{
    private const string ApiKeyEnvVar = "OPENAI_API_KEY";
    private const string ModelEnvVar = "OPENAI_MODEL"; // Default: gpt-4o-mini
    private const string ApiEndpoint = "https://api.openai.com/v1/chat/completions";

    public bool IsConfigured => DotEnv.HasValue(ApiKeyEnvVar);

    public async Task<SupplierCategorySuggestion> GetSupplierCategorySuggestionAsync(
        ReceiptAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(request);
        var response = await CallOpenAiAsync(prompt, cancellationToken);
        return ParseResponse(response);
    }

    private string BuildPrompt(ReceiptAnalysisRequest request)
    {
        // See detailed prompt below
    }
}
```

#### 1.4 AI Prompt Design

```
You are an AI assistant helping categorize business expenses. Analyze the receipt data and suggest the best matching supplier and category.

## Receipt Data
- Vendor Name: "{vendorName}"
- Line Items: {lineItems}
- Total Amount: {amount}
- Raw Text: "{rawText}"

## Existing Suppliers
{suppliers as JSON array}

## Existing Categories (Purchase type only)
{categories as JSON array}

## Instructions
1. SUPPLIER: Find the best matching supplier from the existing list. Consider:
   - Exact name matches
   - Partial matches (e.g., "Walmart" matches "Walmart Inc.")
   - Common abbreviations and variations
   - If no good match (confidence < 0.7), suggest creating a new supplier

2. CATEGORY: Find the best matching category based on:
   - Line item descriptions
   - Vendor type (e.g., grocery store → Food & Beverages)
   - Common business expense categories
   - If no good match (confidence < 0.7), suggest a new category

## Response Format (JSON only, no markdown)
{
  "supplier": {
    "matchedId": "SUP-001" | null,
    "matchedName": "Supplier Name" | null,
    "confidence": 0.95,
    "shouldCreateNew": false,
    "newSupplier": null | { "name": "New Supplier", "notes": "Auto-created from receipt scan" },
    "reasoning": "Brief explanation"
  },
  "category": {
    "matchedId": "CAT-PUR-001" | null,
    "matchedName": "Category Name" | null,
    "confidence": 0.85,
    "shouldCreateNew": false,
    "newCategory": null | { "name": "Office Supplies", "description": "...", "icon": "📎", "color": "#..." },
    "reasoning": "Brief explanation"
  }
}
```

---

### Phase 2: ViewModel Integration

#### 2.1 Update ReceiptsModalsViewModel

**Add new properties**:

```csharp
// AI suggestion state
[ObservableProperty]
private bool _isLoadingAiSuggestions;

[ObservableProperty]
private bool _hasAiSuggestions;

[ObservableProperty]
private SupplierCategorySuggestion? _aiSuggestion;

// Supplier suggestion display
[ObservableProperty]
private string? _suggestedSupplierName;

[ObservableProperty]
private double _supplierConfidence;

[ObservableProperty]
private bool _showCreateSupplierOption;

[ObservableProperty]
private string? _newSupplierName;

// Category suggestion display
[ObservableProperty]
private string? _suggestedCategoryName;

[ObservableProperty]
private double _categoryConfidence;

[ObservableProperty]
private bool _showCreateCategoryOption;

[ObservableProperty]
private string? _newCategoryName;
```

#### 2.2 Add AI Suggestion Methods

```csharp
private async Task GetAiSuggestionsAsync(ReceiptScanResult result)
{
    var openAiService = App.ServiceProvider?.GetService<IOpenAiService>();
    if (openAiService == null || !openAiService.IsConfigured)
    {
        // Fall back to basic matching
        return;
    }

    IsLoadingAiSuggestions = true;

    try
    {
        var request = BuildAnalysisRequest(result);
        var suggestion = await openAiService.GetSupplierCategorySuggestionAsync(request);

        ApplyAiSuggestion(suggestion);
    }
    catch (Exception ex)
    {
        // Log error, continue with manual selection
        Debug.WriteLine($"AI suggestion failed: {ex.Message}");
    }
    finally
    {
        IsLoadingAiSuggestions = false;
    }
}

private ReceiptAnalysisRequest BuildAnalysisRequest(ReceiptScanResult result)
{
    var companyData = App.CompanyManager?.CompanyData;

    return new ReceiptAnalysisRequest
    {
        VendorName = result.VendorName ?? string.Empty,
        RawText = result.RawText,
        LineItemDescriptions = result.LineItems.Select(i => i.Description).ToList(),
        TotalAmount = result.TotalAmount ?? 0,
        ExistingSuppliers = companyData?.Suppliers?.Select(s => new ExistingSupplierInfo
        {
            Id = s.Id,
            Name = s.Name,
            Notes = s.Notes
        }).ToList() ?? [],
        ExistingCategories = companyData?.Categories?
            .Where(c => c.Type == CategoryType.Purchase)
            .Select(c => new ExistingCategoryInfo
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                ItemType = c.ItemType
            }).ToList() ?? []
    };
}

private void ApplyAiSuggestion(SupplierCategorySuggestion suggestion)
{
    AiSuggestion = suggestion;
    HasAiSuggestions = true;

    // Apply supplier suggestion
    if (!string.IsNullOrEmpty(suggestion.MatchedSupplierId))
    {
        var supplier = SupplierOptions.FirstOrDefault(s => s.Id == suggestion.MatchedSupplierId);
        if (supplier != null)
        {
            SelectedSupplier = supplier;
            SuggestedSupplierName = supplier.Name;
            SupplierConfidence = suggestion.SupplierConfidence;
        }
    }
    else if (suggestion.ShouldCreateNewSupplier && suggestion.NewSupplier != null)
    {
        ShowCreateSupplierOption = true;
        NewSupplierName = suggestion.NewSupplier.Name;
    }

    // Apply category suggestion
    if (!string.IsNullOrEmpty(suggestion.MatchedCategoryId))
    {
        var category = CategoryOptions.FirstOrDefault(c => c.Id == suggestion.MatchedCategoryId);
        if (category != null)
        {
            SelectedCategory = category;
            SuggestedCategoryName = category.Name;
            CategoryConfidence = suggestion.CategoryConfidence;
        }
    }
    else if (suggestion.ShouldCreateNewCategory && suggestion.NewCategory != null)
    {
        ShowCreateCategoryOption = true;
        NewCategoryName = suggestion.NewCategory.Name;
    }
}
```

#### 2.3 Add Auto-Create Commands

```csharp
[RelayCommand]
private void AcceptAndCreateSupplier()
{
    if (AiSuggestion?.NewSupplier == null) return;

    var companyData = App.CompanyManager?.CompanyData;
    if (companyData == null) return;

    // Generate ID
    companyData.IdCounters.Supplier++;
    var newId = $"SUP-{companyData.IdCounters.Supplier:D4}";

    // Create supplier
    var newSupplier = new Supplier
    {
        Id = newId,
        Name = AiSuggestion.NewSupplier.Name,
        Notes = AiSuggestion.NewSupplier.Notes ?? "Auto-created from AI receipt scan"
    };

    companyData.Suppliers.Add(newSupplier);

    // Add to options and select
    var option = new SupplierOption { Id = newId, Name = newSupplier.Name };
    SupplierOptions.Add(option);
    SelectedSupplier = option;

    ShowCreateSupplierOption = false;

    App.AddNotification("Supplier Created", $"Created new supplier: {newSupplier.Name}", NotificationType.Success);
}

[RelayCommand]
private void AcceptAndCreateCategory()
{
    if (AiSuggestion?.NewCategory == null) return;

    var companyData = App.CompanyManager?.CompanyData;
    if (companyData == null) return;

    // Generate ID
    companyData.IdCounters.Category++;
    var newId = $"CAT-PUR-{companyData.IdCounters.Category:D3}";

    // Create category
    var newCategory = new Category
    {
        Id = newId,
        Type = CategoryType.Purchase,
        Name = AiSuggestion.NewCategory.Name,
        Description = AiSuggestion.NewCategory.Description,
        ItemType = AiSuggestion.NewCategory.ItemType,
        Icon = AiSuggestion.NewCategory.Icon,
        Color = AiSuggestion.NewCategory.Color
    };

    companyData.Categories.Add(newCategory);

    // Add to options and select
    var option = new CategoryOption { Id = newId, Name = newCategory.Name };
    CategoryOptions.Add(option);
    SelectedCategory = option;

    ShowCreateCategoryOption = false;

    App.AddNotification("Category Created", $"Created new category: {newCategory.Name}", NotificationType.Success);
}
```

---

### Phase 3: UI Updates

#### 3.1 Add AI Suggestion Indicators

**File**: `ArgoBooks/Modals/ReceiptsModals.axaml`

```xml
<!-- Supplier Selection with AI Suggestion -->
<StackPanel>
    <Grid ColumnDefinitions="*,Auto">
        <controls:SearchableDropdown
            ItemsSource="{Binding SupplierOptions}"
            SelectedItem="{Binding SelectedSupplier, Mode=TwoWay}"
            DisplayMemberPath="Name"
            Placeholder="Search for a supplier..." />

        <!-- AI Confidence Badge -->
        <Border Grid.Column="1"
                IsVisible="{Binding HasAiSuggestions}"
                Background="{Binding SupplierConfidence, Converter={StaticResource ConfidenceToColorConverter}}"
                CornerRadius="4" Padding="6,2" Margin="8,0,0,0">
            <TextBlock Text="{Binding SupplierConfidence, StringFormat='{}{0:P0} match'}"
                       FontSize="11" Foreground="White"/>
        </Border>
    </Grid>

    <!-- Create New Supplier Option -->
    <Border IsVisible="{Binding ShowCreateSupplierOption}"
            Background="#FFF3CD" CornerRadius="4" Padding="12" Margin="0,8,0,0">
        <StackPanel Orientation="Horizontal">
            <TextBlock Text="AI suggests creating: " VerticalAlignment="Center"/>
            <TextBlock Text="{Binding NewSupplierName}" FontWeight="Bold" VerticalAlignment="Center"/>
            <Button Content="Create" Command="{Binding AcceptAndCreateSupplierCommand}"
                    Margin="12,0,0,0" Classes="accent"/>
        </StackPanel>
    </Border>
</StackPanel>

<!-- Similar pattern for Category selection -->
```

#### 3.2 Add Loading State

```xml
<!-- AI Loading Indicator -->
<Border IsVisible="{Binding IsLoadingAiSuggestions}"
        Background="#E3F2FD" CornerRadius="4" Padding="12" Margin="0,0,0,16">
    <StackPanel Orientation="Horizontal">
        <ProgressRing Width="16" Height="16" IsActive="True"/>
        <TextBlock Text="AI is analyzing receipt..." Margin="8,0,0,0"/>
    </StackPanel>
</Border>
```

---

### Phase 4: Configuration

#### 4.1 Environment Variables

Add to `.env` file:

```env
# OpenAI Configuration
OPENAI_API_KEY=sk-your-api-key-here
OPENAI_MODEL=gpt-4o-mini  # Optional, defaults to gpt-4o-mini (cost-effective)
```

#### 4.2 Settings Page Integration (Optional)

Add toggle in Settings to enable/disable AI suggestions:

```csharp
[ObservableProperty]
private bool _enableAiSuggestions = true;
```

---

## File Summary

### New Files to Create

| File | Purpose |
|------|---------|
| `ArgoBooks.Core/Services/IOpenAiService.cs` | Service interface |
| `ArgoBooks.Core/Services/OpenAiService.cs` | OpenAI API implementation |
| `ArgoBooks.Core/Models/AI/ReceiptAnalysisRequest.cs` | Request model |
| `ArgoBooks.Core/Models/AI/SupplierCategorySuggestion.cs` | Response model |

### Files to Modify

| File | Changes |
|------|---------|
| `ArgoBooks/ViewModels/ReceiptsModalsViewModel.cs` | Add AI suggestion logic |
| `ArgoBooks/Modals/ReceiptsModals.axaml` | Add UI for AI suggestions |
| `ArgoBooks/App.axaml.cs` | Register OpenAI service in DI |

---

## Cost Considerations

### OpenAI API Pricing (as of 2024)

| Model | Input | Output | Notes |
|-------|-------|--------|-------|
| gpt-4o-mini | $0.15/1M tokens | $0.60/1M tokens | Recommended - best cost/performance |
| gpt-4o | $2.50/1M tokens | $10.00/1M tokens | Higher accuracy, higher cost |
| gpt-3.5-turbo | $0.50/1M tokens | $1.50/1M tokens | Legacy option |

### Estimated Cost per Receipt Scan

- Average prompt: ~500-800 tokens (suppliers/categories list + receipt data)
- Average response: ~200-300 tokens
- **Cost per scan with gpt-4o-mini**: ~$0.0002-0.0003 (fractions of a cent)

---

## Alternative: Use Azure OpenAI

If you prefer to use Azure OpenAI (same API as OpenAI but hosted on Azure):

```env
AZURE_OPENAI_ENDPOINT=https://your-resource.openai.azure.com/
AZURE_OPENAI_API_KEY=your-api-key
AZURE_OPENAI_DEPLOYMENT=gpt-4o-mini
```

This would require minor modifications to the service to support Azure's API format.

---

## Testing Plan

1. **Unit Tests**
   - Test prompt generation with various receipt data
   - Test response parsing with mock API responses
   - Test error handling (API failures, invalid responses)

2. **Integration Tests**
   - Test with real API calls (development key)
   - Test supplier matching accuracy
   - Test category suggestion relevance

3. **User Acceptance Testing**
   - Verify UI shows suggestions correctly
   - Verify auto-create functionality works
   - Verify fallback behavior when AI is unavailable

---

## Rollout Strategy

1. **Phase 1**: Implement core service with supplier matching only
2. **Phase 2**: Add category suggestions
3. **Phase 3**: Add auto-create functionality
4. **Phase 4**: Add Settings toggle and user preferences

This phased approach allows for incremental testing and user feedback.
