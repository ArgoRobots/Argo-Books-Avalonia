using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using ArgoBooks.Core.Models.AI;

using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// OpenAI API service for AI-powered supplier and category suggestions.
/// Routes requests through the argorobots.com server proxy.
/// </summary>
public class OpenAiService : IOpenAiService
{
    private const string DefaultModel = "gpt-4o-mini";
    private static readonly string ApiEndpoint = $"{ApiConfig.BaseUrl}/api/ai/completions.php";

    private readonly HttpClient _httpClient;
    private readonly IErrorLogger? _errorLogger;
    private readonly ITelemetryManager? _telemetryManager;

    /// <summary>
    /// Creates a new instance of the OpenAI service.
    /// </summary>
    public OpenAiService(IErrorLogger? errorLogger = null, ITelemetryManager? telemetryManager = null)
    {
        _httpClient = new HttpClient();
        _errorLogger = errorLogger;
        _telemetryManager = telemetryManager;
        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public bool IsConfigured => LicenseAuthHelper.IsConfigured;

    /// <inheritdoc />
    public async Task<SupplierCategorySuggestion?> GetSupplierCategorySuggestionAsync(
        ReceiptAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        var stopwatch = Stopwatch.StartNew();
        var model = DefaultModel;
        var success = false;

        try
        {
            var prompt = BuildPrompt(request);
            var response = await SendApiRequestAsync(
                "You are a helpful assistant that categorizes business expenses. Always respond with valid JSON only, no markdown.",
                prompt,
                500,
                0.3,
                cancellationToken);

            if (string.IsNullOrEmpty(response))
                return null;

            success = true;
            return ParseResponse(response, request);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "OpenAI API call failed");
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _ = _telemetryManager?.TrackApiCallAsync(
                ApiName.OpenAI,
                stopwatch.ElapsedMilliseconds,
                success,
                model,
                cancellationToken: cancellationToken);
        }
    }

    /// <inheritdoc />
    public async Task<string?> SendChatAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens = 4000,
        double temperature = 0.1,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        var stopwatch = Stopwatch.StartNew();
        var model = DefaultModel;
        var success = false;

