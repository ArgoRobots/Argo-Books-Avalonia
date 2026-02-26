using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Portal;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.ViewModels;
using ArgoBooks.Views;

namespace ArgoBooks;

public class App : Application
{
    /// <summary>
    /// Gets or sets the update service. Set by the platform-specific host (e.g., Desktop)
    /// before the application starts. Null on platforms that don't support auto-update.
    /// </summary>
    public static IUpdateService? UpdateService { get; set; }

    /// <summary>
    /// Gets the navigation service instance.
    /// </summary>
    public static NavigationService? NavigationService { get; private set; }

    /// <summary>
    /// Gets the company manager instance.
    /// </summary>
    public static CompanyManager? CompanyManager { get; private set; }

    /// <summary>
    /// Gets the global settings service instance.
    /// </summary>
    public static GlobalSettingsService? SettingsService { get; private set; }

    /// <summary>
    /// Gets the file service instance for sample company creation.
    /// </summary>
    private static FileService? _fileService;

    /// <summary>
    /// Gets the payment portal service instance for online payment integration.
    /// </summary>
    public static PaymentPortalService? PaymentPortalService { get; private set; }

    /// <summary>
    /// Gets the license service instance for secure license storage.
    /// </summary>
    public static LicenseService? LicenseService { get; private set; }

    /// <summary>
    /// Gets the error logger instance for centralized error logging.
    /// </summary>
    public static IErrorLogger? ErrorLogger { get; private set; }

    /// <summary>
    /// Gets the telemetry manager instance for anonymous usage tracking.
    /// </summary>
    public static ITelemetryManager? TelemetryManager { get; private set; }

    /// <summary>
    /// Gets the shared undo/redo manager instance.
    /// </summary>
    public static UndoRedoManager UndoRedoManager => HeaderViewModel.SharedUndoRedoManager;

    /// <summary>
    /// Gets the customer modals view model for shared access.
    /// </summary>
    public static CustomerModalsViewModel? CustomerModalsViewModel => _appShellViewModel?.CustomerModalsViewModel;

    /// <summary>
    /// Gets the product modals view model for shared access.
    /// </summary>
    public static ProductModalsViewModel? ProductModalsViewModel => _appShellViewModel?.ProductModalsViewModel;

    /// <summary>
    /// Gets the category modals view model for shared access.
    /// </summary>
    public static CategoryModalsViewModel? CategoryModalsViewModel => _appShellViewModel?.CategoryModalsViewModel;

    /// <summary>
    /// Gets the department modals view model for shared access.
    /// </summary>
    public static DepartmentModalsViewModel? DepartmentModalsViewModel => _appShellViewModel?.DepartmentModalsViewModel;

    /// <summary>
    /// Gets the supplier modals view model for shared access.
    /// </summary>
    public static SupplierModalsViewModel? SupplierModalsViewModel => _appShellViewModel?.SupplierModalsViewModel;

    /// <summary>
    /// Gets the rental inventory modals view model for shared access.
    /// </summary>
    public static RentalInventoryModalsViewModel? RentalInventoryModalsViewModel => _appShellViewModel?.RentalInventoryModalsViewModel;

    /// <summary>
    /// Gets the rental records modals view model for shared access.
    /// </summary>
    public static RentalRecordsModalsViewModel? RentalRecordsModalsViewModel => _appShellViewModel?.RentalRecordsModalsViewModel;

    /// <summary>
    /// Gets the payment modals view model for shared access.
    /// </summary>
    public static PaymentModalsViewModel? PaymentModalsViewModel => _appShellViewModel?.PaymentModalsViewModel;

    /// <summary>
    /// Gets the invoice modals view model for shared access.
    /// </summary>
    public static InvoiceModalsViewModel? InvoiceModalsViewModel => _appShellViewModel?.InvoiceModalsViewModel;

    /// <summary>
    /// Gets the invoice template designer view model for shared access.
    /// </summary>
    public static InvoiceTemplateDesignerViewModel? InvoiceTemplateDesignerViewModel => _appShellViewModel?.InvoiceTemplateDesignerViewModel;

    /// <summary>
    /// Gets the expense modals view model for shared access.
    /// </summary>
    public static ExpenseModalsViewModel? ExpenseModalsViewModel => _appShellViewModel?.ExpenseModalsViewModel;

    /// <summary>
    /// Gets the revenue modals view model for shared access.
    /// </summary>
    public static RevenueModalsViewModel? RevenueModalsViewModel => _appShellViewModel?.RevenueModalsViewModel;

    /// <summary>
    /// Gets the stock levels modals view model for shared access.
    /// </summary>
    public static StockLevelsModalsViewModel? StockLevelsModalsViewModel => _appShellViewModel?.StockLevelsModalsViewModel;

    /// <summary>
    /// Gets the locations modals view model for shared access.
    /// </summary>
    public static LocationsModalsViewModel? LocationsModalsViewModel => _appShellViewModel?.LocationsModalsViewModel;

    /// <summary>
    /// Gets the stock adjustments modals view model for shared access.
    /// </summary>
    public static StockAdjustmentsModalsViewModel? StockAdjustmentsModalsViewModel => _appShellViewModel?.StockAdjustmentsModalsViewModel;

    /// <summary>
    /// Gets the purchase orders modals view model for shared access.
    /// </summary>
    public static PurchaseOrdersModalsViewModel? PurchaseOrdersModalsViewModel => _appShellViewModel?.PurchaseOrdersModalsViewModel;

    /// <summary>
    /// Gets the receipts modals view model for shared access.
    /// </summary>
    public static ReceiptsModalsViewModel? ReceiptsModalsViewModel => _appShellViewModel?.ReceiptsModalsViewModel;

    /// <summary>
    /// Gets the lost/damaged modals view model for shared access.
    /// </summary>
    public static LostDamagedModalsViewModel? LostDamagedModalsViewModel => _appShellViewModel?.LostDamagedModalsViewModel;

    /// <summary>
    /// Gets the returns modals view model for shared access.
    /// </summary>
    public static ReturnsModalsViewModel? ReturnsModalsViewModel => _appShellViewModel?.ReturnsModalsViewModel;

    /// <summary>
    /// Gets the report modals view model for shared access.
    /// </summary>
    public static ReportModalsViewModel? ReportModalsViewModel => _appShellViewModel?.ReportModalsViewModel;

    /// <summary>
    /// Gets the prediction info modal view model for shared access.
    /// </summary>
    public static PredictionInfoModalViewModel? PredictionInfoModalViewModel => _appShellViewModel?.PredictionInfoModalViewModel;

    /// <summary>
    /// Gets the past predictions modal view model for shared access.
    /// </summary>
    public static PastPredictionsModalViewModel? PastPredictionsModalViewModel => _appShellViewModel?.PastPredictionsModalViewModel;

    /// <summary>
    /// Gets the quick actions settings modal view model for shared access.
    /// </summary>
    public static QuickActionsSettingsModalViewModel? QuickActionsSettingsModalViewModel => _appShellViewModel?.QuickActionsSettingsModalViewModel;

    /// <summary>
    /// Gets the settings modal view model for shared access.
    /// </summary>
    public static SettingsModalViewModel? SettingsModalViewModel => _appShellViewModel?.SettingsModalViewModel;

    /// <summary>
    /// Gets the header view model for shared access.
    /// </summary>
    public static HeaderViewModel? HeaderViewModel => _appShellViewModel?.HeaderViewModel;

    /// <summary>
    /// Gets the version history modal view model for shared access.
    /// </summary>
    public static VersionHistoryModalViewModel? VersionHistoryModalViewModel => _appShellViewModel?.VersionHistoryModalViewModel;

    /// <summary>
    /// Gets the event log service for version history tracking.
    /// </summary>
    public static Services.EventLogService? EventLogService { get; private set; }

    /// <summary>
    /// Gets the categories tutorial view model for first-visit tutorial.
    /// </summary>
    public static CategoriesTutorialViewModel? CategoriesTutorialViewModel => _mainWindowViewModel?.CategoriesTutorialViewModel;

    /// <summary>
    /// Gets the products tutorial view model for first-visit tutorial.
    /// </summary>
    public static ProductsTutorialViewModel? ProductsTutorialViewModel => _mainWindowViewModel?.ProductsTutorialViewModel;

