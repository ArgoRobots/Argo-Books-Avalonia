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
    BankMatchConfirmed,

    // Inventory
    ProductCreated,
    CategoryCreated,
    LocationCreated,
    StockAdjusted,
    PurchaseOrderCreated,
    ReturnRecorded,
    LostDamagedRecorded,

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
    CompanyCreated,
    ChecklistStepCompleted,
    OnboardingCompleted,
    OnboardingSkipped
}
