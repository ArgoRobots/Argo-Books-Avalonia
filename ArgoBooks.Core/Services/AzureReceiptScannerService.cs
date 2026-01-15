using Azure;
using Azure.AI.FormRecognizer.DocumentAnalysis;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Azure Document Intelligence implementation of receipt scanning.
/// Uses the prebuilt receipt model for high-accuracy extraction.
/// Credentials are loaded from .env file (AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT and AZURE_DOCUMENT_INTELLIGENCE_API_KEY).
/// </summary>
public class AzureReceiptScannerService : IReceiptScannerService
{
    private const string EndpointEnvVar = "AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT";
    private const string ApiKeyEnvVar = "AZURE_DOCUMENT_INTELLIGENCE_API_KEY";

    private DocumentAnalysisClient? _client;
    private string? _lastEndpoint;
    private string? _lastApiKey;

    /// <summary>
    /// Creates a new instance of the Azure receipt scanner service.
    /// Credentials are loaded from .env file.
    /// </summary>
    public AzureReceiptScannerService()
    {
        // Ensure .env file is loaded
        DotEnv.Load();
    }

    /// <inheritdoc />
    public bool IsConfigured
    {
        get
        {
            return DotEnv.HasValue(EndpointEnvVar) && DotEnv.HasValue(ApiKeyEnvVar);
        }
    }

    /// <inheritdoc />
    public async Task<ReceiptScanResult> ScanReceiptAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = GetOrCreateClient();
            if (client == null)
            {
                return ReceiptScanResult.Failed("Azure Document Intelligence is not configured. Please add AZURE_DOCUMENT_INTELLIGENCE_ENDPOINT and AZURE_DOCUMENT_INTELLIGENCE_API_KEY to your .env file.");
            }

            // Validate file size (4MB limit for free tier)
            const int maxFileSizeBytes = 4 * 1024 * 1024;
            if (imageData.Length > maxFileSizeBytes)
            {
                return ReceiptScanResult.Failed($"File size exceeds the 4MB limit. Please use a smaller image.");
            }

            // Validate file type
            var contentType = GetContentType(fileName);
            if (contentType == null)
            {
                return ReceiptScanResult.Failed("Unsupported file type. Please use JPEG, PNG, or PDF files.");
            }

            // Analyze the receipt using Azure's prebuilt receipt model
            using var stream = new MemoryStream(imageData);
            var operation = await client.AnalyzeDocumentAsync(
                WaitUntil.Completed,
                "prebuilt-receipt",
                stream,
                cancellationToken: cancellationToken);

