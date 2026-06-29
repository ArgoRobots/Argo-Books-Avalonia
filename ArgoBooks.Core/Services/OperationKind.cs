namespace ArgoBooks.Core.Services;

/// <summary>
/// The distinct AI/import operations whose durations are tracked and estimated so the
/// UI can show accurate progress. The string tags match the server's
/// <c>ai_call_timings.operation</c> values and the <c>operation</c> field the desktop
/// app sends to <c>/api/ai/completions.php</c>, so client estimates and pooled
/// server priors line up per operation.
/// </summary>
public enum OperationKind
{
    /// <summary>Single receipt scan (vision call).</summary>
    ReceiptScan,

    /// <summary>Bank statement line categorization (one call over the pending lines).</summary>
    BankCategorize,

    /// <summary>Supplier + category suggestion for a scanned receipt.</summary>
    SupplierCategory,

    /// <summary>Spreadsheet structure analysis (Tier 1 column mapping / type detection).</summary>
    SpreadsheetAnalysis,

    /// <summary>Spreadsheet Tier 2 row-to-entity processing (per chunk).</summary>
    SpreadsheetProcess,

    /// <summary>Bank statement PDF extraction (via <c>/api/bank/extract.php</c>).</summary>
    BankPdfExtract,

    /// <summary>Uncategorized completion (fallback when no specific operation is given).</summary>
    Completion,
}

/// <summary>Maps <see cref="OperationKind"/> to and from the server string tags.</summary>
public static class OperationKindExtensions
{
    public static string ToServerTag(this OperationKind kind) => kind switch
    {
        OperationKind.ReceiptScan => "receipt_scan",
        OperationKind.BankCategorize => "bank_categorize",
        OperationKind.SupplierCategory => "supplier_category",
        OperationKind.SpreadsheetAnalysis => "spreadsheet_analysis",
        OperationKind.SpreadsheetProcess => "spreadsheet_process",
        OperationKind.BankPdfExtract => "bank_pdf_extract",
        _ => "completion",
    };

    public static OperationKind FromServerTag(string? tag) => tag switch
    {
        "receipt_scan" => OperationKind.ReceiptScan,
        "bank_categorize" => OperationKind.BankCategorize,
        "supplier_category" => OperationKind.SupplierCategory,
        "spreadsheet_analysis" => OperationKind.SpreadsheetAnalysis,
        "spreadsheet_process" => OperationKind.SpreadsheetProcess,
        "bank_pdf_extract" => OperationKind.BankPdfExtract,
        _ => OperationKind.Completion,
    };
}
