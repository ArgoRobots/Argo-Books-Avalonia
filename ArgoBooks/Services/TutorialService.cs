using ArgoBooks.Core.Models;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Services;

/// <summary>
/// Service for managing the first-time user tutorial system.
/// </summary>
public class TutorialService
{
    private IGlobalSettingsService? _globalSettingsService;

    /// <summary>
    /// Gets the singleton instance of the TutorialService.
    /// </summary>
    public static TutorialService Instance => field ??= new TutorialService();

    /// <summary>
    /// Setup checklist item identifiers.
    /// </summary>
    public static class ChecklistItems
    {
        public const string CreateCategory = "create_category";
        public const string AddPaymentMethod = "add_payment_method";
        public const string RecordExpense = "record_expense";
        public const string RecordRevenue = "record_revenue";
        public const string AddProduct = "add_product";
        public const string AddCustomer = "add_customer";
        public const string ExploreDashboard = "explore_dashboard";
    }

    /// <summary>
    /// Page identifiers for first-visit hints.
    /// </summary>
    public static class Pages
    {
        public const string Dashboard = "Dashboard";
        public const string Analytics = "Analytics";
        public const string Insights = "Insights";
        public const string Reports = "Reports";
        public const string Expenses = "Expenses";
        public const string Revenue = "Revenue";
        public const string Invoices = "Invoices";
        public const string Products = "Products";
        public const string Customers = "Customers";
        public const string Suppliers = "Suppliers";
        public const string Categories = "Categories";
        public const string StockLevels = "StockLevels";
        public const string Receipts = "Receipts";
        public const string RentalInventory = "RentalInventory";
        public const string RentalRecords = "RentalRecords";
    }

    /// <summary>
    /// Event raised when a checklist item is completed.
    /// </summary>
    public event EventHandler<string>? ChecklistItemCompleted;

    /// <summary>
    /// Event raised when all checklist items are completed.
    /// </summary>
    public event EventHandler? AllChecklistItemsCompleted;

    /// <summary>
    /// Event raised when a page is visited for the first time.
    /// </summary>
    public event EventHandler<string>? PageFirstVisited;

    /// <summary>
    /// Event raised when tutorial state changes.
    /// </summary>
    public event EventHandler? TutorialStateChanged;

    private TutorialSettings Settings =>
        _globalSettingsService?.GetSettings()?.Tutorial ?? new TutorialSettings();

    /// <summary>
    /// Gets whether this is the user's first time using the app (no tutorial completed).
    /// </summary>
    public bool IsFirstTimeUser =>
        !Settings.HasCompletedWelcomeTutorial && Settings.FirstLaunchDate == null;

    /// <summary>
    /// Gets whether the welcome tutorial has been completed.
    /// </summary>
    public bool HasCompletedWelcomeTutorial => Settings.HasCompletedWelcomeTutorial;

    /// <summary>
    /// Gets whether the app tour has been completed.
    /// </summary>
    public bool HasCompletedAppTour => Settings.HasCompletedAppTour;

    /// <summary>
    /// Gets whether the setup checklist should be shown.
    /// </summary>
    public bool ShouldShowSetupChecklist =>
        Settings.ShowSetupChecklist && !AreAllChecklistItemsCompleted();

    /// <summary>
    /// Gets whether first-visit hints should be shown.
    /// </summary>
    public bool ShowFirstVisitHints => Settings.ShowFirstVisitHints;

    /// <summary>
    /// Sets the global settings service for tutorial persistence.
    /// </summary>
    public void SetGlobalSettingsService(IGlobalSettingsService? settingsService)
    {
        _globalSettingsService = settingsService;
    }

