namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// Features that can be tracked.
/// </summary>
public enum FeatureName
{
    // Reports
    ReportGenerated,

    // Receipts
    ReceiptScanned,

    // Data Management
    DataImported,
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

    // Settings
    ThemeChanged,
    LanguageChanged,

    // Onboarding
    OnboardingCompleted,
    OnboardingSkipped
}
