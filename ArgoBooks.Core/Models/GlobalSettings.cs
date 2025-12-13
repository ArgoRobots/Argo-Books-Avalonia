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
}

public class LicenseSettings
{
    public string? StandardKey { get; set; }
    public string? PremiumSubscriptionId { get; set; }
    public DateTime? PremiumExpiryDate { get; set; }
    public DateTime? LastValidationDate { get; set; }
}

public class PrivacySettings
{
    public bool AnonymousDataCollectionConsent { get; set; } = false;
    public DateTime? ConsentDate { get; set; }
}
