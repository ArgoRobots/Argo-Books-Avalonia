using System.Diagnostics;
using System.Net.Http.Headers;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Receipt scanning service that proxies requests through the argorobots.com server.
/// The server handles communication with Azure Document Intelligence.
/// </summary>
public class AzureReceiptScannerService : IReceiptScannerService
{
    private const string ScanEndpoint = "https://argorobots.com/api/receipt/scan.php";

    private readonly HttpClient _httpClient;
    private readonly LicenseService? _licenseService;
    private readonly IErrorLogger? _errorLogger;
    private readonly ITelemetryManager? _telemetryManager;

    /// <summary>
    /// Creates a new instance of the receipt scanner service.
    /// </summary>
    public AzureReceiptScannerService(LicenseService? licenseService = null, IErrorLogger? errorLogger = null, ITelemetryManager? telemetryManager = null)
    {
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(120) }; // Long timeout for Azure processing
        _licenseService = licenseService;
        _errorLogger = errorLogger;
        _telemetryManager = telemetryManager;
    }

    /// <inheritdoc />
    public bool IsConfigured => _licenseService?.GetLicenseKey() != null;

    /// <inheritdoc />
    public async Task<ReceiptScanResult> ScanReceiptAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var licenseKey = _licenseService?.GetLicenseKey();
            if (string.IsNullOrEmpty(licenseKey))
            {
                return ReceiptScanResult.Failed("No active license key found. Please activate your premium subscription.");
            }

            // Validate file size (4MB limit)
            const int maxFileSizeBytes = 4 * 1024 * 1024;
            if (imageData.Length > maxFileSizeBytes)
            {
                return ReceiptScanResult.Failed("File size exceeds the 4MB limit. Please use a smaller image.");
            }

            // Validate file type
            var contentType = GetContentType(fileName);
            if (contentType == null)
            {
                return ReceiptScanResult.Failed("Unsupported file type. Please use JPEG, PNG, or PDF files.");
            }

            // Send image to server proxy
            using var content = new MultipartFormDataContent();
            var imageContent = new ByteArrayContent(imageData);
            imageContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(imageContent, "image", fileName);

            using var request = new HttpRequestMessage(HttpMethod.Post, ScanEndpoint);
            request.Content = content;
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", licenseKey);
            request.Headers.Add("X-License-Key", licenseKey);

            var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _errorLogger?.LogError($"Receipt scan proxy error {response.StatusCode}", ErrorCategory.Api, "Receipt scan");

                if ((int)response.StatusCode == 429)
                    return ReceiptScanResult.Failed("Rate limit exceeded. Please try again later.");
                if ((int)response.StatusCode == 413)
                    return ReceiptScanResult.Failed("File too large for the scanning service.");

                return ReceiptScanResult.Failed("An error occurred communicating with the receipt scanning service. Please try again.");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            var scanResponse = JsonSerializer.Deserialize<JsonElement>(responseBody);

            if (!scanResponse.TryGetProperty("success", out var successProp) || !successProp.GetBoolean())
            {
                var message = scanResponse.TryGetProperty("message", out var msgProp) ? msgProp.GetString() : "Scan failed";
                return ReceiptScanResult.Failed(message ?? "Scan failed");
            }

            success = true;
            _ = _telemetryManager?.TrackFeatureAsync(FeatureName.ReceiptScanned, cancellationToken: cancellationToken);
            return ParseProxyResponse(scanResponse);
        }
        catch (TaskCanceledException)
        {
            return ReceiptScanResult.Failed("Scan operation was cancelled or timed out.");
        }
        catch (HttpRequestException ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "Receipt scan network error");
            var innerMsg = ex.InnerException?.Message;
            var fullMsg = innerMsg != null ? $"Network error: {ex.Message} ({innerMsg})" : $"Network error: {ex.Message}";
            return ReceiptScanResult.Failed(fullMsg);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Api, "Receipt scan failed");
            return ReceiptScanResult.Failed($"Failed to scan receipt: {ex.Message}");
        }
        finally
        {
            stopwatch.Stop();
            _ = _telemetryManager?.TrackApiCallAsync(
                ApiName.AzureDocumentIntelligence,
                stopwatch.ElapsedMilliseconds,
                success,
                cancellationToken: cancellationToken);
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
    public Task<bool> ValidateConfigurationAsync()
    {
        return Task.FromResult(IsConfigured);
    }

    private static ReceiptScanResult ParseProxyResponse(JsonElement response)
    {
        var result = new ReceiptScanResult
        {
            IsSuccess = true,
            LineItems = [],
        };

        if (response.TryGetProperty("supplierName", out var supplier) && supplier.ValueKind != JsonValueKind.Null)
            result.SupplierName = supplier.GetString();

        if (response.TryGetProperty("transactionDate", out var date) && date.ValueKind != JsonValueKind.Null)
        {
            if (DateTime.TryParse(date.GetString(), out var parsedDate))
                result.TransactionDate = parsedDate;
        }

        if (response.TryGetProperty("subtotal", out var subtotal) && subtotal.ValueKind != JsonValueKind.Null)
            result.Subtotal = subtotal.GetDecimal();

        if (response.TryGetProperty("total", out var total) && total.ValueKind != JsonValueKind.Null)
            result.TotalAmount = total.GetDecimal();

        if (response.TryGetProperty("tax", out var tax) && tax.ValueKind != JsonValueKind.Null)
            result.TaxAmount = tax.GetDecimal();

        if (response.TryGetProperty("currency", out var currency) && currency.ValueKind != JsonValueKind.Null)
            result.CurrencyCode = currency.GetString();

        if (response.TryGetProperty("confidence", out var confidence) && confidence.ValueKind != JsonValueKind.Null)
            result.Confidence = confidence.GetDouble();

        if (response.TryGetProperty("rawText", out var rawText) && rawText.ValueKind != JsonValueKind.Null)
            result.RawText = rawText.GetString();

        if (response.TryGetProperty("lineItems", out var lineItems) && lineItems.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in lineItems.EnumerateArray())
            {
                var lineItem = new ScannedLineItem();
                var hasData = false;

                if (item.TryGetProperty("description", out var desc) && desc.ValueKind != JsonValueKind.Null)
                {
                    lineItem.Description = desc.GetString() ?? "";
                    hasData = true;
                }

                if (item.TryGetProperty("quantity", out var qty) && qty.ValueKind != JsonValueKind.Null)
                    lineItem.Quantity = qty.GetDecimal();

                if (item.TryGetProperty("unitPrice", out var unitPrice) && unitPrice.ValueKind != JsonValueKind.Null)
                {
                    lineItem.UnitPrice = unitPrice.GetDecimal();
                    hasData = true;
                }

                if (item.TryGetProperty("totalPrice", out var totalPrice) && totalPrice.ValueKind != JsonValueKind.Null)
                {
                    lineItem.TotalPrice = totalPrice.GetDecimal();
                    hasData = true;
                }

                if (item.TryGetProperty("confidence", out var itemConf) && itemConf.ValueKind != JsonValueKind.Null)
                    lineItem.Confidence = itemConf.GetDouble();

                if (hasData)
                    result.LineItems.Add(lineItem);
            }
        }

        return result;
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
}