    /// <summary>
    /// Initializes the tutorial service for a new user if needed.
    /// </summary>
    public void InitializeForNewUser()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null && settings.Tutorial.FirstLaunchDate == null)
        {
            settings.Tutorial.FirstLaunchDate = DateTime.UtcNow;
            SaveSettings();
        }
    }

    /// <summary>
    /// Marks the welcome tutorial as completed.
    /// </summary>
    public void CompleteWelcomeTutorial()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.HasCompletedWelcomeTutorial = true;
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Marks the app tour as completed.
    /// </summary>
    public void CompleteAppTour()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.HasCompletedAppTour = true;
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Checks if a checklist item is completed.
    /// </summary>
    public bool IsChecklistItemCompleted(string itemId)
    {
        return Settings.CompletedChecklistItems.Contains(itemId);
    }

    /// <summary>
    /// Marks a checklist item as completed.
    /// </summary>
    public void CompleteChecklistItem(string itemId)
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null && !settings.Tutorial.CompletedChecklistItems.Contains(itemId))
        {
            settings.Tutorial.CompletedChecklistItems.Add(itemId);
            SaveSettings();
            ChecklistItemCompleted?.Invoke(this, itemId);

            if (AreAllChecklistItemsCompleted())
            {
                AllChecklistItemsCompleted?.Invoke(this, EventArgs.Empty);
            }

            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the list of completed checklist items.
    /// </summary>
    public IReadOnlyList<string> GetCompletedChecklistItems()
    {
        return Settings.CompletedChecklistItems.AsReadOnly();
    }

    /// <summary>
    /// Checks if all checklist items are completed.
    /// </summary>
    public bool AreAllChecklistItemsCompleted()
    {
        var completed = Settings.CompletedChecklistItems;
        return completed.Contains(ChecklistItems.CreateCategory) &&
               completed.Contains(ChecklistItems.AddPaymentMethod) &&
               completed.Contains(ChecklistItems.RecordExpense) &&
               completed.Contains(ChecklistItems.RecordRevenue);
    }

    /// <summary>
    /// Gets the count of completed checklist items.
    /// </summary>
    public int GetCompletedChecklistCount()
    {
        return Settings.CompletedChecklistItems.Count;
    }

    /// <summary>
    /// Gets the total count of checklist items.
    /// </summary>
    public int GetTotalChecklistCount()
    {
        // Core items that all users should complete
        return 4;
    }

    /// <summary>
    /// Checks if a page has been visited before.
    /// </summary>
    public bool HasVisitedPage(string pageId)
    {
        return Settings.VisitedPages.Contains(pageId);
    }

    /// <summary>
    /// Marks a page as visited.
    /// </summary>
    public void MarkPageVisited(string pageId)
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null && !settings.Tutorial.VisitedPages.Contains(pageId))
        {
            settings.Tutorial.VisitedPages.Add(pageId);
            SaveSettings();
            PageFirstVisited?.Invoke(this, pageId);
        }
    }

    /// <summary>
    /// Hides the setup checklist.
    /// </summary>
    public void HideSetupChecklist()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.ShowSetupChecklist = false;
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Shows the setup checklist.
    /// </summary>
    public void ShowSetupChecklist()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.ShowSetupChecklist = true;
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Disables first-visit hints.
    /// </summary>
    public void DisableFirstVisitHints()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.ShowFirstVisitHints = false;
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Enables first-visit hints.
    /// </summary>
    public void EnableFirstVisitHints()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.ShowFirstVisitHints = true;
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resets all tutorial progress (for restart functionality).
    /// </summary>
    public void ResetAllTutorials()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.HasCompletedWelcomeTutorial = false;
            settings.Tutorial.HasCompletedAppTour = false;
            settings.Tutorial.ShowSetupChecklist = true;
            settings.Tutorial.CompletedChecklistItems.Clear();
            settings.Tutorial.VisitedPages.Clear();
            settings.Tutorial.ShowFirstVisitHints = true;
            // Keep FirstLaunchDate to track they're not actually new
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resets only the app tour (allows re-watching).
    /// </summary>
    public void ResetAppTour()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.HasCompletedAppTour = false;
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Resets the setup checklist progress.
    /// </summary>
    public void ResetSetupChecklist()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Tutorial != null)
        {
            settings.Tutorial.CompletedChecklistItems.Clear();
            settings.Tutorial.ShowSetupChecklist = true;
            SaveSettings();
            TutorialStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// Gets the first-visit hint text for a page.
    /// </summary>
    public static string? GetFirstVisitHint(string pageId)
    {
        return pageId switch
        {
            Pages.Dashboard => "This is your business overview. Key metrics and recent activity appear here.",
            Pages.Analytics => "Explore your business data with interactive charts. Try the different tabs to see various insights.",
            Pages.Insights => "AI-powered recommendations to help grow your business appear here.",
            Pages.Reports => "Create and customize financial reports. Use templates or build your own.",
            Pages.Expenses => "Record business expenses here. Use categories to organize them for tax time.",
            Pages.Revenue => "Track your income and sales. Each entry can have line items for detailed records.",
            Pages.Invoices => "Create professional invoices for your customers. Track payments and send reminders.",
            Pages.Products => "Manage your product catalog with pricing, categories, and inventory tracking.",
            Pages.Customers => "Keep track of customer information, purchase history, and contact details.",
            Pages.Suppliers => "Manage your vendors and suppliers for easy reordering.",
            Pages.Categories => "Organize your transactions and products with custom categories.",
            Pages.StockLevels => "Monitor inventory levels and get alerts when stock runs low.",
            Pages.Receipts => "Upload receipt images and our AI will extract the details automatically.",
            Pages.RentalInventory => "Track equipment available for rental with availability status.",
            Pages.RentalRecords => "Record rental transactions and manage rental periods.",
            _ => null
        };
    }

    private void SaveSettings()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings != null)
        {
            _globalSettingsService?.SaveSettings(settings);
        }
    }
}
