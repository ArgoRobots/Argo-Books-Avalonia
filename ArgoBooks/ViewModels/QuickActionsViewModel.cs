using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using ArgoBooks.Utilities;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Quick Actions panel (command palette).
/// </summary>
public partial class QuickActionsViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;
    private readonly List<QuickActionItem> _allActions = [];
    private SidebarViewModel? _sidebarViewModel;

    #region Properties

    [ObservableProperty]
    private string? _searchQuery;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isDropdownMode;

    [ObservableProperty]
    private int _selectedIndex;

    /// <summary>
    /// Gets the sidebar width for layout positioning.
    /// </summary>
    public double SidebarWidth => _sidebarViewModel?.Width ?? 250;

    /// <summary>
    /// Filtered quick actions based on search query.
    /// </summary>
    public ObservableCollection<QuickActionItem> QuickActions { get; } = [];

    /// <summary>
    /// Filtered navigation pages based on search query.
    /// </summary>
    public ObservableCollection<QuickActionItem> NavigationItems { get; } = [];

    /// <summary>
    /// Filtered tools and settings based on search query.
    /// </summary>
    public ObservableCollection<QuickActionItem> ToolsItems { get; } = [];

    /// <summary>
    /// Gets whether there are any quick actions visible.
    /// </summary>
    public bool HasQuickActions => QuickActions.Count > 0;

    /// <summary>
    /// Gets whether there are any navigation items visible.
    /// </summary>
    public bool HasNavigationItems => NavigationItems.Count > 0;

    /// <summary>
    /// Gets whether there are any tools items visible.
    /// </summary>
    public bool HasToolsItems => ToolsItems.Count > 0;

    /// <summary>
    /// Gets whether there are any results at all.
    /// </summary>
    public bool HasResults => HasQuickActions || HasNavigationItems || HasToolsItems;

    #endregion

    /// <summary>
    /// Creates a new QuickActionsViewModel.
    /// </summary>
    public QuickActionsViewModel() : this(null)
    {
    }

    /// <summary>
    /// Creates a new QuickActionsViewModel with navigation service.
    /// </summary>
    public QuickActionsViewModel(INavigationService? navigationService)
    {
        _navigationService = navigationService;
        InitializeActions();
        FilterActions(null);
    }

    /// <summary>
    /// Sets the sidebar view model to track sidebar width for centering.
    /// </summary>
    public void SetSidebarViewModel(SidebarViewModel sidebarViewModel)
    {
        _sidebarViewModel = sidebarViewModel;
        _sidebarViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(SidebarViewModel.Width))
            {
                OnPropertyChanged(nameof(SidebarWidth));
            }
        };
    }

    /// <summary>
    /// Initializes all available actions.
    /// </summary>
    private void InitializeActions()
    {
        // Quick Actions - Creation tasks
        _allActions.AddRange([
            new QuickActionItem("New Invoice", "Create a new invoice", "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z", QuickActionType.QuickAction, "Invoices", "OpenAddModal"),
            new QuickActionItem("New Expense", "Record a new expense", "M20 12l-1.41-1.41L13 16.17V4h-2v12.17l-5.58-5.59L4 12l8 8 8-8z", QuickActionType.QuickAction, "Expenses", "OpenAddModal"),
            new QuickActionItem("New Revenue", "Record a new revenue entry", "M4 12l1.41 1.41L11 7.83V20h2V7.83l5.58 5.59L20 12l-8-8-8 8z", QuickActionType.QuickAction, "Revenue", "OpenAddModal"),
            new QuickActionItem("Scan Receipt", "Scan and import a receipt", "M9.4 16.6L4.8 12l4.6-4.6L8 6l-6 6 6 6 1.4-1.4zm5.2 0l4.6-4.6-4.6-4.6L16 6l6 6-6 6-1.4-1.4z", QuickActionType.QuickAction, "Receipts"),
            new QuickActionItem("New Customer", "Add a new customer", "M15 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm-9-2V7H4v3H1v2h3v3h2v-3h3v-2H6zm9 4c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z", QuickActionType.QuickAction, "Customers", "OpenAddModal"),
            new QuickActionItem("New Product", "Add a new product or service", "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-2 10h-4v4h-2v-4H7v-2h4V7h2v4h4v2z", QuickActionType.QuickAction, "Products", "OpenAddModal"),
            new QuickActionItem("New Supplier", "Add a new supplier", "M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4z", QuickActionType.QuickAction, "Suppliers", "OpenAddModal"),
            new QuickActionItem("Record Payment", "Record a payment received", "M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-10H4V6h16v2z", QuickActionType.QuickAction, "Payments", "OpenAddModal"),
            new QuickActionItem("New Rental Item", "Add a new rental item", "M17 1H7c-1.1 0-2 .9-2 2v18c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V3c0-1.1-.9-2-2-2zM7 4V3h10v1H7zm0 14V6h10v12H7zm0 3v-1h10v1H7z", QuickActionType.QuickAction, "RentalInventory", "OpenAddModal"),
            new QuickActionItem("New Rental Record", "Create a new rental transaction", "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 14l-5-5 1.41-1.41L12 14.17l4.59-4.58L18 11l-6 6z", QuickActionType.QuickAction, "RentalRecords", "OpenAddModal"),
            new QuickActionItem("New Category", "Add a new category", "M12 2l-5.5 9h11L12 2zm0 3.84L13.93 9h-3.87L12 5.84z", QuickActionType.QuickAction, "Categories", "OpenAddModal"),
        ]);

        // Navigation - Go to pages
        _allActions.AddRange([
            // Main
            new QuickActionItem("Dashboard", "Go to Dashboard", "M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z", QuickActionType.Navigation, "Dashboard"),
            new QuickActionItem("Analytics", "View analytics and charts", "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z", QuickActionType.Navigation, "Analytics"),
            new QuickActionItem("Reports", "Generate and view reports", "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z", QuickActionType.Navigation, "Reports"),

            // Transactions
            new QuickActionItem("Expenses", "View and manage expenses", "M20 12l-1.41-1.41L13 16.17V4h-2v12.17l-5.58-5.59L4 12l8 8 8-8z", QuickActionType.Navigation, "Expenses"),
            new QuickActionItem("Revenue", "View and manage revenue", "M4 12l1.41 1.41L11 7.83V20h2V7.83l5.58 5.59L20 12l-8-8-8 8z", QuickActionType.Navigation, "Revenue"),
            new QuickActionItem("Invoices", "Manage invoices", "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6z", QuickActionType.Navigation, "Invoices"),
            new QuickActionItem("Payments", "View payment records", "M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2z", QuickActionType.Navigation, "Payments"),

            // Contacts
            new QuickActionItem("Customers", "Manage customers", "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3z", QuickActionType.Navigation, "Customers"),
            new QuickActionItem("Suppliers", "Manage suppliers", "M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4z", QuickActionType.Navigation, "Suppliers"),
            new QuickActionItem("Employees", "Manage employees", "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z", QuickActionType.Navigation, "Employees"),

            // Inventory
            new QuickActionItem("Products", "Manage products and services", "M20 2H4c-1 0-2 .9-2 2v3.01c0 .72.43 1.34 1 1.69V20c0 1.1 1.1 2 2 2h14c.9 0 2-.9 2-2V8.7c.57-.35 1-.97 1-1.69V4c0-1.1-1-2-2-2z", QuickActionType.Navigation, "Products"),
            new QuickActionItem("Stock Levels", "Monitor inventory levels", "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM7 10h2v7H7zm4-3h2v10h-2zm4 6h2v4h-2z", QuickActionType.Navigation, "StockLevels"),
            new QuickActionItem("Categories", "Manage categories", "M12 2l-5.5 9h11L12 2zm0 3.84L13.93 9h-3.87L12 5.84z", QuickActionType.Navigation, "Categories"),

            // Rentals
            new QuickActionItem("Rental Inventory", "Manage rental items", "M17 1H7c-1.1 0-2 .9-2 2v18c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V3c0-1.1-.9-2-2-2z", QuickActionType.Navigation, "RentalInventory"),
            new QuickActionItem("Rental Records", "View rental transactions", "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 14l-5-5 1.41-1.41L12 14.17l4.59-4.58L18 11l-6 6z", QuickActionType.Navigation, "RentalRecords"),
        ]);

        // Tools & Settings
        _allActions.AddRange([
            new QuickActionItem("Settings", "Configure application settings", "M19.14 12.94c.04-.31.06-.63.06-.94s-.02-.63-.06-.94l2.03-1.58c.18-.14.23-.41.12-.61l-1.92-3.32c-.12-.22-.37-.29-.59-.22l-2.39.96c-.5-.38-1.03-.7-1.62-.94l-.36-2.54c-.04-.24-.24-.41-.48-.41h-3.84c-.24 0-.43.17-.47.41l-.36 2.54c-.59.24-1.13.57-1.62.94l-2.39-.96c-.22-.08-.47 0-.59.22L2.74 8.87c-.12.21-.08.47.12.61l2.03 1.58c-.04.31-.06.63-.06.94s.02.63.06.94l-2.03 1.58c-.18.14-.23.41-.12.61l1.92 3.32c.12.22.37.29.59.22l2.39-.96c.5.38 1.03.7 1.62.94l.36 2.54c.05.24.24.41.48.41h3.84c.24 0 .44-.17.47-.41l.36-2.54c.59-.24 1.13-.56 1.62-.94l2.39.96c.22.08.47 0 .59-.22l1.92-3.32c.12-.22.07-.47-.12-.61l-2.01-1.58zM12 15.6c-1.98 0-3.6-1.62-3.6-3.6s1.62-3.6 3.6-3.6 3.6 1.62 3.6 3.6-1.62 3.6-3.6 3.6z", QuickActionType.Tools, null, "OpenSettings"),
            new QuickActionItem("Help & Support", "Get help and documentation", "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 17h-2v-2h2v2zm2.07-7.75l-.9.92C13.45 12.9 13 13.5 13 15h-2v-.5c0-1.1.45-2.1 1.17-2.83l1.24-1.26c.37-.36.59-.86.59-1.41 0-1.1-.9-2-2-2s-2 .9-2 2H8c0-2.21 1.79-4 4-4s4 1.79 4 4c0 .88-.36 1.68-.93 2.25z", QuickActionType.Tools, null, "OpenHelp"),
            new QuickActionItem("Export Data", "Export your data", "M19 9h-4V3H9v6H5l7 7 7-7zM5 18v2h14v-2H5z", QuickActionType.Tools, null, "OpenExport"),
            new QuickActionItem("Import Data", "Import data from file", "M9 16h6v-6h4l-7-7-7 7h4v6zm-4 2h14v2H5v-2z", QuickActionType.Tools, null, "OpenImport"),
        ]);
    }

    /// <summary>
    /// Filters actions based on search query.
    /// </summary>
    partial void OnSearchQueryChanged(string? value)
    {
        FilterActions(value);
    }

    /// <summary>
    /// Filters all action lists based on query using fuzzy matching.
    /// </summary>
    private void FilterActions(string? query)
    {
        QuickActions.Clear();
        NavigationItems.Clear();
        ToolsItems.Clear();

        IEnumerable<(QuickActionItem Item, double Score)> scoredItems;

        if (string.IsNullOrWhiteSpace(query))
        {
            // No query - show all items with score 1
            scoredItems = _allActions.Select(a => (a, 1.0));
        }
        else
        {
            // Use Levenshtein-based fuzzy search
            scoredItems = _allActions
                .Select(a =>
                {
                    // Get best score from title or description
                    var titleScore = LevenshteinDistance.ComputeSearchScore(query, a.Title);
                    var descScore = LevenshteinDistance.ComputeSearchScore(query, a.Description);
                    return (Item: a, Score: Math.Max(titleScore, descScore));
                })
                .Where(x => x.Score > 0) // Only include matches
                .OrderByDescending(x => x.Score);
        }

        var filteredItems = scoredItems.Select(x => x.Item).ToList();

        foreach (var item in filteredItems.Where(a => a.Type == QuickActionType.QuickAction).Take(6))
            QuickActions.Add(item);

        foreach (var item in filteredItems.Where(a => a.Type == QuickActionType.Navigation).Take(8))
            NavigationItems.Add(item);

        foreach (var item in filteredItems.Where(a => a.Type == QuickActionType.Tools).Take(4))
            ToolsItems.Add(item);

        OnPropertyChanged(nameof(HasQuickActions));
        OnPropertyChanged(nameof(HasNavigationItems));
        OnPropertyChanged(nameof(HasToolsItems));
        OnPropertyChanged(nameof(HasResults));

        SelectedIndex = -1;
    }

    #region Commands

    /// <summary>
    /// Opens the Quick Actions panel in dropdown mode (below search box).
    /// Preserves any existing search query.
    /// </summary>
    [RelayCommand]
    private void OpenDropdown()
    {
        IsDropdownMode = true;
        IsOpen = true;
        // Don't clear SearchQuery - it may already be set from the header
        FilterActions(SearchQuery);
    }

    /// <summary>
    /// Opens the Quick Actions panel in modal mode (centered).
    /// </summary>
    [RelayCommand]
    private void OpenModal()
    {
        IsDropdownMode = false;
        IsOpen = true;
        SearchQuery = null;
        FilterActions(null);
    }

    /// <summary>
    /// Opens the Quick Actions panel (defaults to modal mode).
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        OpenModal();
    }

    /// <summary>
    /// Closes the Quick Actions panel.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
        SearchQuery = null;
    }

    /// <summary>
    /// Executes the selected action.
    /// </summary>
    [RelayCommand]
    private void ExecuteAction(QuickActionItem? action)
    {
        if (action == null)
            return;

        Close();

        if (!string.IsNullOrEmpty(action.NavigationTarget))
        {
            _navigationService?.NavigateTo(action.NavigationTarget);
        }

        // Execute action after navigation if specified
        if (!string.IsNullOrEmpty(action.ActionName))
        {
            ActionRequested?.Invoke(this, new QuickActionEventArgs(action.NavigationTarget, action.ActionName));
        }
    }

    /// <summary>
    /// Event raised when an action needs to be executed after navigation.
    /// </summary>
    public event EventHandler<QuickActionEventArgs>? ActionRequested;

    /// <summary>
    /// Moves selection up.
    /// </summary>
    [RelayCommand]
    private void MoveUp()
    {
        var totalCount = QuickActions.Count + NavigationItems.Count + ToolsItems.Count;
        if (totalCount == 0) return;
        SelectedIndex = (SelectedIndex - 1 + totalCount) % totalCount;
    }

    /// <summary>
    /// Moves selection down.
    /// </summary>
    [RelayCommand]
    private void MoveDown()
    {
        var totalCount = QuickActions.Count + NavigationItems.Count + ToolsItems.Count;
        if (totalCount == 0) return;
        SelectedIndex = (SelectedIndex + 1) % totalCount;
    }

    /// <summary>
    /// Executes the currently selected action.
    /// </summary>
    [RelayCommand]
    private void ExecuteSelected()
    {
        var allItems = QuickActions.Concat(NavigationItems).Concat(ToolsItems).ToList();
        if (SelectedIndex >= 0 && SelectedIndex < allItems.Count)
        {
            ExecuteAction(allItems[SelectedIndex]);
        }
    }

    #endregion
}