    /// <summary>
    /// Adds a notification to the notification panel.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="type">The notification type.</param>
    public static void AddNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        _appShellViewModel?.AddNotification(title, message, type);
    }

    /// <summary>
    /// Shows a modal error message box.
    /// </summary>
    private static async Task ShowErrorMessageBoxAsync(string title, string message)
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow
            && mainWindow.MessageBoxService is { } messageBoxService)
        {
            await messageBoxService.ShowErrorAsync(title, message);
        }
    }

    /// <summary>
    /// Checks for low stock items, out of stock items, overdue invoices, and overdue rentals,
    /// and sends notifications if enabled. Only sends once per day to avoid duplicates.
    /// </summary>
    private static void CheckAndSendNotifications()
    {
        var companyData = CompanyManager?.CompanyData;
        if (companyData == null)
            return;

        var settings = companyData.Settings.Notifications;

        // Only send startup notifications once per day to avoid duplicates
        var today = DateTime.Today;
        if (settings.LastAlertCheckDate.HasValue && settings.LastAlertCheckDate.Value.Date == today)
            return;

        // Update the last check date
        settings.LastAlertCheckDate = today;

        // Check for low stock items
        if (settings.LowStockAlert)
        {
            var lowStockItems = companyData.Inventory
                .Where(item => item.CalculateStatus() == InventoryStatus.LowStock)
                .ToList();

            if (lowStockItems.Count > 0)
            {
                var message = lowStockItems.Count == 1
                    ? "1 item is running low on stock.".Translate()
                    : "{0} items are running low on stock.".TranslateFormat(lowStockItems.Count);

                AddNotification(
                    "Low Stock Alert".Translate(),
                    message,
                    NotificationType.Warning);
            }
        }

        // Check for out of stock items
        if (settings.OutOfStockAlert)
        {
            var outOfStockItems = companyData.Inventory
                .Where(item => item.CalculateStatus() == InventoryStatus.OutOfStock)
                .ToList();

            if (outOfStockItems.Count > 0)
            {
                var message = outOfStockItems.Count == 1
                    ? "1 item is out of stock.".Translate()
                    : "{0} items are out of stock.".TranslateFormat(outOfStockItems.Count);

                AddNotification(
                    "Out of Stock Alert".Translate(),
                    message,
                    NotificationType.Warning);
            }
        }

        // Check for overdue invoices
        if (settings.InvoiceOverdueAlert)
        {
            var overdueInvoices = companyData.Invoices
                .Where(invoice => invoice.IsOverdue)
                .ToList();

            if (overdueInvoices.Count > 0)
            {
                var message = overdueInvoices.Count == 1
                    ? "1 invoice is overdue.".Translate()
                    : "{0} invoices are overdue.".TranslateFormat(overdueInvoices.Count);

                AddNotification(
                    "Invoice Overdue".Translate(),
                    message,
                    NotificationType.Warning);
            }
        }

        // Check for overdue rentals
        if (settings.RentalOverdueAlert)
        {
            var overdueRentals = companyData.Rentals
                .Where(rental => rental.IsOverdue)
                .ToList();

            if (overdueRentals.Count > 0)
            {
                var message = overdueRentals.Count == 1
                    ? "1 rental is overdue.".Translate()
                    : "{0} rentals are overdue.".TranslateFormat(overdueRentals.Count);

                AddNotification(
                    "Rental Overdue".Translate(),
                    message,
                    NotificationType.Warning);
            }
        }
    }

    /// <summary>
    /// Checks if an inventory item is low stock or out of stock and sends a notification if enabled.
    /// Call this after saving a stock adjustment.
    /// </summary>
    /// <param name="item">The inventory item to check.</param>
    public static void CheckAndNotifyStockStatus(InventoryItem item)
    {
        var settings = CompanyManager?.CompanyData?.Settings.Notifications;
        if (settings == null)
            return;

        var status = item.CalculateStatus();

        if (settings.OutOfStockAlert && status == InventoryStatus.OutOfStock)
        {
            var product = CompanyManager?.CompanyData?.GetProduct(item.ProductId);
            var productName = product?.Name ?? "Item";
            AddNotification(
                "Out of Stock".Translate(),
                "{0} is now out of stock.".TranslateFormat(productName),
                NotificationType.Warning);
        }
        else if (settings.LowStockAlert && status == InventoryStatus.LowStock)
        {
            var product = CompanyManager?.CompanyData?.GetProduct(item.ProductId);
            var productName = product?.Name ?? "Item";
            AddNotification(
                "Low Stock".Translate(),
                "{0} is running low on stock.".TranslateFormat(productName),
                NotificationType.Warning);
        }
    }

    /// <summary>
    /// Checks if an invoice is overdue and sends a notification if enabled.
    /// Call this after saving an invoice.
    /// </summary>
    /// <param name="invoice">The invoice to check.</param>
    public static void CheckAndNotifyInvoiceOverdue(Invoice invoice)
    {
        var settings = CompanyManager?.CompanyData?.Settings.Notifications;
        if (settings == null || !settings.InvoiceOverdueAlert)
            return;

        if (invoice.IsOverdue)
        {
            AddNotification(
                "Invoice Overdue".Translate(),
                "Invoice {0} is overdue.".TranslateFormat(invoice.InvoiceNumber),
                NotificationType.Warning);
        }
    }

    /// <summary>
    /// Checks if a rental is overdue and sends a notification if enabled.
    /// Call this after saving a rental record.
    /// </summary>
    /// <param name="rental">The rental record to check.</param>
    public static void CheckAndNotifyRentalOverdue(RentalRecord rental)
    {
        var settings = CompanyManager?.CompanyData?.Settings.Notifications;
        if (settings == null || !settings.RentalOverdueAlert)
            return;

        if (rental.IsOverdue)
        {
            var customer = CompanyManager?.CompanyData?.GetCustomer(rental.CustomerId);
            var customerName = customer?.Name ?? "Customer";
            AddNotification(
                "Rental Overdue".Translate(),
                "Rental for {0} is overdue.".TranslateFormat(customerName),
                NotificationType.Warning);
        }
    }

    #region Plan Status Events

    /// <summary>
    /// Event raised when the plan status changes (e.g., user upgrades).
    /// </summary>
    public static event EventHandler<PlanStatusChangedEventArgs>? PlanStatusChanged;

    /// <summary>
    /// Raises the PlanStatusChanged event.
    /// </summary>
    public static void RaisePlanStatusChanged(bool hasPremium)
    {
        PlanStatusChanged?.Invoke(null, new PlanStatusChangedEventArgs(hasPremium));
    }

    #endregion

    #region Shared Table Column Widths

    /// <summary>
    /// Gets the shared column widths for the Revenue table.
    /// </summary>
    public static Controls.ColumnWidths.RevenueTableColumnWidths RevenueColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Expenses table.
    /// </summary>
    public static Controls.ColumnWidths.TableColumnWidths ExpensesColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Invoices table.
    /// </summary>
    public static Controls.ColumnWidths.InvoicesTableColumnWidths InvoicesColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Customers table.
    /// </summary>
    public static Controls.ColumnWidths.CustomersTableColumnWidths CustomersColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Suppliers table.
    /// </summary>
    public static Controls.ColumnWidths.SuppliersTableColumnWidths SuppliersColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Products table.
    /// </summary>
    public static Controls.ColumnWidths.ProductsTableColumnWidths ProductsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Categories table.
    /// </summary>
    public static Controls.ColumnWidths.CategoriesTableColumnWidths CategoriesColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Stock Levels table.
    /// </summary>
    public static Controls.ColumnWidths.StockLevelsTableColumnWidths StockLevelsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Departments table.
    /// </summary>
    public static Controls.ColumnWidths.DepartmentsTableColumnWidths DepartmentsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Payments table.
    /// </summary>
    public static Controls.ColumnWidths.PaymentsTableColumnWidths PaymentsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Receipts table.
    /// </summary>
    public static Controls.ColumnWidths.ReceiptsTableColumnWidths ReceiptsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Returns table.
    /// </summary>
    public static Controls.ColumnWidths.ReturnsTableColumnWidths ReturnsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Lost/Damaged table.
    /// </summary>
    public static Controls.ColumnWidths.LostDamagedTableColumnWidths LostDamagedColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Rental Records table.
    /// </summary>
    public static Controls.ColumnWidths.RentalRecordsTableColumnWidths RentalRecordsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Rental Inventory table.
    /// </summary>
    public static Controls.ColumnWidths.RentalInventoryTableColumnWidths RentalInventoryColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Locations table.
    /// </summary>
    public static Controls.ColumnWidths.LocationsTableColumnWidths LocationsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Stock Adjustments table.
    /// </summary>
    public static Controls.ColumnWidths.StockAdjustmentsTableColumnWidths StockAdjustmentsColumnWidths { get; } = new();

    /// <summary>
    /// Gets the shared column widths for the Purchase Orders table.
    /// </summary>
    public static Controls.ColumnWidths.PurchaseOrdersTableColumnWidths PurchaseOrdersColumnWidths { get; } = new();

    #endregion

    // View models stored for event wiring
    private static MainWindowViewModel? _mainWindowViewModel;
    private static AppShellViewModel? _appShellViewModel;
    private static WelcomeScreenViewModel? _welcomeScreenViewModel;
    private static IdleDetectionService? _idleDetectionService;

    // Cached page ViewModels to improve performance and prevent memory leaks from event subscriptions
    private static DashboardPageViewModel? _dashboardPageViewModel;
    private static AnalyticsPageViewModel? _analyticsPageViewModel;
    private static InsightsPageViewModel? _insightsPageViewModel;
    private static ReportsPageViewModel? _reportsPageViewModel;
    private static RevenuePageViewModel? _revenuePageViewModel;
    private static ExpensesPageViewModel? _expensesPageViewModel;
    private static InvoicesPageViewModel? _invoicesPageViewModel;
    private static PaymentsPageViewModel? _paymentsPageViewModel;
    private static ProductsPageViewModel? _productsPageViewModel;
    private static StockLevelsPageViewModel? _stockLevelsPageViewModel;
    private static LocationsPageViewModel? _locationsPageViewModel;
    private static StockAdjustmentsPageViewModel? _stockAdjustmentsPageViewModel;
    private static PurchaseOrdersPageViewModel? _purchaseOrdersPageViewModel;
    private static CategoriesPageViewModel? _categoriesPageViewModel;
    private static CustomersPageViewModel? _customersPageViewModel;
    private static SuppliersPageViewModel? _suppliersPageViewModel;
    private static DepartmentsPageViewModel? _departmentsPageViewModel;
    private static RentalInventoryPageViewModel? _rentalInventoryPageViewModel;
    private static RentalRecordsPageViewModel? _rentalRecordsPageViewModel;
    private static ReturnsPageViewModel? _returnsPageViewModel;
    private static LostDamagedPageViewModel? _lostDamagedPageViewModel;
    private static ReceiptsPageViewModel? _receiptsPageViewModel;

    /// <summary>
    /// Clears all cached page ViewModels to ensure fresh state when opening a new company.
    /// </summary>
    private static void ClearPageCaches()
    {
        _dashboardPageViewModel = null;
        _analyticsPageViewModel = null;
        _insightsPageViewModel = null;
        _reportsPageViewModel = null;
        _revenuePageViewModel = null;
        _expensesPageViewModel = null;
        _invoicesPageViewModel = null;
        _paymentsPageViewModel = null;
        _productsPageViewModel = null;
        _stockLevelsPageViewModel = null;
        _locationsPageViewModel = null;
        _stockAdjustmentsPageViewModel = null;
        _purchaseOrdersPageViewModel = null;
        _categoriesPageViewModel = null;
        _customersPageViewModel = null;
        _suppliersPageViewModel = null;
        _departmentsPageViewModel = null;
        _rentalInventoryPageViewModel = null;
        _rentalRecordsPageViewModel = null;
        _returnsPageViewModel = null;
        _lostDamagedPageViewModel = null;
        _receiptsPageViewModel = null;
    }

    // File watchers for recent companies - watches directories containing recent company files
    private static readonly Dictionary<string, FileSystemWatcher> _recentCompanyWatchers = new();

    // When true, the CompanySaved event handler skips showing the "Saved" indicator
    private static bool _suppressSavedFeedback;

    /// <summary>
    /// Gets the confirmation dialog ViewModel for showing confirmation dialogs from anywhere.
    /// </summary>
    public static ConfirmationDialogViewModel? ConfirmationDialog { get; private set; }

    /// <summary>
    /// Gets the unsaved changes dialog ViewModel for showing save prompts with change lists.
    /// </summary>
    public static UnsavedChangesDialogViewModel? UnsavedChangesDialog { get; private set; }

    /// <summary>
    /// Gets the receipt viewer modal ViewModel for viewing receipt images.
    /// </summary>
    public static ReceiptViewerModalViewModel? ReceiptViewerModal { get; private set; }

    /// <summary>
    /// Checks if the reports page has unsaved changes.
    /// </summary>
    public static bool HasReportsPageUnsavedChanges => _appShellViewModel?.HasReportsPageUnsavedChanges ?? false;

    /// <summary>
    /// Shows a confirmation dialog for reports unsaved changes and returns whether to proceed.
    /// </summary>
    public static Task<bool> ConfirmReportsUnsavedChangesAsync()
    {
        return _appShellViewModel?.ConfirmReportsUnsavedChangesAsync() ?? Task.FromResult(true);
    }

    /// <summary>
    /// Gets the change tracking service for aggregating changes from all sources.
    /// </summary>
    public static ChangeTrackingService? ChangeTrackingService { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            // Initialize error logging first so it's available for all services
            var errorLogger = new ErrorLogger();
            ErrorLogger = errorLogger;

            // Initialize core services
            var compressionService = new CompressionService();
            var footerService = new FooterService();
            var encryptionService = new EncryptionService();
            _fileService = new FileService(compressionService, footerService, encryptionService);
            SettingsService = new GlobalSettingsService();
            LicenseService = new LicenseService(encryptionService, SettingsService, errorLogger);
            CompanyManager = new CompanyManager(_fileService, SettingsService, footerService, errorLogger);

            // Initialize telemetry services
            var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            var geoLocationService = new GeoLocationService(httpClient, errorLogger);
            var telemetryStorageService = new TelemetryStorageService(errorLogger: errorLogger);
            var appVersion = Services.AppInfo.VersionNumber;
            var telemetryUploadService = new TelemetryUploadService(telemetryStorageService, httpClient, errorLogger, appVersion);
            TelemetryManager = new TelemetryManager(
                telemetryStorageService,
                telemetryUploadService,
                geoLocationService,
                SettingsService,
                errorLogger,
                appVersion);

            // Initialize payment portal service
            PaymentPortalService = new PaymentPortalService();

            // Create navigation service
            NavigationService = new NavigationService();

            _mainWindowViewModel = new MainWindowViewModel();
            ConfirmationDialog = new ConfirmationDialogViewModel();
            UnsavedChangesDialog = new UnsavedChangesDialogViewModel();
            ReceiptViewerModal = new ReceiptViewerModalViewModel();
            ChangeTrackingService = new ChangeTrackingService();
            _idleDetectionService = new IdleDetectionService();

            // Create app shell with navigation service and optional update service
            _appShellViewModel = new AppShellViewModel(NavigationService, UpdateService);

            // Ensure no unsaved changes indicator on startup
            _mainWindowViewModel.HasUnsavedChanges = false;
            _appShellViewModel.HeaderViewModel.HasUnsavedChanges = false;

            var appShell = new AppShell
            {
                DataContext = _appShellViewModel
            };

            // Register pages with navigation service
            RegisterPages(NavigationService, _appShellViewModel);

            // Set navigation callback to update current page in AppShell
            NavigationService.SetNavigationCallback(page => _appShellViewModel.CurrentPage = page);

            // Track page views for telemetry and dismiss tutorial guidance on navigation
            NavigationService.Navigated += (_, args) =>
            {
                _ = TelemetryManager?.TrackPageViewAsync(args.PageName);

                // Dismiss tutorial completion guidance when user navigates
                TutorialService.Instance.DismissCompletionGuidance();
            };

            // Set initial view
            _mainWindowViewModel.NavigateTo(appShell);

            // Wire up company manager events
            WireCompanyManagerEvents();

            // Wire up modal change events (separate from company manager)
            WireModalChangeEvents();

            // Sync HasUnsavedChanges with undo/redo state (both MainWindow and Header)
            UndoRedoManager.StateChanged += (_, _) =>
            {
                var hasChanges = !UndoRedoManager.IsAtSavedState;
                _mainWindowViewModel.HasUnsavedChanges = hasChanges;
                _appShellViewModel.HeaderViewModel.HasUnsavedChanges = hasChanges;
            };

            // Wire up file menu events
            WireFileMenuEvents(desktop);

            // Wire up create company wizard events
            WireCreateCompanyEvents(desktop);

            // Wire up welcome screen events
            WireWelcomeScreenEvents(desktop);

            // Wire up company switcher events
            WireCompanySwitcherEvents(desktop);

            // Wire up settings modal events
            WireSettingsModalEvents();

            // Wire up export as modal events
            WireExportEvents(desktop);

            // Wire up import modal events
            WireImportEvents(desktop);

            // Wire up header save request
            _appShellViewModel.HeaderViewModel.SaveRequested += async (_, _) =>
            {
                if (CompanyManager?.IsCompanyOpen == true)
                {
                    // Sample company cannot be saved directly - redirect to Save As
                    if (CompanyManager.IsSampleCompany)
                    {
                        await SaveCompanyAsDialogAsync(desktop);
                        return;
                    }

                    try
                    {
                        await CompanyManager.SaveCompanyAsync();
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to save company on close");
                        await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to save: {0}".TranslateFormat(ex.Message));
                    }
                }
            };

            // Share CreateCompanyViewModel with MainWindow for full-screen overlay
            _mainWindowViewModel.CreateCompanyViewModel = _appShellViewModel.CreateCompanyViewModel;

            // Share WelcomeScreenViewModel with MainWindow for full-screen overlay
            _mainWindowViewModel.WelcomeScreenViewModel = _welcomeScreenViewModel;

            // Share PasswordPromptModalViewModel with MainWindow for password dialog overlay
            _mainWindowViewModel.PasswordPromptModalViewModel = _appShellViewModel.PasswordPromptModalViewModel;

            // Share ConfirmationDialogViewModel with MainWindow for confirmation dialogs
            _mainWindowViewModel.ConfirmationDialogViewModel = ConfirmationDialog;

            // Share UnsavedChangesDialogViewModel with MainWindow for unsaved changes dialogs
            _mainWindowViewModel.UnsavedChangesDialogViewModel = UnsavedChangesDialog;

            // Share ReceiptViewerModalViewModel with MainWindow for receipt viewing
            _mainWindowViewModel.ReceiptViewerModalViewModel = ReceiptViewerModal;

            // Initialize tutorial ViewModels for first-time user experience
            _mainWindowViewModel.TutorialWelcomeViewModel = new TutorialWelcomeViewModel();
            _mainWindowViewModel.AppTourViewModel = new AppTourViewModel();
            _mainWindowViewModel.CategoriesTutorialViewModel = new CategoriesTutorialViewModel();
            _mainWindowViewModel.ProductsTutorialViewModel = new ProductsTutorialViewModel();

            // Wire up tutorial flow: Welcome -> App Tour
            _mainWindowViewModel.TutorialWelcomeViewModel.StartTourRequested += (_, _) =>
            {
                _mainWindowViewModel.AppTourViewModel?.StartTour();
            };

            // Final reset of unsaved changes before window is shown - ensures clean startup state
            _mainWindowViewModel.HasUnsavedChanges = false;
            _appShellViewModel.HeaderViewModel.HasUnsavedChanges = false;

            // Load settings and recent companies BEFORE showing window to prevent flicker
            // Use Task.Run to avoid deadlock with synchronization context
            try
            {
                Task.Run(async () =>
                {
                    if (SettingsService != null)
                        await SettingsService.LoadGlobalSettingsAsync();
                    await LoadRecentCompaniesAsync();
                }).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                ErrorLogger?.LogWarning($"Failed to load settings/recent companies during startup: {ex.Message}", "Startup");
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel
            };

            // Wire up idle detection for auto-logout (needs MainWindow to exist)
            WireIdleDetection(desktop);

            // Load settings and recent companies asynchronously after window is shown
            _ = InitializeAsync();
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Performs async initialization after the main window is displayed.
    /// </summary>
    private static async Task InitializeAsync()
    {
        try
        {
            // Register file type associations on Windows
            RegisterFileTypeAssociationsAsync();

            // Post-update recovery: auto-reopen the last company after an update restart
            await TryAutoOpenRecentCompanyAfterUpdateAsync();

            // Settings and recent companies are loaded synchronously before window is shown
            // to prevent flicker. Just initialize services that depend on settings here.
            if (SettingsService != null)
            {
                // Initialize theme service with settings
                ThemeService.Instance.SetGlobalSettingsService(SettingsService);
                ThemeService.Instance.Initialize();

                // Initialize tutorial service with settings
                TutorialService.Instance.SetGlobalSettingsService(SettingsService);

                // Initialize welcome screen tutorial mode after tutorial service is set up
                _welcomeScreenViewModel?.InitializeTutorialMode();
            }

            // Initialize telemetry session (respects user consent)
            if (TelemetryManager != null)
            {
                await TelemetryManager.InitializeAsync();
            }

            // Initialize language service for localization
            LanguageService.Instance.Initialize();

            // Apply global language setting
            if (SettingsService != null)
            {
                var language = SettingsService.GlobalSettings.Ui.Language;
                if (!string.IsNullOrEmpty(language) && language != "English")
                {
                    await LanguageService.Instance.SetLanguageAsync(language);
                }
            }

            // Initialize exchange rate service for currency conversion
            await InitializeExchangeRateServiceAsync();

            // Load and apply saved license status
            if (LicenseService != null && _appShellViewModel != null)
            {
                var hasPremium = LicenseService.LoadLicense();
                if (hasPremium)
                {
                    _appShellViewModel.SetPlanStatus(hasPremium);

                    // Validate license online in the background
                    _ = ValidateLicenseOnStartupAsync();
                }
            }

            // Check for updates in the background (desktop only)
            await CheckForUpdatesInBackgroundAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            ErrorLogger?.LogError(ex, ErrorCategory.Unknown, "Error during async initialization");
        }
    }

    /// <summary>
    /// Validates the stored license key online on startup.
    /// Checks subscription status and device ownership. If invalid, clears premium and shows a message.
    /// </summary>
    private static async Task ValidateLicenseOnStartupAsync()
    {
        try
        {
            if (LicenseService == null || _appShellViewModel == null)
                return;

            var result = await LicenseService.ValidateLicenseOnlineAsync();

            switch (result.Status)
            {
                case LicenseValidationStatus.Valid:
                    // License is valid — nothing to do
                    return;

                case LicenseValidationStatus.NetworkError:
                    // No internet or server unreachable — allow offline use
                    return;

                case LicenseValidationStatus.InvalidKey:
                    await LicenseService.ClearLicenseAsync();
                    _appShellViewModel.SetPlanStatus(false);
                    RaisePlanStatusChanged(false);
                    await ShowErrorMessageBoxAsync(
                        "License Issue".Translate(),
                        "Your license key is no longer valid. Please contact support or enter a new key.".Translate());
                    break;

                case LicenseValidationStatus.ExpiredSubscription:
                    await LicenseService.ClearLicenseAsync();
                    _appShellViewModel.SetPlanStatus(false);
                    RaisePlanStatusChanged(false);
                    await ShowErrorMessageBoxAsync(
                        "Subscription Expired".Translate(),
                        "Your premium subscription has expired. Please renew your subscription to continue using premium features.".Translate());
                    break;

                case LicenseValidationStatus.WrongDevice:
                    await LicenseService.ClearLicenseAsync();
                    _appShellViewModel.SetPlanStatus(false);
                    RaisePlanStatusChanged(false);
                    await ShowErrorMessageBoxAsync(
                        "License Deactivated".Translate(),
                        "Your license key has been activated on a different device. Premium features have been deactivated on this device. You can re-enter your key in the Upgrade menu to reactivate.".Translate());
                    break;
            }
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogError(ex, ErrorCategory.License, "Error during startup license validation");
        }
    }

    /// <summary>
    /// Performs a silent background check for updates.
    /// If an update is found, notifies the CheckForUpdateModal ViewModel so the user
    /// can be informed the next time they open the modal. Does not show UI automatically.
    /// </summary>
    private static async Task CheckForUpdatesInBackgroundAsync()
    {
        if (UpdateService == null || _appShellViewModel == null)
            return;

        // Wire the ApplyingUpdate event to save user data before the app exits
        UpdateService.ApplyingUpdate += (_, _) =>
        {
            try
            {
                if (CompanyManager?.IsCompanyOpen == true)
                {
                    // Use synchronous wait since we must complete before the process exits
                    Task.Run(async () => await CompanyManager.SaveCompanyAsync())
                        .GetAwaiter().GetResult();
                }

                if (SettingsService != null)
                {
                    // Flag that we're updating so we can auto-reopen the company after restart
                    SettingsService.GlobalSettings.Updates.AutoOpenRecentAfterUpdate = true;
                    Task.Run(async () => await SettingsService.SaveGlobalSettingsAsync())
                        .GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                ErrorLogger?.LogWarning($"Failed to save data before update: {ex.Message}", "AutoUpdate");
            }
        };

        try
        {
            var update = await UpdateService.CheckForUpdateAsync();
            if (update != null)
            {
                _appShellViewModel.CheckForUpdateModalViewModel.NotifyUpdateAvailable(update);
                _appShellViewModel.ShowUpdateBanner($"V.{update.Version}");
            }
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogWarning($"Background update check failed: {ex.Message}", "AutoUpdate");
        }
    }

    /// <summary>
    /// After an update, automatically reopens the company that was open before the restart.
    /// Checks the AutoOpenRecentAfterUpdate flag, clears it, and opens the most recent company.
    /// </summary>
    private static async Task TryAutoOpenRecentCompanyAfterUpdateAsync()
    {
        if (SettingsService == null)
            return;

        var updateSettings = SettingsService.GlobalSettings.Updates;
        if (!updateSettings.AutoOpenRecentAfterUpdate)
            return;

        // Clear the flag immediately so it doesn't fire again on next startup
        updateSettings.AutoOpenRecentAfterUpdate = false;
        await SettingsService.SaveGlobalSettingsAsync();

        try
        {
            var recentCompanies = SettingsService.GetValidRecentCompanies();
            if (recentCompanies.Count > 0)
            {
                var mostRecent = recentCompanies[0];
                if (File.Exists(mostRecent))
                {
                    await OpenCompanyWithRetryAsync(mostRecent);
                }
            }
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogWarning($"Failed to auto-open company after update: {ex.Message}", "AutoUpdate");
        }
    }

    /// <summary>
    /// Initializes the exchange rate service for currency conversion.
    /// </summary>
    private static async Task InitializeExchangeRateServiceAsync()
    {
        try
        {
            var exchangeService = new ExchangeRateService();
            await exchangeService.InitializeAsync();

            if (!exchangeService.HasApiKey)
            {
                Console.WriteLine("Exchange rate service initialized without API key - currency conversion will use cached rates only");
            }
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogError(ex, ErrorCategory.Unknown, "Failed to initialize exchange rate service");
        }
    }

    /// <summary>
    /// Registers file type associations for .argo files on Windows.
    /// Extracts the embedded icon to disk and associates it with the file extension.
    /// </summary>
    private static void RegisterFileTypeAssociationsAsync()
    {
        try
        {
            // Only register on Windows
            if (!OperatingSystem.IsWindows())
                return;

            var platformService = PlatformServiceFactory.GetPlatformService();

            // Extract the icon from embedded resources to a file
            var iconPath = ExtractIconToFile();
            if (string.IsNullOrEmpty(iconPath))
                return;

            // Register file type associations
            platformService.RegisterFileTypeAssociations(iconPath);
        }
        catch (Exception ex)
        {
            // Log but don't crash - file association is not critical
            ErrorLogger?.LogWarning($"Failed to register file type associations: {ex.Message}", "FileAssociation");
        }
    }

    /// <summary>
    /// Extracts the embedded icon resource to a file in LocalAppData.
    /// </summary>
    /// <returns>Path to the extracted icon file, or null if extraction failed.</returns>
    private static string? ExtractIconToFile()
    {
        try
        {
            // Destination path in LocalAppData
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var iconDirectory = Path.Combine(localAppData, "ArgoBooks");
            var iconPath = Path.Combine(iconDirectory, "argo-logo.ico");

            // Ensure directory exists
            Directory.CreateDirectory(iconDirectory);

            // Try Avalonia's asset loader first (for AvaloniaResource items)
            try
            {
                var uri = new Uri("avares://ArgoBooks/Assets/argo-logo.ico");
                using var avaloniaStream = Avalonia.Platform.AssetLoader.Open(uri);
                using var fileStream = new FileStream(iconPath, FileMode.Create, FileAccess.Write);
                avaloniaStream.CopyTo(fileStream);
                return iconPath;
            }
            catch
            {
                // Avalonia asset loader failed, try other methods
            }

            // Try manifest resource stream
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "ArgoBooks.Assets.argo-logo.ico";
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream != null)
            {
                using var fileStream = new FileStream(iconPath, FileMode.Create, FileAccess.Write);
                stream.CopyTo(fileStream);
                return iconPath;
            }

            // Fall back to copying from the executable directory if available
            var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
            if (!string.IsNullOrEmpty(exeDir))
            {
                var sourceIcon = Path.Combine(exeDir, "Assets", "argo-logo.ico");
                if (File.Exists(sourceIcon))
                {
                    File.Copy(sourceIcon, iconPath, overwrite: true);
                    return iconPath;
                }
            }

            Console.WriteLine("Could not find icon resource");
            return null;
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogWarning($"Failed to extract icon: {ex.Message}", "IconExtraction");
            return null;
        }
    }

    /// <summary>
    /// Syncs the event log to CompanyData before saving. Call this before any SaveCompanyAsync().
    /// </summary>
    private static void SyncEventLogBeforeSave()
    {
        if (EventLogService != null && CompanyManager?.CompanyData != null)
        {
            // Commit pending events: remove events for actions that were undone,
            // and mark remaining unsaved events as saved
            EventLogService.CommitPendingEvents(UndoRedoManager.UndoHistory);

            EventLogService.SyncToCompanyData(CompanyManager.CompanyData);
        }
    }

    /// <summary>
    /// Wires up CompanyManager events to update UI.
    /// </summary>
    private static void WireCompanyManagerEvents()
    {
        if (CompanyManager == null || _mainWindowViewModel == null || _appShellViewModel == null)
            return;

        // Sync event log to CompanyData before every save (centralized handler)
        CompanyManager.CompanySaving += (_, _) => SyncEventLogBeforeSave();

        CompanyManager.CompanyOpened += async (_, args) =>
        {
            _mainWindowViewModel.OpenCompany(args.CompanyName);
            var logo = LoadBitmapFromPath(CompanyManager.CurrentCompanyLogoPath);
            _appShellViewModel.SetCompanyInfo(args.CompanyName, logo);
            _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany(args.CompanyName, args.FilePath, logo);
            _appShellViewModel.FileMenuPanelViewModel.SetCurrentCompany(args.FilePath);
            _mainWindowViewModel.HideLoading();

            // Clear undo/redo history for fresh start with new company
            UndoRedoManager.Clear();

            // Initialize the event log service with persisted events from the company file
            if (EventLogService == null)
            {
                EventLogService = new Services.EventLogService();

                // Wire bidirectional sync between UndoRedoManager and EventLogService
                EventLogService.SetUndoRedoManager(UndoRedoManager);

                // Wire UndoRedoManager to automatically record audit events
                // when any CRUD operation records an undoable action
                UndoRedoManager.ActionRecorded += (_, e) =>
                {
                    EventLogService.RecordFromAction(e.Action);
                };
            }
            if (CompanyManager.CompanyData != null)
            {
                EventLogService.Initialize(CompanyManager.CompanyData.EventLog, CompanyManager.CompanyData);

                // Remove this line when done testing the version history UI
                // EventLogService.GenerateTestEvents(80);

                _appShellViewModel.VersionHistoryModalViewModel.SetEventLogService(EventLogService);
            }

            // Reset unsaved changes state - opening a company starts with no unsaved changes
            _mainWindowViewModel.HasUnsavedChanges = false;
            _appShellViewModel.HeaderViewModel.HasUnsavedChanges = false;

            // Load and apply language setting from company settings
            var companySettings = CompanyManager.CompanyData?.Settings;
            if (companySettings != null)
            {
                var language = companySettings.Localization.Language;
                if (!string.IsNullOrEmpty(language))
                {
                    await LanguageService.Instance.SetLanguageAsync(language);

                    // Also update global setting so WelcomeScreen uses same language
                    if (SettingsService != null)
                    {
                        SettingsService.GlobalSettings.Ui.Language = language;
                        _ = SettingsService.SaveGlobalSettingsAsync();
                    }
                }
            }

            // Check for low stock and overdue invoice notifications
            CheckAndSendNotifications();

            // Navigate to Dashboard when company is opened
            NavigationService?.NavigateTo("Dashboard");
        };

        CompanyManager.CompanyClosed += async (_, _) =>
        {
            _mainWindowViewModel.CloseCompany();
            _appShellViewModel.SetCompanyInfo(null);
            _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany("");
            _appShellViewModel.FileMenuPanelViewModel.SetCurrentCompany(null);
            _mainWindowViewModel.HideLoading();
            _mainWindowViewModel.HasUnsavedChanges = false;
            _appShellViewModel.HeaderViewModel.HasUnsavedChanges = false;

            UndoRedoManager.Clear();
            EventLogService?.Clear();
            ChangeTrackingService?.ClearAllChanges();
            _appShellViewModel.HeaderViewModel.ClearNotifications();

            // Clear cached page ViewModels to ensure fresh state when opening a new company
            ClearPageCaches();

            var globalLanguage = SettingsService?.GlobalSettings.Ui.Language ?? "English";
            await LanguageService.Instance.SetLanguageAsync(globalLanguage);

            NavigationService?.NavigateTo("Welcome");
            _welcomeScreenViewModel?.InitializeTutorialMode();
        };

        CompanyManager.CompanySaved += (_, _) =>
        {
            _mainWindowViewModel.HideLoading();

            if (_suppressSavedFeedback)
                _suppressSavedFeedback = false;
            else
                _appShellViewModel.HeaderViewModel.ShowSavedFeedback();

            _mainWindowViewModel.HasUnsavedChanges = false;

            // Mark undo/redo state as saved so IsAtSavedState returns true
            UndoRedoManager.MarkSaved();

            // Clear tracked changes after saving
            ChangeTrackingService?.ClearAllChanges();
        };

        CompanyManager.CompanyDataChanged += (_, _) =>
        {
            _mainWindowViewModel.HasUnsavedChanges = true;
            _appShellViewModel.HeaderViewModel.HasUnsavedChanges = true;
        };

        // Use async callback for password requests (allows proper awaiting)
        CompanyManager.PasswordRequestCallback = async (filePath) =>
        {
            // Hide the loading modal before showing password prompt
            _mainWindowViewModel.HideLoading();

            // Get company name and settings from footer if possible
            var footer = await CompanyManager.GetFileInfoAsync(filePath);
            var companyName = footer?.CompanyName ?? Path.GetFileNameWithoutExtension(filePath);

            // Check if Windows Hello is enabled for this file and available on the system
            var windowsHelloEnabled = footer?.BiometricEnabled ?? false;
            var windowsHelloAvailable = false;
            var platformService = PlatformServiceFactory.GetPlatformService();

            if (windowsHelloEnabled)
            {
                windowsHelloAvailable = await platformService.IsBiometricAvailableAsync();
            }

            var password = await _appShellViewModel.PasswordPromptModalViewModel.ShowAsync(
                companyName, filePath, windowsHelloAvailable);

            // Handle Windows Hello success - retrieve stored password
            if (password == "__WINDOWS_HELLO__")
            {
                var fileId = GetBiometricFileId(filePath);
                password = platformService.GetPasswordForBiometric(fileId);

                if (string.IsNullOrEmpty(password))
                {
                    // Password not found in secure storage - fall back to manual entry
                    _appShellViewModel.PasswordPromptModalViewModel.ShowError(
                        "Stored password not found. Please enter the password manually.".Translate());
                    password = await _appShellViewModel.PasswordPromptModalViewModel.WaitForPasswordAsync();
                }
                else
                {
                    _mainWindowViewModel.ShowLoading("Opening company...".Translate());
                }
            }
            else if (!string.IsNullOrEmpty(password))
            {
                // Show loading again after user enters password (if they didn't cancel)
                _mainWindowViewModel.ShowLoading("Opening company...".Translate());
            }

            return password;
        };

        // Wire up Windows Hello authentication request from password modal
        _appShellViewModel.PasswordPromptModalViewModel.WindowsHelloAuthRequested += async (_, _) =>
        {
            var passwordModal = _appShellViewModel.PasswordPromptModalViewModel;
            var platformService = PlatformServiceFactory.GetPlatformService();

            try
            {
                var success = await platformService.AuthenticateWithBiometricAsync(
                    "Verify your identity to open {0}".TranslateFormat(passwordModal.CompanyName));

                if (success)
                {
                    passwordModal.OnWindowsHelloSuccess();
                }
                else
                {
                    passwordModal.OnWindowsHelloFailed();
                }
            }
            catch
            {
                passwordModal.OnWindowsHelloFailed();
            }
        };
    }

    /// <summary>
    /// Generates a stable file ID for biometric password storage from a file path.
    /// </summary>
    private static string GetBiometricFileId(string filePath)
    {
        // Use a hash of the normalized file path as the ID
        var normalizedPath = Path.GetFullPath(filePath).ToLowerInvariant();
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(normalizedPath));
        return Convert.ToHexString(bytes)[..32]; // First 32 hex chars (16 bytes)
    }

    /// <summary>
    /// Wires up modal save/delete events to update HasUnsavedChanges.
    /// This is separate from WireCompanyManagerEvents because it doesn't depend on CompanyManager.
    /// </summary>
    private static void WireModalChangeEvents()
    {
        if (_mainWindowViewModel == null || _appShellViewModel == null)
            return;

        var mainWindowVm = _mainWindowViewModel;
        var appShellVm = _appShellViewModel;

        void MarkUnsavedChanges(object? sender, EventArgs e)
        {
            mainWindowVm.HasUnsavedChanges = true;
            appShellVm.HeaderViewModel.HasUnsavedChanges = true;
        }

        // Customer modals
        _appShellViewModel.CustomerModalsViewModel.CustomerSaved += MarkUnsavedChanges;
        _appShellViewModel.CustomerModalsViewModel.CustomerDeleted += MarkUnsavedChanges;

        // Product modals
        _appShellViewModel.ProductModalsViewModel.ProductSaved += MarkUnsavedChanges;
        _appShellViewModel.ProductModalsViewModel.ProductDeleted += MarkUnsavedChanges;

        // Category modals
        _appShellViewModel.CategoryModalsViewModel.CategorySaved += MarkUnsavedChanges;
        _appShellViewModel.CategoryModalsViewModel.CategoryDeleted += MarkUnsavedChanges;

        // Department modals
        _appShellViewModel.DepartmentModalsViewModel.DepartmentSaved += MarkUnsavedChanges;
        _appShellViewModel.DepartmentModalsViewModel.DepartmentDeleted += MarkUnsavedChanges;

        // Supplier modals
        _appShellViewModel.SupplierModalsViewModel.SupplierSaved += MarkUnsavedChanges;
        _appShellViewModel.SupplierModalsViewModel.SupplierDeleted += MarkUnsavedChanges;

        // Rental inventory modals
        _appShellViewModel.RentalInventoryModalsViewModel.ItemSaved += MarkUnsavedChanges;
        _appShellViewModel.RentalInventoryModalsViewModel.ItemDeleted += MarkUnsavedChanges;
        _appShellViewModel.RentalInventoryModalsViewModel.RentalCreated += MarkUnsavedChanges;

        // Rental records modals
        _appShellViewModel.RentalRecordsModalsViewModel.RecordSaved += MarkUnsavedChanges;
        _appShellViewModel.RentalRecordsModalsViewModel.RecordDeleted += MarkUnsavedChanges;
        _appShellViewModel.RentalRecordsModalsViewModel.RecordReturned += MarkUnsavedChanges;

        // Payment modals
        _appShellViewModel.PaymentModalsViewModel.PaymentSaved += MarkUnsavedChanges;
        _appShellViewModel.PaymentModalsViewModel.PaymentDeleted += MarkUnsavedChanges;

        // Invoice modals
        _appShellViewModel.InvoiceModalsViewModel.InvoiceSaved += MarkUnsavedChanges;
        _appShellViewModel.InvoiceModalsViewModel.InvoiceDeleted += MarkUnsavedChanges;

        // Invoice template designer
        _appShellViewModel.InvoiceTemplateDesignerViewModel.TemplateSaved += MarkUnsavedChanges;
        _appShellViewModel.InvoiceTemplateDesignerViewModel.BrowseLogoRequested += async (_, _) =>
        {
            if (Avalonia.Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
                return;

            var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Logo".Translate(),
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Images")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg"]
                    }
                ]
            });

            if (files.Count > 0)
            {
                _appShellViewModel.InvoiceTemplateDesignerViewModel.SetLogoFromFile(files[0].Path.LocalPath);
            }
        };

        // Expense modals
        _appShellViewModel.ExpenseModalsViewModel.ExpenseSaved += MarkUnsavedChanges;
        _appShellViewModel.ExpenseModalsViewModel.ExpenseDeleted += MarkUnsavedChanges;

        // Revenue modals
        _appShellViewModel.RevenueModalsViewModel.RevenueSaved += MarkUnsavedChanges;
        _appShellViewModel.RevenueModalsViewModel.RevenueDeleted += MarkUnsavedChanges;
    }

    /// <summary>
    /// Wires up file menu events.
    /// </summary>
    private static void WireFileMenuEvents(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_appShellViewModel == null)
            return;

        var fileMenu = _appShellViewModel.FileMenuPanelViewModel;

        // Open Company
        fileMenu.OpenCompanyRequested += async (_, _) =>
        {
            await OpenCompanyFileDialogAsync(desktop);
        };

        // Save
        fileMenu.SaveRequested += async (_, _) =>
        {
            if (CompanyManager?.IsCompanyOpen == true)
            {
                // Sample company cannot be saved directly - redirect to Save As
                if (CompanyManager.IsSampleCompany)
                {
                    await SaveCompanyAsDialogAsync(desktop);
                    return;
                }

                try
                {
                    await CompanyManager.SaveCompanyAsync();
                }
                catch (Exception ex)
                {
                    ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to save company");
                    await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to save: {0}".TranslateFormat(ex.Message));
                }
            }
        };

        // Save As
        fileMenu.SaveAsRequested += async (_, _) =>
        {
            if (CompanyManager?.IsCompanyOpen == true)
            {
                await SaveCompanyAsDialogAsync(desktop);
            }
        };

        // Close Company
        fileMenu.CloseCompanyRequested += async (_, _) =>
        {
            if (CompanyManager?.IsCompanyOpen == true)
            {
                // Use UndoRedoManager's saved state which correctly handles undo back to original
                if (UndoRedoManager.IsAtSavedState == false)
                {
                    var result = await ShowUnsavedChangesDialogAsync();
                    switch (result)
                    {
                        case UnsavedChangesResult.Save:
                            // Sample company cannot be saved directly - redirect to Save As
                            if (CompanyManager.IsSampleCompany)
                            {
                                var saved = await SaveCompanyAsDialogAsync(desktop);
                                if (!saved) return; // User cancelled Save As, don't close
                            }
                            else
                            {
                                await CompanyManager.SaveCompanyAsync();
                            }
                            await CompanyManager.CloseCompanyAsync();
                            break;
                        case UnsavedChangesResult.DontSave:
                            await CompanyManager.CloseCompanyAsync();
                            break;
                        case UnsavedChangesResult.Cancel:
                        case UnsavedChangesResult.None:
                            // User cancelled, do nothing
                            return;
                    }
                }
                else
                {
                    await CompanyManager.CloseCompanyAsync();
                }
            }
        };

        // Show in Folder
        fileMenu.ShowInFolderRequested += (_, _) =>
        {
            CompanyManager?.ShowInFolder();
        };

        // Open Recent Company
        fileMenu.OpenRecentCompanyRequested += async (_, company) =>
        {
            if (string.IsNullOrEmpty(company.FilePath)) return;
            await OpenCompanyWithRetryAsync(company.FilePath);
        };
    }

    /// <summary>
    /// Wires up create company wizard events.
    /// </summary>
    private static void WireCreateCompanyEvents(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_appShellViewModel == null)
            return;

        var createCompany = _appShellViewModel.CreateCompanyViewModel;

        createCompany.CompanyCreated += async (_, args) =>
        {
            // Show save dialog
            var file = await ShowSaveFileDialogAsync(desktop, args.CompanyName);
            if (file == null) return;

            var filePath = file.Path.LocalPath;

            _mainWindowViewModel?.ShowLoading("Creating company...".Translate());
            try
            {
                var companyInfo = new CompanyInfo
                {
                    Name = args.CompanyName,
                    BusinessType = args.BusinessType,
                    Industry = args.Industry,
                    Phone = args.PhoneNumber,
                    Country = args.Country,
                    City = args.City,
                    ProvinceState = args.ProvinceState,
                    Address = args.Address
                };

                await CompanyManager!.CreateCompanyAsync(
                    filePath,
                    args.CompanyName,
                    args.Password,
                    companyInfo);

                // Apply default currency if specified
                if (!string.IsNullOrEmpty(args.DefaultCurrency))
                {
                    CompanyManager.CompanyData!.Settings.Localization.Currency = args.DefaultCurrency;
                }

                // Apply logo if one was selected
                if (!string.IsNullOrEmpty(args.LogoPath))
                {
                    await CompanyManager.SetCompanyLogoAsync(args.LogoPath);

                    // Refresh sidebar/UI with the newly set logo
                    var logo = LoadBitmapFromPath(CompanyManager.CurrentCompanyLogoPath);
                    _appShellViewModel.SetCompanyInfo(args.CompanyName, logo);
                    _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany(
                        args.CompanyName, filePath, logo);
                }

                _suppressSavedFeedback = true;
                await CompanyManager.SaveCompanyAsync();

                await LoadRecentCompaniesAsync();
            }
            catch (Exception ex)
            {
                _mainWindowViewModel?.HideLoading();
                ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to create company");
                await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to create company: {0}".TranslateFormat(ex.Message));
            }
        };

        createCompany.BrowseLogoRequested += async (_, _) =>
        {
            var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Company Logo".Translate(),
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Images")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg"]
                    }
                ]
            });

            if (files.Count > 0)
            {
                var path = files[0].Path.LocalPath;
                try
                {
                    var bitmap = new Bitmap(path);
                    createCompany.SetLogo(path, bitmap);
                }
                catch (Exception ex)
                {
                    ErrorLogger?.LogWarning($"Failed to load logo image: {ex.Message}", "CreateCompanyLogo");
                }
            }
        };
    }

    /// <summary>
    /// Wires up welcome screen events.
    /// </summary>
    private static void WireWelcomeScreenEvents(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_welcomeScreenViewModel == null)
            return;

        // Create new company - show create company wizard
        _welcomeScreenViewModel.CreateNewCompanyRequested += (_, _) =>
        {
            _appShellViewModel?.CreateCompanyViewModel.OpenCommand.Execute(null);
        };

        // Open company - show file picker
        _welcomeScreenViewModel.OpenCompanyRequested += async (_, _) =>
        {
            await OpenCompanyFileDialogAsync(desktop);
        };

        // Open recent company
        _welcomeScreenViewModel.OpenRecentCompanyRequested += async (_, company) =>
        {
            if (string.IsNullOrEmpty(company.FilePath)) return;
            await OpenCompanyWithRetryAsync(company.FilePath);
        };

        // Remove from recent companies
        _welcomeScreenViewModel.RemoveFromRecentRequested += async (_, company) =>
        {
            if (string.IsNullOrEmpty(company.FilePath)) return;
            SettingsService?.RemoveRecentCompany(company.FilePath);
            if (SettingsService != null)
            {
                await SettingsService.SaveGlobalSettingsAsync();
            }
        };

        // Clear all recent companies
        _welcomeScreenViewModel.ClearRecentRequested += async (_, _) =>
        {
            if (SettingsService != null)
            {
                SettingsService.GlobalSettings.RecentCompanies.Clear();
                await SettingsService.SaveGlobalSettingsAsync();
            }
        };

        // Open sample company
        _welcomeScreenViewModel.OpenSampleCompanyRequested += async (_, _) =>
        {
            await OpenSampleCompanyAsync();
        };
    }

    /// <summary>
    /// Creates and opens a sample company with pre-populated demo data.
    /// </summary>
    private static async Task OpenSampleCompanyAsync()
    {
        if (CompanyManager == null || _mainWindowViewModel == null || _appShellViewModel == null || _fileService == null)
            return;

        try
        {
            var sampleFilePath = SampleCompanyService.GetSampleCompanyPath();
            var needsCreation = true;

            if (File.Exists(sampleFilePath))
            {
                var footer = await CompanyManager.GetFileInfoAsync(sampleFilePath);
                if (footer != null && IsVersionUpToDate(footer.Version))
                    needsCreation = false;
                else
                    SampleCompanyService.CleanupSampleCompanyFiles();
            }

            _mainWindowViewModel.ShowLoading("Opening sample company...".Translate());

            if (needsCreation)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "ArgoBooks.Resources.SampleCompanyData.xlsx";

                using var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    _mainWindowViewModel.HideLoading();
                    await ShowErrorMessageBoxAsync("Error".Translate(), "Sample company data not found.".Translate());
                    return;
                }

                var importService = new SpreadsheetImportService(ErrorLogger, TelemetryManager);
                var sampleService = new SampleCompanyService(_fileService, importService);

                // Validate first (same as regular imports)
                var validationContext = await sampleService.ValidateSampleCompanyAsync(stream);
                var validationResult = validationContext.ValidationResult;

                _mainWindowViewModel.HideLoading();

                // Check for issues and show validation dialog
                if (validationResult.HasIssues)
                {
                    var validationDialog = _appShellViewModel.ImportValidationDialogViewModel;
                    var dialogResult = await validationDialog.ShowAsync(validationResult);

                    if (dialogResult == ImportValidationDialogResult.Cancel)
                    {
                        SampleCompanyService.CleanupValidationContext(validationContext);
                        return;
                    }
                }

                _mainWindowViewModel.ShowLoading("Opening sample company...".Translate());

                // Finish import
                sampleFilePath = await sampleService.FinishSampleCompanyCreationAsync(validationContext);
            }

            var success = await CompanyManager.OpenCompanyAsync(sampleFilePath);

            if (success)
            {
                if (CompanyManager.CompanyData != null)
                {
                    if (SampleCompanyService.TimeShiftSampleData(CompanyManager.CompanyData))
                    {
                        CompanyManager.NotifyDataChanged();

                        // Suppress the "Saved" indicator for this internal save
                        _suppressSavedFeedback = true;
                        await CompanyManager.SaveCompanyAsync();
                    }
                    CompanyManager.CompanyData.MarkAsSaved();

                    // Reset unsaved changes since time-shift is automatic
                    _mainWindowViewModel.HasUnsavedChanges = false;
                    _appShellViewModel.HeaderViewModel.HasUnsavedChanges = false;

                    // Set date range to show full year of sample data
                    ChartSettingsService.Instance.SelectedDateRange = "Last 365 Days";
                }

                await LoadRecentCompaniesAsync();
            }
            else
            {
                _mainWindowViewModel.HideLoading();
                await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to open sample company.".Translate());
            }
        }
        catch (Exception ex)
        {
            _mainWindowViewModel.HideLoading();
            ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to open sample company");
            await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to open sample company: {0}".TranslateFormat(ex.Message));
        }
    }

    /// <summary>
    /// Checks if the sample company version is up to date with the current app version.
    /// </summary>
    private static bool IsVersionUpToDate(string sampleVersion)
    {
        if (!Version.TryParse(sampleVersion, out var sampleVer))
            return false;

        var appVersion = Services.AppInfo.AssemblyVersion;
        if (appVersion == null)
            return false;

        // Sample is up to date if major.minor.build matches
        return sampleVer.Major == appVersion.Major &&
               sampleVer.Minor == appVersion.Minor &&
               sampleVer.Build == appVersion.Build;
    }

    /// <summary>
    /// Wires up company switcher panel events.
    /// </summary>
    private static void WireCompanySwitcherEvents(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_appShellViewModel == null)
            return;

        var companySwitcher = _appShellViewModel.CompanySwitcherPanelViewModel;

        // Switch to a recent company
        companySwitcher.SwitchCompanyRequested += async (_, company) =>
        {
            if (string.IsNullOrEmpty(company.FilePath)) return;
            await OpenCompanyWithRetryAsync(company.FilePath);
        };

        // Open company from file dialog
        companySwitcher.OpenCompanyRequested += async (_, _) =>
        {
            await OpenCompanyFileDialogAsync(desktop);
        };

        // Edit current company
        companySwitcher.EditCompanyRequested += (_, _) =>
        {
            OpenEditCompanyModal();
        };

        // Edit company from quick action
        _appShellViewModel.EditCompanyRequested += (_, _) =>
        {
            OpenEditCompanyModal();
        };

        // Restart tutorial from help panel
        _appShellViewModel.RestartTutorialRequested += async (_, _) =>
        {
            if (CompanyManager?.IsCompanyOpen == true)
            {
                if (!CompanyManager.IsSampleCompany)
                {
                    try { await CompanyManager.SaveCompanyAsync(); }
                    catch { /* Continue even if save fails */ }
                }
                await CompanyManager.CloseCompanyAsync();
            }

            if (_welcomeScreenViewModel != null)
                _welcomeScreenViewModel.IsTutorialMode = true;
        };

        // Wire up edit company modal events
        WireEditCompanyEvents(desktop);
    }

    /// <summary>
    /// Opens the edit company modal with the current company information.
    /// </summary>
    private static void OpenEditCompanyModal()
    {
        if (CompanyManager?.IsCompanyOpen != true || _appShellViewModel == null) return;

        var settings = CompanyManager.CurrentCompanySettings;
        var logoPath = CompanyManager.CurrentCompanyLogoPath;
        var logo = LoadBitmapFromPath(logoPath);
        _appShellViewModel.EditCompanyModalViewModel.Open(
            settings?.Company.Name ?? "",
            settings?.Company.BusinessType,
            settings?.Company.Industry,
            logo,
            settings?.Company.Phone,
            settings?.Company.Country,
            settings?.Company.City,
            settings?.Company.Address,
            settings?.Company.Email);
    }

    /// <summary>
    /// Wires up edit company modal events.
    /// </summary>
    private static void WireEditCompanyEvents(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_appShellViewModel == null)
            return;

        var editCompany = _appShellViewModel.EditCompanyModalViewModel;

        // Save company changes
        editCompany.CompanySaved += async (_, args) =>
        {
            if (CompanyManager?.IsCompanyOpen != true) return;

            try
            {
                // Update company settings
                var settings = CompanyManager.CurrentCompanySettings;
                if (settings != null)
                {
                    // Capture old values for undo
                    var oldName = settings.Company.Name;
                    var oldBusinessType = settings.Company.BusinessType;
                    var oldIndustry = settings.Company.Industry;
                    var oldPhone = settings.Company.Phone;
                    var oldEmail = settings.Company.Email;
                    var oldCountry = settings.Company.Country;
                    var oldCity = settings.Company.City;
                    var oldAddress = settings.Company.Address;
                    var oldLogoFileName = settings.Company.LogoFileName;

                    // Save old logo bytes for potential undo restore
                    byte[]? oldLogoBytes = null;
                    var oldLogoFilePath = CompanyManager.CurrentCompanyLogoPath;
                    if (!string.IsNullOrEmpty(oldLogoFilePath) && File.Exists(oldLogoFilePath))
                    {
                        oldLogoBytes = await Task.Run(() => File.ReadAllBytes(oldLogoFilePath));
                    }

                    var nameChanged = oldName != args.CompanyName;

                    // Apply new values
                    settings.Company.Name = args.CompanyName;
                    settings.Company.BusinessType = args.BusinessType;
                    settings.Company.Industry = args.Industry;
                    settings.Company.Phone = args.Phone;
                    settings.Company.Email = args.Email;
                    settings.Company.Country = args.Country;
                    settings.Company.City = args.City;
                    settings.Company.Address = args.Address;

                    // Handle logo update if a new one was uploaded
                    if (!string.IsNullOrEmpty(args.LogoPath))
                    {
                        await CompanyManager.SetCompanyLogoAsync(args.LogoPath);

                        // Sync logo to payment portal (fire-and-forget)
                        if (PortalSettings.IsConfigured && PaymentPortalService != null)
                        {
                            var portalLogoPath = CompanyManager.CurrentCompanyLogoPath;
                            if (!string.IsNullOrEmpty(portalLogoPath))
                            {
                                _ = Task.Run(async () =>
                                {
                                    try { await PaymentPortalService.UploadCompanyLogoAsync(portalLogoPath); }
                                    catch (Exception ex) { ErrorLogger?.LogError(ex, ErrorCategory.Network, "Failed to upload logo to portal"); }
                                });
                            }
                        }
                    }
                    else if (args.LogoSource == null && CompanyManager.CurrentCompanyLogoPath != null)
                    {
                        // Logo was removed
                        await CompanyManager.RemoveCompanyLogoAsync();

                        // Remove logo from payment portal (fire-and-forget)
                        if (PortalSettings.IsConfigured && PaymentPortalService != null)
                        {
                            _ = Task.Run(async () =>
                            {
                                try { await PaymentPortalService.DeleteCompanyLogoAsync(); }
                                catch (Exception ex) { ErrorLogger?.LogError(ex, ErrorCategory.Network, "Failed to delete logo from portal"); }
                            });
                        }
                    }

                    // Capture new logo state after changes
                    var newLogoFileName = settings.Company.LogoFileName;
                    byte[]? newLogoBytes = null;
                    var newLogoFilePath = CompanyManager.CurrentCompanyLogoPath;
                    if (!string.IsNullOrEmpty(newLogoFilePath) && File.Exists(newLogoFilePath))
                    {
                        newLogoBytes = await Task.Run(() => File.ReadAllBytes(newLogoFilePath));
                    }

                    // Derive temp directory for logo file operations during undo/redo
                    var logoTempDir = !string.IsNullOrEmpty(oldLogoFilePath)
                        ? Path.GetDirectoryName(oldLogoFilePath)
                        : (!string.IsNullOrEmpty(newLogoFilePath)
                            ? Path.GetDirectoryName(newLogoFilePath)
                            : null);

                    // Mark settings as changed
                    settings.ChangesMade = true;

                    // If company name changed, schedule a file rename (skip for sample company).
                    // The rename is deferred to save time so that closing without saving
                    // leaves the original file untouched.
                    var oldFilePath = CompanyManager.CurrentFilePath;
                    if (nameChanged && CompanyManager.CurrentFilePath != null && !CompanyManager.IsSampleCompany)
                    {
                        var currentPath = CompanyManager.CurrentFilePath;
                        var directory = Path.GetDirectoryName(currentPath);
                        var newFileName = args.CompanyName + ".argo";
                        var newPath = Path.Combine(directory!, newFileName);

                        if (currentPath != newPath && !File.Exists(newPath))
                        {
                            CompanyManager.SetPendingRename(newPath);
                        }
                    }
                    var newFilePath = CompanyManager.PendingRenamePath ?? CompanyManager.CurrentFilePath;

                    // Update UI
                    _mainWindowViewModel?.OpenCompany(args.CompanyName);
                    var logo = LoadBitmapFromPath(CompanyManager.CurrentCompanyLogoPath);
                    _appShellViewModel.SetCompanyInfo(args.CompanyName, logo);
                    _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany(
                        args.CompanyName,
                        CompanyManager.PendingRenamePath ?? CompanyManager.CurrentFilePath,
                        logo);
                    _reportsPageViewModel?.RefreshCanvas();

                    // Record undo/redo action
                    var newName = args.CompanyName;
                    var newBusinessType = args.BusinessType;
                    var newIndustry = args.Industry;
                    var newPhone = args.Phone;
                    var newCountry = args.Country;
                    var newCity = args.City;
                    var newAddress = args.Address;

                    UndoRedoManager.RecordAction(new DelegateAction(
                        $"Edit company '{newName}'",
                        () =>
                        {
                            // Restore old values
                            settings.Company.Name = oldName;
                            settings.Company.BusinessType = oldBusinessType;
                            settings.Company.Industry = oldIndustry;
                            settings.Company.Phone = oldPhone;
                            settings.Company.Email = oldEmail;
                            settings.Company.Country = oldCountry;
                            settings.Company.City = oldCity;
                            settings.Company.Address = oldAddress;

                            // Restore old logo
                            RestoreCompanyLogo(settings, oldLogoFileName, oldLogoBytes, logoTempDir);

                            // Clear pending rename (revert to original file name)
                            if (oldFilePath != newFilePath)
                            {
                                CompanyManager!.ClearPendingRename();
                            }

                            settings.ChangesMade = true;
                            RefreshCompanyUI(oldName);
                        },
                        () =>
                        {
                            // Re-apply new values
                            settings.Company.Name = newName;
                            settings.Company.BusinessType = newBusinessType;
                            settings.Company.Industry = newIndustry;
                            settings.Company.Phone = newPhone;
                            settings.Company.Country = newCountry;
                            settings.Company.City = newCity;
                            settings.Company.Address = newAddress;

                            // Restore new logo
                            RestoreCompanyLogo(settings, newLogoFileName, newLogoBytes, logoTempDir);

                            // Re-schedule the file rename
                            if (oldFilePath != newFilePath && newFilePath != null)
                            {
                                CompanyManager!.SetPendingRename(newFilePath);
                            }

                            settings.ChangesMade = true;
                            RefreshCompanyUI(newName);
                        }));
                }
            }
            catch (Exception ex)
            {
                ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to update company");
                await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to update company: {0}".TranslateFormat(ex.Message));
            }
        };

        // Browse logo
        editCompany.BrowseLogoRequested += async (_, _) =>
        {
            var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Company Logo".Translate(),
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Images")
                    {
                        Patterns = ["*.png", "*.jpg", "*.jpeg"]
                    }
                ]
            });

            if (files.Count > 0)
            {
                var path = files[0].Path.LocalPath;
                try
                {
                    var bitmap = new Bitmap(path);
                    editCompany.SetLogo(path, bitmap);
                }
                catch (Exception ex)
                {
                    ErrorLogger?.LogWarning($"Failed to load logo image: {ex.Message}", "EditCompanyLogo");
                }
            }
        };
    }

    /// <summary>
    /// Restores a company logo file and updates the LogoFileName setting.
    /// Used by undo/redo to restore logo state.
    /// </summary>
    private static void RestoreCompanyLogo(CompanySettings settings, string? logoFileName, byte[]? logoBytes, string? tempDir)
    {
        settings.Company.LogoFileName = logoFileName;
        if (tempDir != null && logoFileName != null && logoBytes != null)
        {
            var path = Path.Combine(tempDir, logoFileName);
            File.WriteAllBytes(path, logoBytes);
        }
    }

    /// <summary>
    /// Refreshes company-related UI elements (sidebar, company switcher, main window title).
    /// Used by undo/redo to update the UI after restoring company data.
    /// </summary>
    private static void RefreshCompanyUI(string companyName)
    {
        if (_appShellViewModel == null) return;
        _mainWindowViewModel?.OpenCompany(companyName);
        var logo = LoadBitmapFromPath(CompanyManager?.CurrentCompanyLogoPath);
        _appShellViewModel.SetCompanyInfo(companyName, logo);
        _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany(
            companyName,
            CompanyManager?.CurrentFilePath,
            logo);
        _reportsPageViewModel?.RefreshCanvas();
    }

    /// <summary>
    /// Wires up settings modal events for password management.
    /// </summary>
    private static void WireSettingsModalEvents()
    {
        if (_appShellViewModel == null)
            return;

        var settings = _appShellViewModel.SettingsModalViewModel;

        // Initialize HasPassword based on current company
        if (CompanyManager != null)
        {
            CompanyManager.CompanyOpened += (_, args) =>
            {
                settings.HasPassword = args.IsEncrypted;
            };

            CompanyManager.CompanyClosed += (_, _) =>
            {
                settings.HasPassword = false;
            };
        }

        // Add password
        settings.AddPasswordRequested += async (_, args) =>
        {
            if (CompanyManager?.IsCompanyOpen != true || args.NewPassword == null) return;

            try
            {
                await CompanyManager.ChangePasswordAsync(args.NewPassword);
                _appShellViewModel.AddNotification("Success".Translate(), "Password has been set.".Translate(), NotificationType.Success);
            }
            catch (Exception ex)
            {
                settings.HasPassword = false;
                ErrorLogger?.LogError(ex, ErrorCategory.Authentication, "Failed to set password");
                await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to set password: {0}".TranslateFormat(ex.Message));
            }
        };

        // Change password
        settings.ChangePasswordRequested += async (_, args) =>
        {
            if (CompanyManager?.IsCompanyOpen != true || args.NewPassword == null) return;

            // Verify the current password before changing
            if (!CompanyManager.VerifyCurrentPassword(args.CurrentPassword))
            {
                settings.OnPasswordVerificationFailed();
                return;
            }

            try
            {
                await CompanyManager.ChangePasswordAsync(args.NewPassword);
                settings.OnPasswordChanged();
                _appShellViewModel.AddNotification("Success".Translate(), "Password has been changed.".Translate(), NotificationType.Success);
            }
            catch (Exception ex)
            {
                settings.OnPasswordVerificationFailed();
                ErrorLogger?.LogError(ex, ErrorCategory.Authentication, "Failed to change password");
                await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to change password: {0}".TranslateFormat(ex.Message));
            }
        };

        // Remove password
        settings.RemovePasswordRequested += async (_, args) =>
        {
            if (CompanyManager?.IsCompanyOpen != true) return;

            // Verify the current password before removing
            if (!CompanyManager.VerifyCurrentPassword(args.CurrentPassword))
            {
                settings.OnPasswordVerificationFailed();
                return;
            }

            try
            {
                await CompanyManager.ChangePasswordAsync(null);
                settings.OnPasswordRemoved();
                _appShellViewModel.AddNotification("Success".Translate(), "Password has been removed.".Translate(), NotificationType.Success);
            }
            catch (Exception ex)
            {
                settings.OnPasswordVerificationFailed();
                ErrorLogger?.LogError(ex, ErrorCategory.Authentication, "Failed to remove password");
                await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to remove password: {0}".TranslateFormat(ex.Message));
            }
        };

        // Auto-lock settings changed
        settings.AutoLockSettingsChanged += (_, args) =>
        {
            if (_idleDetectionService != null)
            {
                var enabled = args.TimeoutMinutes > 0;
                _idleDetectionService.Configure(enabled, args.TimeoutMinutes);
            }

            // Save to company settings
            if (CompanyManager?.CurrentCompanySettings != null)
            {
                CompanyManager.CurrentCompanySettings.Security.AutoLockEnabled = args.TimeoutMinutes > 0;
                CompanyManager.CurrentCompanySettings.Security.AutoLockMinutes = args.TimeoutMinutes;
                CompanyManager.MarkAsChanged();
            }
        };

        // Windows Hello authentication requested (before enabling)
        settings.WindowsHelloAuthRequested += async (_, _) =>
        {
            var platformService = PlatformServiceFactory.GetPlatformService();

            try
            {
                // Check if Windows Hello is available
                var available = await platformService.IsBiometricAvailableAsync();
                if (!available)
                {
                    // Get detailed reason why Windows Hello is not available
                    var details = "Unknown reason";
                    if (platformService is WindowsPlatformService winService)
                    {
#pragma warning disable CA1416 // Platform compatibility - already checked by type check above
                        details = await winService.GetBiometricAvailabilityDetailsAsync();
#pragma warning restore CA1416
                    }

                    var dialog = ConfirmationDialog;
                    if (dialog != null)
                    {
                        await dialog.ShowAsync(new ConfirmationDialogOptions
                        {
                            Title = "Windows Hello Not Available".Translate(),
                            Message = "Windows Hello cannot be enabled on this device.\n\nReason: {0}".TranslateFormat(details),
                            PrimaryButtonText = "OK".Translate(),
                            CancelButtonText = ""
                        });
                    }
                    settings.OnWindowsHelloAuthResult(false);
                    return;
                }

                // Request authentication
                var success = await platformService.AuthenticateWithBiometricAsync("Verify your identity to enable Windows Hello".Translate());
                settings.OnWindowsHelloAuthResult(success);

                if (!success)
                {
                    var dialog = ConfirmationDialog;
                    if (dialog != null)
                    {
                        await dialog.ShowAsync(new ConfirmationDialogOptions
                        {
                            Title = "Windows Hello".Translate(),
                            Message = "Authentication was cancelled or failed. Windows Hello has not been enabled.".Translate(),
                            PrimaryButtonText = "OK".Translate(),
                            CancelButtonText = ""
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger?.LogError(ex, ErrorCategory.Authentication, "Windows Hello authentication failed");
                var dialog = ConfirmationDialog;
                if (dialog != null)
                {
                    await dialog.ShowAsync(new ConfirmationDialogOptions
                    {
                        Title = "Windows Hello Error".Translate(),
                        Message = "Failed to authenticate with Windows Hello:\n\n{0}".TranslateFormat(ex.Message),
                        PrimaryButtonText = "OK".Translate(),
                        CancelButtonText = ""
                    });
                }
                settings.OnWindowsHelloAuthResult(false);
            }
        };

        // Windows Hello setting changed (after successful authentication)
        settings.WindowsHelloChanged += (_, args) =>
        {
            // Save to company settings
            if (CompanyManager?.CurrentCompanySettings != null)
            {
                CompanyManager.CurrentCompanySettings.Security.BiometricEnabled = args.Enabled;
                CompanyManager.MarkAsChanged();

                // Store or clear password for biometric unlock
                if (CompanyManager.CurrentFilePath != null)
                {
                    var fileId = GetBiometricFileId(CompanyManager.CurrentFilePath);
                    var platformService = PlatformServiceFactory.GetPlatformService();

                    if (args.Enabled && CompanyManager.IsEncrypted)
                    {
                        // Store the current password for biometric unlock
                        // Note: We need to get the password from CompanyManager
                        var password = CompanyManager.GetCurrentPassword();
                        if (!string.IsNullOrEmpty(password))
                        {
                            platformService.StorePasswordForBiometric(fileId, password);
                        }
                    }
                    else
                    {
                        // Clear stored password
                        platformService.ClearPasswordForBiometric(fileId);
                    }
                }
            }
        };

        // Load Windows Hello setting when company opens
        if (CompanyManager != null)
        {
            CompanyManager.CompanyOpened += (_, _) =>
            {
                var securitySettings = CompanyManager.CurrentCompanySettings?.Security;
                if (securitySettings != null)
                {
                    // Use SetWindowsHelloWithoutAuth to avoid triggering authentication on load
                    settings.SetWindowsHelloWithoutAuth(securitySettings.BiometricEnabled);
                }
            };
        }

    }

    /// <summary>
    /// Wires up export as modal events for spreadsheet export.
    /// </summary>
    private static void WireExportEvents(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_appShellViewModel == null)
            return;

        var exportModal = _appShellViewModel.ExportAsModalViewModel;

        // Refresh record counts when modal opens
        exportModal.RefreshRecordCountsRequested += (_, _) =>
        {
            exportModal.RefreshRecordCounts(CompanyManager?.CompanyData);
        };

        // Handle export request
        exportModal.ExportRequested += async (_, args) =>
        {
            if (args.Format == "backup")
            {
                if (CompanyManager?.IsCompanyOpen != true)
                {
                    await ShowErrorMessageBoxAsync("Error".Translate(), "No company is currently open.".Translate());
                    return;
                }

                // Show save file dialog for backup
                var backupFile = await desktop.MainWindow!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Export Backup".Translate(),
                    SuggestedFileName = $"{CompanyManager.CurrentCompanyName ?? "Backup"}-{DateTime.Now:yyyy-MM-dd}.argobk",
                    DefaultExtension = "argobk",
                    FileTypeChoices =
                    [
                        new FilePickerFileType("Argo Books Backup")
                        {
                            Patterns = ["*.argobk"]
                        }
                    ]
                });

                if (backupFile == null) return;

                var backupPath = backupFile.Path.LocalPath;
                _mainWindowViewModel?.ShowLoading("Exporting backup...".Translate());

                var backupStopwatch = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    await CompanyManager.ExportBackupAsync(backupPath);

                    backupStopwatch.Stop();
                    _mainWindowViewModel?.HideLoading();

                    // Track telemetry
                    var fileSize = new FileInfo(backupPath).Length;
                    _ = TelemetryManager?.TrackExportAsync(ExportType.Backup, backupStopwatch.ElapsedMilliseconds, fileSize);

                    // Open the containing folder
                    try
                    {
                        var directory = Path.GetDirectoryName(backupPath);
                        if (!string.IsNullOrEmpty(directory))
                        {
                            if (OperatingSystem.IsWindows())
                                System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{backupPath}\"");
                            else if (OperatingSystem.IsMacOS())
                                System.Diagnostics.Process.Start("open", $"-R \"{backupPath}\"");
                            else if (OperatingSystem.IsLinux())
                                System.Diagnostics.Process.Start("xdg-open", directory);
                        }
                    }
                    catch (Exception ex)
                    {
                        ErrorLogger?.LogWarning($"Failed to open folder after backup export: {ex.Message}", "BackupExportFolder");
                    }

                    _appShellViewModel.AddNotification(
                        "Backup Complete".Translate(),
                        "Company backup exported successfully.".Translate(),
                        NotificationType.Success);
                }
                catch (Exception ex)
                {
                    backupStopwatch.Stop();
                    _mainWindowViewModel?.HideLoading();
                    ErrorLogger?.LogError(ex, ErrorCategory.Export, "Failed to export backup");
                    await ShowErrorMessageBoxAsync("Export Failed".Translate(), "Failed to export backup: {0}".TranslateFormat(ex.Message));
                }

                return;
            }

            // Spreadsheet export
            if (args.SelectedDataItems.Count == 0)
            {
                _appShellViewModel.AddNotification("Warning".Translate(), "Please select at least one data type to export.".Translate(), NotificationType.Warning);
                return;
            }

            if (CompanyManager?.CompanyData == null)
            {
                await ShowErrorMessageBoxAsync("Error".Translate(), "No company is currently open.".Translate());
                return;
            }

            // Show save file dialog
            var extension = args.Format;
            var filterName = args.Format.ToUpperInvariant() switch
            {
                "XLSX" => "Excel Workbook",
                "CSV" => "CSV File",
                "PDF" => "PDF Document",
                _ => "File"
            };

            var file = await desktop.MainWindow!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export Data".Translate(),
                SuggestedFileName = $"{CompanyManager.CurrentCompanyName ?? "Export"}-{DateTime.Now:yyyy-MM-dd}.{extension}",
                DefaultExtension = extension,
                FileTypeChoices =
                [
                    new FilePickerFileType(filterName)
                    {
                        Patterns = [$"*.{extension}"]
                    }
                ]
            });

            if (file == null) return;

            var filePath = file.Path.LocalPath;
            _mainWindowViewModel?.ShowLoading("Exporting data...".Translate());

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var exportService = new SpreadsheetExportService();

                switch (args.Format.ToLowerInvariant())
                {
                    case "xlsx":
                        await exportService.ExportToExcelAsync(
                            filePath,
                            CompanyManager.CompanyData,
                            args.SelectedDataItems,
                            args.StartDate,
                            args.EndDate);
                        break;

                    case "csv":
                        await exportService.ExportToCsvAsync(
                            filePath,
                            CompanyManager.CompanyData,
                            args.SelectedDataItems,
                            args.StartDate,
                            args.EndDate);
                        break;

                    case "pdf":
                        await exportService.ExportToPdfAsync(
                            filePath,
                            CompanyManager.CompanyData,
                            args.SelectedDataItems,
                            args.StartDate,
                            args.EndDate);
                        break;
                }

                stopwatch.Stop();
                _mainWindowViewModel?.HideLoading();

                // Track export telemetry
                var fileSize = new FileInfo(filePath).Length;
                var exportType = args.Format.ToLowerInvariant() switch
                {
                    "xlsx" => ExportType.Excel,
                    "csv" => ExportType.Csv,
                    "pdf" => ExportType.Pdf,
                    _ => ExportType.Excel
                };
                _ = TelemetryManager?.TrackExportAsync(exportType, stopwatch.ElapsedMilliseconds, fileSize);

                // Open the containing folder
                try
                {
                    var directory = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(directory))
                    {
                        if (OperatingSystem.IsWindows())
                        {
                            System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{filePath}\"");
                        }
                        else if (OperatingSystem.IsMacOS())
                        {
                            System.Diagnostics.Process.Start("open", $"-R \"{filePath}\"");
                        }
                        else if (OperatingSystem.IsLinux())
                        {
                            System.Diagnostics.Process.Start("xdg-open", directory);
                        }
                    }
                }
                catch (Exception ex)
                {
                    ErrorLogger?.LogWarning($"Failed to open folder after export: {ex.Message}", "ExportFolder");
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _mainWindowViewModel?.HideLoading();
                ErrorLogger?.LogError(ex, ErrorCategory.Export, $"Failed to export {args.Format}");
                await ShowErrorMessageBoxAsync("Export Failed".Translate(), "Failed to export data: {0}".TranslateFormat(ex.Message));
            }
        };
    }

    /// <summary>
    /// Wires up import modal events for spreadsheet import.
    /// </summary>
    private static void WireImportEvents(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_appShellViewModel == null)
            return;

        var importModal = _appShellViewModel.ImportModalViewModel;

        // Handle format selection
        importModal.FormatSelected += async (_, format) =>
        {
            if (CompanyManager?.CompanyData == null)
            {
                await ShowErrorMessageBoxAsync("Error".Translate(), "No company is currently open.".Translate());
                return;
            }

            if (format.ToUpperInvariant() == "BACKUP")
            {
                await RestoreFromBackupAsync(desktop);
                return;
            }

            // Only Excel import is supported for now
            if (format.ToUpperInvariant() != "EXCEL")
            {
                _appShellViewModel.AddNotification("Info".Translate(), "{0} import will be available in a future update.".TranslateFormat(format));
                return;
            }

            // Show open file dialog
            var file = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import Excel File".Translate(),
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new FilePickerFileType("Excel Workbook")
                    {
                        Patterns = ["*.xlsx"]
                    }
                ]
            });

            if (file.Count == 0) return;

            var filePath = file[0].Path.LocalPath;
            var companyData = CompanyManager.CompanyData;

            _mainWindowViewModel?.ShowLoading("Validating import file...".Translate());

            try
            {
                var importService = new SpreadsheetImportService(ErrorLogger, TelemetryManager);

                // First validate the import file
                var validationResult = await importService.ValidateImportAsync(filePath, companyData);

                _mainWindowViewModel?.HideLoading();

                // Check for any issues (errors, warnings, or missing refs) and show validation dialog
                var importOptions = new ImportOptions();

                if (validationResult.HasIssues && _appShellViewModel != null)
                {
                    var validationDialog = _appShellViewModel.ImportValidationDialogViewModel;
                    var dialogResult = await validationDialog.ShowAsync(validationResult);

                    if (dialogResult == ImportValidationDialogResult.Cancel)
                    {
                        return;
                    }

                    // If user chose to create missing references
                    if (dialogResult == ImportValidationDialogResult.CreateMissingAndImport)
                    {
                        importOptions.AutoCreateMissingReferences = true;
                    }

                    // If there are critical errors, don't allow import
                    if (validationResult.Errors.Count > 0)
                    {
                        return;
                    }
                }

                // Create snapshot of current data for undo
                var snapshot = CreateCompanyDataSnapshot(companyData);

                _mainWindowViewModel?.ShowLoading("Importing data...".Translate());

                await importService.ImportFromExcelAsync(filePath, companyData, importOptions);

                _mainWindowViewModel?.HideLoading();

                // Create snapshot of imported data for redo
                var importedSnapshot = CreateCompanyDataSnapshot(companyData);

                // Record undo action
                UndoRedoManager.RecordAction(new DelegateAction(
                    "Import spreadsheet data".Translate(),
                    () => { RestoreCompanyDataFromSnapshot(companyData, snapshot); CompanyManager.MarkAsChanged(); },
                    () => { RestoreCompanyDataFromSnapshot(companyData, importedSnapshot); CompanyManager.MarkAsChanged(); }
                ));

                // Mark as changed - this will trigger CompanyDataChanged event
                CompanyManager.MarkAsChanged();

                // Build success message with summary
                var successMessage = "Data has been imported successfully.".Translate();
                if (validationResult.ImportSummaries.Count > 0)
                {
                    var summaryLines = validationResult.ImportSummaries
                        .Where(s => s.Value.TotalInFile > 0)
                        .Select(s => "{0}: {1} new, {2} updated".TranslateFormat(s.Key, s.Value.NewRecords, s.Value.UpdatedRecords));
                    if (summaryLines.Any())
                    {
                        successMessage += $"\n\n{string.Join("\n", summaryLines)}";
                    }
                }
                successMessage += "\n\n" + "Please save to persist changes.".Translate();

                _appShellViewModel?.AddNotification("Import Complete".Translate(), successMessage, NotificationType.Success);
            }
            catch (Exception ex)
            {
                _mainWindowViewModel?.HideLoading();
                ErrorLogger?.LogError(ex, ErrorCategory.Import, "Failed to import spreadsheet data");
                var errorDialog = ConfirmationDialog;
                if (errorDialog != null)
                {
                    await errorDialog.ShowAsync(new ConfirmationDialogOptions
                    {
                        Title = "Import Failed".Translate(),
                        Message = "Failed to import data:\n\n{0}".TranslateFormat(ex.Message),
                        PrimaryButtonText = "OK".Translate(),
                        CancelButtonText = ""
                    });
                }
            }
        };
    }

    /// <summary>
    /// Handles restoring from a .argobk backup file.
    /// Copies the backup to a new .argo file chosen by the user, then opens it as a new company.
    /// The original company file is left untouched.
    /// </summary>
    private static async Task RestoreFromBackupAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (CompanyManager == null || _appShellViewModel == null) return;

        // Show open file dialog for backup files
        var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Restore from Backup".Translate(),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Argo Books Backup")
                {
                    Patterns = ["*.argobk"]
                }
            ]
        });

        if (files.Count == 0) return;

        var backupPath = files[0].Path.LocalPath;

        // Suggest a name based on the backup filename (strip .argobk, add .argo)
        var suggestedName = Path.GetFileNameWithoutExtension(backupPath);

        // Show save dialog to choose where to restore the company file
        var saveFile = await desktop.MainWindow!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Restored Company As".Translate(),
            SuggestedFileName = $"{suggestedName}.argo",
            DefaultExtension = "argo",
            FileTypeChoices =
            [
                new FilePickerFileType("Argo Books Files")
                {
                    Patterns = ["*.argo"]
                }
            ]
        });

        if (saveFile == null) return;

        var destPath = saveFile.Path.LocalPath;

        try
        {
            // Copy the backup file to the new .argo path
            File.Copy(backupPath, destPath, overwrite: true);

            // Open it as a new company (this closes the current one)
            await OpenCompanyWithRetryAsync(destPath);
        }
        catch (Exception ex)
        {
            _mainWindowViewModel?.HideLoading();
            ErrorLogger?.LogError(ex, ErrorCategory.Import, "Failed to restore from backup");
            await ShowErrorMessageBoxAsync("Restore Failed".Translate(), "Failed to restore from backup: {0}".TranslateFormat(ex.Message));
        }
    }

    /// <summary>
    /// Creates a JSON snapshot of the company data collections for undo/redo.
    /// </summary>
    private static string CreateCompanyDataSnapshot(Core.Data.CompanyData data)
    {
        var snapshot = new
        {
            data.IdCounters,
            data.Customers,
            data.Products,
            data.Suppliers,
            data.Employees,
            data.Departments,
            data.Categories,
            data.Locations,
            data.Revenues,
            data.Expenses,
            data.Invoices,
            data.Payments,
            data.RecurringInvoices,
            data.Inventory,
            data.StockAdjustments,
            data.StockTransfers,
            data.PurchaseOrders,
            data.RentalInventory,
            data.Rentals,
            data.Returns,
            data.LostDamaged,
            data.Receipts,
            data.ReportTemplates,
            data.EventLog
        };
        return System.Text.Json.JsonSerializer.Serialize(snapshot);
    }

    /// <summary>
    /// Restores company data collections from a JSON snapshot.
    /// </summary>
    private static void RestoreCompanyDataFromSnapshot(Core.Data.CompanyData data, string snapshotJson)
    {
        var options = new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        using var doc = System.Text.Json.JsonDocument.Parse(snapshotJson);
        var root = doc.RootElement;

        // Helper to deserialize a list property
        void RestoreList<T>(List<T> list, string propertyName)
        {
            list.Clear();
            if (root.TryGetProperty(propertyName, out var prop))
            {
                var items = System.Text.Json.JsonSerializer.Deserialize<List<T>>(prop.GetRawText(), options);
                if (items != null)
                    list.AddRange(items);
            }
        }

        // Restore IdCounters
        if (root.TryGetProperty("IdCounters", out var counters))
        {
            var restoredCounters = System.Text.Json.JsonSerializer.Deserialize<Core.Data.IdCounters>(counters.GetRawText(), options);
            if (restoredCounters != null)
            {
                data.IdCounters.Customer = restoredCounters.Customer;
                data.IdCounters.Product = restoredCounters.Product;
                data.IdCounters.Supplier = restoredCounters.Supplier;
                data.IdCounters.Employee = restoredCounters.Employee;
                data.IdCounters.Department = restoredCounters.Department;
                data.IdCounters.Category = restoredCounters.Category;
                data.IdCounters.Location = restoredCounters.Location;
                data.IdCounters.Revenue = restoredCounters.Revenue;
                data.IdCounters.Expense = restoredCounters.Expense;
                data.IdCounters.Invoice = restoredCounters.Invoice;
                data.IdCounters.Payment = restoredCounters.Payment;
                data.IdCounters.RecurringInvoice = restoredCounters.RecurringInvoice;
                data.IdCounters.InventoryItem = restoredCounters.InventoryItem;
                data.IdCounters.StockAdjustment = restoredCounters.StockAdjustment;
                data.IdCounters.PurchaseOrder = restoredCounters.PurchaseOrder;
                data.IdCounters.RentalItem = restoredCounters.RentalItem;
                data.IdCounters.Rental = restoredCounters.Rental;
            }
        }

        // Restore all collections
        RestoreList(data.Customers, "Customers");
        RestoreList(data.Products, "Products");
        RestoreList(data.Suppliers, "Suppliers");
        RestoreList(data.Employees, "Employees");
        RestoreList(data.Departments, "Departments");
        RestoreList(data.Categories, "Categories");
        RestoreList(data.Locations, "Locations");
        RestoreList(data.Revenues, "Revenues");
        RestoreList(data.Expenses, "Expenses");
        RestoreList(data.Invoices, "Invoices");
        RestoreList(data.Payments, "Payments");
        RestoreList(data.RecurringInvoices, "RecurringInvoices");
        RestoreList(data.Inventory, "Inventory");
        RestoreList(data.StockAdjustments, "StockAdjustments");
        RestoreList(data.StockTransfers, "StockTransfers");
        RestoreList(data.PurchaseOrders, "PurchaseOrders");
        RestoreList(data.RentalInventory, "RentalInventory");
        RestoreList(data.Rentals, "Rentals");
        RestoreList(data.Returns, "Returns");
        RestoreList(data.LostDamaged, "LostDamaged");
        RestoreList(data.Receipts, "Receipts");
        RestoreList(data.ReportTemplates, "ReportTemplates");
        RestoreList(data.EventLog, "EventLog");
    }

    /// <summary>
    /// Wires up idle detection for auto-logout functionality.
    /// </summary>
    private static void WireIdleDetection(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (_idleDetectionService == null || CompanyManager == null || _appShellViewModel == null)
            return;

        // Handle idle timeout - close the company
        _idleDetectionService.IdleTimeoutReached += async (_, _) =>
        {
            // Must run on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
            {
                if (CompanyManager.IsCompanyOpen != true) return;

                // Check for unsaved changes (skip auto-save for sample company)
                if (CompanyManager.HasUnsavedChanges && !CompanyManager.IsSampleCompany)
                {
                    // Auto-save before locking
                    try
                    {
                        _mainWindowViewModel?.ShowLoading("Auto-saving before lock...".Translate());
                        await CompanyManager.SaveCompanyAsync();
                        _mainWindowViewModel?.HideLoading();
                    }
                    catch (Exception ex)
                    {
                        _mainWindowViewModel?.HideLoading();
                        ErrorLogger?.LogWarning($"Auto-save before lock failed: {ex.Message}", "AutoSave");
                        // Continue to close even if save fails - user can reopen
                    }
                }

                // Close the company - this will trigger navigation back to welcome screen
                await CompanyManager.CloseCompanyAsync();

                // Show notification
                _appShellViewModel.AddNotification(
                    "Session Locked",
                    "Your session was locked due to inactivity. Please reopen your company file.",
                    NotificationType.Warning);

                // Re-enable idle detection for next session
                _idleDetectionService.ResetIdleTimer();
            });
        };

        // Configure based on current company settings when company opens
        CompanyManager.CompanyOpened += (_, _) =>
        {
            var companySettings = CompanyManager.CurrentCompanySettings;
            if (companySettings != null)
            {
                var security = companySettings.Security;
                _idleDetectionService.Configure(security.AutoLockEnabled, security.AutoLockMinutes);

                // Sync the UI with company settings
                var timeoutString = security.AutoLockMinutes switch
                {
                    0 => "Never",
                    60 => "1 hour",
                    _ => $"{security.AutoLockMinutes} minutes"
                };
                
                // Use SetAutoLockWithoutNotify to avoid triggering MarkAsChanged during load
                _appShellViewModel.SettingsModalViewModel.SetAutoLockWithoutNotify(timeoutString);
            }
        };

        // Disable idle detection when company closes
        CompanyManager.CompanyClosed += (_, _) =>
        {
            _idleDetectionService.Configure(false, 0);
        };

        // Record activity on main window pointer/key events
        if (desktop.MainWindow != null)
        {
            desktop.MainWindow.PointerMoved += (_, _) => _idleDetectionService.RecordActivity();
            desktop.MainWindow.KeyDown += (_, _) => _idleDetectionService.RecordActivity();
            desktop.MainWindow.PointerPressed += (_, _) => _idleDetectionService.RecordActivity();
        }
    }

    /// <summary>
    /// Opens the file dialog to select a company file.
    /// </summary>
    private static async Task OpenCompanyFileDialogAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Company".Translate(),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Argo Books Files")
                {
                    Patterns = ["*.argo"]
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = ["*.*"]
                }
            ]
        });

        if (files.Count > 0)
        {
            var filePath = files[0].Path.LocalPath;
            await OpenCompanyWithRetryAsync(filePath);
        }
    }

    /// <summary>
    /// Opens a company file with password retry support.
    /// Shows password modal on encrypted files and retries on wrong password.
    /// </summary>
    private static async Task OpenCompanyWithRetryAsync(string filePath)
    {
        if (CompanyManager == null || _mainWindowViewModel == null || _appShellViewModel == null) return;

        var passwordModal = _appShellViewModel.PasswordPromptModalViewModel;

        _mainWindowViewModel.ShowLoading("Opening company...".Translate());

        try
        {
            var success = await CompanyManager.OpenCompanyAsync(filePath);
            if (success)
            {
                // Close the password modal if it was open
                passwordModal.Close();
                await LoadRecentCompaniesAsync();
            }
            else
            {
                // User cancelled password prompt
                _mainWindowViewModel.HideLoading();
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Wrong password - show error and retry
            _mainWindowViewModel.HideLoading();

            passwordModal.ShowError("Invalid password. Please try again.".Translate());

            // Wait for the user to retry
            var newPassword = await passwordModal.WaitForPasswordAsync();

            if (string.IsNullOrEmpty(newPassword))
            {
                // User cancelled
                passwordModal.Close();
                return;
            }

            // Retry with the new password
            await OpenCompanyWithPasswordRetryAsync(filePath, newPassword);
        }
        catch (FileNotFoundException)
        {
            _mainWindowViewModel.HideLoading();
            passwordModal.Close();
            if (ConfirmationDialog != null)
            {
                await ConfirmationDialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "File Not Found".Translate(),
                    Message = "The company file no longer exists.".Translate(),
                    PrimaryButtonText = "OK".Translate(),
                    SecondaryButtonText = null,
                    CancelButtonText = null
                });
            }
            SettingsService?.RemoveRecentCompany(filePath);
            await LoadRecentCompaniesAsync();
        }
        catch (Exception ex)
        {
            _mainWindowViewModel.HideLoading();
            passwordModal.Close();
            ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to open company file");
            await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to open file: {0}".TranslateFormat(ex.Message));
        }
    }

    /// <summary>
    /// Retries opening a company file with a specific password.
    /// </summary>
    private static async Task<bool> OpenCompanyWithPasswordRetryAsync(string filePath, string password)
    {
        if (CompanyManager == null || _mainWindowViewModel == null || _appShellViewModel == null)
            return false;

        var passwordModal = _appShellViewModel.PasswordPromptModalViewModel;

        _mainWindowViewModel.ShowLoading("Opening company...".Translate());

        try
        {
            var success = await CompanyManager.OpenCompanyAsync(filePath, password);
            if (success)
            {
                passwordModal.Close();
                await LoadRecentCompaniesAsync();
                return true;
            }
            else
            {
                _mainWindowViewModel.HideLoading();
                return false;
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Wrong password again - show error and retry
            _mainWindowViewModel.HideLoading();

            passwordModal.ShowError("Invalid password. Please try again.".Translate());

            // Wait for the user to retry
            var newPassword = await passwordModal.WaitForPasswordAsync();

            if (string.IsNullOrEmpty(newPassword))
            {
                // User cancelled
                passwordModal.Close();
                return false;
            }

            // Retry recursively
            return await OpenCompanyWithPasswordRetryAsync(filePath, newPassword);
        }
        catch (Exception ex)
        {
            _mainWindowViewModel.HideLoading();
            passwordModal.Close();
            ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to open company file with password");
            await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to open file: {0}".TranslateFormat(ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Opens the save dialog for Save As.
    /// </summary>
    /// <returns>True if the file was saved successfully, false if cancelled or failed.</returns>
    private static async Task<bool> SaveCompanyAsDialogAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var suggestedName = CompanyManager?.CurrentCompanyName ?? "Company";

        // For sample company, suggest a copy name but don't show warning
        // (user already chose Save As, so they intend to create a copy)
        if (CompanyManager?.IsSampleCompany == true)
        {
            suggestedName += " (copy)";
        }

        var file = await ShowSaveFileDialogAsync(desktop, suggestedName);
        if (file == null) return false;

        var filePath = file.Path.LocalPath;

        try
        {
            await CompanyManager!.SaveCompanyAsAsync(filePath);

            // Refresh UI with the (possibly updated) company name
            var newName = CompanyManager.CurrentCompanyName ?? "Company";
            RefreshCompanyUI(newName);

            await LoadRecentCompaniesAsync();
            return true;
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to save company as new file");
            await ShowErrorMessageBoxAsync("Error".Translate(), "Failed to save file: {0}".TranslateFormat(ex.Message));
            return false;
        }
    }

    /// <summary>
    /// Public entry point for SaveAs dialog, callable from MainWindow.
    /// </summary>
    /// <returns>True if the file was saved successfully, false if cancelled or failed.</returns>
    public static async Task<bool> SaveCompanyAsFromWindowAsync()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return false;

        return await SaveCompanyAsDialogAsync(desktop);
    }

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    private static async Task<IStorageFile?> ShowSaveFileDialogAsync(IClassicDesktopStyleApplicationLifetime desktop, string suggestedFileName)
    {
        return await desktop.MainWindow!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Company".Translate(),
            SuggestedFileName = $"{suggestedFileName}.argo",
            DefaultExtension = "argo",
            FileTypeChoices =
            [
                new FilePickerFileType("Argo Books Files")
                {
                    Patterns = ["*.argo"]
                }
            ]
        });
    }

    /// <summary>
    /// Loads recent companies into the UI.
    /// </summary>
    private static async Task LoadRecentCompaniesAsync()
    {
        if (CompanyManager == null || _appShellViewModel == null || SettingsService == null)
            return;

        try
        {
            var recentCompanies = await CompanyManager.GetRecentCompaniesAsync();

            // Update file menu
            _appShellViewModel.FileMenuPanelViewModel.RecentCompanies.Clear();
            foreach (var company in recentCompanies.Take(10))
            {
                _appShellViewModel.FileMenuPanelViewModel.RecentCompanies.Add(new RecentCompanyItem
                {
                    Name = company.CompanyName,
                    FilePath = company.FilePath,
                    LastOpened = company.ModifiedAt,
                    Icon = company.IsEncrypted ? "Lock" : "Building"
                });
            }

            // Update company switcher
            _appShellViewModel.CompanySwitcherPanelViewModel.RecentCompanies.Clear();
            foreach (var company in recentCompanies.Take(5))
            {
                _appShellViewModel.CompanySwitcherPanelViewModel.AddRecentCompany(
                    company.CompanyName,
                    company.FilePath);
            }

            // Update welcome screen
            if (_welcomeScreenViewModel != null)
            {
                _welcomeScreenViewModel.RecentCompanies.Clear();

                var companiesForWelcome = recentCompanies.Take(10).ToList();

                foreach (var company in companiesForWelcome)
                {
                    // Use logo thumbnail from footer (instant, no decompression needed)
                    byte[]? logoBytes = null;
                    if (company.LogoThumbnail != null)
                    {
                        try { logoBytes = Convert.FromBase64String(company.LogoThumbnail); }
                        catch { /* corrupted thumbnail, skip */ }
                    }

                    _welcomeScreenViewModel.RecentCompanies.Add(new RecentCompanyItem
                    {
                        Name = company.CompanyName,
                        FilePath = company.FilePath,
                        LastOpened = company.ModifiedAt,
                        Icon = company.IsEncrypted ? "Lock" : "Building",
                        Logo = LoadBitmapFromBytes(logoBytes)
                    });
                }
                _welcomeScreenViewModel.HasRecentCompanies = _welcomeScreenViewModel.RecentCompanies.Count > 0;
                _welcomeScreenViewModel.IsRecentCompaniesLoaded = true;
            }

            // Set up file watchers to detect when recent company files are deleted
            SetupRecentCompanyFileWatchers(recentCompanies.Select(c => c.FilePath).ToList());
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogWarning($"Failed to load recent companies: {ex.Message}", "RecentCompanies");
            // Mark as loaded even on error to prevent indefinite hidden state
            if (_welcomeScreenViewModel != null)
                _welcomeScreenViewModel.IsRecentCompaniesLoaded = true;
        }
    }

    /// <summary>
    /// Sets up file system watchers to detect when recent company files are deleted.
    /// </summary>
    private static void SetupRecentCompanyFileWatchers(List<string> filePaths)
    {
        // Dispose existing watchers
        foreach (var watcher in _recentCompanyWatchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        _recentCompanyWatchers.Clear();

        // Group files by directory to minimize number of watchers
        var directories = filePaths
            .Select(Path.GetDirectoryName)
            .Where(d => !string.IsNullOrEmpty(d))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var directory in directories)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                continue;

            try
            {
                var watcher = new FileSystemWatcher(directory)
                {
                    Filter = "*.argo",
                    NotifyFilter = NotifyFilters.FileName,
                    EnableRaisingEvents = true
                };

                watcher.Deleted += OnRecentCompanyFileDeleted;
                watcher.Renamed += OnRecentCompanyFileRenamed;

                _recentCompanyWatchers[directory] = watcher;
            }
            catch (Exception ex)
            {
                ErrorLogger?.LogWarning($"Failed to create file watcher for {directory}: {ex.Message}", "FileWatcher");
            }
        }
    }

    /// <summary>
    /// Handles when a recent company file is deleted from the file system.
    /// </summary>
    private static void OnRecentCompanyFileDeleted(object sender, FileSystemEventArgs e)
    {
        // Run on UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            RemoveRecentCompanyFromUI(e.FullPath);
        });
    }

    /// <summary>
    /// Handles when a recent company file is renamed (which also removes it from our tracked list).
    /// </summary>
    private static void OnRecentCompanyFileRenamed(object sender, RenamedEventArgs e)
    {
        // Run on UI thread - remove the old path
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            RemoveRecentCompanyFromUI(e.OldFullPath);
        });
    }

    /// <summary>
    /// Removes a company from all recent company UI lists and persists the change.
    /// </summary>
    private static void RemoveRecentCompanyFromUI(string filePath)
    {
        if (_appShellViewModel == null || SettingsService == null)
            return;

        // Remove from file menu
        var fileMenuItem = _appShellViewModel.FileMenuPanelViewModel.RecentCompanies
            .FirstOrDefault(c => string.Equals(c.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (fileMenuItem != null)
            _appShellViewModel.FileMenuPanelViewModel.RecentCompanies.Remove(fileMenuItem);

        // Remove from company switcher
        var switcherItem = _appShellViewModel.CompanySwitcherPanelViewModel.RecentCompanies
            .FirstOrDefault(c => string.Equals(c.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
        if (switcherItem != null)
            _appShellViewModel.CompanySwitcherPanelViewModel.RecentCompanies.Remove(switcherItem);

        // Remove from welcome screen
        if (_welcomeScreenViewModel != null)
        {
            var welcomeItem = _welcomeScreenViewModel.RecentCompanies
                .FirstOrDefault(c => string.Equals(c.FilePath, filePath, StringComparison.OrdinalIgnoreCase));
            if (welcomeItem != null)
            {
                _welcomeScreenViewModel.RecentCompanies.Remove(welcomeItem);
                _welcomeScreenViewModel.HasRecentCompanies = _welcomeScreenViewModel.RecentCompanies.Count > 0;
            }
        }

        // Persist the removal to settings
        SettingsService.RemoveRecentCompany(filePath);
        _ = SettingsService.SaveGlobalSettingsAsync();
    }

    /// <summary>
    /// Shows the unsaved changes dialog with a list of all changes.
    /// </summary>
    /// <returns>The user's choice.</returns>
    private static async Task<UnsavedChangesResult> ShowUnsavedChangesDialogAsync()
    {
        if (UnsavedChangesDialog == null)
            return UnsavedChangesResult.Cancel;

        // Get changes from the change tracking service if available
        var categories = ChangeTrackingService?.GetAllChangeCategories();

        return await UnsavedChangesDialog.ShowAsync(categories);
    }

    /// <summary>
    /// Registers all available pages with the navigation service.
    /// </summary>
    private static void RegisterPages(NavigationService navigationService, AppShellViewModel appShellViewModel)
    {
        // Register placeholder pages - these will be replaced with actual views as they're implemented
        // The page factory receives optional parameters and returns a view or viewmodel

        // Welcome Screen (shown when no company is open)
        _welcomeScreenViewModel = new WelcomeScreenViewModel();
        navigationService.RegisterPage("Welcome", _ => new WelcomeScreen { DataContext = _welcomeScreenViewModel });

        // Main Section
        navigationService.RegisterPage("Dashboard", _ =>
        {
            if (_dashboardPageViewModel == null)
            {
                _dashboardPageViewModel = new DashboardPageViewModel();
                // Wire up Google Sheets export notifications (only once)
                _dashboardPageViewModel.GoogleSheetsExportStatusChanged += async (_, args) =>
                {
                    if (args.IsExporting)
                    {
                        _mainWindowViewModel?.ShowLoading("Exporting to Google Sheets...".Translate());
                    }
                    else if (args.IsSuccess)
                    {
                        _mainWindowViewModel?.HideLoading();
                        // No notification - the browser opens automatically
                    }
                    else if (!string.IsNullOrEmpty(args.ErrorMessage))
                    {
                        _mainWindowViewModel?.HideLoading();
                        await ShowErrorMessageBoxAsync("Export Failed".Translate(), args.ErrorMessage);
                    }
                };
            }
            if (CompanyManager?.IsCompanyOpen == true)
            {
                _dashboardPageViewModel.Initialize(CompanyManager);
            }
            return new DashboardPage { DataContext = _dashboardPageViewModel };
        });
        navigationService.RegisterPage("Analytics", _ =>
        {
            _analyticsPageViewModel ??= new AnalyticsPageViewModel();
            if (CompanyManager?.IsCompanyOpen == true)
            {
                _analyticsPageViewModel.Initialize(CompanyManager);
            }
            return new AnalyticsPage { DataContext = _analyticsPageViewModel };
        });
        navigationService.RegisterPage("Insights", _ => new InsightsPage { DataContext = _insightsPageViewModel ??= new InsightsPageViewModel() });
        navigationService.RegisterPage("Reports", _ => new ReportsPage { DataContext = _reportsPageViewModel ??= new ReportsPageViewModel() });

        // Transactions Section
        navigationService.RegisterPage("Revenue", param =>
        {
            _revenuePageViewModel ??= new RevenuePageViewModel();
            // Clear any previous highlight first
            _revenuePageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _revenuePageViewModel.HighlightTransactionId = navParam.TransactionId;
                _revenuePageViewModel.ApplyHighlight();
            }
            return new RevenuePage { DataContext = _revenuePageViewModel };
        });
        navigationService.RegisterPage("Expenses", param =>
        {
            _expensesPageViewModel ??= new ExpensesPageViewModel();
            // Clear any previous highlight first
            _expensesPageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _expensesPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _expensesPageViewModel.ApplyHighlight();
            }
            return new ExpensesPage { DataContext = _expensesPageViewModel };
        });
        navigationService.RegisterPage("Invoices", param =>
        {
            _invoicesPageViewModel ??= new InvoicesPageViewModel();
            if (param is RentalInvoiceNavigationParameter rentalParam)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    InvoiceModalsViewModel?.OpenCreateFromRental(rentalParam.RentalRecordId);
                });
            }
            return new InvoicesPage { DataContext = _invoicesPageViewModel };
        });
        navigationService.RegisterPage("Payments", _ => new PaymentsPage { DataContext = _paymentsPageViewModel ??= new PaymentsPageViewModel() });

        // Inventory Section
        navigationService.RegisterPage("Products", param =>
        {
            if (_productsPageViewModel == null)
            {
                _productsPageViewModel = new ProductsPageViewModel();
                // Wire up upgrade request to open upgrade modal (only once)
                _productsPageViewModel.UpgradeRequested += (_, _) => _appShellViewModel?.UpgradeModalViewModel.OpenCommand.Execute(null);
            }
            // Update plan status each time (may have changed)
            _productsPageViewModel.HasPremium = _appShellViewModel?.SidebarViewModel.HasPremium ?? false;
            // Reset modal state
            _productsPageViewModel.IsAddModalOpen = false;
            if (param is Dictionary<string, object?> dict)
            {
                // Check if we should select a specific tab (0 = Expenses, 1 = Revenue)
                if (dict.TryGetValue("selectedTabIndex", out var tabIndex) && tabIndex is int index)
                {
                    _productsPageViewModel.SelectedTabIndex = index;
                }
                // Check if we should open the add modal
                if (dict.TryGetValue("openAddModal", out var openAdd) && openAdd is true)
                {
                    _productsPageViewModel.IsAddModalOpen = true;
                }
            }
            return new ProductsPage { DataContext = _productsPageViewModel };
        });
        navigationService.RegisterPage("StockLevels", _ => new StockLevelsPage { DataContext = _stockLevelsPageViewModel ??= new StockLevelsPageViewModel() });
        navigationService.RegisterPage("Locations", param =>
        {
            _locationsPageViewModel ??= new LocationsPageViewModel();
            // Check if we should open the add modal
            if (param is Dictionary<string, object?> dict && dict.TryGetValue("openAddModal", out var openAdd) && openAdd is true)
            {
                LocationsModalsViewModel?.OpenAddModal();
            }
            return new LocationsPage { DataContext = _locationsPageViewModel };
        });
        navigationService.RegisterPage("StockAdjustments", _ => new StockAdjustmentsPage { DataContext = _stockAdjustmentsPageViewModel ??= new StockAdjustmentsPageViewModel() });
        navigationService.RegisterPage("PurchaseOrders", _ => new PurchaseOrdersPage { DataContext = _purchaseOrdersPageViewModel ??= new PurchaseOrdersPageViewModel() });
        navigationService.RegisterPage("Categories", param =>
        {
            _categoriesPageViewModel ??= new CategoriesPageViewModel();
            // Reset modal state
            _categoriesPageViewModel.IsAddModalOpen = false;
            if (param is Dictionary<string, object?> dict)
            {
                // Check if we should select a specific tab (0 = Expenses, 1 = Revenue)
                if (dict.TryGetValue("selectedTabIndex", out var tabIndex) && tabIndex is int index)
                {
                    _categoriesPageViewModel.SelectedTabIndex = index;
                }
                // Check if we should open the add modal
                if (dict.TryGetValue("openAddModal", out var openAdd) && openAdd is true)
                {
                    _categoriesPageViewModel.IsAddModalOpen = true;
                }
            }
            return new CategoriesPage { DataContext = _categoriesPageViewModel };
        });

        // Contacts Section
        navigationService.RegisterPage("Customers", _ => new CustomersPage { DataContext = _customersPageViewModel ??= new CustomersPageViewModel() });
        navigationService.RegisterPage("Suppliers", _ => new SuppliersPage { DataContext = _suppliersPageViewModel ??= new SuppliersPageViewModel() });
        navigationService.RegisterPage("Employees", _ => CreatePlaceholderPage("Employees", "Manage employee records"));
        navigationService.RegisterPage("Departments", _ => new DepartmentsPage { DataContext = _departmentsPageViewModel ??= new DepartmentsPageViewModel() });
        navigationService.RegisterPage("Accountants", _ => CreatePlaceholderPage("Accountants", "Manage accountant information"));

        // Rentals Section
        navigationService.RegisterPage("RentalInventory", _ => new RentalInventoryPage { DataContext = _rentalInventoryPageViewModel ??= new RentalInventoryPageViewModel() });
        navigationService.RegisterPage("RentalRecords", param =>
        {
            _rentalRecordsPageViewModel ??= new RentalRecordsPageViewModel();
            _rentalRecordsPageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _rentalRecordsPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _rentalRecordsPageViewModel.ApplyHighlight();
            }
            return new RentalRecordsPage { DataContext = _rentalRecordsPageViewModel };
        });

        // Tracking Section
        navigationService.RegisterPage("Returns", _ => new ReturnsPage { DataContext = _returnsPageViewModel ??= new ReturnsPageViewModel() });
        navigationService.RegisterPage("LostDamaged", _ => new LostDamagedPage { DataContext = _lostDamagedPageViewModel ??= new LostDamagedPageViewModel() });
        navigationService.RegisterPage("Receipts", _ =>
        {
            _receiptsPageViewModel ??= new ReceiptsPageViewModel();
            // Update plan status each time (may have changed)
            _receiptsPageViewModel.HasPremium = _appShellViewModel?.SidebarViewModel.HasPremium ?? false;
            return new ReceiptsPage { DataContext = _receiptsPageViewModel };
        });

    }

    /// <summary>
    /// Creates a placeholder page view for pages not yet implemented.
    /// </summary>
    private static object CreatePlaceholderPage(string title, string description)
    {
        return new PlaceholderPage
        {
            DataContext = new PlaceholderPageViewModel(title, description)
        };
    }

    /// <summary>
    /// Loads a Bitmap from a file path.
    /// </summary>
    private static Bitmap? LoadBitmapFromPath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return null;

        try
        {
            return new Bitmap(path);
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogWarning($"Failed to load bitmap from path: {ex.Message}", "BitmapLoader");
            return null;
        }
    }

    private static Bitmap? LoadBitmapFromBytes(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return null;

        try
        {
            using var ms = new MemoryStream(data);
            return new Bitmap(ms);
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogWarning($"Failed to load bitmap from bytes: {ex.Message}", "BitmapLoader");
            return null;
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}

/// <summary>
/// Event arguments for plan status changes.
/// </summary>
public class PlanStatusChangedEventArgs(bool hasPremium) : EventArgs
{
    public bool HasPremium { get; } = hasPremium;
}