            var result = operation.Value;
            return ParseAnalyzeResult(result);
        }
        catch (RequestFailedException ex) when (ex.Status == 401)
        {
            return ReceiptScanResult.Failed("Invalid Azure API key. Please check your AZURE_DOCUMENT_INTELLIGENCE_API_KEY in .env file.");
        }
        catch (RequestFailedException ex) when (ex.Status == 403)
        {
            return ReceiptScanResult.Failed("Azure API access denied. Please verify your subscription and endpoint in .env file.");
        }
        catch (RequestFailedException ex) when (ex.Status == 429)
        {
            return ReceiptScanResult.Failed("Azure API rate limit exceeded. Please try again later or upgrade your plan.");
        }
        catch (RequestFailedException ex)
        {
            return ReceiptScanResult.Failed($"Azure API error: {ex.Message}");
        }
        catch (TaskCanceledException)
        {
            return ReceiptScanResult.Failed("Scan operation was cancelled.");
        }
        catch (Exception ex)
        {
            return ReceiptScanResult.Failed($"Failed to scan receipt: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<ReceiptScanResult> ScanReceiptFromFileAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                return ReceiptScanResult.Failed("File not found.");
            }

            var imageData = await File.ReadAllBytesAsync(filePath, cancellationToken);
            var fileName = Path.GetFileName(filePath);
            return await ScanReceiptAsync(imageData, fileName, cancellationToken);
        }
        catch (IOException ex)
        {
            return ReceiptScanResult.Failed($"Failed to read file: {ex.Message}");
        }
    }

    /// <inheritdoc />
    public async Task<bool> ValidateConfigurationAsync()
    {
        if (!IsConfigured)
            return false;

        try
        {
            var client = GetOrCreateClient();
            if (client == null)
                return false;

            // Try to get service info to validate credentials
            // We'll do a minimal request to check if the API is accessible
            // Note: There's no direct "ping" endpoint, so we just verify the client was created
            await Task.CompletedTask;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private DocumentAnalysisClient? GetOrCreateClient()
    {
        var envEndpoint = DotEnv.Get(EndpointEnvVar);
        var envApiKey = DotEnv.Get(ApiKeyEnvVar);

        if (string.IsNullOrWhiteSpace(envEndpoint) || string.IsNullOrWhiteSpace(envApiKey))
        {
            return null;
        }

        // Recreate client if settings changed
        if (_client == null || _lastEndpoint != envEndpoint || _lastApiKey != envApiKey)
        {
            var endpoint = envEndpoint.TrimEnd('/');
            if (!endpoint.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                endpoint = "https://" + endpoint;
            }

            _client = new DocumentAnalysisClient(new Uri(endpoint), new AzureKeyCredential(envApiKey));
            _lastEndpoint = envEndpoint;
            _lastApiKey = envApiKey;
        }

        return _client;
    }

    private static ReceiptScanResult ParseAnalyzeResult(AnalyzeResult result)
    {
        var scanResult = new ReceiptScanResult
        {
            IsSuccess = true,
            LineItems = [],
            RawText = result.Content
        };

        // Calculate overall confidence from document confidence
        var confidenceScores = new List<double>();

        foreach (var document in result.Documents)
        {
            confidenceScores.Add(document.Confidence);

            foreach (var field in document.Fields)
            {
                var fieldName = field.Key;
                var fieldValue = field.Value;

                switch (fieldName)
                {
                    case "MerchantName":
                        scanResult.VendorName = fieldValue.Content;
                        break;

                    case "TransactionDate":
                        if (fieldValue.FieldType == DocumentFieldType.Date && fieldValue.Value.AsDate() is { } date)
                        {
                            scanResult.TransactionDate = date.DateTime;
                        }
                        break;

                    case "Subtotal":
                        scanResult.Subtotal = ExtractDecimalValue(fieldValue, out var subtotalCurrency);
                        scanResult.CurrencyCode ??= subtotalCurrency;
                        break;

                    case "TotalTax":
                    case "Tax":
                        scanResult.TaxAmount = ExtractDecimalValue(fieldValue, out _);
                        break;

                    case "Total":
                    case "Tip":
                        if (fieldName == "Tip")
                        {
                            // Skip tips, we want the actual total
                            break;
                        }
                        scanResult.TotalAmount = ExtractDecimalValue(fieldValue, out var totalCurrency);
                        scanResult.CurrencyCode ??= totalCurrency;
                        break;

                    case "Items":
                        if (fieldValue.FieldType == DocumentFieldType.List)
                        {
                            foreach (var item in fieldValue.Value.AsList())
                            {
                                if (item.FieldType == DocumentFieldType.Dictionary)
                                {
                                    var lineItem = ParseLineItem(item.Value.AsDictionary());
                                    if (lineItem != null)
                                    {
                                        scanResult.LineItems.Add(lineItem);
                                    }
                                }
                            }
                        }
                        break;
                }
            }
        }

        // Calculate average confidence
        scanResult.Confidence = confidenceScores.Count > 0
            ? confidenceScores.Average()
            : 0.5;

        // If we didn't get a subtotal but have total and tax, calculate it
        if (scanResult.Subtotal == null && scanResult.TotalAmount != null && scanResult.TaxAmount != null)
        {
            scanResult.Subtotal = scanResult.TotalAmount - scanResult.TaxAmount;
        }

        // If we didn't get a total but have line items, calculate it
        if (scanResult.TotalAmount == null && scanResult.LineItems.Count > 0)
        {
            scanResult.TotalAmount = scanResult.LineItems.Sum(li => li.TotalPrice);
        }

        return scanResult;
    }

    private static ScannedLineItem? ParseLineItem(IReadOnlyDictionary<string, DocumentField> itemFields)
    {
        var lineItem = new ScannedLineItem();
        var hasData = false;
        var confidenceScores = new List<double>();

        foreach (var field in itemFields)
        {
            switch (field.Key)
            {
                case "Description":
                    lineItem.Description = field.Value.Content ?? string.Empty;
                    hasData = true;
                    if (field.Value.Confidence.HasValue)
                        confidenceScores.Add(field.Value.Confidence.Value);
                    break;

                case "Quantity":
                    var qtyValue = ExtractDecimalValue(field.Value, out _);
                    if (qtyValue.HasValue)
                    {
                        lineItem.Quantity = qtyValue.Value;
                        if (field.Value.Confidence.HasValue)
                            confidenceScores.Add(field.Value.Confidence.Value);
                    }
                    break;

                case "Price":
                case "UnitPrice":
                    var priceValue = ExtractDecimalValue(field.Value, out _);
                    if (priceValue.HasValue)
                    {
                        lineItem.UnitPrice = priceValue.Value;
                        hasData = true;
                        if (field.Value.Confidence.HasValue)
                            confidenceScores.Add(field.Value.Confidence.Value);
                    }
                    break;

                case "TotalPrice":
                case "Amount":
                    var totalPriceValue = ExtractDecimalValue(field.Value, out _);
                    if (totalPriceValue.HasValue)
                    {
                        lineItem.TotalPrice = totalPriceValue.Value;
                        hasData = true;
                        if (field.Value.Confidence.HasValue)
                            confidenceScores.Add(field.Value.Confidence.Value);
                    }
                    break;
            }
        }

        if (!hasData)
            return null;

        // Calculate total price if not provided
        if (lineItem.TotalPrice == 0 && lineItem.UnitPrice > 0)
        {
            lineItem.TotalPrice = lineItem.UnitPrice * lineItem.Quantity;
        }

        // Calculate unit price if only total was provided
        if (lineItem.UnitPrice == 0 && lineItem.TotalPrice > 0 && lineItem.Quantity > 0)
        {
            lineItem.UnitPrice = lineItem.TotalPrice / lineItem.Quantity;
        }

        lineItem.Confidence = confidenceScores.Count > 0 ? confidenceScores.Average() : 0.5;

        return lineItem;
    }

    private static string? GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".pdf" => "application/pdf",
            ".bmp" => "image/bmp",
            ".tiff" or ".tif" => "image/tiff",
            _ => null
        };
    }

    /// <summary>
    /// Extracts a decimal value from a document field, trying multiple approaches.
    /// </summary>
    private static decimal? ExtractDecimalValue(DocumentField field, out string? currencyCode)
    {
        currencyCode = null;

        // Try Currency type first
        if (field.FieldType == DocumentFieldType.Currency)
        {
            try
            {
                var currency = field.Value.AsCurrency();
                currencyCode = currency.Code;
                return (decimal)currency.Amount;
            }
            catch
            {
                // Fall through to other methods
            }
        }

        // Try Double type
        if (field.FieldType == DocumentFieldType.Double)
        {
            try
            {
                return (decimal)field.Value.AsDouble();
            }
            catch
            {
                // Fall through to content parsing
            }
        }

        // Try Int64 type
        if (field.FieldType == DocumentFieldType.Int64)
        {
            try
            {
                return field.Value.AsInt64();
            }
            catch
            {
                // Fall through to content parsing
            }
        }

        // Try parsing from Content string as last resort
        if (!string.IsNullOrWhiteSpace(field.Content))
        {
            // Remove currency symbols and parse
            var content = field.Content
                .Replace("$", "")
                .Replace("€", "")
                .Replace("£", "")
                .Replace("¥", "")
                .Replace(",", "")
                .Trim();

            if (decimal.TryParse(content, out var parsed))
            {
                return parsed;
            }
        }

        return null;
    }
}

