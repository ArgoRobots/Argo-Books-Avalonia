using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Portal;

namespace ArgoBooks.Core.Models;

/// <summary>
/// Company-specific settings stored inside the .argo file.
/// </summary>
public class CompanySettings
{
    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = "1.0.0";

    /// <summary>
    /// Runtime-only flag to track unsaved changes. Not persisted to JSON.
    /// </summary>
    [JsonIgnore]
    public bool ChangesMade { get; set; } = false;

    /// <summary>
    /// Tracks the newest month of data when backtesting was last run.
    /// Format: "2025-12" (year-month). Used to avoid redundant backtests.
    /// </summary>
    [JsonPropertyName("lastBacktestedMonth")]
    public string? LastBacktestedMonth { get; set; }

    /// <summary>
    /// Tracks the backtest algorithm version. When the algorithm changes,
    /// this triggers a full re-backtest with the updated logic.
    /// </summary>
    [JsonPropertyName("backtestVersion")]
    public string? BacktestVersion { get; set; }

    [JsonPropertyName("company")]
    public CompanyInfo Company { get; set; } = new();
    [JsonPropertyName("localization")]
    public LocalizationSettings Localization { get; set; } = new();
    [JsonPropertyName("notifications")]
    public NotificationSettings Notifications { get; set; } = new();
    [JsonPropertyName("security")]
    public SecuritySettings Security { get; set; } = new();
    [JsonPropertyName("invoiceEmail")]
    public InvoiceEmailSettings InvoiceEmail { get; set; } = new();
    [JsonPropertyName("paymentPortal")]
    public PortalSettings PaymentPortal { get; set; } = new();
}

public class CompanyInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    [JsonPropertyName("businessType")]
    public string? BusinessType { get; set; }
    [JsonPropertyName("industry")]
    public string? Industry { get; set; }
    [JsonPropertyName("email")]
    public string? Email { get; set; }
    [JsonPropertyName("phone")]
    public string? Phone { get; set; }
    [JsonPropertyName("address")]
    public string? Address { get; set; }
    [JsonPropertyName("city")]
    public string? City { get; set; }
    [JsonPropertyName("provinceState")]
    public string? ProvinceState { get; set; }
    [JsonPropertyName("country")]
    public string? Country { get; set; }
    [JsonPropertyName("logoFileName")]
    public string? LogoFileName { get; set; }
}

public class LocalizationSettings
{
    /// <summary>
    /// The display language name (e.g., "English", "French", "German").
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; } = "English";
    [JsonPropertyName("currency")]
    public string Currency { get; set; } = "USD";
    [JsonPropertyName("dateFormat")]
    public string DateFormat { get; set; } = "MM/DD/YYYY";
}

public class NotificationSettings
{
    [JsonPropertyName("lowStockAlert")]
    public bool LowStockAlert { get; set; } = true;
    [JsonPropertyName("outOfStockAlert")]
    public bool OutOfStockAlert { get; set; } = true;
    [JsonPropertyName("invoiceOverdueAlert")]
    public bool InvoiceOverdueAlert { get; set; } = true;
    [JsonPropertyName("rentalOverdueAlert")]
    public bool RentalOverdueAlert { get; set; } = true;
    [JsonPropertyName("unsavedChangesReminder")]
    public bool UnsavedChangesReminder { get; set; } = true;
    [JsonPropertyName("unsavedChangesReminderMinutes")]
    public int UnsavedChangesReminderMinutes { get; set; } = 5;

    /// <summary>
    /// The date when startup notifications were last checked/sent.
    /// Used to avoid sending duplicate notifications on each app open.
    /// </summary>
    [JsonPropertyName("lastAlertCheckDate")]
    public DateTime? LastAlertCheckDate { get; set; }
}

public class SecuritySettings
{
    [JsonPropertyName("autoLockEnabled")]
    public bool AutoLockEnabled { get; set; } = false;
    [JsonPropertyName("autoLockMinutes")]
    public int AutoLockMinutes { get; set; } = 5;
    [JsonPropertyName("biometricEnabled")]
    public bool BiometricEnabled { get; set; } = false;
}
