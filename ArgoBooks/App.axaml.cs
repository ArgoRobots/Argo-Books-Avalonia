using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.AI;
using ArgoBooks.Core.Models.Portal;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.ViewModels;
using ArgoBooks.Views;

namespace ArgoBooks;

public partial class App : Application
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
    /// Client for the portal refund + email-verification + email-change endpoints.
    /// Created once at startup; reads the active per-company API key on each call.
    /// </summary>
    public static RefundService? RefundService { get; private set; }

    /// <summary>
    /// Coordinator for the refund / email-verify / email-change modals.
    /// Hosted at AppShell level so the ModalOverlay can dim the whole window.
    /// </summary>
    public static RefundModalsViewModel? RefundModalsViewModel => _appShellViewModel?.RefundModalsViewModel;

    /// <summary>
    /// Gets the invoice usage service for tracking free-tier send limits.
    /// </summary>
    public static InvoiceUsageService? InvoiceUsageService { get; private set; }

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
    /// Gets the rental availability modal view model for shared access (per-item calendar).
    /// </summary>
    public static RentalAvailabilityModalViewModel? RentalAvailabilityModalViewModel => _appShellViewModel?.RentalAvailabilityModalViewModel;

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

    public static BankMatchingModalsViewModel? BankMatchingModalsViewModel => _appShellViewModel?.BankMatchingModalsViewModel;

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
    public static EventLogService? EventLogService { get; private set; }

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
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow
            && mainWindow.MessageBoxService is { } messageBoxService)
        {
            await messageBoxService.ShowErrorAsync(title, message);
        }
    }

    /// <summary>
    /// Shows a modal info message box.
    /// </summary>
    private static async Task ShowInfoMessageBoxAsync(string title, string message)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow
            && mainWindow.MessageBoxService is { } messageBoxService)
        {
            await messageBoxService.ShowInfoAsync(title, message);
        }
    }

    /// <summary>
    /// Shows a modal warning message box.
    /// </summary>
    public static async Task ShowWarningMessageBoxAsync(string title, string message)
    {
        if (Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
            && desktop.MainWindow is MainWindow mainWindow
            && mainWindow.MessageBoxService is { } messageBoxService)
        {
            await messageBoxService.ShowWarningAsync(title, message);
        }
    }

    private static int _isAutoSyncing;
    private static Timer? _portalSyncTimer;

    /// <summary>
    /// Auto-syncs online payments from the portal so invoice statuses stay up-to-date.
    /// Safe to call from multiple places — concurrent calls are deduplicated.
    /// </summary>
    internal static async Task AutoSyncPortalPaymentsAsync()
    {
        if (Interlocked.CompareExchange(ref _isAutoSyncing, 1, 0) != 0) return;
        try
        {
            var portalService = PaymentPortalService;
            var companyData = CompanyManager?.CompanyData;
            if (portalService == null || companyData == null || !PortalSettings.IsConfigured)
                return;

            var portalSettings = companyData.Settings.PaymentPortal;

            // Use force sync to also recover any payments that were previously
            // confirmed on the server but never saved locally (e.g. due to app crash).
            // Duplicate prevention in ProcessSyncedPayments handles efficiency.
            var syncResponse = await portalService.SyncPaymentsAsync(since: null, force: true);

            if (!syncResponse.Success)
                return;

            // Always advance the sync timestamp on success to avoid re-querying the same window
            portalSettings.LastSyncTime = syncResponse.SyncTimestamp ?? DateTime.UtcNow;

            if (syncResponse.Payments.Count == 0)
                return;

            var syncResult = PaymentPortalService.ProcessSyncedPayments(
                syncResponse.Payments, companyData);
            var newPayments = syncResult.NewPayments;

            // Only confirm payments that were actually processed into local records.
            // Unprocessed payments (e.g. invoice not found locally) must NOT be
            // confirmed so the server returns them on the next sync attempt.
            var processedPortalIds = newPayments
                .Where(p => p.PortalPaymentId != null)
                .Select(p => int.Parse(p.PortalPaymentId!))
                .ToList();
            if (processedPortalIds.Count > 0)
            {
                await portalService.ConfirmSyncAsync(processedPortalIds);
            }

            // Persist when there are new rows OR existing rows were backfilled
            // with previously-missing fields (e.g. ProcessingFee on pre-fix
            // payments). Without the backfill arm, the in-memory update gets
            // lost on next app launch and the fee disappears again.
            if (newPayments.Count > 0 || syncResult.BackfilledRows > 0)
            {
                try { await CompanyManager!.SavePaymentSyncAsync(); }
                catch (Exception ex)
                {
                    ErrorLogger?.LogWarning($"Failed to persist synced payments: {ex.Message}", "PortalSync");
                }

                // Refresh any already-instantiated page ViewModels so the UI reflects the new data
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    _paymentsPageViewModel?.RefreshPaymentsCommand.Execute(null);
                    _invoicesPageViewModel?.RefreshInvoicesCommand.Execute(null);
                    _revenuePageViewModel?.RefreshRevenueCommand.Execute(null);

                    // Send "Payment Received" notification if enabled. Skipped
                    // for the backfill-only path (no new payments) — there's
                    // nothing the user just received to be notified about.
                    if (newPayments.Count > 0 && companyData.Settings.PaymentPortal.NotifyOnPayment)
                    {
                        var total = newPayments.Sum(p => p.Amount);
                        var message = newPayments.Count == 1
                            ? "{0} online payment received ({1:C})".TranslateFormat(newPayments.Count, total)
                            : "{0} online payments received ({1:C})".TranslateFormat(newPayments.Count, total);

                        AddNotification(
                            "Payment Received".Translate(),
                            message,
                            NotificationType.Success);
                    }
                });
            }
        }
        catch
        {
            // Auto-sync failures are non-critical; silently ignore
        }
        finally
        {
            Interlocked.Exchange(ref _isAutoSyncing, 0);
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

    /// <summary>
    /// Opens the upgrade modal from anywhere in the app.
    /// </summary>
    public static void OpenUpgradeModal()
    {
        _appShellViewModel?.UpgradeModalViewModel.OpenCommand.Execute(null);
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
    private static Timer? _pendingConversionTimer;

    // Cached page ViewModels to improve performance and prevent memory leaks from event subscriptions
    private static DashboardPageViewModel? _dashboardPageViewModel;
    private static AnalyticsPageViewModel? _analyticsPageViewModel;
    private static InsightsPageViewModel? _insightsPageViewModel;
    private static ReportsPageViewModel? _reportsPageViewModel;
    private static RevenuePageViewModel? _revenuePageViewModel;
    private static ExpensesPageViewModel? _expensesPageViewModel;
    private static InvoicesPageViewModel? _invoicesPageViewModel;
    private static PaymentsPageViewModel? _paymentsPageViewModel;
    private static BankMatchingPageViewModel? _bankMatchingPageViewModel;
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
        _bankMatchingPageViewModel = null;
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
    private static readonly Dictionary<string, FileSystemWatcher> RecentCompanyWatchers = new();

    // When true, the CompanySaved event handler skips showing the "Saved" indicator
    private static bool _suppressSavedFeedback;
    private static bool _isOpeningCompany;

    /// <summary>
    /// Suppresses the "Saved" feedback label for the next save operation.
    /// </summary>
    public static void SuppressNextSavedFeedback() => _suppressSavedFeedback = true;

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
    /// Gets the custom date range modal ViewModel for date range selection from anywhere.
    /// </summary>
    public static CustomDateRangeModalViewModel? CustomDateRangeModal { get; private set; }

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

    /// <summary>
    /// Gets the pending conversion service for processing offline transactions.
    /// </summary>
    public static PendingConversionService? PendingConversionService { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
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
                errorLogger,
                appVersion);

            // Initialize payment portal service
            PaymentPortalService = new PaymentPortalService();
            InvoiceUsageService = new InvoiceUsageService(LicenseService, ErrorLogger);

            // Initialize refund service (uses the same shared HttpClient)
            RefundService = new RefundService(httpClient);

            // Create navigation service
            NavigationService = new NavigationService();

            _mainWindowViewModel = new MainWindowViewModel();
            ConfirmationDialog = new ConfirmationDialogViewModel();
            UnsavedChangesDialog = new UnsavedChangesDialogViewModel();
            ReceiptViewerModal = new ReceiptViewerModalViewModel();
            ChangeTrackingService = new ChangeTrackingService();
            PendingConversionService = new PendingConversionService(errorLogger);
            _idleDetectionService = new IdleDetectionService();

            // Create app shell with navigation service and optional update service
            _appShellViewModel = new AppShellViewModel(NavigationService, UpdateService);
            CustomDateRangeModal = _appShellViewModel.CustomDateRangeModalViewModel;

            // Ensure no unsaved changes indicator on startup
            _mainWindowViewModel.HasUnsavedChanges = false;
            _appShellViewModel.HeaderViewModel.HasUnsavedChanges = false;

            var appShell = new AppShell
            {
                DataContext = _appShellViewModel
            };

            // Register pages with navigation service
            RegisterPages(NavigationService);

            // Set navigation callback to update current page in AppShell
            NavigationService.SetNavigationCallback(page => _appShellViewModel.CurrentPage = page);

            // Dismiss tutorial completion guidance when user navigates
            NavigationService.Navigated += (_, _) =>
            {
                TutorialService.Instance.DismissCompletionGuidance();
            };

            // Set initial view
            _mainWindowViewModel.NavigateTo(appShell);

            // Wire up company manager events
            WireCompanyManagerEvents();

            // Wire up pending conversion events
            PendingConversionService.PendingConversionsProcessed += (_, args) =>
            {
                if (args is { ConvertedCount: > 0 })
                {
                    var message = args.ConvertedCount == 1
                        ? "1 pending transaction has been processed successfully.".Translate()
                        : string.Format("{0} pending transactions have been processed successfully.".Translate(), args.ConvertedCount);
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        AddNotification(
                            "Back Online".Translate(),
                            message,
                            NotificationType.Success);

                        // Refresh ViewModels so converted transactions show updated status and amounts
                        _expensesPageViewModel?.RefreshExpensesCommand.Execute(null);
                        _revenuePageViewModel?.RefreshRevenueCommand.Execute(null);
                        NavigationService.RefreshCurrentPage();
                    });
                }
            };

            // Wire up modal change events (separate from company manager)
            WireModalChangeEvents();

            // Sync HasUnsavedChanges with undo/redo state (both MainWindow and Header)
            UndoRedoManager.StateChanged += (_, _) =>
            {
                var hasChanges = !UndoRedoManager.IsAtSavedState;
                _mainWindowViewModel.HasUnsavedChanges = hasChanges;
                _appShellViewModel.HeaderViewModel.HasUnsavedChanges = hasChanges;
            };

            // After any undo/redo, fire CompanyDataChanged so pages
            // subscribed to it (Dashboard, Analytics, Invoices, etc.) refresh
            // their derived data. Individual undo callbacks only call
            // companyData.MarkAsModified() which doesn't fire the event.
            UndoRedoManager.ActionUndone += (_, _) => CompanyManager?.NotifyDataChanged();
            UndoRedoManager.ActionRedone += (_, _) => CompanyManager?.NotifyDataChanged();

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
                        var saved = await SaveCompanyAsDialogAsync(desktop);
                        if (!saved)
                            _appShellViewModel.HeaderViewModel.ShowSavingIndicator = false;
                        return;
                    }

                    try
                    {
                        await CompanyManager.SaveCompanyAsync();
                    }
                    catch (Exception ex)
                    {
                        _appShellViewModel.HeaderViewModel.ShowSavingIndicator = false;
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

            // Load settings synchronously — sidebar state, theme, and language depend on them.
            // Direct sync read avoids the thread-pool marshaling cost of sync-over-async;
            // the settings file is small (<10KB). Recent companies are loaded asynchronously
            // in InitializeAsync after the window is shown.
            try
            {
                SettingsService?.LoadGlobalSettings();
            }
            catch (Exception ex)
            {
                ErrorLogger.LogWarning($"Failed to load settings during startup: {ex.Message}", "Startup");
            }

            // Apply saved sidebar collapsed state after settings are loaded from disk.
            // The SidebarViewModel was created before settings were loaded, so its constructor
            // read the default value. Re-apply the persisted state now.
            var savedCollapsed = SettingsService?.GlobalSettings.Ui.SidebarCollapsed ?? false;
            if (savedCollapsed)
            {
                _appShellViewModel.SidebarViewModel.IsCollapsed = true;
            }

            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel
            };

            // Process pending conversions when window is activated (e.g., user returns after going offline)
            desktop.MainWindow.Activated += async (_, _) =>
            {
                await TryProcessPendingConversionsAsync();
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
            // Load recent companies asynchronously (footer reads from .argo files)
            await LoadRecentCompaniesAsync();

            // Register file type associations on Windows
            RegisterFileTypeAssociationsAsync();

            // Post-update recovery: auto-reopen the last company after an update restart
            await TryAutoOpenRecentCompanyAfterUpdateAsync();

            // Initialize services that depend on settings.
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

            // Report first-run install for referral funnel attribution. Fire-and-forget
            // so app startup isn't blocked on network I/O. The reporter writes a marker
            // after a successful POST so subsequent launches are no-ops. The HttpClient
            // is disposed inside the task so it doesn't leak past the one-shot report.
            try
            {
                var appVersion = Services.AppInfo.VersionNumber;
                var capturedErrorLogger = ErrorLogger;
                _ = Task.Run(async () =>
                {
                    using var firstRunHttpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                    var firstRunReporter = new FirstRunReporter(
                        firstRunHttpClient,
                        appVersion,
                        capturedErrorLogger);
                    await firstRunReporter.ReportIfFirstRunAsync();
                });
            }
            catch (Exception ex)
            {
                ErrorLogger?.LogWarning(
                    $"Failed to start FirstRunReporter: {ex.Message}",
                    context: "App.OnFrameworkInitializationCompleted");
            }

            // Remove stale cached receipt preview/render files from temp (fire-and-forget).
            _ = Services.ReceiptTempCleanup.CleanOldFilesAsync();

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

                // Refresh cached translations once per app version. Without this, users
                // never see translations added after their first language download because
                // DownloadAndCacheLanguageAsync skips when a cached file exists.
                var currentVersion = Services.AppInfo.VersionNumber;
                if (SettingsService.GlobalSettings.Ui.LastLanguageVersion != currentVersion)
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var refreshed = await LanguageService.Instance.UpdateAllCachedTranslationsAsync();
                            if (refreshed)
                            {
                                SettingsService.GlobalSettings.Ui.LastLanguageVersion = currentVersion;
                                await SettingsService.SaveGlobalSettingsAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            ErrorLogger?.LogError(ex, ErrorCategory.Network, "Translation cache refresh failed");
                        }
                    });
                }
            }

            // Initialize exchange rate service for currency conversion
            await InitializeExchangeRateServiceAsync();

            // Load pending conversion queue from disk
            if (PendingConversionService != null)
            {
                await PendingConversionService.LoadAsync();
            }

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
                else
                {
                    // Fetch plan details from API so the upgrade modal is ready
                    _ = _appShellViewModel.UpgradeModalViewModel.FetchPlansAsync();
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
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogError(ex, ErrorCategory.Unknown, "Failed to initialize exchange rate service");
        }
    }

    /// <summary>
    /// Starts a periodic timer that checks for pending conversions and processes them
    /// when connectivity is restored. Runs every 15 seconds.
    /// </summary>
    private static void StartPendingConversionTimer()
    {
        // Dispose any existing timer
        _pendingConversionTimer?.Dispose();

        _pendingConversionTimer = new Timer(state => { _ = TryProcessPendingConversionsAsync(); },
            null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));
    }

    /// <summary>
    /// Attempts to process pending conversions if online and there are pending items.
    /// Called by the timer and on window activation for faster reconnect detection.
    /// </summary>
    private static async Task TryProcessPendingConversionsAsync()
    {
        try
        {
            if (PendingConversionService == null || !PendingConversionService.HasPendingConversions)
                return;

            if (CompanyManager?.CompanyData == null)
                return;

            // Check if we're online before attempting to process
            var connectivityService = new ConnectivityService();
            var isOnline = await connectivityService.IsInternetAvailableAsync();
            if (!isOnline)
                return;

            await PendingConversionService.ProcessPendingConversionsAsync(CompanyManager.CompanyData);
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogWarning($"Pending conversion processing error: {ex.Message}", "App");
        }
    }

    /// <summary>
    /// Stops the pending conversion timer.
    /// </summary>
    private static void StopPendingConversionTimer()
    {
        _pendingConversionTimer?.Dispose();
        _pendingConversionTimer = null;
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

            ErrorLogger?.LogWarning("Could not find icon resource", "IconExtraction");
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
    /// Shows the OS image-picker, decodes the result into a Bitmap, and hands the
    /// pair off to the caller. Centralized so customer and supplier avatar pickers
    /// share the file-type filter and error handling.
    /// </summary>
    private static async Task PickAvatarAsync(string title, Action<string, Bitmap> onPicked, string errorTag)
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = title,
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Images")
                {
                    Patterns = ["*.png", "*.jpg", "*.jpeg"]
                }
            ]
        });

        if (files.Count == 0) return;

        var path = files[0].Path.LocalPath;
        try
        {
            var bitmap = new Bitmap(path);
            onPicked(path, bitmap);
        }
        catch (Exception ex)
        {
            ErrorLogger?.LogWarning($"Failed to load avatar image: {ex.Message}", errorTag);
        }
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
            settings?.Company.ProvinceState,
            settings?.Company.Email,
            CompanyManager.CompanyData!.Settings.Localization.Currency);
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
    private static void RefreshCompanyUi(string companyName)
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

    private static async Task<bool> ConfirmCancelAsync()
    {
        var dialog = ConfirmationDialog;
        if (dialog == null) return true;
        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
        {
            Title = "Cancel Operation?".Translate(),
            Message = "Are you sure you want to cancel?".Translate(),
            PrimaryButtonText = "Yes".Translate(),
            CancelButtonText = "No".Translate()
        });
        return result == ConfirmationResult.Primary;
    }

    /// <summary>
    /// Performs the AI-powered import flow: analyze → review → validate → import.
    /// </summary>
    private static async Task PerformAiImportAsync(string filePath, CompanyData companyData, bool isCsv)
    {
        if (_appShellViewModel == null) return;

        var analysisCts = new CancellationTokenSource();
        _mainWindowViewModel?.ShowLoading("Analyzing spreadsheet structure...".Translate(), "Reading file...", 0, analysisCts, ConfirmCancelAsync);
        await Task.Yield(); // Allow UI to render the loading overlay before heavy work begins

        // Check rate limit via server-side API
        using var usageService = new AiImportUsageService(LicenseService, ErrorLogger);
        var usageCheck = await usageService.CheckUsageAsync();

        if (!usageCheck.CanImport)
        {
            _mainWindowViewModel?.HideLoading();
            await UpgradePromptHelper.ShowAiImportLimitPromptAsync(
                usageCheck.ImportCount,
                usageCheck.MonthlyLimit,
                usageCheck.ResetsAt);
            return;
        }

        var geminiService = new GeminiService(ErrorLogger, TelemetryManager);
        if (!geminiService.IsConfigured)
        {
            _mainWindowViewModel?.HideLoading();
            await ShowErrorMessageBoxAsync(
                "AI Not Configured".Translate(),
                "AI-powered import requires portal access. Please register your company first.".Translate());
            return;
        }

        var analysisService = new SpreadsheetAnalysisService(geminiService, ErrorLogger, CompanyManager!.CurrentCompanySettings?.Company.Country);
        var importService = new SpreadsheetImportService(ErrorLogger, TelemetryManager, geminiService);

        var analysisProgress = new Progress<(string detail, double percent)>(p =>
        {
            _mainWindowViewModel?.ShowLoading("Analyzing spreadsheet structure...".Translate(), p.detail, p.percent, analysisCts, ConfirmCancelAsync);
        });

        try
        {
            // Step 1: AI Analysis
            var analysis = isCsv
                ? await analysisService.AnalyzeCsvAsync(filePath, analysisCts.Token, analysisProgress)
                : await analysisService.AnalyzeAsync(filePath, analysisCts.Token, analysisProgress);

            await Task.Yield();
            _mainWindowViewModel?.HideLoading();

            if (analysis == null || analysis.Sheets.Count == 0)
            {
                await ShowErrorMessageBoxAsync(
                    "Analysis Failed".Translate(),
                    "Could not analyze the file structure. The file may be empty or in an unsupported format.".Translate());
                return;
            }

            // Step 2: Show mapping review dialog
            var mappingDialog = _appShellViewModel.ImportMappingDialogViewModel;
            var dialogResult = await mappingDialog.ShowAsync(analysis, usageCheck.Remaining, usageCheck.MonthlyLimit);

            if (dialogResult == ImportMappingDialogResult.Cancel)
                return;

            // Get user-updated analysis (they may have changed entity types or excluded sheets)
            var updatedAnalysis = mappingDialog.GetUpdatedAnalysis();
            if (updatedAnalysis == null) return;

            // Filter to only included sheets
            var includedSheets = updatedAnalysis.Sheets.Where(s => s.IsIncluded).ToList();
            if (includedSheets.Count == 0)
            {
                await ShowInfoMessageBoxAsync("Info".Translate(), "No sheets were selected for import.".Translate());
                return;
            }

            // Start timing after user approval — excludes UI wait time
            var importStopwatch = System.Diagnostics.Stopwatch.StartNew();

            // Create snapshot for undo
            var snapshot = CreateCompanyDataSnapshot(companyData);

            // Step 3: Split sheets by processing tier
            // Respect the AI's tier recommendation for both Excel and CSV files.
            // Mixed-type CSVs (e.g., expenses + payments in one file) need Tier 2 LLM processing.
            var tier1Sheets = includedSheets.Where(s => s.Tier == ProcessingTier.Tier1_Mapping).ToList();
            var tier2Sheets = includedSheets.Where(s => s.Tier == ProcessingTier.Tier2_LlmProcessing).ToList();

            var importOptions = new ImportOptions
            {
                SkipExistingRecords = mappingDialog.SkipExistingRecords
            };

            // Tier 1: Validate with mappings
            SpreadsheetImportResult? tier1Result = null;
            if (tier1Sheets.Count > 0)
            {
                using var validationCts = new CancellationTokenSource();
                _mainWindowViewModel?.ShowLoading("Validating mapped data...".Translate(), cts: validationCts, cancelConfirmation: ConfirmCancelAsync);

                var validationResult = isCsv
                    ? new ImportValidationResult() // CSV validation is simpler
                    : await importService.ValidateWithMappingsAsync(filePath, companyData, updatedAnalysis);

                _mainWindowViewModel?.HideLoading();

                if (validationResult.HasIssues)
                {
                    var validationDialog = _appShellViewModel.ImportValidationDialogViewModel;
                    var valResult = await validationDialog.ShowAsync(validationResult);

                    if (valResult == ImportValidationDialogResult.Cancel)
                        return;

                    if (valResult == ImportValidationDialogResult.CreateMissingAndImport)
                        importOptions.AutoCreateMissingReferences = true;

                    if (validationResult.Errors.Count > 0)
                        return;
                }

                // Import Tier 1 data
                var importCts = new CancellationTokenSource();
                _mainWindowViewModel?.ShowLoading("Importing data...".Translate(), cts: importCts, cancelConfirmation: ConfirmCancelAsync);

                var importProgress = new Progress<(string detail, double percent)>(p =>
                {
                    _mainWindowViewModel?.ShowLoading("Importing data...".Translate(), p.detail, p.percent, importCts, ConfirmCancelAsync);
                });

                tier1Result = isCsv
                    ? await importService.ImportCsvWithMappingsAsync(filePath, companyData, updatedAnalysis, importOptions, importCts.Token, importProgress)
                    : await importService.ImportWithMappingsAsync(filePath, companyData, updatedAnalysis, importOptions, importCts.Token, importProgress);

                // AI-categorize any products that ended up without a category
                // (skip if Tier 2 sheets will handle it after their processing)
                if (tier2Sheets.Count == 0)
                {
                    _mainWindowViewModel?.ShowLoading("Categorizing products...".Translate(), progress: 95, cts: importCts, cancelConfirmation: ConfirmCancelAsync);
                    await importService.AiCategorizeMissingProductsAsync(companyData, importCts.Token);
                }

                // Yield to let any pending Progress<T> callbacks (dispatched via
                // SynchronizationContext.Post) execute before hiding the loading
                // overlay, otherwise the last callback can re-show it after HideLoading.
                await Task.Yield();
                _mainWindowViewModel?.HideLoading();
            }

            // Tier 2: LLM row processing
            var totalImported = 0;
            var totalSkipped = 0;
            var allSheetResults = new List<SheetImportResult>();
            if (tier2Sheets.Count > 0)
            {
                var tier2Cts = new CancellationTokenSource();

                _mainWindowViewModel?.ShowLoading(
                    "AI processing...".Translate(),
                    $"Processing {tier2Sheets.Count} sheet(s)...",
                    progress: 0,
                    cts: tier2Cts,
                    cancelConfirmation: ConfirmCancelAsync);

                // Pre-read all Tier 2 sheet data from the file once to avoid
                // re-parsing the workbook for each sheet.
                var sheetDataMap = await SpreadsheetAnalysisService.ReadSheetDataAsync(
                    filePath, tier2Sheets, tier2Cts.Token);

                // Compute total rows across all Tier 2 sheets for aggregate progress
                var totalRowsAllSheets = sheetDataMap.Values.Sum(d => d.Rows.Count);
                var processedRowCounts = new int[tier2Sheets.Count];

                // Use a timer to show estimated progress while waiting for
                // the LLM to finish. Chunk-level progress only fires after
                // each chunk completes, so with few rows (< chunk size) the
                // bar would otherwise stay at 0% the entire time.
                var estimatedProgress = 0.0;
                var chunkProgressReceived = false;
                var estimateTimerCts = new CancellationTokenSource();

                var timerTask = Task.Run(async () =>
                {
                    using var estimateTimer = new PeriodicTimer(TimeSpan.FromMilliseconds(500));
                    while (await estimateTimer.WaitForNextTickAsync(estimateTimerCts.Token))
                    {
                        if (chunkProgressReceived) break;
                        estimatedProgress = Math.Min(estimatedProgress + 1, 90);
                        _mainWindowViewModel?.ShowLoading(
                            "AI processing...".Translate(),
                            "Processing rows...",
                            estimatedProgress,
                            tier2Cts,
                            ConfirmCancelAsync);
                    }
                }, tier2Cts.Token);

                // Phase A: Process all Tier 2 sheets in parallel (LLM calls are stateless)
                var sheetTasks = tier2Sheets.Select((sheet, idx) =>
                {
                    if (!sheetDataMap.TryGetValue(sheet.SourceSheetName, out var data))
                        return Task.FromResult(new List<LlmProcessedData>());

                    return analysisService.ProcessAllChunksAsync(
                        data.Headers, data.Rows, sheet,
                        new Progress<(int processed, int total)>(p =>
                        {
                            chunkProgressReceived = true;
                            Interlocked.Exchange(ref processedRowCounts[idx], p.processed);
                            var totalProcessed = processedRowCounts.Sum();
                            var pct = totalRowsAllSheets > 0
                                ? (double)totalProcessed / totalRowsAllSheets * 100
                                : -1;
                            _mainWindowViewModel?.ShowLoading(
                                "AI processing...".Translate(),
                                $"{totalProcessed}/{totalRowsAllSheets} rows",
                                pct,
                                tier2Cts,
                                ConfirmCancelAsync);
                        }),
                        tier2Cts.Token);
                }).ToArray();

                var allProcessedChunks = await Task.WhenAll(sheetTasks);

                // Stop the estimate timer and wait for it to exit cleanly
                estimateTimerCts.Cancel();
                try { await timerTask; } catch (OperationCanceledException) { }

                // Phase B: Import sequentially (CompanyData mutation is not thread-safe)
                for (int i = 0; i < tier2Sheets.Count; i++)
                {
                    var tier2Result = importService.ImportProcessedEntities(
                        companyData, allProcessedChunks[i], tier2Sheets[i].SourceSheetName, importOptions);
                    totalImported += tier2Result.Inserted;
                    totalSkipped += tier2Result.Skipped;
                    allSheetResults.Add(tier2Result);
                }

                // AI-categorize any products that ended up without a category (single call for all sheets)
                _mainWindowViewModel?.ShowLoading(
                    "AI processing...".Translate(),
                    "Categorizing products...",
                    95,
                    tier2Cts,
                    ConfirmCancelAsync);
                await importService.AiCategorizeMissingProductsAsync(companyData, tier2Cts.Token);

                // Yield to let any pending Progress<T> callbacks (dispatched via
                // SynchronizationContext.Post) execute before hiding the loading
                // overlay, otherwise the last callback can re-show it after HideLoading.
                await Task.Yield();
                _mainWindowViewModel?.HideLoading();
            }

            // Track import duration telemetry (covers analysis, validation, all tiers, and categorization)
            importStopwatch.Stop();
            var importContext = isCsv ? "ai-csv" : "ai-xlsx";
            if (tier2Sheets.Count > 0 && tier1Sheets.Count == 0)
                importContext = isCsv ? "ai-csv-tier2" : "ai-xlsx-tier2";
            else if (tier2Sheets.Count > 0)
                importContext = isCsv ? "ai-csv-mixed" : "ai-xlsx-mixed";
            _ = TelemetryManager?.TrackFeatureAsync(
                FeatureName.DataImported, importContext, importStopwatch.ElapsedMilliseconds);

            // Record usage on server
            await usageService.IncrementUsageAsync();

            // Create snapshot for redo
            var importedSnapshot = CreateCompanyDataSnapshot(companyData);

            // Record undo action
            UndoRedoManager.RecordAction(new DelegateAction(
                "AI import spreadsheet data".Translate(),
                () => { RestoreCompanyDataFromSnapshot(companyData, snapshot); CompanyManager.MarkAsChanged(); },
                () => { RestoreCompanyDataFromSnapshot(companyData, importedSnapshot); CompanyManager.MarkAsChanged(); }
            ));

            CompanyManager.MarkAsChanged();

            // Auto-switch date range to "All Time" so imported data is visible on dashboard/analytics
            // (imported data may be from any time period, not necessarily the current month)
            ChartSettingsService.Instance.SelectedDateRange = "All Time";

            // Combine Tier 1 and Tier 2 counts
            var totalUpdated = 0;
            if (tier1Result != null)
            {
                totalImported += tier1Result.TotalImported;
                totalUpdated += tier1Result.TotalUpdated;
                totalSkipped += tier1Result.TotalSkipped;
                allSheetResults.AddRange(tier1Result.SheetResults);
            }

            var totalProcessed = totalImported + totalUpdated;

            // Collect all warnings
            var allWarnings = (tier1Result?.Warnings ?? []).ToList();

            // Collect skip reasons from all sheets
            var allSkipReasons = allSheetResults
                .SelectMany(sr => sr.SkipReasons)
                .GroupBy(r => r)
                .Select(g => g.Count() > 1 ? $"{g.Key} (\u00d7{g.Count()})" : g.Key)
                .ToList();

            // Show import result dialog
            var resultDialog = _appShellViewModel.ImportResultDialogViewModel;
            await resultDialog.ShowAsync(
                Path.GetFileName(filePath),
                allSheetResults,
                totalImported, totalUpdated, totalSkipped,
                allSkipReasons, allWarnings, totalProcessed > 0);
        }
        catch (OperationCanceledException)
        {
            _mainWindowViewModel?.HideLoading();
        }
        catch (Exception ex)
        {
            _mainWindowViewModel?.HideLoading();
            ErrorLogger?.LogError(ex, ErrorCategory.Import, "Failed to perform AI import");
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
    /// <summary>
    /// Imports a bank statement via the smart importer (parse only, no commit) and shows it on the
    /// Bank Matching page. Triggered by the page's Import button.
    /// </summary>
    private static async Task PerformBankImportAsync()
    {
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;
        if (CompanyManager?.CompanyData is not { } companyData)
        {
            await ShowErrorMessageBoxAsync("Error".Translate(), "No company is currently open.".Translate());
            return;
        }

        var file = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import Bank Statement".Translate(),
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Spreadsheets") { Patterns = ["*.xlsx", "*.csv"] }
            ]
        });
        if (file.Count == 0) return;

        var filePath = file[0].Path.LocalPath;
        var isCsv = filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase);

        _mainWindowViewModel?.ShowLoading("Scanning bank statement...".Translate());
        await Task.Yield();

        try
        {
            var parser = new BankStatementImportService(ErrorLogger);

            // Try local column detection first (instant, no AI). Fall back to AI column mapping
            // only when the headers aren't recognized locally.
            var lines = isCsv
                ? await parser.ParseCsvAsync(filePath)
                : await parser.ParseExcelAsync(filePath);

            if (lines.Count == 0)
                lines = await TryAiParseBankStatementAsync(filePath, isCsv, parser);

            _mainWindowViewModel?.HideLoading();

            if (lines.Count == 0)
            {
                await ShowInfoMessageBoxAsync("Info".Translate(),
                    "No transactions were found. Make sure the file has Date, Description and Amount (or Debit/Credit) columns.".Translate());
                return;
            }

            // Snapshot before mutation so the import can be undone in one step.
            var snapshot = CreateCompanyDataSnapshot(companyData);

            var session = new ArgoBooks.Core.Models.BankMatching.BankImportSession
            {
                Id = Guid.NewGuid().ToString("N"),
                ImportedAt = DateTime.UtcNow,
                SourceFileName = Path.GetFileName(filePath),
                Lines = lines
            };
            companyData.BankImportSessions.Add(session);
            companyData.MarkAsModified();

            var importedSnapshot = CreateCompanyDataSnapshot(companyData);
            UndoRedoManager.RecordAction(new DelegateAction(
                "Import bank statement".Translate(),
                () => { RestoreCompanyDataFromSnapshot(companyData, snapshot); CompanyManager.MarkAsChanged(); _bankMatchingPageViewModel?.Reload(); },
                () => { RestoreCompanyDataFromSnapshot(companyData, importedSnapshot); CompanyManager.MarkAsChanged(); _bankMatchingPageViewModel?.Reload(); }
            ));

            CompanyManager.MarkAsChanged();

            // Show all dates so just-imported lines are visible regardless of their statement period.
            ChartSettingsService.Instance.SelectedDateRange = "All Time";

            _bankMatchingPageViewModel?.Reload();
            NavigationService?.NavigateTo(PageNames.BankMatching);
        }
        catch (OperationCanceledException)
        {
            _mainWindowViewModel?.HideLoading();
        }
        catch (Exception ex)
        {
            _mainWindowViewModel?.HideLoading();
            ErrorLogger?.LogError(ex, ErrorCategory.Import, "Bank statement import failed");
            await ShowErrorMessageBoxAsync("Import Failed".Translate(), "Failed to import bank statement:\n\n{0}".TranslateFormat(ex.Message));
        }
    }

    /// <summary>
    /// Backup parser: uses the smart importer's AI column mapping when local header detection
    /// couldn't recognize the statement's columns. Consumes one AI import credit on success.
    /// Returns an empty list if AI isn't available or finds nothing.
    /// </summary>
    private static async Task<List<ArgoBooks.Core.Models.BankMatching.BankStatementLine>> TryAiParseBankStatementAsync(
        string filePath, bool isCsv, BankStatementImportService parser)
    {
        var gemini = new GeminiService(ErrorLogger, TelemetryManager);
        if (!gemini.IsConfigured) return [];

        using var usage = new AiImportUsageService(LicenseService, ErrorLogger);
        var usageCheck = await usage.CheckUsageAsync();
        if (!usageCheck.CanImport) return [];

        var analysisService = new SpreadsheetAnalysisService(gemini, ErrorLogger, CompanyManager?.CurrentCompanySettings?.Company.Country);
        var analysis = isCsv
            ? await analysisService.AnalyzeCsvAsync(filePath)
            : await analysisService.AnalyzeAsync(filePath);

        var sheet = analysis?.Sheets.FirstOrDefault();
        if (sheet == null) return [];

        var lines = isCsv
            ? await parser.ParseCsvWithAnalysisAsync(filePath, sheet)
            : await parser.ParseExcelWithAnalysisAsync(filePath, sheet);

        if (lines.Count > 0)
            await usage.IncrementUsageAsync();

        return lines;
    }

    /// <summary>
    /// Runs AI matching over the lines still unmatched on the Bank Matching page. Suggestions are
    /// never auto-applied. Triggered by the page's "Run AI suggestions" button.
    /// </summary>
    private static async Task PerformBankAiSuggestionsAsync()
    {
        if (_bankMatchingPageViewModel is not { } vm) return;
        if (CompanyManager?.CompanyData is not { } companyData) return;

        var unmatched = vm.GetUnmatchedLines();
        if (unmatched.Count == 0) return;

        var geminiService = new GeminiService(ErrorLogger, TelemetryManager);
        if (!geminiService.IsConfigured)
        {
            await ShowErrorMessageBoxAsync("Not Available".Translate(),
                "Match suggestions require portal access. Please register your company first.".Translate());
            return;
        }

        // AI suggestions consume the same monthly AI quota as imports.
        using var usageService = new AiImportUsageService(LicenseService, ErrorLogger);
        var usageCheck = await usageService.CheckUsageAsync();
        if (!usageCheck.CanImport)
        {
            await UpgradePromptHelper.ShowAiImportLimitPromptAsync(usageCheck.ImportCount, usageCheck.MonthlyLimit, usageCheck.ResetsAt);
            return;
        }

        vm.IsAiBusy = true;
        _mainWindowViewModel?.ShowLoading("Looking for matches...".Translate());
        try
        {
            var matcher = new BankMatchingService(geminiService, ErrorLogger);
            var suggestions = await matcher.SuggestWithAiAsync(unmatched, companyData, vm.Options);
            await usageService.IncrementUsageAsync();
            var applied = vm.ApplyAiSuggestions(suggestions);
            _mainWindowViewModel?.HideLoading();

            await ShowInfoMessageBoxAsync(
                "Bank Matching".Translate(),
                applied > 0
                    ? "Found {0} more possible match(es). Review and accept the suggestions.".TranslateFormat(applied)
                    : "No additional matches were found for the remaining lines.".Translate());
        }
        catch (Exception ex)
        {
            _mainWindowViewModel?.HideLoading();
            ErrorLogger?.LogError(ex, ErrorCategory.Import, "Bank match suggestions failed");
        }
        finally
        {
            vm.IsAiBusy = false;
        }
    }

    private static string CreateCompanyDataSnapshot(CompanyData data)
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
            data.EventLog,
            data.BankImportSessions
        };
        return System.Text.Json.JsonSerializer.Serialize(snapshot);
    }

    /// <summary>
    /// Restores company data collections from a JSON snapshot.
    /// </summary>
    private static void RestoreCompanyDataFromSnapshot(CompanyData data, string snapshotJson)
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
            var restoredCounters = System.Text.Json.JsonSerializer.Deserialize<IdCounters>(counters.GetRawText(), options);
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
        RestoreList(data.BankImportSessions, "BankImportSessions");
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

        _isOpeningCompany = true;
        _mainWindowViewModel.ShowLoading("Opening company...".Translate());

        try
        {
            var success = await CompanyManager.OpenCompanyAsync(filePath);
            _isOpeningCompany = false;
            if (success)
            {
                // Close the password modal if it was open
                passwordModal.Close();

                // Time-shift sample company data if needed
                if (CompanyManager.IsSampleCompany && CompanyManager.CompanyData != null)
                {
                    if (SampleCompanyService.TimeShiftSampleData(CompanyManager.CompanyData))
                    {
                        CompanyManager.NotifyDataChanged();
                        _suppressSavedFeedback = true;
                        await CompanyManager.SaveCompanyAsync();
                    }
                    CompanyManager.CompanyData.MarkAsSaved();
                    _mainWindowViewModel.HasUnsavedChanges = false;
                    _appShellViewModel.HeaderViewModel.HasUnsavedChanges = false;
                    ChartSettingsService.Instance.SelectedDateRange = "Last 365 Days";
                }

                await LoadRecentCompaniesAsync();
            }
            else
            {
                // User cancelled password prompt
                _isOpeningCompany = false;
                _mainWindowViewModel.HideLoading();
            }
        }
        catch (UnauthorizedAccessException)
        {
            // Wrong password - show error and retry
            _isOpeningCompany = false;
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

            // Close the modal and retry with the new password
            passwordModal.Close();
            await OpenCompanyWithPasswordRetryAsync(filePath, newPassword);
        }
        catch (FileNotFoundException)
        {
            _isOpeningCompany = false;
            _mainWindowViewModel.HideLoading();
            passwordModal.Close();
            if (ConfirmationDialog != null)
            {
                await ConfirmationDialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Company File Not Found".Translate(),
                    Message = "The company file no longer exists.".Translate(),
                    PrimaryButtonText = "OK".Translate(),
                    SecondaryButtonText = null,
                    CancelButtonText = null
                });
            }
            SettingsService?.RemoveRecentCompany(filePath);
            await LoadRecentCompaniesAsync();
        }
        catch (CompanyFileTooNewException ex)
        {
            // File was saved by a newer Argo Books build. Use ConfirmationDialog (same path as
            // the FileNotFoundException case) rather than the message-box service, because the
            // latter races with the loading overlay and the dialog ends up queued behind the
            // next user action.
            _isOpeningCompany = false;
            _mainWindowViewModel.HideLoading();
            passwordModal.Close();
            ErrorLogger?.LogError(ex, ErrorCategory.FileSystem, "Cannot open company file: newer than running app");
            if (ConfirmationDialog != null)
            {
                await ConfirmationDialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Update Argo Books".Translate(),
                    Message = "This company file was created by Argo Books {0}. You are running Argo Books {1}. Please update to Argo Books {0} or later to open it.".TranslateFormat(ex.FileVersion, ex.AppVersion),
                    PrimaryButtonText = "OK".Translate(),
                    SecondaryButtonText = null,
                    CancelButtonText = null
                });
            }
        }
        catch (Exception ex)
        {
            _isOpeningCompany = false;
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

            // Close the modal and retry recursively
            passwordModal.Close();
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
            // Suppress the default CompanySaved feedback — we show our own with forceSaved
            _suppressSavedFeedback = true;
            await CompanyManager!.SaveCompanyAsAsync(filePath);

            // Refresh UI with the (possibly updated) company name
            var newName = CompanyManager.CurrentCompanyName ?? "Company";
            RefreshCompanyUi(newName);

            _appShellViewModel!.HeaderViewModel.ShowSavedFeedback(forceSaved: true);

            await LoadRecentCompaniesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _suppressSavedFeedback = false;
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
        if (Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
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
        foreach (var watcher in RecentCompanyWatchers.Values)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Dispose();
        }
        RecentCompanyWatchers.Clear();

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

                RecentCompanyWatchers[directory] = watcher;
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
        // FileService.SaveCompanyAsync writes to <path>.tmp then File.Move(tmp, path,
        // overwrite: true). On Windows that overwrite-move emits a Deleted event for
        // the destination even though the rename itself is atomic — by the time we
        // observe it, the file is already back. Treating it as a real deletion would
        // strip the entry from settings.json on every save. Skip if the file exists.
        if (File.Exists(e.FullPath))
            return;

        // Run on UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Re-check on the UI thread in case the file came back during the post hop.
            if (File.Exists(e.FullPath))
                return;
            RemoveRecentCompanyFromUi(e.FullPath);
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
            RemoveRecentCompanyFromUi(e.OldFullPath);
        });
    }

    /// <summary>
    /// Removes a company from all recent company UI lists and persists the change.
    /// </summary>
    private static void RemoveRecentCompanyFromUi(string filePath)
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
    private static void RegisterPages(NavigationService navigationService)
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
                        _mainWindowViewModel?.ShowLoading(
                            "Exporting to Google Sheets...".Translate(),
                            cts: args.CancellationTokenSource);
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
                    else
                    {
                        // Cancelled or no error message
                        _mainWindowViewModel?.HideLoading();
                    }
                };
            }
            _dashboardPageViewModel.HasPremium = _appShellViewModel?.SidebarViewModel.HasPremium ?? false;
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
        navigationService.RegisterPage("Insights", _ =>
        {
            _insightsPageViewModel ??= new InsightsPageViewModel();
            _insightsPageViewModel.HasPremium = _appShellViewModel?.SidebarViewModel.HasPremium ?? false;
            return new InsightsPage { DataContext = _insightsPageViewModel };
        });
        navigationService.RegisterPage("Reports", _ => new ReportsPage { DataContext = _reportsPageViewModel ??= new ReportsPageViewModel() });

        // Transactions Section
        navigationService.RegisterPage("Revenue", param =>
        {
            _revenuePageViewModel ??= new RevenuePageViewModel();
            _revenuePageViewModel.HasPremium = _appShellViewModel?.SidebarViewModel.HasPremium ?? false;
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
            _expensesPageViewModel.HasPremium = _appShellViewModel?.SidebarViewModel.HasPremium ?? false;
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
            if (_invoicesPageViewModel == null)
            {
                _invoicesPageViewModel = new InvoicesPageViewModel();
                _invoicesPageViewModel.UpgradeRequested += (_, _) => _appShellViewModel!.UpgradeModalViewModel.OpenCommand.Execute(null);
            }
            _invoicesPageViewModel.HasPremium = _appShellViewModel!.SidebarViewModel.HasPremium;
            _invoicesPageViewModel.HighlightTransactionId = null;
            if (param is RentalInvoiceNavigationParameter rentalParam)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    InvoiceModalsViewModel?.OpenCreateFromRental(rentalParam.RentalRecordId);
                });
            }
            else if (param is TransactionNavigationParameter navParam)
            {
                _invoicesPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _invoicesPageViewModel.ApplyHighlight();
            }
            return new InvoicesPage { DataContext = _invoicesPageViewModel };
        });
        navigationService.RegisterPage("Payments", param =>
        {
            _paymentsPageViewModel ??= new PaymentsPageViewModel();
            _paymentsPageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _paymentsPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _paymentsPageViewModel.ApplyHighlight();
            }
            _ = AutoSyncPortalPaymentsAsync();
            return new PaymentsPage { DataContext = _paymentsPageViewModel };
        });

        // Inventory Section
        navigationService.RegisterPage("Products", param =>
        {
            if (_productsPageViewModel == null)
            {
                _productsPageViewModel = new ProductsPageViewModel();
                // Wire up upgrade request to open upgrade modal (only once)
                _productsPageViewModel.UpgradeRequested += (_, _) => _appShellViewModel!.UpgradeModalViewModel.OpenCommand.Execute(null);
            }
            // Update plan status each time (may have changed)
            _productsPageViewModel.HasPremium = _appShellViewModel!.SidebarViewModel.HasPremium;
            // Reset modal state
            _productsPageViewModel.IsAddModalOpen = false;
            _productsPageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _productsPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _productsPageViewModel.ApplyHighlight();
            }
            else if (param is Dictionary<string, object?> dict)
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
        navigationService.RegisterPage("StockLevels", param =>
        {
            _stockLevelsPageViewModel ??= new StockLevelsPageViewModel();
            _stockLevelsPageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _stockLevelsPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _stockLevelsPageViewModel.ApplyHighlight();
            }
            return new StockLevelsPage { DataContext = _stockLevelsPageViewModel };
        });
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
        navigationService.RegisterPage("BankMatching", _ =>
        {
            if (_bankMatchingPageViewModel == null)
            {
                _bankMatchingPageViewModel = new BankMatchingPageViewModel();
                _bankMatchingPageViewModel.ImportRequested += async (_, _) => await PerformBankImportAsync();
                _bankMatchingPageViewModel.AiSuggestionsRequested += async (_, _) => await PerformBankAiSuggestionsAsync();
            }
            return new BankMatchingPage { DataContext = _bankMatchingPageViewModel };
        });
        navigationService.RegisterPage("PurchaseOrders", param =>
        {
            _purchaseOrdersPageViewModel ??= new PurchaseOrdersPageViewModel();
            _purchaseOrdersPageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _purchaseOrdersPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _purchaseOrdersPageViewModel.ApplyHighlight();
            }
            return new PurchaseOrdersPage { DataContext = _purchaseOrdersPageViewModel };
        });
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
        navigationService.RegisterPage("Customers", param =>
        {
            _customersPageViewModel ??= new CustomersPageViewModel();
            _customersPageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _customersPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _customersPageViewModel.ApplyHighlight();
            }
            return new CustomersPage { DataContext = _customersPageViewModel };
        });
        navigationService.RegisterPage("Suppliers", param =>
        {
            _suppliersPageViewModel ??= new SuppliersPageViewModel();
            _suppliersPageViewModel.HighlightTransactionId = null;
            if (param is TransactionNavigationParameter navParam)
            {
                _suppliersPageViewModel.HighlightTransactionId = navParam.TransactionId;
                _suppliersPageViewModel.ApplyHighlight();
            }
            return new SuppliersPage { DataContext = _suppliersPageViewModel };
        });
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

}

/// <summary>
/// Event arguments for plan status changes.
/// </summary>
public class PlanStatusChangedEventArgs(bool hasPremium) : EventArgs
{
    public bool HasPremium { get; } = hasPremium;
}
