namespace ArgoBooks.Core.Services;

/// <summary>
/// Result of scanning a receipt image using AI/OCR.
/// </summary>
public class ReceiptScanResult
{
    /// <summary>
    /// Whether the scan was successful.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Error message if scan failed.
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Extracted supplier name.
    /// </summary>
    public string? SupplierName { get; set; }

    /// <summary>
    /// Extracted transaction date.
    /// </summary>
    public DateTime? TransactionDate { get; set; }

    /// <summary>
    /// Extracted subtotal (before tax).
    /// </summary>
    public decimal? Subtotal { get; set; }

    /// <summary>
    /// Extracted tax amount.
    /// </summary>
    public decimal? TaxAmount { get; set; }

    /// <summary>
    /// Extracted total amount.
    /// </summary>
    public decimal? TotalAmount { get; set; }

    /// <summary>
    /// Extracted line items from the receipt.
    /// </summary>
    public List<ScannedLineItem> LineItems { get; set; } = [];

    /// <summary>
    /// Overall confidence score (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// Raw extracted text from the receipt.
    /// </summary>
    public string? RawText { get; set; }

    /// <summary>
    /// Detected currency code (e.g., "USD", "EUR").
    /// </summary>
    public string? CurrencyCode { get; set; }

    /// <summary>
    /// Creates a failed result with an error message.
    /// </summary>
    public static ReceiptScanResult Failed(string errorMessage) => new()
    {
        IsSuccess = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// A line item extracted from a scanned receipt.
/// </summary>
public class ScannedLineItem
{
    /// <summary>
    /// Item description/name.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of items.
    /// </summary>
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit price per item.
    /// </summary>
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total price for this line item.
    /// </summary>
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Confidence score for this line item extraction (0.0 to 1.0).
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// Service interface for AI-powered receipt scanning.
/// </summary>
public interface IReceiptScannerService
{
    /// <summary>
    /// Whether the service is configured and ready to use.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Scans a receipt image and extracts data.
    /// </summary>
    /// <param name="imageData">The raw image bytes (JPEG, PNG, or PDF).</param>
    /// <param name="fileName">The original file name (used for MIME type detection).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scan result containing extracted data.</returns>
    Task<ReceiptScanResult> ScanReceiptAsync(byte[] imageData, string fileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Scans a receipt from a file path.
    /// </summary>
    /// <param name="filePath">Path to the receipt image file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The scan result containing extracted data.</returns>
    Task<ReceiptScanResult> ScanReceiptFromFileAsync(string filePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates that the service is properly configured.
    /// </summary>
    /// <returns>True if configured correctly, false otherwise.</returns>
    Task<bool> ValidateConfigurationAsync();
}
