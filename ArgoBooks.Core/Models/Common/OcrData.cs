
namespace ArgoBooks.Core.Models.Common;

/// <summary>
/// Data extracted from a receipt via OCR (Optical Character Recognition).
/// </summary>
public class OcrData
{
    /// <summary>
    /// Vendor/merchant name extracted from receipt.
    /// </summary>
    [JsonPropertyName("extractedVendor")]
    public string? ExtractedVendor { get; set; }

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
    /// Individual items extracted from receipt.
    /// </summary>
    [JsonPropertyName("extractedItems")]
    public List<string> ExtractedItems { get; set; } = [];

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
