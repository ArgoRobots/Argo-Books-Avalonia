using ArgoBooks.Core.Models.Invoices;

namespace ArgoBooks.Core.Models;

/// <summary>
/// Company-specific settings stored inside the .argo file.
/// </summary>
public class CompanySettings
{
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
    public string? LastBacktestedMonth { get; set; }

    public CompanyInfo Company { get; set; } = new();
    public LocalizationSettings Localization { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
    public InvoiceEmailSettings InvoiceEmail { get; set; } = new();
}

public class CompanyInfo
{
    public string Name { get; set; } = string.Empty;
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? LogoFileName { get; set; }
}

public class LocalizationSettings
{
    /// <summary>
    /// The display language name (e.g., "English", "French", "German").
    /// </summary>
    public string Language { get; set; } = "English";
    public string Currency { get; set; } = "USD";
    public string DateFormat { get; set; } = "MM/DD/YYYY";
}

public class NotificationSettings
{
    public bool LowStockAlert { get; set; } = true;
    public bool OutOfStockAlert { get; set; } = true;
    public bool InvoiceOverdueAlert { get; set; } = true;
    public bool RentalOverdueAlert { get; set; } = true;
    public bool UnsavedChangesReminder { get; set; } = true;
    public int UnsavedChangesReminderMinutes { get; set; } = 5;

    /// <summary>
    /// The date when startup notifications were last checked/sent.
    /// Used to avoid sending duplicate notifications on each app open.
    /// </summary>
    public DateTime? LastAlertCheckDate { get; set; }
}

public class SecuritySettings
{
    public bool AutoLockEnabled { get; set; } = false;
    public int AutoLockMinutes { get; set; } = 5;
    public bool BiometricEnabled { get; set; } = false;
    public bool FileEncryptionEnabled { get; set; } = false;
}