/// <summary>
/// Represents a quick action item.
/// </summary>
public class QuickActionItem
{
    /// <summary>
    /// Action title.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Action description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Icon path data.
    /// </summary>
    public string IconData { get; }

    /// <summary>
    /// Type of action.
    /// </summary>
    public QuickActionType Type { get; }

    /// <summary>
    /// Navigation target page name (if applicable).
    /// </summary>
    public string? NavigationTarget { get; }

    /// <summary>
    /// Action name to execute after navigation (e.g., "OpenAddModal").
    /// </summary>
    public string? ActionName { get; }

    /// <summary>
    /// Creates a new QuickActionItem.
    /// </summary>
    public QuickActionItem(string title, string description, string iconData, QuickActionType type, string? navigationTarget = null, string? actionName = null)
    {
        Title = title;
        Description = description;
        IconData = iconData;
        Type = type;
        NavigationTarget = navigationTarget;
        ActionName = actionName;
    }
}

/// <summary>
/// Types of quick actions.
/// </summary>
public enum QuickActionType
{
    /// <summary>
    /// Quick action (create, add, etc.).
    /// </summary>
    QuickAction,

    /// <summary>
    /// Navigation to a page.
    /// </summary>
    Navigation,

    /// <summary>
    /// Tools and settings.
    /// </summary>
    Tools
}

/// <summary>
/// Event args for quick action execution.
/// </summary>
public class QuickActionEventArgs : EventArgs
{
    /// <summary>
    /// The navigation target page name.
    /// </summary>
    public string? NavigationTarget { get; }

    /// <summary>
    /// The action to execute after navigation.
    /// </summary>
    public string ActionName { get; }

    /// <summary>
    /// Creates a new QuickActionEventArgs.
    /// </summary>
    public QuickActionEventArgs(string? navigationTarget, string actionName)
    {
        NavigationTarget = navigationTarget;
        ActionName = actionName;
    }
}
