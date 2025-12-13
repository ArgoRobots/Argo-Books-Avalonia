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
    private double _width = 240;

    private const double ExpandedWidth = 240;
    private const double CollapsedWidth = 64;

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
    public ObservableCollection<SidebarItemModel> InventoryItems { get; } = [];
    public ObservableCollection<SidebarItemModel> ContactItems { get; } = [];
    public ObservableCollection<SidebarItemModel> RentalItems { get; } = [];

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
        // Main Section
        MainItems.Add(CreateItem("Dashboard", "Dashboard",
            "M3 13h8V3H3v10zm0 8h8v-6H3v6zm10 0h8V11h-8v10zm0-18v6h8V3h-8z"));
        MainItems.Add(CreateItem("Analytics", "Analytics",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zM9 17H7v-7h2v7zm4 0h-2V7h2v10zm4 0h-2v-4h2v4z"));
        MainItems.Add(CreateItem("Reports", "Reports",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-5 14H7v-2h7v2zm3-4H7v-2h10v2zm0-4H7V7h10v2z"));

        // Transactions Section
        TransactionItems.Add(CreateItem("Revenue", "Revenue",
            "M11.8 10.9c-2.27-.59-3-1.2-3-2.15 0-1.09 1.01-1.85 2.7-1.85 1.78 0 2.44.85 2.5 2.1h2.21c-.07-1.72-1.12-3.3-3.21-3.81V3h-3v2.16c-1.94.42-3.5 1.68-3.5 3.61 0 2.31 1.91 3.46 4.7 4.13 2.5.6 3 1.48 3 2.41 0 .69-.49 1.79-2.7 1.79-2.06 0-2.87-.92-2.98-2.1h-2.2c.12 2.19 1.76 3.42 3.68 3.83V21h3v-2.15c1.95-.37 3.5-1.5 3.5-3.55 0-2.84-2.43-3.81-4.7-4.4z"));
        TransactionItems.Add(CreateItem("Expenses", "Expenses",
            "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1.41 16.09V20h-2.67v-1.93c-1.71-.36-3.16-1.46-3.27-3.4h1.96c.1 1.05.82 1.87 2.65 1.87 1.96 0 2.4-.98 2.4-1.59 0-.83-.44-1.61-2.67-2.14-2.48-.6-4.18-1.62-4.18-3.67 0-1.72 1.39-2.84 3.11-3.21V4h2.67v1.95c1.86.45 2.79 1.86 2.85 3.39H14.3c-.05-1.11-.64-1.87-2.22-1.87-1.5 0-2.4.68-2.4 1.64 0 .84.65 1.39 2.67 1.91s4.18 1.39 4.18 3.91c-.01 1.83-1.38 2.83-3.12 3.16z"));
        TransactionItems.Add(CreateItem("Invoices", "Invoices",
            "M14 2H6c-1.1 0-1.99.9-1.99 2L4 20c0 1.1.89 2 1.99 2H18c1.1 0 2-.9 2-2V8l-6-6zm2 16H8v-2h8v2zm0-4H8v-2h8v2zm-3-5V3.5L18.5 9H13z"));
        TransactionItems.Add(CreateItem("Payments", "Payments",
            "M20 4H4c-1.11 0-1.99.89-1.99 2L2 18c0 1.11.89 2 2 2h16c1.11 0 2-.89 2-2V6c0-1.11-.89-2-2-2zm0 14H4v-6h16v6zm0-10H4V6h16v2z"));

        // Inventory Section
        InventoryItems.Add(CreateItem("Products", "Products",
            "M20 2H4c-1 0-2 .9-2 2v3.01c0 .72.43 1.34 1 1.69V20c0 1.1 1.1 2 2 2h14c.9 0 2-.9 2-2V8.7c.57-.35 1-.97 1-1.69V4c0-1.1-1-2-2-2zm-5 12H9v-2h6v2zm5-7H4V4h16v3z"));
        InventoryItems.Add(CreateItem("Stock Levels", "StockLevels",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm0 16H5V5h14v14zM7 10h2v7H7zm4-3h2v10h-2zm4 6h2v4h-2z"));
        InventoryItems.Add(CreateItem("Purchase Orders", "PurchaseOrders",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-2 10h-4v4h-2v-4H7v-2h4V7h2v4h4v2z"));
        InventoryItems.Add(CreateItem("Categories", "Categories",
            "M12 2l-5.5 9h11L12 2zm0 3.84L13.93 9h-3.87L12 5.84zM17.5 13c-2.49 0-4.5 2.01-4.5 4.5s2.01 4.5 4.5 4.5 4.5-2.01 4.5-4.5-2.01-4.5-4.5-4.5zm0 7c-1.38 0-2.5-1.12-2.5-2.5s1.12-2.5 2.5-2.5 2.5 1.12 2.5 2.5-1.12 2.5-2.5 2.5zM3 21.5h8v-8H3v8zm2-6h4v4H5v-4z"));

        // Contacts Section
        ContactItems.Add(CreateItem("Customers", "Customers",
            "M16 11c1.66 0 2.99-1.34 2.99-3S17.66 5 16 5c-1.66 0-3 1.34-3 3s1.34 3 3 3zm-8 0c1.66 0 2.99-1.34 2.99-3S9.66 5 8 5C6.34 5 5 6.34 5 8s1.34 3 3 3zm0 2c-2.33 0-7 1.17-7 3.5V19h14v-2.5c0-2.33-4.67-3.5-7-3.5zm8 0c-.29 0-.62.02-.97.05 1.16.84 1.97 1.97 1.97 3.45V19h6v-2.5c0-2.33-4.67-3.5-7-3.5z"));
        ContactItems.Add(CreateItem("Suppliers", "Suppliers",
            "M20 8h-3V4H3c-1.1 0-2 .9-2 2v11h2c0 1.66 1.34 3 3 3s3-1.34 3-3h6c0 1.66 1.34 3 3 3s3-1.34 3-3h2v-5l-3-4zM6 18.5c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5zm13.5-9l1.96 2.5H17V9.5h2.5zm-1.5 9c-.83 0-1.5-.67-1.5-1.5s.67-1.5 1.5-1.5 1.5.67 1.5 1.5-.67 1.5-1.5 1.5z"));
        ContactItems.Add(CreateItem("Employees", "Employees",
            "M12 12c2.21 0 4-1.79 4-4s-1.79-4-4-4-4 1.79-4 4 1.79 4 4 4zm0 2c-2.67 0-8 1.34-8 4v2h16v-2c0-2.66-5.33-4-8-4z"));
        ContactItems.Add(CreateItem("Accountants", "Accountants",
            "M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm0 3c1.66 0 3 1.34 3 3s-1.34 3-3 3-3-1.34-3-3 1.34-3 3-3zm0 14.2c-2.5 0-4.71-1.28-6-3.22.03-1.99 4-3.08 6-3.08 1.99 0 5.97 1.09 6 3.08-1.29 1.94-3.5 3.22-6 3.22z"));

        // Rentals Section
        RentalItems.Add(CreateItem("Rental Inventory", "RentalInventory",
            "M17 1H7c-1.1 0-2 .9-2 2v18c0 1.1.9 2 2 2h10c1.1 0 2-.9 2-2V3c0-1.1-.9-2-2-2zM7 4V3h10v1H7zm0 14V6h10v12H7zm0 3v-1h10v1H7z"));
        RentalItems.Add(CreateItem("Rental Records", "RentalRecords",
            "M19 3H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2V5c0-1.1-.9-2-2-2zm-7 14l-5-5 1.41-1.41L12 14.17l4.59-4.58L18 11l-6 6z"));

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
    /// Updates collapsed state on all items.
    /// </summary>
    private void UpdateItemsCollapsedState(bool isCollapsed)
    {
        foreach (var item in MainItems) item.IsCollapsed = isCollapsed;
        foreach (var item in TransactionItems) item.IsCollapsed = isCollapsed;
        foreach (var item in InventoryItems) item.IsCollapsed = isCollapsed;
        foreach (var item in ContactItems) item.IsCollapsed = isCollapsed;
        foreach (var item in RentalItems) item.IsCollapsed = isCollapsed;
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
    /// Navigates to the specified page.
    /// </summary>
    [RelayCommand]
    private void Navigate(string? pageName)
    {
        if (string.IsNullOrEmpty(pageName))
            return;

        SetActivePage(pageName);
        _navigationService?.NavigateTo(pageName);
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
        foreach (var item in InventoryItems) item.IsActive = item.PageName == pageName;
        foreach (var item in ContactItems) item.IsActive = item.PageName == pageName;
        foreach (var item in RentalItems) item.IsActive = item.PageName == pageName;
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
