using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
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
    /// Gets the user panel view model.
    /// </summary>
    public UserPanelViewModel UserPanelViewModel { get; }

    /// <summary>
    /// Gets the file menu panel view model.
    /// </summary>
    public FileMenuPanelViewModel FileMenuPanelViewModel { get; }

    /// <summary>
    /// Gets the profile modal view model.
    /// </summary>
    public ProfileModalViewModel ProfileModalViewModel { get; }

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
    /// Gets the import modal view model.
    /// </summary>
    public ImportModalViewModel ImportModalViewModel { get; }

    /// <summary>
    /// Gets the export as modal view model.
    /// </summary>
    public ExportAsModalViewModel ExportAsModalViewModel { get; }

    /// <summary>
    /// Gets the switch account modal view model.
    /// </summary>
    public SwitchAccountModalViewModel SwitchAccountModalViewModel { get; }

    /// <summary>
    /// Gets the login modal view model.
    /// </summary>
    public LoginModalViewModel LoginModalViewModel { get; }

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
    public CustomerModalsViewModel CustomerModalsViewModel { get; }

    /// <summary>
    /// Gets the product modals view model.
    /// </summary>
    public ProductModalsViewModel ProductModalsViewModel { get; }

    /// <summary>
    /// Gets the category modals view model.
    /// </summary>
    public CategoryModalsViewModel CategoryModalsViewModel { get; }

    /// <summary>
    /// Gets the department modals view model.
    /// </summary>
    public DepartmentModalsViewModel DepartmentModalsViewModel { get; }

    /// <summary>
    /// Gets the supplier modals view model.
    /// </summary>
    public SupplierModalsViewModel SupplierModalsViewModel { get; }

    /// <summary>
    /// Gets the rental inventory modals view model.
    /// </summary>
    public RentalInventoryModalsViewModel RentalInventoryModalsViewModel { get; }

    /// <summary>
    /// Gets the rental records modals view model.
    /// </summary>
    public RentalRecordsModalsViewModel RentalRecordsModalsViewModel { get; }

    /// <summary>
    /// Gets the payment modals view model.
    /// </summary>
    public PaymentModalsViewModel PaymentModalsViewModel { get; }

    /// <summary>
    /// Gets the invoice modals view model.
    /// </summary>
    public InvoiceModalsViewModel InvoiceModalsViewModel { get; }

    /// <summary>
    /// Gets the expense modals view model.
    /// </summary>
    public ExpenseModalsViewModel ExpenseModalsViewModel { get; }

    /// <summary>
    /// Gets the revenue modals view model.
    /// </summary>
    public RevenueModalsViewModel RevenueModalsViewModel { get; }

    /// <summary>
    /// Gets the stock levels modals view model.
    /// </summary>
    public StockLevelsModalsViewModel StockLevelsModalsViewModel { get; }

    /// <summary>
    /// Gets the locations modals view model.
    /// </summary>
    public LocationsModalsViewModel LocationsModalsViewModel { get; }

    /// <summary>
    /// Gets the unsaved changes dialog view model.
    /// </summary>
    public UnsavedChangesDialogViewModel UnsavedChangesDialogViewModel { get; }

    #endregion

    #region Navigation Properties

    /// <summary>
    /// Reference to the current ReportsPageViewModel when on Reports page.
    /// </summary>
    private ReportsPageViewModel? _reportsPageViewModel;

    [ObservableProperty]
    private object? _currentPage;

    [ObservableProperty]
    private string _currentPageName = "Dashboard";

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public AppShellViewModel() : this(null, null)
    {
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="navigationService">Navigation service.</param>
    /// <param name="settingsService">Settings service.</param>
    public AppShellViewModel(INavigationService? navigationService, ISettingsService? settingsService)
    {
        _navigationService = navigationService;

        // Create sidebar with navigation service
        SidebarViewModel = new SidebarViewModel(navigationService, settingsService);

        // Create header with navigation service
        HeaderViewModel = new HeaderViewModel(navigationService);

        // Create quick actions panel with navigation service and link to sidebar
        QuickActionsViewModel = new QuickActionsViewModel(navigationService);
        QuickActionsViewModel.SetSidebarViewModel(SidebarViewModel);

        // Create notification panel with header view model (shares notifications)
        NotificationPanelViewModel = new NotificationPanelViewModel(HeaderViewModel);

        // Create user panel with navigation service and header view model
        UserPanelViewModel = new UserPanelViewModel(navigationService, HeaderViewModel);

        // Create file menu panel with navigation service and link to sidebar for dynamic positioning
        FileMenuPanelViewModel = new FileMenuPanelViewModel(navigationService);
        FileMenuPanelViewModel.SetSidebarViewModel(SidebarViewModel);

        // Create profile modal with navigation service and header view model
        ProfileModalViewModel = new ProfileModalViewModel(navigationService, HeaderViewModel);

        // Create help panel with navigation service
        HelpPanelViewModel = new HelpPanelViewModel(navigationService);

        // Create upgrade modal
        UpgradeModalViewModel = new UpgradeModalViewModel();

        // Create company creation wizard
        CreateCompanyViewModel = new CreateCompanyViewModel();

        // Create company switcher panel
        CompanySwitcherPanelViewModel = new CompanySwitcherPanelViewModel();

        // Create settings modal
        SettingsModalViewModel = new SettingsModalViewModel();

        // Create check for update modal
        CheckForUpdateModalViewModel = new CheckForUpdateModalViewModel();

        // Create import modal
        ImportModalViewModel = new ImportModalViewModel();

        // Create export as modal
        ExportAsModalViewModel = new ExportAsModalViewModel();

        // Create switch account modal
        SwitchAccountModalViewModel = new SwitchAccountModalViewModel();

        // Create login modal
        LoginModalViewModel = new LoginModalViewModel();

        // Create password prompt modal
        PasswordPromptModalViewModel = new PasswordPromptModalViewModel();

        // Create edit company modal
        EditCompanyModalViewModel = new EditCompanyModalViewModel();

        // Create customer modals
        CustomerModalsViewModel = new CustomerModalsViewModel();

        // Create product modals
        ProductModalsViewModel = new ProductModalsViewModel();

        // Create category modals
        CategoryModalsViewModel = new CategoryModalsViewModel();

        // Create department modals
        DepartmentModalsViewModel = new DepartmentModalsViewModel();

        // Create supplier modals
        SupplierModalsViewModel = new SupplierModalsViewModel();

        // Create rental inventory modals
        RentalInventoryModalsViewModel = new RentalInventoryModalsViewModel();

        // Create rental records modals
        RentalRecordsModalsViewModel = new RentalRecordsModalsViewModel();

        // Create payment modals
        PaymentModalsViewModel = new PaymentModalsViewModel();

        // Create invoice modals
        InvoiceModalsViewModel = new InvoiceModalsViewModel();

        // Create expense modals
        ExpenseModalsViewModel = new ExpenseModalsViewModel();

        // Create revenue modals
        RevenueModalsViewModel = new RevenueModalsViewModel();

        // Create stock levels modals
        StockLevelsModalsViewModel = new StockLevelsModalsViewModel();

        // Create locations modals
        LocationsModalsViewModel = new LocationsModalsViewModel();

        // Create unsaved changes dialog
        UnsavedChangesDialogViewModel = new UnsavedChangesDialogViewModel();

        // Register navigation guard for unsaved changes check
        if (_navigationService is NavigationService navService)
        {
            navService.RegisterNavigationGuard(CheckUnsavedChangesBeforeNavigation);
        }

        // Wire up switch account modal's account selected to open login modal
        SwitchAccountModalViewModel.AccountSelected += (_, account) => LoginModalViewModel.OpenForAccount(account);

        // Wire up switch account modal's create account to open company wizard
        SwitchAccountModalViewModel.CreateAccountRequested += (_, _) => CreateCompanyViewModel.OpenCommand.Execute(null);

        // Wire up hamburger menu to toggle sidebar
        HeaderViewModel.ToggleSidebarRequested += (_, _) => SidebarViewModel.IsCollapsed = !SidebarViewModel.IsCollapsed;

        // Wire up header's quick actions button to open the panel in dropdown mode
        HeaderViewModel.OpenQuickActionsRequested += (_, _) => QuickActionsViewModel.OpenDropdownCommand.Execute(null);

        // Wire up header's notification button to toggle the notification panel
        HeaderViewModel.OpenNotificationsRequested += (_, _) => NotificationPanelViewModel.ToggleCommand.Execute(null);

        // Wire up header's user menu button to toggle the user panel
        HeaderViewModel.OpenUserMenuRequested += (_, _) => UserPanelViewModel.ToggleCommand.Execute(null);

        // Wire up header's file menu button to toggle the file menu panel
        HeaderViewModel.OpenFileMenuRequested += (_, _) => FileMenuPanelViewModel.ToggleCommand.Execute(null);

        // Wire up user panel's open profile to open profile modal
        UserPanelViewModel.OpenProfileRequested += (_, _) => ProfileModalViewModel.OpenCommand.Execute(null);

        // Wire up user panel's open help to open help panel
        UserPanelViewModel.OpenHelpRequested += (_, _) => HelpPanelViewModel.ToggleCommand.Execute(null);

        // Wire up user panel's open settings to open settings modal
        UserPanelViewModel.OpenSettingsRequested += (_, _) => SettingsModalViewModel.OpenCommand.Execute(null);

        // Wire up user panel's my plan to open upgrade modal
        UserPanelViewModel.OpenMyPlanRequested += (_, _) => UpgradeModalViewModel.OpenCommand.Execute(null);

        // Wire up notification panel's settings to open settings modal at notifications tab
        NotificationPanelViewModel.OpenNotificationSettingsRequested += (_, _) => SettingsModalViewModel.OpenWithTab(2);

        // Wire up settings modal's upgrade request to open upgrade modal
        SettingsModalViewModel.UpgradeRequested += (_, _) => UpgradeModalViewModel.OpenCommand.Execute(null);

        // Wire up user panel's switch account to open switch account modal
        UserPanelViewModel.SwitchAccountRequested += (_, _) => SwitchAccountModalViewModel.OpenCommand.Execute(null);

        // Wire up header's help button to toggle help panel
        HeaderViewModel.OpenHelpRequested += (_, _) => HelpPanelViewModel.ToggleCommand.Execute(null);

        // Wire up header's upgrade button to open upgrade modal
        HeaderViewModel.OpenUpgradeRequested += (_, _) => UpgradeModalViewModel.OpenCommand.Execute(null);

        // Wire up license verification to enable premium features
        UpgradeModalViewModel.KeyVerified += (_, _) =>
        {
            SidebarViewModel.HasPremium = true;
            UpgradeModalViewModel.HasPremium = true;
            HeaderViewModel.HasPremium = true;
            UserPanelViewModel.HasPremium = true;
        };

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
            switch (e.ActionName)
            {
                case "OpenAddModal":
                    switch (e.NavigationTarget)
                    {
                        case "RentalInventory":
                            RentalInventoryModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case "RentalRecords":
                            RentalRecordsModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case "Customers":
                            CustomerModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case "Products":
                            ProductModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case "Suppliers":
                            SupplierModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case "Invoices":
                            InvoiceModalsViewModel.OpenCreateModal();
                            break;
                        case "Expenses":
                            ExpenseModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case "Revenue":
                            RevenueModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case "Payments":
                            PaymentModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                        case "Categories":
                            CategoryModalsViewModel.OpenAddModalCommand.Execute(null);
                            break;
                    }
                    break;
                case "OpenSettings":
                    SettingsModalViewModel.OpenCommand.Execute(null);
                    break;
                case "OpenHelp":
                    HelpPanelViewModel.ToggleCommand.Execute(null);
                    break;
                case "OpenExport":
                    ExportAsModalViewModel.OpenCommand.Execute(null);
                    break;
                case "OpenImport":
                    ImportModalViewModel.OpenCommand.Execute(null);
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
    private void OnNavigated(object? sender, NavigationEventArgs e)
    {
        CurrentPageName = e.PageName;
        SidebarViewModel.SetActivePage(e.PageName);
        HeaderViewModel.SetPageTitle(e.PageName);

        // Track ReportsPageViewModel when on Reports page
        if (e.PageName == "Reports" && CurrentPage is Avalonia.Controls.Control { DataContext: ReportsPageViewModel reportsVm })
        {
            _reportsPageViewModel = reportsVm;
            // Wire up the unsaved changes confirmation for Previous button
            _reportsPageViewModel.ConfirmDiscardChangesAsync = ConfirmDiscardReportChangesAsync;
        }
        else if (e.PageName != "Reports")
        {
            _reportsPageViewModel = null;
        }
    }

    /// <summary>
    /// Confirms discarding unsaved report changes when going back from Layout Designer.
    /// </summary>
    /// <returns>True if changes should be discarded, false to cancel.</returns>
    private async Task<bool> ConfirmDiscardReportChangesAsync()
    {
        var result = await UnsavedChangesDialogViewModel.ShowSimpleAsync(
            "Unsaved Report Changes",
            "You have unsaved changes in the layout designer. Would you like to save them?");

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
        // Only check when leaving the Reports page
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
        var result = await UnsavedChangesDialogViewModel.ShowSimpleAsync(
            "Unsaved Report Changes",
            "You have unsaved changes in the report designer. Would you like to save them before leaving?");

        switch (result)
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
    public void SetUserInfo(string? displayName, string? email = null, string? role = null)
    {
        HeaderViewModel.SetUserInfo(displayName, email, role);
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
        SidebarViewModel.HasPremium = hasPremium;
    }

    /// <summary>
    /// Sets the standard plan status to show or hide standard features.
    /// </summary>
    public void SetStandardStatus(bool hasStandard)
    {
        SidebarViewModel.HasStandard = hasStandard;
        SettingsModalViewModel.HasStandard = hasStandard;
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
    public void SetPlanStatus(bool hasStandard, bool hasPremium, bool hasEnterprise = false)
    {
        SidebarViewModel.HasStandard = hasStandard;
        SidebarViewModel.HasPremium = hasPremium;
        SidebarViewModel.HasEnterprise = hasEnterprise;
        SettingsModalViewModel.HasStandard = hasStandard;
        UpgradeModalViewModel.HasStandard = hasStandard;
        UpgradeModalViewModel.HasPremium = hasPremium;
        HeaderViewModel.HasPremium = hasPremium;
        UserPanelViewModel.HasPremium = hasPremium;

        // Notify any subscribers (e.g., ProductsPageViewModel) of plan status change
        App.RaisePlanStatusChanged(hasStandard, hasPremium);
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
            "Unsaved Report Changes",
            "You have unsaved changes in the report designer. Would you like to save them?");

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
        UserPanelViewModel.CloseCommand.Execute(null);
        FileMenuPanelViewModel.CloseCommand.Execute(null);
        HelpPanelViewModel.CloseCommand.Execute(null);
        QuickActionsViewModel.CloseCommand.Execute(null);
        CompanySwitcherPanelViewModel.CloseCommand.Execute(null);
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

/// <summary>
/// Observable dictionary that notifies on changes for individual keys.
/// </summary>
public class ObservableDictionary<TKey, TValue> : ObservableCollection<KeyValuePair<TKey, TValue>>
    where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary = new();

    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set
        {
            _dictionary[key] = value;
            OnCollectionChanged(new System.Collections.Specialized.NotifyCollectionChangedEventArgs(
                System.Collections.Specialized.NotifyCollectionChangedAction.Reset));
        }
    }

    public void Add(TKey key, TValue value)
    {
        _dictionary.Add(key, value);
        Add(new KeyValuePair<TKey, TValue>(key, value));
    }

    public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);

    public ICollection<TKey> Keys => _dictionary.Keys;

    public bool TryGetValue(TKey key, out TValue? value) => _dictionary.TryGetValue(key, out value);
}
