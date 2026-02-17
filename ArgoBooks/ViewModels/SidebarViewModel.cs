using System.Collections.ObjectModel;
using Avalonia.Media.Imaging;
using ArgoBooks.Controls;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the sidebar navigation component.
/// </summary>
public partial class SidebarViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;

    #region Sidebar State

    [ObservableProperty]
    private bool _isCollapsed;

    [ObservableProperty]
    private string _collapseTooltip = "Collapse sidebar";

    [ObservableProperty]
    private double _width = 250;

    // Dimensions from mockup: 250px expanded, 70px collapsed
    private const double ExpandedWidth = 250;
    private const double CollapsedWidth = 70;

    #endregion

    #region Company Info

    [ObservableProperty]
    private string? _companyName = "Argo Books";

    [ObservableProperty]
    private string _companyInitial = "A";

    [ObservableProperty]
    private Bitmap? _companyLogo;

    [ObservableProperty]
    private bool _hasCompanyLogo;

    [ObservableProperty]
    private string? _userRole;

    #endregion

    #region Feature Visibility

    [ObservableProperty]
    private bool _showTransactions = true;

    [ObservableProperty]
    private bool _showInventory = true;

    [ObservableProperty]
    private bool _showRentals = true;

    [ObservableProperty]
    private bool _showPayroll = true;

    [ObservableProperty]
    private bool _showTeam; // Hidden until enterprise plan (always false for now)

    [ObservableProperty]
    private bool _hasStandard; // Standard plan or higher

    [ObservableProperty]
    private bool _hasPremium; // Premium plan

    [ObservableProperty]
    private bool _hasEnterprise; // Enterprise plan (always false for now)

    #endregion

    #region Premium Feature Items

    private SidebarItemModel? _insightsItem;
    private SidebarItemModel? _invoicesItem;
    private SidebarItemModel? _paymentsItem;

    #endregion

    #region Enterprise Feature Items

    private SidebarItemModel? _locationsItem;
    private SidebarItemModel? _transfersItem;

    #endregion

    #region Navigation Items

    [ObservableProperty]
    private string _currentPage = "Dashboard";

    public ObservableCollection<SidebarItemModel> MainItems { get; } = [];
    public ObservableCollection<SidebarItemModel> TransactionItems { get; } = [];
    public ObservableCollection<SidebarItemModel> RentalItems { get; } = [];
    public ObservableCollection<SidebarItemModel> ManagementItems { get; } = [];
    public ObservableCollection<SidebarItemModel> InventoryItems { get; } = [];
    public ObservableCollection<SidebarItemModel> TeamItems { get; } = [];
    public ObservableCollection<SidebarItemModel> TrackingItems { get; } = [];

    #endregion

    #region Events

    /// <summary>
    /// Event raised when the company header is clicked to open the company switcher.
    /// </summary>
    public event EventHandler? OpenCompanySwitcherRequested;

    /// <summary>
    /// Event raised when navigating to a page (so panels can be closed).
    /// </summary>
    public event EventHandler? NavigationRequested;

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public SidebarViewModel() : this(null)
    {
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    public SidebarViewModel(INavigationService? navigationService)
    {
        _navigationService = navigationService;

        InitializeNavigationItems();
    }

    /// <summary>
    /// Initializes all navigation items.
    /// </summary>
    private void InitializeNavigationItems()
    {
        // Main Section (mockup: Dashboard, Analytics, Insights, Reports)
        MainItems.Add(CreateItem("Dashboard", "Dashboard", Icons.Dashboard));
        MainItems.Add(CreateItem("Analytics", "Analytics", Icons.Analytics));
        _insightsItem = CreateItem("Insights", "Insights", Icons.Insights);
        _insightsItem.IsVisible = HasPremium; // Hide by default unless premium
        MainItems.Add(_insightsItem);
        MainItems.Add(CreateItem("Reports", "Reports", Icons.Reports));

        // Transactions Section (mockup: Expenses, Revenue, Invoices, Payments)
        TransactionItems.Add(CreateItem("Expenses", "Expenses", Icons.Expenses));
        TransactionItems.Add(CreateItem("Revenue", "Revenue", Icons.Revenue));
        _invoicesItem = CreateItem("Invoices", "Invoices", Icons.Invoices);
        _invoicesItem.IsVisible = HasPremium; // Hide by default unless premium
        TransactionItems.Add(_invoicesItem);
        _paymentsItem = CreateItem("Payments", "Payments", Icons.Payments);
        _paymentsItem.IsVisible = HasPremium; // Hide by default unless premium
        TransactionItems.Add(_paymentsItem);

        // Rentals Section (mockup: Rental Inventory, Rental Records)
        RentalItems.Add(CreateItem("Rental Inventory", "RentalInventory", Icons.RentalInventory));
        RentalItems.Add(CreateItem("Rental Records", "RentalRecords", Icons.RentalRecords));

        // Management Section (mockup: Customers, Products/Services, Categories, Suppliers)
        ManagementItems.Add(CreateItem("Customers", "Customers", Icons.Customers));
        ManagementItems.Add(CreateItem("Products/Services", "Products", Icons.Products));
        ManagementItems.Add(CreateItem("Categories", "Categories", Icons.Categories));
        ManagementItems.Add(CreateItem("Suppliers", "Suppliers", Icons.Suppliers));

        // Inventory Section (mockup: Stock Levels, Adjustments, Locations, Transfers, Purchase Orders)
        InventoryItems.Add(CreateItem("Stock Levels", "StockLevels", Icons.StockLevels));
        InventoryItems.Add(CreateItem("Adjustments", "StockAdjustments", Icons.Adjustments));
        _locationsItem = CreateItem("Locations", "Locations", Icons.Locations);
        InventoryItems.Add(_locationsItem);
        _transfersItem = CreateItem("Transfers", "Transfers", Icons.Transfers);
        _transfersItem.IsVisible = HasEnterprise; // Hidden until enterprise plan
        InventoryItems.Add(_transfersItem);
        InventoryItems.Add(CreateItem("Purchase Orders", "PurchaseOrders", Icons.PurchaseOrders));

        // Team Section (mockup: Employees, Departments, Accountants)
        TeamItems.Add(CreateItem("Employees", "Employees", Icons.Employees));
        TeamItems.Add(CreateItem("Departments", "Departments", Icons.Departments));
        TeamItems.Add(CreateItem("Accountants", "Accountants", Icons.Accountants));

        // Tracking Section (mockup: Returns, Lost/Damaged, Receipts)
        TrackingItems.Add(CreateItem("Returns", "Returns", Icons.Returns));
        TrackingItems.Add(CreateItem("Lost/Damaged", "LostDamaged", Icons.LostDamaged));
        TrackingItems.Add(CreateItem("Receipts", "Receipts", Icons.Receipts));

        // Set Dashboard as active by default
        SetActivePage("Dashboard");
    }

    /// <summary>
    /// Creates a sidebar item model.
    /// </summary>
    private SidebarItemModel CreateItem(string text, string pageName, string iconData)
    {
        return new SidebarItemModel
        {
            Text = text,
            PageName = pageName,
            IconData = iconData,
            Command = NavigateCommand
        };
    }

    /// <summary>
    /// Updates width when collapsed state changes.
    /// </summary>
    partial void OnIsCollapsedChanged(bool value)
    {
        Width = value ? CollapsedWidth : ExpandedWidth;
        CollapseTooltip = value ? "Expand sidebar" : "Collapse sidebar";

        // Update all items with collapsed state
        UpdateItemsCollapsedState(value);
    }

    /// <summary>
    /// Updates the company initial when name changes.
    /// </summary>
    partial void OnCompanyNameChanged(string? value)
    {
        CompanyInitial = string.IsNullOrEmpty(value) ? "A" : value[0].ToString().ToUpper();
    }

    /// <summary>
    /// Updates premium feature visibility when premium status changes.
    /// </summary>
    partial void OnHasPremiumChanged(bool value)
    {
        _insightsItem?.IsVisible = value;
        _invoicesItem?.IsVisible = value;
        _paymentsItem?.IsVisible = value;
    }

    /// <summary>
    /// Updates enterprise feature visibility when enterprise status changes.
    /// </summary>
    partial void OnHasEnterpriseChanged(bool value)
    {
        ShowTeam = value;
        _locationsItem?.IsVisible = value;
        _transfersItem?.IsVisible = value;
    }

    /// <summary>
    /// Updates collapsed state on all items.
    /// </summary>
    private void UpdateItemsCollapsedState(bool isCollapsed)
    {
        foreach (var item in MainItems) item.IsCollapsed = isCollapsed;
        foreach (var item in TransactionItems) item.IsCollapsed = isCollapsed;
        foreach (var item in RentalItems) item.IsCollapsed = isCollapsed;
        foreach (var item in ManagementItems) item.IsCollapsed = isCollapsed;
        foreach (var item in InventoryItems) item.IsCollapsed = isCollapsed;
        foreach (var item in TeamItems) item.IsCollapsed = isCollapsed;
        foreach (var item in TrackingItems) item.IsCollapsed = isCollapsed;
    }

    /// <summary>
    /// Toggles the sidebar collapsed state.
    /// </summary>
    [RelayCommand]
    private void ToggleCollapse()
    {
        IsCollapsed = !IsCollapsed;
    }

    /// <summary>
    /// Opens the company switcher panel.
    /// </summary>
    [RelayCommand]
    private void OpenCompanySwitcher()
    {
        OpenCompanySwitcherRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Navigates to the specified page.
    /// </summary>
    [RelayCommand]
    private async Task NavigateAsync(string? pageName)
    {
        if (string.IsNullOrEmpty(pageName))
            return;

        // Raise event so panels can be closed
        NavigationRequested?.Invoke(this, EventArgs.Empty);

        // Use async navigation to allow navigation guards to check for unsaved changes
        if (_navigationService != null)
        {
            var navigated = await _navigationService.NavigateToAsync(pageName);
            if (navigated)
            {
                SetActivePage(pageName);
            }
        }
    }

    /// <summary>
    /// Sets the active page and updates item states.
    /// </summary>
    public void SetActivePage(string pageName)
    {
        CurrentPage = pageName;

        // Update active state on all items
        foreach (var item in MainItems) item.IsActive = item.PageName == pageName;
        foreach (var item in TransactionItems) item.IsActive = item.PageName == pageName;
        foreach (var item in RentalItems) item.IsActive = item.PageName == pageName;
        foreach (var item in ManagementItems) item.IsActive = item.PageName == pageName;
        foreach (var item in InventoryItems) item.IsActive = item.PageName == pageName;
        foreach (var item in TeamItems) item.IsActive = item.PageName == pageName;
        foreach (var item in TrackingItems) item.IsActive = item.PageName == pageName;
    }

    /// <summary>
    /// Updates feature visibility based on settings.
    /// </summary>
    public void UpdateFeatureVisibility(bool showTransactions, bool showInventory, bool showRentals, bool showPayroll)
    {
        ShowTransactions = showTransactions;
        ShowInventory = showInventory;
        ShowRentals = showRentals;
        ShowPayroll = showPayroll;
    }

    /// <summary>
    /// Sets the company information.
    /// </summary>
    public void SetCompanyInfo(string? name, Bitmap? logo = null, string? userRole = null)
    {
        CompanyName = name ?? "Argo Books";
        CompanyLogo = logo;
        HasCompanyLogo = logo != null;
        UserRole = userRole;
    }
}
