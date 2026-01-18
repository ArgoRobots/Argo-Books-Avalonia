namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Data extracted from a receipt via OCR (Optical Character Recognition).
/// </summary>
public class OcrData
{
    /// <summary>
    /// Supplier name extracted from receipt.
    /// </summary>
    [JsonPropertyName("extractedSupplier")]
    public string? ExtractedSupplier { get; set; }

    /// <summary>
    /// Date extracted from receipt.
    /// </summary>
    [JsonPropertyName("extractedDate")]
    public DateTime? ExtractedDate { get; set; }

    /// <summary>
    /// Total amount extracted from receipt.
    /// </summary>
    [JsonPropertyName("extractedAmount")]
    public decimal? ExtractedAmount { get; set; }

    /// <summary>
    /// Subtotal (before tax) extracted from receipt.
    /// </summary>
    [JsonPropertyName("extractedSubtotal")]
    public decimal? ExtractedSubtotal { get; set; }

    /// <summary>
    /// Tax amount extracted from receipt.
    /// </summary>
    [JsonPropertyName("extractedTaxAmount")]
    public decimal? ExtractedTaxAmount { get; set; }

    /// <summary>
    /// Currency code extracted from receipt (e.g., "USD", "EUR").
    /// </summary>
    [JsonPropertyName("extractedCurrency")]
    public string? ExtractedCurrency { get; set; }

    /// <summary>
    /// Individual items extracted from receipt (legacy: simple string list).
    /// </summary>
    [JsonPropertyName("extractedItems")]
    public List<string> ExtractedItems { get; set; } = [];

    /// <summary>
    /// Detailed line items extracted from receipt with quantity, price, etc.
    /// </summary>
    [JsonPropertyName("lineItems")]
    public List<OcrLineItem> LineItems { get; set; } = [];

    /// <summary>
    /// Confidence score of the OCR extraction (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    /// <summary>
    /// Raw text extracted from the receipt.
    /// </summary>
    [JsonPropertyName("rawText")]
    public string? RawText { get; set; }
}

/// <summary>
/// A line item extracted from a scanned receipt.
/// </summary>
public class OcrLineItem
{
    /// <summary>
    /// Item description/name.
    /// </summary>
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Quantity of items.
    /// </summary>
    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; } = 1;

    /// <summary>
    /// Unit price per item.
    /// </summary>
    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    /// <summary>
    /// Total price for this line item.
    /// </summary>
    [JsonPropertyName("totalPrice")]
    public decimal TotalPrice { get; set; }

    /// <summary>
    /// Confidence score for this line item extraction (0.0 to 1.0).
    /// </summary>
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
}
