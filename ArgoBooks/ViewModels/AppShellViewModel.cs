using System.Reflection;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.ViewModels.Dashboard;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the application shell, managing sidebar and header.
/// </summary>
public partial class AppShellViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;
    private bool _hasPremium;

    // Lazy modal VM backing fields
    private CustomerModalsViewModel? _customerModalsViewModel;
    private ProductModalsViewModel? _productModalsViewModel;
    private CategoryModalsViewModel? _categoryModalsViewModel;
    private DepartmentModalsViewModel? _departmentModalsViewModel;
    private SupplierModalsViewModel? _supplierModalsViewModel;
    private RentalInventoryModalsViewModel? _rentalInventoryModalsViewModel;
    private RentalRecordsModalsViewModel? _rentalRecordsModalsViewModel;
    private PaymentModalsViewModel? _paymentModalsViewModel;
    private InvoiceModalsViewModel? _invoiceModalsViewModel;
    private InvoiceTemplateDesignerViewModel? _invoiceTemplateDesignerViewModel;
    private ExpenseModalsViewModel? _expenseModalsViewModel;
    private RevenueModalsViewModel? _revenueModalsViewModel;
    private StockLevelsModalsViewModel? _stockLevelsModalsViewModel;
    private LocationsModalsViewModel? _locationsModalsViewModel;
    private StockAdjustmentsModalsViewModel? _stockAdjustmentsModalsViewModel;
    private PurchaseOrdersModalsViewModel? _purchaseOrdersModalsViewModel;
    private ReceiptsModalsViewModel? _receiptsModalsViewModel;
    private LostDamagedModalsViewModel? _lostDamagedModalsViewModel;
    private ReturnsModalsViewModel? _returnsModalsViewModel;
    private ReportModalsViewModel? _reportModalsViewModel;

    /// <summary>
    /// Raised when any modal ViewModel saves or deletes data (for unsaved-changes tracking).
    /// </summary>
    internal event EventHandler? UnsavedChangesMade;

    private void RaiseUnsavedChanges(object? sender, EventArgs e) =>
        UnsavedChangesMade?.Invoke(this, EventArgs.Empty);

    /// <summary>
    /// Raised when the user requests to open a file for scanning via quick action.
    /// The view should handle this by opening a file picker.
    /// </summary>
    public event EventHandler? OpenFileScanRequested;

    /// <summary>
    /// Raised when the user requests to edit the company via quick action.
    /// </summary>
    public event EventHandler? EditCompanyRequested;

    /// <summary>
    /// Raised when the user requests to restart the tutorial from the Help panel.
    /// </summary>
    public event EventHandler? RestartTutorialRequested;

    #region ViewModels

    /// <summary>
    /// Gets the sidebar view model.
    /// </summary>
    public SidebarViewModel SidebarViewModel { get; }

    /// <summary>
    /// Gets the header view model.
    /// </summary>
    public HeaderViewModel HeaderViewModel { get; }

    /// <summary>
    /// Gets the quick actions view model.
    /// </summary>
    public QuickActionsViewModel QuickActionsViewModel { get; }

    /// <summary>
    /// Gets the notification panel view model.
    /// </summary>
    public NotificationPanelViewModel NotificationPanelViewModel { get; }

    /// <summary>
    /// Gets the file menu panel view model.
    /// </summary>
    public FileMenuPanelViewModel FileMenuPanelViewModel { get; }

    /// <summary>
    /// Gets the help panel view model.
    /// </summary>
    public HelpPanelViewModel HelpPanelViewModel { get; }

    /// <summary>
    /// Gets the upgrade modal view model.
    /// </summary>
    public UpgradeModalViewModel UpgradeModalViewModel { get; }

    /// <summary>
    /// Gets the create company wizard view model.
    /// </summary>
    public CreateCompanyViewModel CreateCompanyViewModel { get; }

    /// <summary>
    /// Gets the company switcher panel view model.
    /// </summary>
    public CompanySwitcherPanelViewModel CompanySwitcherPanelViewModel { get; }

    /// <summary>
    /// Gets the settings modal view model.
    /// </summary>
    public SettingsModalViewModel SettingsModalViewModel { get; }

    /// <summary>
    /// Gets the check for update modal view model.
    /// </summary>
    public CheckForUpdateModalViewModel CheckForUpdateModalViewModel { get; }

    /// <summary>
    /// Gets the prediction info modal view model.
    /// </summary>
    public PredictionInfoModalViewModel PredictionInfoModalViewModel { get; }

    /// <summary>
    /// Gets the past predictions modal view model.
    /// </summary>
    public PastPredictionsModalViewModel PastPredictionsModalViewModel { get; }

    /// <summary>
    /// Gets the import modal view model.
    /// </summary>
    public ImportModalViewModel ImportModalViewModel { get; }

    /// <summary>
    /// Gets the import validation dialog view model.
    /// </summary>
    public ImportValidationDialogViewModel ImportValidationDialogViewModel { get; }

    /// <summary>
    /// Gets the import result dialog view model.
    /// </summary>
    public ImportResultDialogViewModel ImportResultDialogViewModel { get; }

    /// <summary>
    /// Gets the import mapping dialog view model.
    /// </summary>
    public ImportMappingDialogViewModel ImportMappingDialogViewModel { get; }

    /// <summary>
    /// Gets the export as modal view model.
    /// </summary>
    public ExportAsModalViewModel ExportAsModalViewModel { get; }

    /// <summary>
    /// Gets the switch account modal view model.
    /// </summary>
    public SwitchAccountModalViewModel SwitchAccountModalViewModel { get; }

    /// <summary>
    /// Gets the password prompt modal view model.
    /// </summary>
    public PasswordPromptModalViewModel PasswordPromptModalViewModel { get; }

    /// <summary>
    /// Gets the edit company modal view model.
    /// </summary>
    public EditCompanyModalViewModel EditCompanyModalViewModel { get; }

    /// <summary>
    /// Gets the customer modals view model.
    /// </summary>
    public CustomerModalsViewModel CustomerModalsViewModel
    {
        get
        {
            if (_customerModalsViewModel == null)
            {
                _customerModalsViewModel = new CustomerModalsViewModel();
                _customerModalsViewModel.CustomerSaved += RaiseUnsavedChanges;
                _customerModalsViewModel.CustomerDeleted += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _customerModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the product modals view model.
    /// </summary>
    public ProductModalsViewModel ProductModalsViewModel
    {
        get
        {
            if (_productModalsViewModel == null)
            {
                _productModalsViewModel = new ProductModalsViewModel();
                _productModalsViewModel.ProductSaved += RaiseUnsavedChanges;
                _productModalsViewModel.ProductDeleted += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _productModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the category modals view model.
    /// </summary>
    public CategoryModalsViewModel CategoryModalsViewModel
    {
        get
        {
            if (_categoryModalsViewModel == null)
            {
                _categoryModalsViewModel = new CategoryModalsViewModel();
                _categoryModalsViewModel.CategorySaved += RaiseUnsavedChanges;
                _categoryModalsViewModel.CategoryDeleted += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _categoryModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the department modals view model.
    /// </summary>
    public DepartmentModalsViewModel DepartmentModalsViewModel
    {
        get
        {
            if (_departmentModalsViewModel == null)
            {
                _departmentModalsViewModel = new DepartmentModalsViewModel();
                _departmentModalsViewModel.DepartmentSaved += RaiseUnsavedChanges;
                _departmentModalsViewModel.DepartmentDeleted += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _departmentModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the supplier modals view model.
    /// </summary>
    public SupplierModalsViewModel SupplierModalsViewModel
    {
        get
        {
            if (_supplierModalsViewModel == null)
            {
                _supplierModalsViewModel = new SupplierModalsViewModel();
                _supplierModalsViewModel.SupplierSaved += RaiseUnsavedChanges;
                _supplierModalsViewModel.SupplierDeleted += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _supplierModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the rental inventory modals view model.
    /// </summary>
    public RentalInventoryModalsViewModel RentalInventoryModalsViewModel
    {
        get
        {
            if (_rentalInventoryModalsViewModel == null)
            {
                _rentalInventoryModalsViewModel = new RentalInventoryModalsViewModel();
                _rentalInventoryModalsViewModel.ItemSaved += RaiseUnsavedChanges;
                _rentalInventoryModalsViewModel.ItemDeleted += RaiseUnsavedChanges;
                _rentalInventoryModalsViewModel.RentalCreated += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _rentalInventoryModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the rental records modals view model.
    /// </summary>
    public RentalRecordsModalsViewModel RentalRecordsModalsViewModel
    {
        get
        {
            if (_rentalRecordsModalsViewModel == null)
            {
                _rentalRecordsModalsViewModel = new RentalRecordsModalsViewModel();
                _rentalRecordsModalsViewModel.RecordSaved += RaiseUnsavedChanges;
                _rentalRecordsModalsViewModel.RecordDeleted += RaiseUnsavedChanges;
                _rentalRecordsModalsViewModel.RecordReturned += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _rentalRecordsModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the payment modals view model.
    /// </summary>
    public PaymentModalsViewModel PaymentModalsViewModel
    {
        get
        {
            if (_paymentModalsViewModel == null)
            {
                _paymentModalsViewModel = new PaymentModalsViewModel();
                _paymentModalsViewModel.PaymentSaved += RaiseUnsavedChanges;
                _paymentModalsViewModel.PaymentDeleted += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _paymentModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the invoice modals view model.
    /// </summary>
    public InvoiceModalsViewModel InvoiceModalsViewModel
    {
        get
        {
            if (_invoiceModalsViewModel == null)
            {
                _invoiceModalsViewModel = new InvoiceModalsViewModel();
                _invoiceModalsViewModel.InvoiceDeleted += RaiseUnsavedChanges;
                _invoiceModalsViewModel.HasPremium = _hasPremium;
                OnPropertyChanged();
            }
            return _invoiceModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the invoice template designer modal view model.
    /// </summary>
    public InvoiceTemplateDesignerViewModel InvoiceTemplateDesignerViewModel
    {
        get
        {
            if (_invoiceTemplateDesignerViewModel == null)
            {
                _invoiceTemplateDesignerViewModel = new InvoiceTemplateDesignerViewModel();
                _invoiceTemplateDesignerViewModel.TemplateSaved += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _invoiceTemplateDesignerViewModel;
        }
    }

    /// <summary>
    /// Gets the expense modals view model.
    /// </summary>
    public ExpenseModalsViewModel ExpenseModalsViewModel
    {
        get
        {
            if (_expenseModalsViewModel == null)
            {
                _expenseModalsViewModel = new ExpenseModalsViewModel();
                _expenseModalsViewModel.ExpenseSaved += RaiseUnsavedChanges;
                _expenseModalsViewModel.ExpenseDeleted += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _expenseModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the revenue modals view model.
    /// </summary>
    public RevenueModalsViewModel RevenueModalsViewModel
    {
        get
        {
            if (_revenueModalsViewModel == null)
            {
                _revenueModalsViewModel = new RevenueModalsViewModel();
                _revenueModalsViewModel.RevenueSaved += RaiseUnsavedChanges;
                _revenueModalsViewModel.RevenueDeleted += RaiseUnsavedChanges;
                OnPropertyChanged();
            }
            return _revenueModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the stock levels modals view model.
    /// </summary>
    public StockLevelsModalsViewModel StockLevelsModalsViewModel
    {
        get
        {
            if (_stockLevelsModalsViewModel == null)
            {
                _stockLevelsModalsViewModel = new StockLevelsModalsViewModel();
                OnPropertyChanged();
            }
            return _stockLevelsModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the locations modals view model.
    /// </summary>
    public LocationsModalsViewModel LocationsModalsViewModel
    {
        get
        {
            if (_locationsModalsViewModel == null)
            {
                _locationsModalsViewModel = new LocationsModalsViewModel();
                OnPropertyChanged();
            }
            return _locationsModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the stock adjustments modals view model.
    /// </summary>
    public StockAdjustmentsModalsViewModel StockAdjustmentsModalsViewModel
    {
        get
        {
            if (_stockAdjustmentsModalsViewModel == null)
            {
                _stockAdjustmentsModalsViewModel = new StockAdjustmentsModalsViewModel();
                OnPropertyChanged();
            }
            return _stockAdjustmentsModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the purchase orders modals view model.
    /// </summary>
    public PurchaseOrdersModalsViewModel PurchaseOrdersModalsViewModel
    {
        get
        {
            if (_purchaseOrdersModalsViewModel == null)
            {
                _purchaseOrdersModalsViewModel = new PurchaseOrdersModalsViewModel();
                OnPropertyChanged();
            }
            return _purchaseOrdersModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the receipts modals view model.
    /// </summary>
    public ReceiptsModalsViewModel ReceiptsModalsViewModel
    {
        get
        {
            if (_receiptsModalsViewModel == null)
            {
                _receiptsModalsViewModel = new ReceiptsModalsViewModel();
                if (_hasPremium)
                    _receiptsModalsViewModel.InvalidateScanServices();
                OnPropertyChanged();
            }
            return _receiptsModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the lost/damaged modals view model.
    /// </summary>
    public LostDamagedModalsViewModel LostDamagedModalsViewModel
    {
        get
        {
            if (_lostDamagedModalsViewModel == null)
            {
                _lostDamagedModalsViewModel = new LostDamagedModalsViewModel();
                OnPropertyChanged();
            }
            return _lostDamagedModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the returns modals view model.
    /// </summary>
    public ReturnsModalsViewModel ReturnsModalsViewModel
    {
        get
        {
            if (_returnsModalsViewModel == null)
            {
                _returnsModalsViewModel = new ReturnsModalsViewModel();
                OnPropertyChanged();
            }
            return _returnsModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the report modals view model.
    /// </summary>
    public ReportModalsViewModel ReportModalsViewModel
    {
        get
        {
            if (_reportModalsViewModel == null)
            {
                _reportModalsViewModel = new ReportModalsViewModel();
                OnPropertyChanged();
            }
            return _reportModalsViewModel;
        }
    }

    /// <summary>
    /// Gets the version history modal view model.
    /// </summary>
    public VersionHistoryModalViewModel VersionHistoryModalViewModel { get; }

    /// <summary>
    /// Gets the unsaved changes dialog view model.
    /// </summary>
    public UnsavedChangesDialogViewModel UnsavedChangesDialogViewModel { get; }

    /// <summary>
    /// Gets the custom date range modal view model.
    /// </summary>
    public CustomDateRangeModalViewModel CustomDateRangeModalViewModel { get; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Reference to the current ReportsPageViewModel when on Reports page.
    /// </summary>
    private ReportsPageViewModel? _reportsPageViewModel;

    /// <summary>
    /// Widget catalog VM exposed so AppShell can show the catalog modal above the entire window.
    /// </summary>
    [ObservableProperty]
    private WidgetCatalogViewModel? _widgetCatalog;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _currentPageName = "Dashboard";

    #endregion

    #region Update Available Banner

    [ObservableProperty]
    private bool _showUpdateAvailableBanner;

    [ObservableProperty]
    private string _updateBannerMessage = "";

    /// <summary>
    /// Shows the update available banner with the given version.
    /// </summary>
    public void ShowUpdateBanner(string version)
    {
        UpdateBannerMessage = $"{"A new version".Translate()} {version} {"is available.".Translate()}";
        ShowUpdateAvailableBanner = true;
    }

    /// <summary>
    /// Dismisses the update available banner.
    /// </summary>
    [RelayCommand]
    private void DismissUpdateBanner()
    {
        ShowUpdateAvailableBanner = false;
    }

    /// <summary>
    /// Opens the Check for Update modal from the banner and starts downloading.
    /// </summary>
    [RelayCommand]
    private void OpenUpdateModalFromBanner()
    {
        ShowUpdateAvailableBanner = false;
        CheckForUpdateModalViewModel.OpenAndDownloadCommand.Execute(null);
    }

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public AppShellViewModel() : this(null)
    {
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="navigationService">Navigation service.</param>
    /// <param name="updateService">Optional update service (desktop only).</param>
    public AppShellViewModel(INavigationService? navigationService, IUpdateService? updateService = null)
    {
        _navigationService = navigationService;

        // Create sidebar with navigation service
        SidebarViewModel = new SidebarViewModel(navigationService);

        // Create header with navigation service
        HeaderViewModel = new HeaderViewModel(navigationService);

        // Create quick actions panel with navigation service and link to sidebar
        QuickActionsViewModel = new QuickActionsViewModel(navigationService);
        QuickActionsViewModel.SetSidebarViewModel(SidebarViewModel);

        // Create notification panel with header view model (shares notifications)
        NotificationPanelViewModel = new NotificationPanelViewModel(HeaderViewModel);

        // Create file menu panel with navigation service and link to sidebar for dynamic positioning
        FileMenuPanelViewModel = new FileMenuPanelViewModel();
        FileMenuPanelViewModel.SetSidebarViewModel(SidebarViewModel);

        // Create help panel with navigation service
        HelpPanelViewModel = new HelpPanelViewModel();

        // Create upgrade modal
        UpgradeModalViewModel = new UpgradeModalViewModel();

        // Wire up license verification to enable premium features
        UpgradeModalViewModel.KeyVerified += (_, _) =>
        {
            SetPremiumStatus(true);
        };

        // Create company creation wizard
        CreateCompanyViewModel = new CreateCompanyViewModel();

        // Create company switcher panel
        CompanySwitcherPanelViewModel = new CompanySwitcherPanelViewModel();

        // Create settings modal
        SettingsModalViewModel = new SettingsModalViewModel();

        // Create check for update modal (with real update service on desktop, stub otherwise)
        CheckForUpdateModalViewModel = updateService != null
            ? new CheckForUpdateModalViewModel(updateService)
            : new CheckForUpdateModalViewModel();

        // Create prediction info modal
        PredictionInfoModalViewModel = new PredictionInfoModalViewModel();

        // Create past predictions modal
        PastPredictionsModalViewModel = new PastPredictionsModalViewModel();

        // Create import modal
        ImportModalViewModel = new ImportModalViewModel();

        // Create import validation dialog
        ImportValidationDialogViewModel = new ImportValidationDialogViewModel();
        ImportResultDialogViewModel = new ImportResultDialogViewModel();

        // Create import mapping dialog
        ImportMappingDialogViewModel = new ImportMappingDialogViewModel();

        // Create export as modal
        ExportAsModalViewModel = new ExportAsModalViewModel();

        // Create switch account modal
        SwitchAccountModalViewModel = new SwitchAccountModalViewModel();

        // Create password prompt modal
        PasswordPromptModalViewModel = new PasswordPromptModalViewModel();

        // Create edit company modal
        EditCompanyModalViewModel = new EditCompanyModalViewModel();

        // Create version history modal
        VersionHistoryModalViewModel = new VersionHistoryModalViewModel();

        // Create unsaved changes dialog
        UnsavedChangesDialogViewModel = new UnsavedChangesDialogViewModel();

        // Create custom date range modal
        CustomDateRangeModalViewModel = new CustomDateRangeModalViewModel();

        // Register navigation guard for unsaved changes check
        if (_navigationService is NavigationService navService)
        {
            navService.RegisterNavigationGuard(CheckUnsavedChangesBeforeNavigation);
        }

        // Wire up switch account modal's create account to open company wizard
        SwitchAccountModalViewModel.CreateAccountRequested += (_, _) => CreateCompanyViewModel.OpenCommand.Execute(null);

        // Wire up hamburger menu to toggle sidebar
        HeaderViewModel.ToggleSidebarRequested += (_, _) => SidebarViewModel.IsCollapsed = !SidebarViewModel.IsCollapsed;

        // Wire up header's quick actions button to open the panel in dropdown mode
        HeaderViewModel.OpenQuickActionsRequested += (_, _) => QuickActionsViewModel.OpenDropdownCommand.Execute(null);

        // Wire up header's notification button to toggle the notification panel
        HeaderViewModel.OpenNotificationsRequested += (_, _) => NotificationPanelViewModel.ToggleCommand.Execute(null);

        // Wire up header's file menu button to toggle the file menu panel
        HeaderViewModel.OpenFileMenuRequested += (_, _) => FileMenuPanelViewModel.ToggleCommand.Execute(null);

        // Wire up notification panel's settings to open settings modal at notifications tab
        NotificationPanelViewModel.OpenNotificationSettingsRequested += (_, _) => SettingsModalViewModel.OpenWithTab(1);

        // Wire up settings modal's upgrade request to open upgrade modal
        SettingsModalViewModel.UpgradeRequested += (_, _) => UpgradeModalViewModel.OpenCommand.Execute(null);

        // Wire up header's help button to toggle help panel
        HeaderViewModel.OpenHelpRequested += (_, _) => HelpPanelViewModel.ToggleCommand.Execute(null);

        // Wire up header's upgrade button to open upgrade modal
        HeaderViewModel.OpenUpgradeRequested += (_, _) => UpgradeModalViewModel.OpenCommand.Execute(null);

        // Wire up header's settings button to open settings modal
        HeaderViewModel.OpenSettingsRequested += (_, _) => SettingsModalViewModel.OpenCommand.Execute(null);

        // Wire up header's history button to open version history modal
        HeaderViewModel.OpenHistoryRequested += (_, _) => VersionHistoryModalViewModel.OpenCommand.Execute(null);

        // Wire up file menu's create new company to open the wizard
        FileMenuPanelViewModel.CreateNewCompanyRequested += (_, _) => CreateCompanyViewModel.OpenCommand.Execute(null);

        // Wire up sidebar's company header click to open the company switcher
        SidebarViewModel.OpenCompanySwitcherRequested += (_, _) => CompanySwitcherPanelViewModel.ToggleCommand.Execute(null);

        // Wire up sidebar navigation to close all panels
        SidebarViewModel.NavigationRequested += (_, _) => CloseAllPanels();

        // Wire up company switcher's create new company to open the wizard
        CompanySwitcherPanelViewModel.CreateNewCompanyRequested += (_, _) => CreateCompanyViewModel.OpenCommand.Execute(null);

        // Wire up help panel's check for updates to open the check for update modal
        HelpPanelViewModel.CheckForUpdatesRequested += (_, _) => CheckForUpdateModalViewModel.OpenCommand.Execute(null);

        // Wire up help panel's restart tutorial to show the tutorial welcome
        HelpPanelViewModel.RestartTutorialRequested += OnRestartTutorialRequested;

        // Wire up file menu's import to open the import modal
        FileMenuPanelViewModel.ImportRequested += (_, _) => ImportModalViewModel.OpenCommand.Execute(null);

        // Wire up file menu's export as to open the export as modal
        FileMenuPanelViewModel.ExportAsRequested += (_, _) => ExportAsModalViewModel.OpenCommand.Execute(null);

        // Sync search query between header and quick actions
        HeaderViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(HeaderViewModel.SearchQuery))
            {
                QuickActionsViewModel.SearchQuery = HeaderViewModel.SearchQuery;
            }
        };

        // Wire up header search key events to quick actions panel
        HeaderViewModel.SearchKeyPressed += (_, action) =>
        {
            switch (action)
            {
                case SearchKeyAction.Escape:
                    QuickActionsViewModel.CloseCommand.Execute(null);
                    HeaderViewModel.SearchQuery = null;
                    break;
                case SearchKeyAction.Up:
                    QuickActionsViewModel.MoveUpCommand.Execute(null);
                    break;
                case SearchKeyAction.Down:
                    QuickActionsViewModel.MoveDownCommand.Execute(null);
                    break;
                case SearchKeyAction.Enter:
                    QuickActionsViewModel.ExecuteSelectedCommand.Execute(null);
                    break;
            }
        };

        // Wire up quick action execution for modals
        QuickActionsViewModel.ActionRequested += (_, e) =>
        {
            // Handle actions that open modals after navigation
            switch (QuickActionNameExtensions.ParseQuickActionName(e.ActionName))
            {
                case QuickActionName.OpenAddModal:
                    switch (NavigationTargetExtensions.ParseNavigationTarget(e.NavigationTarget))
                    {
                        case NavigationTarget.RentalInventory:
                            RentalInventoryModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case NavigationTarget.RentalRecords:
                            RentalRecordsModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case NavigationTarget.Customers:
                            CustomerModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case NavigationTarget.Products:
                            ProductModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case NavigationTarget.Suppliers:
                            SupplierModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case NavigationTarget.Invoices:
                            InvoiceModalsViewModel.OpenCreateModal();
                            break;
                        case NavigationTarget.Expenses:
                            ExpenseModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case NavigationTarget.Revenue:
                            RevenueModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case NavigationTarget.Payments:
                            PaymentModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case NavigationTarget.Categories:
                            CategoryModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                    }
                    break;
                case QuickActionName.OpenSettings:
                    SettingsModalViewModel.OpenCommand.Execute(null);
                    break;
                case QuickActionName.OpenHelp:
                    HelpPanelViewModel.ToggleCommand.Execute(null);
                    break;
                case QuickActionName.OpenExport:
                    ExportAsModalViewModel.OpenCommand.Execute(null);
                    break;
                case QuickActionName.OpenImport:
                    ImportModalViewModel.OpenCommand.Execute(null);
                    break;
                case QuickActionName.OpenScanModal:
                    OpenFileScanRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case QuickActionName.OpenEditCompany:
                    EditCompanyRequested?.Invoke(this, EventArgs.Empty);
                    break;
                case QuickActionName.OpenCheckForUpdates:
                    CheckForUpdateModalViewModel.OpenCommand.Execute(null);
                    break;
            }
        };

        // Subscribe to navigation events to update UI state
        if (_navigationService != null)
        {
            _navigationService.Navigated += OnNavigated;
        }
    }

    /// <summary>
    /// Opens the quick actions panel in modal mode (Ctrl+K).
    /// </summary>
    [RelayCommand]
    private void OpenQuickActions()
    {
        QuickActionsViewModel.OpenModalCommand.Execute(null);
    }

    /// <summary>
    /// Handles navigation events to update UI state.
    /// </summary>
    private void OnNavigated(object? sender, Core.Services.NavigationEventArgs e)
    {
        CurrentPageName = e.PageName;
        SidebarViewModel.SetActivePage(e.PageName);
        HeaderViewModel.SetPageTitle(e.PageName);

        // Track DashboardPageViewModel when on Dashboard page
        if (e.PageName == "Dashboard" && CurrentPage is Control { DataContext: DashboardPageViewModel dashVm })
        {
            WidgetCatalog = dashVm.LayoutViewModel.Catalog;
        }
        else
        {
            // Close the catalog before clearing the reference, otherwise the
            // panel's IsVisible binding breaks (null DataContext → default true)
            if (WidgetCatalog != null)
                WidgetCatalog.IsOpen = false;
            WidgetCatalog = null;
        }

        // Track ReportsPageViewModel when on Reports page
        if (e.PageName == "Reports" && CurrentPage is Control { DataContext: ReportsPageViewModel reportsVm })
        {
            _reportsPageViewModel = reportsVm;
            // Wire up the unsaved changes confirmation for Previous button
            _reportsPageViewModel.ConfirmDiscardChangesAsync = ConfirmDiscardReportChangesAsync;
            // Set the ReportsPageViewModel on the ReportModalsViewModel for modal access
            ReportModalsViewModel.ReportsPageViewModel = reportsVm;
        }
        else if (e.PageName != "Reports")
        {
            _reportsPageViewModel = null;
            ReportModalsViewModel.ReportsPageViewModel = null;
        }
    }

    /// <summary>
    /// Confirms discarding unsaved report changes when going back from Layout Designer.
    /// </summary>
    /// <returns>True if changes should be discarded, false to cancel.</returns>
    private async Task<bool> ConfirmDiscardReportChangesAsync()
    {
        var result = await UnsavedChangesDialogViewModel.ShowSimpleAsync(
            "Unsaved Report Changes".Translate(),
            "You have unsaved changes in the layout designer. Would you like to save them?".Translate());

        switch (result)
        {
            case UnsavedChangesResult.Save:
                // Open the save template modal and wait for it to complete
                if (_reportsPageViewModel != null)
                {
                    var saved = await _reportsPageViewModel.OpenSaveTemplateAndWaitAsync();
                    return saved;
                }
                return false;

            case UnsavedChangesResult.DontSave:
                return true; // Discard changes and go back

            case UnsavedChangesResult.Cancel:
            default:
                return false; // Cancel, stay on current step
        }
    }

    /// <summary>
    /// Navigation guard that checks for unsaved changes when leaving the Reports page.
    /// </summary>
    private async Task<bool> CheckUnsavedChangesBeforeNavigation(string fromPage, string toPage)
    {
        // Check when leaving the Dashboard page in edit mode
        if (fromPage == "Dashboard" && CurrentPage is Control { DataContext: DashboardPageViewModel dashVm }
            && dashVm.LayoutViewModel.IsEditMode)
        {
            var result = await UnsavedChangesDialogViewModel.ShowSimpleAsync(
                "Unsaved Dashboard Changes".Translate(),
                "You have unsaved changes to the dashboard layout. Would you like to save them before leaving?".Translate());

            switch (result)
            {
                case UnsavedChangesResult.Save:
                    await dashVm.LayoutViewModel.SaveEditCommand.ExecuteAsync(null);
                    return true;

                case UnsavedChangesResult.DontSave:
                    dashVm.LayoutViewModel.CancelEditCommand.Execute(null);
                    return true;

                case UnsavedChangesResult.Cancel:
                default:
                    return false;
            }
        }

        // Check when leaving the Reports page
        if (fromPage != "Reports" || _reportsPageViewModel == null)
        {
            return true; // Allow navigation
        }

        // Check if there are unsaved changes
        if (!_reportsPageViewModel.HasUnsavedChanges)
        {
            return true; // No changes, allow navigation
        }

        // Show unsaved changes dialog
        var result2 = await UnsavedChangesDialogViewModel.ShowSimpleAsync(
            "Unsaved Report Changes".Translate(),
            "You have unsaved changes in the report designer. Would you like to save them before leaving?".Translate());

        switch (result2)
        {
            case UnsavedChangesResult.Save:
                // Open the save template modal and wait for it to complete
                var saved = await _reportsPageViewModel.OpenSaveTemplateAndWaitAsync();
                // If user cancelled the save modal, don't proceed with navigation
                return saved;

            case UnsavedChangesResult.DontSave:
                return true; // Discard changes and navigate

            case UnsavedChangesResult.Cancel:
            default:
                return false; // Cancel navigation
        }
    }

    /// <summary>
    /// Sets the company information on the sidebar.
    /// </summary>
    public void SetCompanyInfo(string? companyName, Bitmap? logo = null, string? userRole = null)
    {
        SidebarViewModel.SetCompanyInfo(companyName, logo, userRole);
    }

    /// <summary>
    /// Sets the user information on the header.
    /// </summary>
    public void SetUserInfo(string? displayName, string? email = null, string? role = null, int userId = 0)
    {
        HeaderViewModel.SetUserInfo(displayName, email, role, userId: userId);
    }

    /// <summary>
    /// Updates feature visibility based on settings.
    /// </summary>
    public void UpdateFeatureVisibility(bool showTransactions, bool showInventory, bool showRentals, bool showPayroll)
    {
        SidebarViewModel.UpdateFeatureVisibility(showTransactions, showInventory, showRentals, showPayroll);
    }

    /// <summary>
    /// Sets the premium status to show or hide premium features.
    /// </summary>
    public void SetPremiumStatus(bool hasPremium)
    {
        _hasPremium = hasPremium;

        SidebarViewModel.HasPremium = hasPremium;
        SettingsModalViewModel.HasPremium = hasPremium;
        UpgradeModalViewModel.HasPremium = hasPremium;
        HeaderViewModel.HasPremium = hasPremium;

        if (_invoiceModalsViewModel != null)
            _invoiceModalsViewModel.HasPremium = hasPremium;

        // Only invalidate if already created
        _receiptsModalsViewModel?.InvalidateScanServices();

        // Notify lazily-created page ViewModels (e.g., InsightsPageViewModel)
        App.RaisePlanStatusChanged(hasPremium);
    }

    /// <summary>
    /// Sets the enterprise plan status to show or hide enterprise features.
    /// </summary>
    public void SetEnterpriseStatus(bool hasEnterprise)
    {
        SidebarViewModel.HasEnterprise = hasEnterprise;
    }

    /// <summary>
    /// Sets all plan statuses at once.
    /// </summary>
    public void SetPlanStatus(bool hasPremium, bool hasEnterprise = false)
    {
        SidebarViewModel.HasPremium = hasPremium;
        SidebarViewModel.HasEnterprise = hasEnterprise;
        SettingsModalViewModel.HasPremium = hasPremium;
        UpgradeModalViewModel.HasPremium = hasPremium;
        HeaderViewModel.HasPremium = hasPremium;


        // Notify any subscribers (e.g., ProductsPageViewModel) of plan status change
        App.RaisePlanStatusChanged(hasPremium);
    }

    /// <summary>
    /// Navigates to a page programmatically.
    /// </summary>
    public void NavigateTo(string pageName)
    {
        SidebarViewModel.SetActivePage(pageName);
        HeaderViewModel.SetPageTitle(pageName);
        CurrentPageName = pageName;
        _navigationService?.NavigateTo(pageName);
    }

    /// <summary>
    /// Checks if the reports page has unsaved changes.
    /// </summary>
    public bool HasReportsPageUnsavedChanges => _reportsPageViewModel?.HasUnsavedChanges ?? false;

    /// <summary>
    /// Shows the unsaved changes dialog for reports and returns whether to proceed.
    /// </summary>
    /// <returns>True if should proceed (save or discard), false if cancelled.</returns>
    public async Task<bool> ConfirmReportsUnsavedChangesAsync()
    {
        if (_reportsPageViewModel == null || !_reportsPageViewModel.HasUnsavedChanges)
        {
            return true;
        }

        var result = await UnsavedChangesDialogViewModel.ShowSimpleAsync(
            "Unsaved Report Changes".Translate(),
            "You have unsaved changes in the report designer. Would you like to save them?".Translate());

        switch (result)
        {
            case UnsavedChangesResult.Save:
                // Open the save template modal and wait for it to complete
                var saved = await _reportsPageViewModel.OpenSaveTemplateAndWaitAsync();
                // If user cancelled the save modal, don't proceed with navigation
                return saved;

            case UnsavedChangesResult.DontSave:
                return true;

            case UnsavedChangesResult.Cancel:
            default:
                return false;
        }
    }

    /// <summary>
    /// Closes all open panels.
    /// </summary>
    private void CloseAllPanels()
    {
        NotificationPanelViewModel.CloseCommand.Execute(null);
        FileMenuPanelViewModel.CloseCommand.Execute(null);
        HelpPanelViewModel.CloseCommand.Execute(null);
        QuickActionsViewModel.CloseCommand.Execute(null);
        CompanySwitcherPanelViewModel.CloseCommand.Execute(null);
        ClosePageContextMenus();
    }

    /// <summary>
    /// Closes any open context menus on the current page (column visibility, chart context, reports context).
    /// </summary>
    public void ClosePageContextMenus()
    {
        if (CurrentPage is not Control { DataContext: { } vm })
            return;

        var type = vm.GetType();

        // Close column visibility menu
        type.GetProperty("IsColumnMenuOpen", BindingFlags.Public | BindingFlags.Instance)?
            .SetValue(vm, false);

        // Close chart context menu
        type.GetProperty("IsChartContextMenuOpen", BindingFlags.Public | BindingFlags.Instance)?
            .SetValue(vm, false);

        // Close reports context menu
        type.GetProperty("IsContextMenuOpen", BindingFlags.Public | BindingFlags.Instance)?
            .SetValue(vm, false);
    }

    /// <summary>
    /// Handles the restart tutorial request from the Help panel.
    /// </summary>
    private void OnRestartTutorialRequested(object? sender, EventArgs e)
    {
        RestartTutorialRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Adds a notification.
    /// </summary>
    /// <param name="title">Notification title.</param>
    /// <param name="message">Notification message.</param>
    /// <param name="type">Notification type.</param>
    public void AddNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        HeaderViewModel.AddNotification(new NotificationItem
        {
            Title = title,
            Message = message,
            Type = type
        });
    }
}