        try
        {
            var response = await SendApiRequestAsync(systemPrompt, userPrompt, maxTokens, temperature, cancellationToken);
            if (!string.IsNullOrEmpty(response))
                success = true;
            return response;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "OpenAI API call failed");
            return null;
        }
        finally
        {
            stopwatch.Stop();
            _ = _telemetryManager?.TrackApiCallAsync(
                ApiName.OpenAI,
                stopwatch.ElapsedMilliseconds,
                success,
                model,
                cancellationToken: cancellationToken);
        }
    }

    private void ConfigureHttpClient()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    private static string BuildPrompt(ReceiptAnalysisRequest request)
    {
        var suppliersJson = JsonSerializer.Serialize(request.ExistingSuppliers.Select(s => new { s.Id, s.Name }));
        var categoriesJson = JsonSerializer.Serialize(request.ExistingCategories.Select(c => new { c.Id, c.Name, c.Description }));
        var lineItemsText = request.LineItemDescriptions.Count > 0
            ? string.Join(", ", request.LineItemDescriptions.Take(10))
            : "N/A";

        return $@"You are an AI assistant helping categorize business expenses. Analyze the receipt data and suggest the best matching supplier and category.

## Receipt Data
- Supplier Name: ""{request.SupplierName}""
- Line Items: {lineItemsText}
- Total Amount: {request.TotalAmount:F2}

## Existing Suppliers
{suppliersJson}

## Existing Categories (Expense type)
{categoriesJson}

## Instructions
1. SUPPLIER: Find the best matching supplier from the existing list. Consider:
   - Exact name matches (highest confidence)
   - Partial matches (e.g., ""Walmart"" matches ""Walmart Inc."")
   - Common abbreviations and variations
   - If no good match exists (confidence < 0.6), set shouldCreateNew=true and suggest a clean supplier name

2. CATEGORY: Find the best matching category based on:
   - Line item descriptions (most important — use these to determine what was actually purchased)
   - What the supplier typically sells
   - Common business expense categories
   - If no good match exists (confidence < 0.6), set shouldCreateNew=true and suggest a SPECIFIC category name
   - IMPORTANT: Be specific! Use descriptive names based on the actual items (e.g., ""Groceries"", ""Cooking Ingredients"", ""Office Supplies"", ""Cleaning Products""). NEVER use vague or generic names like ""Purchases"", ""General"", ""General Expenses"", ""Miscellaneous"", ""Expenses"", or any combination of these words

## Response Format (JSON only, no markdown code blocks)
{{
  ""supplier"": {{
    ""matchedId"": ""<supplier-id or null>"",
    ""matchedName"": ""<supplier-name or null>"",
    ""confidence"": <0.0-1.0>,
    ""shouldCreateNew"": <true/false>,
    ""newName"": ""<suggested name if shouldCreateNew>"",
    ""newNotes"": ""<optional notes>""
  }},
  ""category"": {{
    ""matchedId"": ""<category-id or null>"",
    ""matchedName"": ""<category-name or null>"",
    ""confidence"": <0.0-1.0>,
    ""shouldCreateNew"": <true/false>,
    ""newName"": ""<suggested name if shouldCreateNew>"",
    ""newDescription"": ""<optional description>"",
    ""newItemType"": ""Product""
  }}
}}

Respond with JSON only.";
    }

    private async Task<string?> SendApiRequestAsync(
        string systemPrompt,
        string userPrompt,
        int maxTokens = 500,
        double temperature = 0.3,
        CancellationToken cancellationToken = default)
    {
        var requestBody = new
        {
            systemPrompt,
            userPrompt,
            model = DefaultModel,
            maxTokens,
            temperature
        };

        var json = JsonSerializer.Serialize(requestBody);

        using var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoint);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        LicenseAuthHelper.AddAuthHeaders(request);

        var response = await _httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _errorLogger?.LogError($"AI proxy error {response.StatusCode}", ErrorCategory.Api, "AI chat completion");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);
        var root = doc.RootElement;

        if (root.TryGetProperty("success", out var successProp) && successProp.GetBoolean()
            && root.TryGetProperty("content", out var contentProp))
        {
            return contentProp.GetString();
        }

        return null;
    }

    private SupplierCategorySuggestion? ParseResponse(string response, ReceiptAnalysisRequest request)
    {
        try
        {
            // Clean up response - remove markdown code blocks if present
            var cleanResponse = response.Trim();
            if (cleanResponse.StartsWith("```"))
            {
                var startIndex = cleanResponse.IndexOf('\n') + 1;
                var endIndex = cleanResponse.LastIndexOf("```", StringComparison.Ordinal);
                if (endIndex > startIndex)
                {
                    cleanResponse = cleanResponse[startIndex..endIndex].Trim();
                }
            }

            using var doc = JsonDocument.Parse(cleanResponse);
            var root = doc.RootElement;

            var result = new SupplierCategorySuggestion();

            // Parse supplier
            if (root.TryGetProperty("supplier", out var supplier))
            {
                if (supplier.TryGetProperty("matchedId", out var matchedId) && matchedId.ValueKind != JsonValueKind.Null)
                {
                    result.MatchedSupplierId = matchedId.GetString();
                }

                if (supplier.TryGetProperty("matchedName", out var matchedName) && matchedName.ValueKind != JsonValueKind.Null)
                {
                    result.MatchedSupplierName = matchedName.GetString();
                }

                if (supplier.TryGetProperty("confidence", out var confidence))
                {
                    result.SupplierConfidence = confidence.GetDouble();
                }

                if (supplier.TryGetProperty("shouldCreateNew", out var shouldCreateNew))
                {
                    result.ShouldCreateNewSupplier = shouldCreateNew.GetBoolean();
                }

                if (result.ShouldCreateNewSupplier)
                {
                    result.NewSupplier = new NewSupplierSuggestion();

                    if (supplier.TryGetProperty("newName", out var newName) && newName.ValueKind != JsonValueKind.Null)
                    {
                        result.NewSupplier.Name = newName.GetString() ?? request.SupplierName;
                    }
                    else
                    {
                        result.NewSupplier.Name = request.SupplierName;
                    }

                    if (supplier.TryGetProperty("newNotes", out var newNotes) && newNotes.ValueKind != JsonValueKind.Null)
                    {
                        result.NewSupplier.Notes = newNotes.GetString();
                    }
                }
            }

            // Parse category
            if (root.TryGetProperty("category", out var category))
            {
                if (category.TryGetProperty("matchedId", out var matchedId) && matchedId.ValueKind != JsonValueKind.Null)
                {
                    result.MatchedCategoryId = matchedId.GetString();
                }

                if (category.TryGetProperty("matchedName", out var matchedName) && matchedName.ValueKind != JsonValueKind.Null)
                {
                    result.MatchedCategoryName = matchedName.GetString();
                }

                if (category.TryGetProperty("confidence", out var confidence))
                {
                    result.CategoryConfidence = confidence.GetDouble();
                }

                if (category.TryGetProperty("shouldCreateNew", out var shouldCreateNew))
                {
                    result.ShouldCreateNewCategory = shouldCreateNew.GetBoolean();
                }

                if (result.ShouldCreateNewCategory)
                {
                    result.NewCategory = new NewCategorySuggestion();

                    var suggestedName = "General";
                    if (category.TryGetProperty("newName", out var newName) && newName.ValueKind != JsonValueKind.Null)
                    {
                        suggestedName = newName.GetString() ?? "General";
                    }

                    // Reject vague category names — the AI sometimes suggests these
                    var vagueName = IsVagueCategoryName(suggestedName);
                    if (vagueName && category.TryGetProperty("newDescription", out var descFallback)
                        && descFallback.ValueKind != JsonValueKind.Null
                        && !string.IsNullOrWhiteSpace(descFallback.GetString()))
                    {
                        // Use the description as the name if it's more specific
                        var desc = descFallback.GetString()!;
                        if (!IsVagueCategoryName(desc) && desc.Length <= 40)
                            suggestedName = desc;
                    }

                    result.NewCategory.Name = suggestedName;

                    if (category.TryGetProperty("newDescription", out var newDesc) && newDesc.ValueKind != JsonValueKind.Null)
                    {
                        result.NewCategory.Description = newDesc.GetString();
                    }

                    if (category.TryGetProperty("newItemType", out var itemType) && itemType.ValueKind != JsonValueKind.Null)
                    {
                        result.NewCategory.ItemType = itemType.GetString() ?? "Product";
                    }
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Parsing, "Failed to parse OpenAI response");
            return null;
        }
    }

    private static bool IsVagueCategoryName(string name)
    {
        var normalized = name.Trim().ToLowerInvariant();
        var vagueExact = new[] { "purchases", "general", "miscellaneous", "expenses", "other", "various", "items", "goods" };
        if (vagueExact.Contains(normalized))
            return true;

        // Catch compound vague names like "general expenses", "other purchases", "miscellaneous items"
        var words = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length >= 1 && words.All(w => vagueExact.Contains(w));
    }
}
