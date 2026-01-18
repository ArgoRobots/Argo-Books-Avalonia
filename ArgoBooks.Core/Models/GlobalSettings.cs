using ArgoBooks.Core.Services;

namespace ArgoBooks.Core.Models;

/// <summary>
/// Global application settings stored in the AppData directory.
/// </summary>
public class GlobalSettings
{
    public WelcomeSettings Welcome { get; set; } = new();
    public List<string> RecentCompanies { get; set; } = [];
    public UpdateSettings Updates { get; set; } = new();
    public UiSettings Ui { get; set; } = new();
    public LicenseSettings License { get; set; } = new();
    public PrivacySettings Privacy { get; set; } = new();
    public WindowStateSettings? WindowState { get; set; }
    public ReportExportSettings ReportExport { get; set; } = new();
}

public class WelcomeSettings
{
    public bool ShowWelcomeForm { get; set; } = true;
    public bool EulaAccepted { get; set; } = false;
}

public class UpdateSettings
{
    public DateTime? LastUpdateCheck { get; set; }
    public bool AutoOpenRecentAfterUpdate { get; set; } = true;
}

public class UiSettings
{
    public bool SidebarCollapsed { get; set; } = false;
    public bool ReportsElementPanelCollapsed { get; set; } = false;
    public string Theme { get; set; } = "Dark";
    public string AccentColor { get; set; } = "Blue";
    public string Language { get; set; } = "English";
    public ChartSettings Chart { get; set; } = new();
    public QuickActionsSettings QuickActions { get; set; } = new();
}

public class QuickActionsSettings
{
    // Primary actions (shown by default)
    public bool ShowNewInvoice { get; set; } = true;
    public bool ShowNewExpense { get; set; } = true;
    public bool ShowNewRevenue { get; set; } = true;
    public bool ShowScanReceipt { get; set; } = true;

    // Contact actions
    public bool ShowNewCustomer { get; set; } = false;
    public bool ShowNewSupplier { get; set; } = false;

    // Product & Inventory actions
    public bool ShowNewProduct { get; set; } = false;
    public bool ShowRecordPayment { get; set; } = false;

    // Rental actions
    public bool ShowNewRentalItem { get; set; } = false;
    public bool ShowNewRentalRecord { get; set; } = true;

    // Organization actions
    public bool ShowNewCategory { get; set; } = false;
    public bool ShowNewDepartment { get; set; } = false;
    public bool ShowNewLocation { get; set; } = false;

    // Order & Stock actions
    public bool ShowNewPurchaseOrder { get; set; } = false;
    public bool ShowNewStockAdjustment { get; set; } = false;
}

public class ChartSettings
{
    public string ChartType { get; set; } = "Line";
    public string DateRange { get; set; } = "This Month";
    public DateTime? CustomStartDate { get; set; }
    public DateTime? CustomEndDate { get; set; }

    /// <summary>
    /// Maximum number of slices to show in pie charts before grouping into "Other".
    /// </summary>
    public int MaxPieSlices { get; set; } = 6;
}

public class LicenseSettings
{
    /// <summary>
    /// Obfuscated license data (encrypted with machine-specific key).
    /// </summary>
    public string? LicenseData { get; set; }

    /// <summary>
    /// Salt used for obfuscation.
    /// </summary>
    public string? Salt { get; set; }

    /// <summary>
    /// IV used for obfuscation.
    /// </summary>
    public string? Iv { get; set; }

    /// <summary>
    /// Last license validation date.
    /// </summary>
    public DateTime? LastValidationDate { get; set; }
}

public class PrivacySettings
{
    public bool AnonymousDataCollectionConsent { get; set; } = false;
    public DateTime? ConsentDate { get; set; }
}

public class ReportExportSettings
{
    public string? LastExportDirectory { get; set; }
    public bool OpenAfterExport { get; set; } = true;
    public bool IncludeMetadata { get; set; } = true;
}
