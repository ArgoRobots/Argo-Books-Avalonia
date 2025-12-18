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

    #endregion

    #region Navigation Properties

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

        // Create file menu panel with navigation service
        FileMenuPanelViewModel = new FileMenuPanelViewModel(navigationService);

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

        // Wire up notification panel's settings to open settings modal at notifications tab
        NotificationPanelViewModel.OpenNotificationSettingsRequested += (_, _) => SettingsModalViewModel.OpenWithTab(2);

        // Wire up user panel's switch account to open switch account modal
        UserPanelViewModel.SwitchAccountRequested += (_, _) => SwitchAccountModalViewModel.OpenCommand.Execute(null);

        // Wire up header's help button to toggle help panel
        HeaderViewModel.OpenHelpRequested += (_, _) => HelpPanelViewModel.ToggleCommand.Execute(null);

        // Wire up header's upgrade button to open upgrade modal
        HeaderViewModel.OpenUpgradeRequested += (_, _) => UpgradeModalViewModel.OpenCommand.Execute(null);

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
