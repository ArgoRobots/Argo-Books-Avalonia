using System.Text.Json.Serialization;

namespace ArgoBooks.Core.Services.Sync;

/// <summary>Which side of the books a phone-captured receipt becomes.</summary>
public enum CapturedTransactionType
{
    Expense,
    Revenue
}

/// <summary>
/// One line item on a phone-scanned receipt, decrypted from the mobile upload payload.
/// Mirrors the fields <c>ReceiptsModalsViewModel</c> pulls off a scanned line (see
/// <c>ScannedLineItemViewModel</c>/<c>LineItem</c>), plus <see cref="ProductName"/> since the
/// phone sends the OCR'd product text rather than a resolved <c>ProductId</c>.
/// </summary>
public sealed class CapturedLineItem
{
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; } = 1;

    [JsonPropertyName("unitPrice")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }
}

/// <summary>
/// The decrypted DTO a paired phone uploads for a scanned receipt. <see cref="CaptureIngestService"/>
/// turns this into an <c>Expense</c>/<c>Revenue</c> + <c>Receipt</c> pair in <c>CompanyData</c>,
/// exactly like <c>ReceiptsModalsViewModel.CreateExpenseTransaction</c>/<c>CreateRevenueTransaction</c>
/// do for a desktop-scanned receipt. Property names are pinned with <see cref="JsonPropertyNameAttribute"/>
/// so the desktop/phone wire format doesn't drift with C# member-casing changes.
/// </summary>
public sealed class CapturedTransaction
{
    [JsonPropertyName("type")]
    public CapturedTransactionType Type { get; set; }

    [JsonPropertyName("supplierOrCustomer")]
    public string? SupplierOrCustomer { get; set; }

    [JsonPropertyName("date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("tax")]
    public decimal Tax { get; set; }

    [JsonPropertyName("lineItems")]
    public List<CapturedLineItem> LineItems { get; set; } = [];

    /// <summary>Base64-encoded receipt image, if the phone attached one. Stored as <c>Receipt.FileData</c>.</summary>
    [JsonPropertyName("imageBase64")]
    public string? ImageBase64 { get; set; }

    /// <summary>
    /// Phone-assigned unique id (GUID) for this capture, stable across retries/re-delivery. Lets
    /// <c>CaptureIngestService</c> de-dupe restart-safe via <c>CompanyData.IngestedScanUids</c>.
    /// </summary>
    [JsonPropertyName("scanUid")]
    public string ScanUid { get; set; } = string.Empty;
}
