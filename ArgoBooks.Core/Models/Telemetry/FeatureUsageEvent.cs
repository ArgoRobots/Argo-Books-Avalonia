namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// Feature usage event for tracking which features are used.
/// </summary>
public class FeatureUsageEvent : TelemetryEvent
{
    /// <inheritdoc />
    public override TelemetryDataType DataType => TelemetryDataType.FeatureUsage;

    /// <summary>
    /// Name of the feature used.
    /// </summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public FeatureName FeatureName { get; set; }

    /// <summary>
    /// Additional context about the feature usage (e.g., chart type, report type).
    /// </summary>
    public string? Context { get; set; }
}

/// <summary>
/// Features that can be tracked.
/// </summary>
public enum FeatureName
{
    // Navigation
    PageView,

    // Charts
    ChartViewed,
    ChartTypeChanged,

    // Reports
    ReportGenerated,
    ReportPrinted,

    // Receipts
    ReceiptScanned,
    ReceiptManualEntry,

    // Data Management
    DataImported,
    DataExported,
    BackupCreated,
    BackupRestored,

    // Transactions
    InvoiceCreated,
    ExpenseCreated,
    RevenueCreated,
    PaymentRecorded,

    // Inventory
    ProductCreated,
    StockAdjusted,
    PurchaseOrderCreated,

    // Contacts
    CustomerCreated,
    SupplierCreated,

    // Rentals
    RentalItemCreated,
    RentalRecordCreated,

    // AI Features
    AiSearchUsed,
    AiSuggestionAccepted,

    // Settings
    ThemeChanged,
    LanguageChanged
}
