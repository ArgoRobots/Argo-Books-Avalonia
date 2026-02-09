using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// OpenAI API service for AI-powered supplier and category suggestions.
/// Follows the same pattern as AIQueryTranslator from Argo-Books-WinForms.
/// </summary>
public class OpenAiService : IOpenAiService
{
    private const string ApiKeyEnvVar = "OPENAI_API_KEY";
    private const string ModelEnvVar = "OPENAI_MODEL";
    private const string DefaultModel = "gpt-4o-mini";
    private const string ApiEndpoint = "https://api.openai.com/v1/chat/completions";

    private readonly HttpClient _httpClient;
    private readonly IErrorLogger? _errorLogger;
    private readonly ITelemetryManager? _telemetryManager;
    private string? _lastApiKey;

    /// <summary>
    /// Creates a new instance of the OpenAI service.
    /// </summary>
    public OpenAiService(IErrorLogger? errorLogger = null, ITelemetryManager? telemetryManager = null)
    {
        DotEnv.Load();
        _httpClient = new HttpClient();
        _errorLogger = errorLogger;
        _telemetryManager = telemetryManager;
        ConfigureHttpClient();
    }

    /// <inheritdoc />
    public bool IsConfigured => DotEnv.HasValue(ApiKeyEnvVar);

    /// <inheritdoc />
    public async Task<SupplierCategorySuggestion?> GetSupplierCategorySuggestionAsync(
        ReceiptAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return null;

        // Reconfigure if API key changed
        var currentApiKey = DotEnv.Get(ApiKeyEnvVar);
        if (_lastApiKey != currentApiKey)
        {
            ConfigureHttpClient();
        }

        var stopwatch = Stopwatch.StartNew();
        var model = DotEnv.Get(ModelEnvVar);
        if (string.IsNullOrEmpty(model))
            model = DefaultModel;
        var success = false;

        try
        {
            var prompt = BuildPrompt(request);
            var response = await SendApiRequestAsync(prompt, cancellationToken);

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

    private void ConfigureHttpClient()
    {
        var apiKey = DotEnv.Get(ApiKeyEnvVar);
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _lastApiKey = apiKey;
        }
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
   - What the supplier typically sells
   - Line item descriptions if available
   - Common business expense categories
   - If no good match exists (confidence < 0.6), set shouldCreateNew=true and suggest an appropriate category

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

    private async Task<string?> SendApiRequestAsync(string prompt, CancellationToken cancellationToken)
    {
        var model = DotEnv.Get(ModelEnvVar);
        if (string.IsNullOrEmpty(model))
            model = DefaultModel;

        var requestBody = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant that categorizes business expenses. Always respond with valid JSON only, no markdown." },
                new { role = "user", content = prompt }
            },
            temperature = 0.3,
            max_tokens = 500
        };

        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(ApiEndpoint, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            System.Diagnostics.Debug.WriteLine($"OpenAI API error {response.StatusCode}: {errorBody}");
            return null;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(responseBody);

        var choices = doc.RootElement.GetProperty("choices");
        if (choices.GetArrayLength() > 0)
        {
            var messageContent = choices[0].GetProperty("message").GetProperty("content").GetString();
            return messageContent;
        }

        return null;
    }

    private static SupplierCategorySuggestion? ParseResponse(string response, ReceiptAnalysisRequest request)
    {
        try
        {
            // Clean up response - remove markdown code blocks if present
            var cleanResponse = response.Trim();
            if (cleanResponse.StartsWith("```"))
            {
                var startIndex = cleanResponse.IndexOf('\n') + 1;
                var endIndex = cleanResponse.LastIndexOf("```");
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

                    if (category.TryGetProperty("newName", out var newName) && newName.ValueKind != JsonValueKind.Null)
                    {
                        result.NewCategory.Name = newName.GetString() ?? "General";
                    }
                    else
                    {
                        result.NewCategory.Name = "General";
                    }

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
            System.Diagnostics.Debug.WriteLine($"Failed to parse OpenAI response: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Response was: {response}");
            return null;
        }
    }
}
