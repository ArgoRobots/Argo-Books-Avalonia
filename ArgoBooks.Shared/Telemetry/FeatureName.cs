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

    // Charts
    ChartExportedToGoogleSheets,
    ChartExportedToExcel,

    // Settings
    ThemeChanged,
    LanguageChanged,

    // Onboarding
    CompanyCreated,
    ChecklistStepCompleted,
    OnboardingCompleted,
    OnboardingSkipped,

    /// <summary>
    /// The user chose the demo company from the welcome screen rather than creating one.
    /// Their subsequent activity is evaluation, not real bookkeeping, and without this
    /// event the two are indistinguishable.
    /// </summary>
    SampleCompanyOpened
}
