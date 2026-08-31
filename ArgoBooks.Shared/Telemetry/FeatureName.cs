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
    SampleCompanyOpened,

    // Attempts, so a completion can be divided by one. Without the denominator,
    // abandonment is invisible.
    CompanyCreateOpened,
    ReceiptScanOpened,
    InvoiceCreateOpened,

    // Payroll reported only its exceptions, so a company could run payroll all year and file
    // its T4s without producing a single event. Zero errors read the same as nobody opening
    // the page, which is the one thing worth knowing about the most complex feature here.
    //
    // Drafted then approved is the pair that matters: the gap between them is people who
    // started a pay run and could not finish it.
    PayRunDrafted,
    PayRunApproved,
    PayStubsExported,
    T4SlipsGenerated,

    /// <summary>The CRA submission file itself, so actually filing is separable from previewing.</summary>
    T4XmlGenerated
}
