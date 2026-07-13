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
    public string Description { get; set; } = string.Empty;
    public decimal Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string? ProductName { get; set; }
}

/// <summary>
/// The decrypted DTO a paired phone uploads for a scanned receipt. <see cref="CaptureIngestService"/>
/// turns this into an <c>Expense</c>/<c>Revenue</c> + <c>Receipt</c> pair in <c>CompanyData</c>,
/// exactly like <c>ReceiptsModalsViewModel.CreateExpenseTransaction</c>/<c>CreateRevenueTransaction</c>
/// do for a desktop-scanned receipt.
/// </summary>
public sealed class CapturedTransaction
{
    public CapturedTransactionType Type { get; set; }
    public string? SupplierOrCustomer { get; set; }
    public DateTime Date { get; set; }
    public decimal Total { get; set; }
    public decimal Tax { get; set; }
    public List<CapturedLineItem> LineItems { get; set; } = [];

    /// <summary>Base64-encoded receipt image, if the phone attached one. Stored as <c>Receipt.FileData</c>.</summary>
    public string? ImageBase64 { get; set; }
}
