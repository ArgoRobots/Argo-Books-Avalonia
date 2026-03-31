using System.Diagnostics;
using ArgoBooks.Core.Models.Telemetry;
using SkiaSharp;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Receipt scanning service using GPT-4o-mini vision through the argorobots.com proxy.
/// Replaces Azure Document Intelligence with better product name extraction and discount support.
/// </summary>
public class OpenAiReceiptScannerService(
    LicenseService? licenseService = null,
    IErrorLogger? errorLogger = null,
    ITelemetryManager? telemetryManager = null)
    : IReceiptScannerService
{
    private readonly OpenAiService _openAiService = new(errorLogger, telemetryManager);

    private const string SystemPrompt = @"You are a receipt data extraction system. You must extract EVERY item and ALL data from the receipt image into structured JSON. Be thorough — missing items is unacceptable.

Return JSON only (no markdown code blocks), with this exact format:
{
  ""supplierName"": ""Store or business name"",
  ""transactionDate"": ""YYYY-MM-DD"",
  ""subtotal"": 0.00,
  ""taxes"": [{""name"": ""GST"", ""amount"": 0.00}, {""name"": ""PST"", ""amount"": 0.00}],
  ""discounts"": [{""name"": ""Member Discount"", ""amount"": 0.00}],
  ""totalAmount"": 0.00,
  ""currencyCode"": ""USD"",
  ""paymentMethod"": ""Credit Card"",
  ""confidence"": 0.95,
  ""lineItems"": [
    {""description"": ""Product Name"", ""quantity"": 1, ""unitPrice"": 0.00, ""totalPrice"": 0.00, ""confidence"": 0.9}
  ]
}

Rules:
1. LINE ITEMS — Extract EVERY purchased item on the receipt. Scan the entire receipt top to bottom. Grocery receipts often have 20-40+ items — include ALL of them. Do not summarize or skip items. Each product line with a price is a line item. Return items in the same order they appear on the receipt.
2. TAX — Return EACH tax line separately in the ""taxes"" array. Do NOT sum them — list every individual tax with its label and amount. Common tax labels: GST, G-GST, PST, P-PST, HST, QST, TVQ, TPS, VAT, state tax, county tax, city tax, sales tax, excise tax. If there is only one tax line, still return it as a single-element array.
3. PRODUCT NAMES — Transcribe EXACTLY as printed on the receipt, character by character. Do NOT normalize, expand abbreviations, correct spelling, or rename items. Keep the original abbreviations and casing. If a character is hard to read, use your best guess but do not substitute a different word. Only remove SKU codes, barcodes, and internal item numbers that are clearly not part of the product name.
4. MONETARY VALUES — All as numbers. Use 0.00 for missing values, null for unknown fields.
5. CONFIDENCE — 0.0-1.0, your certainty of the overall extraction accuracy.
6. PRICES vs DISCOUNTS — When a product has two numbers near it (a price and a discount/savings below it), the product's line item should use the FULL PRICE (the larger, positive number), not the discounted price. The discount is a separate entry in the ""discounts"" array.
7. DISCOUNTS — ANY line on the receipt with a negative amount or a minus sign is a discount. This includes lines labeled ""Member Pricing"", ""Member Discount"", ""SAVE"", ""OFF"", ""DISCOUNT"", coupons, promos, loyalty savings, price reductions, markdowns, or any other negative adjustment. Return EACH one separately in the ""discounts"" array with the label and amount as a positive number. Do NOT include discounts as line items — they belong only in the ""discounts"" array. Do NOT skip or ignore negative amounts.
8. ERROR — If the image is not a receipt or is completely unreadable, return: {""error"": ""Not a valid receipt"", ""confidence"": 0.0}
9. DATE — YYYY-MM-DD format. Best guess if only partial date is visible.
10. CURRENCY — Infer the currency from location clues on the receipt: store address, city, province/state, country name, language, tax labels (e.g. GST/PST = CAD, VAT/TVA = EUR/GBP, IVA = EUR/MXN), and currency symbols ($ is ambiguous, £ = GBP, € = EUR, ¥ = JPY/CNY). Map the identified country to its ISO 4217 currency code. Default to ""USD"" only if there are genuinely no location or currency clues.
11. PAYMENT METHOD — One of ""Credit Card"", ""Debit Card"", ""Cash"", ""Check"", or null. ""MASTERCARD"", ""VISA"", ""AMEX"" = ""Credit Card"". ""INTERAC"", ""DEBIT"" = ""Debit Card"".
12. QUANTITY — Default to 1 if not shown. For weighted items (e.g. ""1.340 kg @ $1.92/kg""), use the weight as quantity and per-unit rate as unitPrice.
13. SUPPLIER - This is often the largest and boldest text on the receipt, and usually at the very top.
14. SPATIAL ALIGNMENT — Grocery receipts use a two-column layout: product name on the LEFT, its price on the RIGHT of the SAME row. Match each product name to the price that is horizontally aligned with it, NOT the price on the row above or below. Characters on the same printed line share the same vertical position even if there is a large horizontal gap between the name and the price. If a line has only a name with no price on its right, it is likely a description or category header — do not assign it a price from an adjacent row. IMPORTANT: The receipt photo may be tilted or at an angle. Mentally straighten the image first, then read each row. Two items at the same vertical position on a tilted receipt will appear at slightly different heights in the photo — follow the angle of the printed text lines, not strict horizontal.
15. CROSS-CHECK — After extracting all items, count the number of distinct price values visible on the right side of the receipt and compare to the number of line items you extracted. If you have fewer line items than prices, you missed an item — re-scan. Every price on the receipt must be accounted for as either a line item, a tax, a discount, or a total/subtotal.";

    /// <inheritdoc />
    public bool IsConfigured => licenseService?.GetLicenseKey() != null || licenseService?.GetDeviceId() != null;

    /// <inheritdoc />
    public async Task<ReceiptScanResult> ScanReceiptAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var success = false;

        try
        {
            var licenseKey = licenseService?.GetLicenseKey();
            var deviceId = licenseService?.GetDeviceId();
            if (string.IsNullOrEmpty(licenseKey) && string.IsNullOrEmpty(deviceId))
            {
                return ReceiptScanResult.Failed("No active license key or device ID found.");
            }

            // PDF is not supported by GPT vision
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            if (extension == ".pdf")
            {
                return ReceiptScanResult.Failed("PDF receipt scanning is not supported. Please convert to JPEG or PNG.");
            }

            // Re-encode non-JPEG formats (BMP/PNG/TIFF) as JPEG to reduce payload size
            // without any resolution or quality loss. Never downscale — preserve full resolution.
            if (extension is ".bmp" or ".tiff" or ".tif" or ".png")
            {
                imageData = ReencodeAsJpeg(imageData);
                fileName = Path.ChangeExtension(fileName, ".jpg");
            }

            // Validate file type
            var mimeType = ReceiptImageHelper.GetContentType(fileName);
            if (mimeType == null)
            {
                return ReceiptScanResult.Failed("Unsupported file type. Please use JPEG, PNG, or BMP files.");
            }

            // Convert to base64 for vision API
            var base64Image = Convert.ToBase64String(imageData);

            // Call GPT-4o vision for scanning on receipts
            var response = await _openAiService.SendVisionChatAsync(
                SystemPrompt,
                "Extract all data from this receipt image. Respond with JSON only.",
                base64Image,
                mimeType,
                maxTokens: 4000,
                temperature: 0.1,
                model: "gpt-4o",
                cancellationToken: cancellationToken);

            if (string.IsNullOrEmpty(response))
            {
                return ReceiptScanResult.Failed("No response from the AI service. Please try again.");
            }

            var result = ParseResponse(response);
            if (result.IsSuccess)
            {
                success = true;
                _ = telemetryManager?.TrackFeatureAsync(FeatureName.ReceiptScanned, cancellationToken: cancellationToken);
            }
            return result;
        }
        catch (TaskCanceledException)
        {
            return ReceiptScanResult.Failed("Scan operation was cancelled or timed out.");
        }
        catch (HttpRequestException ex)
        {
            errorLogger?.LogError(ex, ErrorCategory.Api, "Receipt scan network error");
            return ReceiptScanResult.Failed("Network error: unable to reach the scanning service. Please check your internet connection.");
        }
        catch (Exception ex)
        {
            errorLogger?.LogError(ex, ErrorCategory.Api, "Receipt scan failed");
            return ReceiptScanResult.Failed("Failed to scan receipt. Please try again.");
        }
        finally
        {
            stopwatch.Stop();
            _ = telemetryManager?.TrackApiCallAsync(
                ApiName.OpenAI,
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

    // IMPORTANT: Make this internal so tests can call it
    public static ReceiptScanResult ParseResponse(string response)
    {
        try
        {
            var cleanResponse = JsonResponseHelper.StripMarkdownCodeBlock(response);

            using var doc = JsonDocument.Parse(cleanResponse);
            var root = doc.RootElement;

            // Check for error response
            if (root.TryGetProperty("error", out var errorProp) && errorProp.ValueKind == JsonValueKind.String)
            {
                return ReceiptScanResult.Failed(errorProp.GetString() ?? "Scan failed");
            }

            var result = new ReceiptScanResult
            {
                IsSuccess = true,
                LineItems = []
            };

            if (root.TryGetProperty("supplierName", out var supplier) && supplier.ValueKind != JsonValueKind.Null)
                result.SupplierName = supplier.GetString();

            if (root.TryGetProperty("transactionDate", out var date) && date.ValueKind != JsonValueKind.Null)
            {
                if (DateTime.TryParse(date.GetString(), out var parsedDate))
                    result.TransactionDate = parsedDate;
            }

            if (root.TryGetProperty("subtotal", out var subtotal) && subtotal.ValueKind == JsonValueKind.Number)
                result.Subtotal = subtotal.GetDecimal();

            // Sum individual tax lines returned by the LLM
            if (root.TryGetProperty("taxes", out var taxes) && taxes.ValueKind == JsonValueKind.Array)
            {
                decimal taxTotal = 0;
                foreach (var taxLine in taxes.EnumerateArray())
                {
                    if (taxLine.TryGetProperty("amount", out var amt) && amt.ValueKind == JsonValueKind.Number)
                        taxTotal += amt.GetDecimal();
                }
                result.TaxAmount = taxTotal;
            }
            else if (root.TryGetProperty("taxAmount", out var tax) && tax.ValueKind == JsonValueKind.Number)
            {
                // Fallback for single taxAmount field
                result.TaxAmount = tax.GetDecimal();
            }

            if (root.TryGetProperty("totalAmount", out var total) && total.ValueKind == JsonValueKind.Number)
                result.TotalAmount = total.GetDecimal();

            // Sum individual discount lines returned by the LLM
            if (root.TryGetProperty("discounts", out var discounts) && discounts.ValueKind == JsonValueKind.Array)
            {
                decimal discountTotal = 0;
                foreach (var discountLine in discounts.EnumerateArray())
                {
                    if (discountLine.TryGetProperty("amount", out var amt) && amt.ValueKind == JsonValueKind.Number)
                        discountTotal += amt.GetDecimal();
                }
                result.Discount = discountTotal;
            }
            else if (root.TryGetProperty("discount", out var discount) && discount.ValueKind == JsonValueKind.Number)
            {
                // Fallback for single discount field
                result.Discount = discount.GetDecimal();
            }

            if (root.TryGetProperty("currencyCode", out var currency) && currency.ValueKind != JsonValueKind.Null)
                result.CurrencyCode = currency.GetString();

            if (root.TryGetProperty("confidence", out var confidence) && confidence.ValueKind == JsonValueKind.Number)
                result.Confidence = confidence.GetDouble();

            if (root.TryGetProperty("rawText", out var rawText) && rawText.ValueKind != JsonValueKind.Null)
                result.RawText = rawText.GetString();

            if (root.TryGetProperty("paymentMethod", out var paymentMethod) && paymentMethod.ValueKind != JsonValueKind.Null)
                result.PaymentMethod = paymentMethod.GetString();

            if (root.TryGetProperty("lineItems", out var lineItems) && lineItems.ValueKind == JsonValueKind.Array)
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

                    if (item.TryGetProperty("quantity", out var qty) && qty.ValueKind == JsonValueKind.Number)
                        lineItem.Quantity = qty.GetDecimal();

                    if (item.TryGetProperty("unitPrice", out var unitPrice) && unitPrice.ValueKind == JsonValueKind.Number)
                    {
                        lineItem.UnitPrice = unitPrice.GetDecimal();
                        hasData = true;
                    }

                    if (item.TryGetProperty("totalPrice", out var totalPrice) && totalPrice.ValueKind == JsonValueKind.Number)
                    {
                        lineItem.TotalPrice = totalPrice.GetDecimal();
                        hasData = true;
                    }

                    if (item.TryGetProperty("confidence", out var itemConf) && itemConf.ValueKind == JsonValueKind.Number)
                        lineItem.Confidence = itemConf.GetDouble();

                    if (hasData)
                    {
                        // Negative line items are discounts — add to discount total, not line items
                        if (lineItem.TotalPrice < 0)
                        {
                            result.Discount = (result.Discount ?? 0) + Math.Abs(lineItem.TotalPrice);
                        }
                        else
                        {
                            result.LineItems.Add(lineItem);
                        }
                    }
                }
            }

            return result;
        }
        catch (JsonException ex)
        {
            return ReceiptScanResult.Failed($"Failed to parse AI response: {ex.Message}");
        }
        catch (Exception)
        {
            return ReceiptScanResult.Failed("Failed to process the scan result.");
        }
    }

    /// <summary>
    /// Re-encodes an image as high-quality JPEG at full resolution.
    /// Used to convert BMP/PNG/TIFF to a smaller format without any quality or resolution loss.
    /// </summary>
    private static byte[] ReencodeAsJpeg(byte[] imageData)
    {
        using var original = SKBitmap.Decode(imageData);
        if (original == null)
            return imageData;

        return ReceiptImageHelper.EncodeAsJpeg(original, 100);
    }
}
