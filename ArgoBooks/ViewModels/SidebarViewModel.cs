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
    private readonly ISettingsService? _settingsService;

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
    public SidebarViewModel() : this(null, null)
    {
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    public SidebarViewModel(INavigationService? navigationService, ISettingsService? settingsService)
    {
        _navigationService = navigationService;
        _settingsService = settingsService;

        InitializeNavigationItems();
    }

    /// <summary>
    /// Initializes all navigation items.
    /// </summary>
    private void InitializeNavigationItems()
    {
        // Main Section (mockup: Dashboard, Analytics, Insights, Reports)
        MainItems.Add(CreateItem("Dashboard", "Dashboard",
            "M10 20v-6h4v6h5v-8h3L12 3 2 12h3v8z")); // fa-home
        MainItems.Add(CreateItem("Analytics", "Analytics",
            "M3.5 18.49l6-6.01 4 4L22 6.92l-1.41-1.41-7.09 7.97-4-4L2 16.99z")); // fa-chart-line
        MainItems.Add(CreateItem("Insights", "Insights",
            "M9 21c0 .55.45 1 1 1h4c.55 0 1-.45 1-1v-1H9v1zm3-19C8.14 2 5 5.14 5 9c0 2.38 1.19 4.47 3 5.74V17c0 .55.45 1 1 1h6c.55 0 1-.45 1-1v-2.26c1.81-1.27 3-3.36 3-5.74 0-3.86-3.14-7-7-7zm2.85 11.1l-.85.6V16h-4v-2.3l-.85-.6C7.8 12.16 7 10.63 7 9c0-2.76 2.24-5 5-5s5 2.24 5 5c0 1.63-.8 3.16-2.15 4.1z")); // fa-lightbulb
        MainItems.Add(CreateItem("Reports", "Reports",
            "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z")); // fa-file-alt

        // Transactions Section (mockup: Expenses, Revenue, Invoices, Payments)
        TransactionItems.Add(CreateItem("Expenses", "Expenses",
            "M20 12l-1.41-1.41L13 16.17V4h-2v12.17l-5.58-5.59L4 12l8 8 8-8z")); // fa-arrow-down
        TransactionItems.Add(CreateItem("Revenue", "Revenue",
            "M4 12l1.41 1.41L11 7.83V20h2V7.83l5.58 5.59L20 12l-8-8-8 8z")); // fa-arrow-up
        TransactionItems.Add(CreateItem("Invoices", "Invoices",
            "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z")); // fa-file-invoice
        TransactionItems.Add(CreateItem("Payments", "Payments",
            "M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-10H4V6h16v2z")); // fa-credit-card

        // Rentals Section (mockup: Rental Inventory, Rental Records)
        RentalItems.Add(CreateItem("Rental Inventory", "RentalInventory",
            "M21 3H3C1.9 3 1 3.9 1 5v14c0 1.1.9 2 2 2h18c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H3V5h18v14zM5 15h14v2H5zm0-4h14v2H5zm0-4h14v2H5z")); // fa-box
        RentalItems.Add(CreateItem("Rental Records", "RentalRecords",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 14l-5-5 1.41-1.41L12 14.17l4.59-4.58L18 11l-6 6z")); // fa-clipboard-list (with check)

        // Management Section (mockup: Customers, Products/Services, Categories, Suppliers)
        ManagementItems.Add(CreateItem("Customers", "Customers",
            "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z")); // fa-users
        ManagementItems.Add(CreateItem("Products/Services", "Products",
            "M21 16.5c0 .38-.21.71-.53.88l-7.9 4.44c-.16.12-.36.18-.57.18-.21 0-.41-.06-.57-.18l-7.9-4.44A.991.991 0 0 1 3 16.5v-9c0-.38.21-.71.53-.88l7.9-4.44c.16-.12.36-.18.57-.18.21 0 .41.06.57.18l7.9 4.44c.32.17.53.5.53.88v9zM12 4.15L6.04 7.5 12 10.85l5.96-3.35L12 4.15z")); // fa-cube
        ManagementItems.Add(CreateItem("Categories", "Categories",
            "M17.63 5.84C17.27 5.33 16.67 5 16 5L5 5.01C3.9 5.01 3 5.9 3 7v10c0 1.1.9 1.99 2 1.99L16 19c.67 0 1.27-.33 1.63-.84L22 12l-4.37-6.16z")); // fa-tags
        ManagementItems.Add(CreateItem("Suppliers", "Suppliers",
            "M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4zM6 18.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zm13.5-9l1.96 2.5H17V9.5h2.5zm-1.5 9c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z")); // fa-truck

        // Inventory Section (mockup: Stock Levels, Adjustments, Locations, Transfers, Purchase Orders)
        InventoryItems.Add(CreateItem("Stock Levels", "StockLevels",
            "M22 18V3H2v15H0v2h24v-2h-2zm-2-1H4V5h16v12zM6 15h2v-5H6v5zm4 0h2V8h-2v7zm4 0h2v-3h-2v3z")); // fa-warehouse
        InventoryItems.Add(CreateItem("Adjustments", "Adjustments",
            "M3 17v2h6v-2H3zM3 5v2h10V5H3zm10 16v-2h8v-2h-8v-2h-2v6h2zM7 9v2H3v2h4v2h2V9H7zm14 4v-2H11v2h10zm-6-4h2V7h4V5h-4V3h-2v6z")); // fa-sliders-h
        InventoryItems.Add(CreateItem("Locations", "Locations",
            "M12 2C8.13 2 5 5.13 5 9c0 5.25 7 13 7 13s7-7.75 7-13c0-3.87-3.13-7-7-7zm0 9.5c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5z")); // fa-map-marker-alt
        InventoryItems.Add(CreateItem("Transfers", "Transfers",
            "M6.99 11L3 15l3.99 4v-3H14v-2H6.99v-3zM21 9l-3.99-4v3H10v2h7.01v3L21 9z")); // fa-exchange-alt
        InventoryItems.Add(CreateItem("Purchase Orders", "PurchaseOrders",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 14l-5-5 1.41-1.41L12 14.17l4.59-4.58L18 11l-6 6z")); // fa-clipboard-list

        // Team Section (mockup: Employees, Departments, Accountants)
        TeamItems.Add(CreateItem("Employees", "Employees",
            "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z")); // fa-user-tie (simplified)
        TeamItems.Add(CreateItem("Departments", "Departments",
            "M12 7V3H2v18h20V7H12zM6 19H4v-2h2v2zm0-4H4v-2h2v2zm0-4H4V9h2v2zm0-4H4V5h2v2zm4 12H8v-2h2v2zm0-4H8v-2h2v2zm0-4H8V9h2v2zm0-4H8V5h2v2zm10 12h-8v-2h2v-2h-2v-2h2v-2h-2V9h8v10zm-2-8h-2v2h2v-2zm0 4h-2v2h2v-2z")); // fa-building
        TeamItems.Add(CreateItem("Accountants", "Accountants",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-4 10h-4v4H9v-4H5v-2h4V7h2v4h4v2z")); // fa-calculator (simplified)

        // Tracking Section (mockup: Returns, Lost/Damaged, Receipts)
        TrackingItems.Add(CreateItem("Returns", "Returns",
            "M12.5 8c-2.65 0-5.05.99-6.9 2.6L2 7v9h9l-3.62-3.62c1.39-1.16 3.16-1.88 5.12-1.88 3.54 0 6.55 2.31 7.6 5.5l2.37-.78C21.08 11.03 17.15 8 12.5 8z")); // fa-undo
        TrackingItems.Add(CreateItem("Lost/Damaged", "LostDamaged",
            "M1 21h22L12 2 1 21zm12-3h-2v-2h2v2zm0-4h-2v-4h2v4z")); // fa-exclamation-triangle
        TrackingItems.Add(CreateItem("Receipts", "Receipts",
            "M18 17H6v-2h12v2zm0-4H6v-2h12v2zm0-4H6V7h12v2zM3 22l1.5-1.5L6 22l1.5-1.5L9 22l1.5-1.5L12 22l1.5-1.5L15 22l1.5-1.5L18 22l1.5-1.5L21 22V2l-1.5 1.5L18 2l-1.5 1.5L15 2l-1.5 1.5L12 2l-1.5 1.5L9 2 7.5 3.5 6 2 4.5 3.5 3 2v20z")); // fa-receipt

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
            Command = NavigateAsyncCommand
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
