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
    public AppearanceSettings Appearance { get; set; } = new();
    public EnabledModulesSettings EnabledModules { get; set; } = new();
    public NotificationSettings Notifications { get; set; } = new();
    public SecuritySettings Security { get; set; } = new();
}

public class CompanyInfo
{
    public string Name { get; set; } = string.Empty;
    public string? BusinessType { get; set; }
    public string? Industry { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
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

public class AppearanceSettings
{
    public string Theme { get; set; } = "System";
    public string AccentColor { get; set; } = "Blue";
}

public class EnabledModulesSettings
{
    public bool Invoices { get; set; } = true;
    public bool Payments { get; set; } = true;
    public bool Inventory { get; set; } = true;
    public bool Employees { get; set; } = true;
    public bool Rentals { get; set; } = true;
}

public class NotificationSettings
{
    public bool LowStockAlert { get; set; } = true;
    public bool OutOfStockAlert { get; set; } = true;
    public bool InvoiceOverdueAlert { get; set; } = true;
    public bool RentalOverdueAlert { get; set; } = true;
}

public class SecuritySettings
{
    public bool AutoLockEnabled { get; set; } = false;
    public int AutoLockMinutes { get; set; } = 5;
    public bool BiometricEnabled { get; set; } = false;
    public bool FileEncryptionEnabled { get; set; } = false;
}
