using System.Reflection;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ArgoBooks.Core.Models;
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
    /// Gets the license service instance for secure license storage.
    /// </summary>
    public static LicenseService? LicenseService { get; private set; }

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
    /// Adds a notification to the notification panel.
    /// </summary>
    /// <param name="title">The notification title.</param>
    /// <param name="message">The notification message.</param>
    /// <param name="type">The notification type.</param>
    public static void AddNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        _appShellViewModel?.AddNotification(title, message, type);
    }

    #region Plan Status Events

    /// <summary>
    /// Event raised when the plan status changes (e.g., user upgrades).
    /// </summary>
    public static event EventHandler<PlanStatusChangedEventArgs>? PlanStatusChanged;

    /// <summary>
    /// Raises the PlanStatusChanged event.
    /// </summary>
    public static void RaisePlanStatusChanged(bool hasStandard, bool hasPremium)
    {
        PlanStatusChanged?.Invoke(null, new PlanStatusChangedEventArgs(hasStandard, hasPremium));
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

    /// <summary>
    /// Gets the confirmation dialog ViewModel for showing confirmation dialogs from anywhere.
    /// </summary>
    public static ConfirmationDialogViewModel? ConfirmationDialog { get; private set; }

    /// <summary>
    /// Gets the unsaved changes dialog ViewModel for showing save prompts with change lists.
    /// </summary>
    public static UnsavedChangesDialogViewModel? UnsavedChangesDialog { get; private set; }

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

            // Initialize services synchronously
            var compressionService = new CompressionService();
            var footerService = new FooterService();
            var encryptionService = new EncryptionService();
            var fileService = new FileService(compressionService, footerService, encryptionService);
            SettingsService = new GlobalSettingsService();
            LicenseService = new LicenseService(encryptionService, SettingsService);
            CompanyManager = new CompanyManager(fileService, SettingsService, footerService);

            // Create navigation service
            NavigationService = new NavigationService();

            _mainWindowViewModel = new MainWindowViewModel();
            ConfirmationDialog = new ConfirmationDialogViewModel();
            UnsavedChangesDialog = new UnsavedChangesDialogViewModel();
            ChangeTrackingService = new ChangeTrackingService();
            _idleDetectionService = new IdleDetectionService();

            // Create app shell with navigation service
            _appShellViewModel = new AppShellViewModel(NavigationService);

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
                    try
                    {
                        await CompanyManager.SaveCompanyAsync();
                    }
                    catch (Exception ex)
                    {
                        _appShellViewModel.AddNotification("Error".Translate(), "Failed to save: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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

            // Final reset of unsaved changes before window is shown - ensures clean startup state
            _mainWindowViewModel.HasUnsavedChanges = false;
            _appShellViewModel.HeaderViewModel.HasUnsavedChanges = false;

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

            // Load global settings
            if (SettingsService != null)
            {
                await SettingsService.LoadGlobalSettingsAsync();

                // Initialize theme service with settings
                ThemeService.Instance.SetGlobalSettingsService(SettingsService);
                ThemeService.Instance.Initialize();
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
                var (hasStandard, hasPremium) = LicenseService.LoadLicense();
                if (hasStandard || hasPremium)
                {
                    _appShellViewModel.SetPlanStatus(hasStandard, hasPremium);
                }
            }

            // Load and display recent companies
            await LoadRecentCompaniesAsync();
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Error during async initialization: {ex.Message}");
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
                System.Diagnostics.Debug.WriteLine("Exchange rate service initialized without API key - currency conversion will use cached rates only");
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to initialize exchange rate service: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"Failed to register file type associations: {ex.Message}");
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

            System.Diagnostics.Debug.WriteLine("Could not find icon resource");
            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to extract icon: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Wires up CompanyManager events to update UI.
    /// </summary>
    private static void WireCompanyManagerEvents()
    {
        if (CompanyManager == null || _mainWindowViewModel == null || _appShellViewModel == null)
            return;

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

            // Clear undo/redo history when company is closed
            UndoRedoManager.Clear();

            // Clear tracked changes when company is closed
            ChangeTrackingService?.ClearAllChanges();

            // Reset to global language setting when company is closed
            var globalLanguage = SettingsService?.GlobalSettings.Ui.Language ?? "English";
            await LanguageService.Instance.SetLanguageAsync(globalLanguage);

            // Navigate back to Welcome screen when company is closed
            NavigationService?.NavigateTo("Welcome");
        };

        CompanyManager.CompanySaved += (_, _) =>
        {
            _mainWindowViewModel.HideLoading();
            // Call ShowSavedFeedback FIRST - it checks HasUnsavedChanges before clearing it
            _appShellViewModel.HeaderViewModel.ShowSavedFeedback();
            // Then clear the main window's flag (ShowSavedFeedback handles the header's flag)
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
                try
                {
                    await CompanyManager.SaveCompanyAsync();
                }
                catch (Exception ex)
                {
                    _appShellViewModel.AddNotification("Error".Translate(), "Failed to save: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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
                            await CompanyManager.SaveCompanyAsync();
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
                    Email = args.Email,
                    Phone = args.PhoneNumber,
                    Address = BuildAddress(args)
                };

                await CompanyManager!.CreateCompanyAsync(
                    filePath,
                    args.CompanyName,
                    args.Password,
                    companyInfo);

                await LoadRecentCompaniesAsync();
            }
            catch (Exception ex)
            {
                _mainWindowViewModel?.HideLoading();
                _appShellViewModel.AddNotification("Error".Translate(), "Failed to create company: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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
                catch
                {
                    // Invalid image
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
            if (CompanyManager?.IsCompanyOpen != true) return;

            var settings = CompanyManager.CurrentCompanySettings;
            var logoPath = CompanyManager.CurrentCompanyLogoPath;
            var logo = LoadBitmapFromPath(logoPath);
            _appShellViewModel.EditCompanyModalViewModel.Open(
                settings?.Company.Name ?? "",
                settings?.Company.BusinessType,
                settings?.Company.Industry,
                logo);
        };

        // Wire up edit company modal events
        WireEditCompanyEvents(desktop);
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
                    var oldName = settings.Company.Name;
                    var nameChanged = oldName != args.CompanyName;

                    settings.Company.Name = args.CompanyName;
                    settings.Company.BusinessType = args.BusinessType;
                    settings.Company.Industry = args.Industry;

                    // Handle logo update if a new one was uploaded
                    if (!string.IsNullOrEmpty(args.LogoPath))
                    {
                        await CompanyManager.SetCompanyLogoAsync(args.LogoPath);
                    }
                    else if (args.LogoSource == null && CompanyManager.CurrentCompanyLogoPath != null)
                    {
                        // Logo was removed
                        await CompanyManager.RemoveCompanyLogoAsync();
                    }

                    // Mark settings as changed
                    settings.ChangesMade = true;

                    // If company name changed, save and rename file
                    if (nameChanged && CompanyManager.CurrentFilePath != null)
                    {
                        // Save first to persist the new name in the file
                        await CompanyManager.SaveCompanyAsync();

                        // Rename the file
                        var currentPath = CompanyManager.CurrentFilePath;
                        var directory = Path.GetDirectoryName(currentPath);
                        var newFileName = args.CompanyName + ".argo";
                        var newPath = Path.Combine(directory!, newFileName);

                        if (currentPath != newPath && !File.Exists(newPath))
                        {
                            File.Move(currentPath, newPath);
                            CompanyManager.UpdateFilePath(newPath);
                        }

                        _appShellViewModel.HeaderViewModel.ShowSavedFeedback();
                    }

                    // Update UI
                    _mainWindowViewModel?.OpenCompany(args.CompanyName);
                    var logo = LoadBitmapFromPath(CompanyManager.CurrentCompanyLogoPath);
                    _appShellViewModel.SetCompanyInfo(args.CompanyName, logo);
                    _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany(
                        args.CompanyName,
                        CompanyManager.CurrentFilePath,
                        logo);
                }

                _appShellViewModel.AddNotification("Updated".Translate(), "Company information updated.".Translate(), NotificationType.Success);
            }
            catch (Exception ex)
            {
                _appShellViewModel.AddNotification("Error".Translate(), "Failed to update company: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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
                catch
                {
                    // Invalid image
                }
            }
        };
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
                // Mark as having changes so SavedFeedback shows "Saved" not "No changes found"
                _appShellViewModel.HeaderViewModel.HasUnsavedChanges = true;
                await CompanyManager.ChangePasswordAsync(args.NewPassword);
                _appShellViewModel.AddNotification("Success".Translate(), "Password has been set.".Translate(), NotificationType.Success);
            }
            catch (Exception ex)
            {
                settings.HasPassword = false;
                _appShellViewModel.AddNotification("Error".Translate(), "Failed to set password: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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
                // Mark as having changes so SavedFeedback shows "Saved" not "No changes found"
                _appShellViewModel.HeaderViewModel.HasUnsavedChanges = true;
                await CompanyManager.ChangePasswordAsync(args.NewPassword);
                settings.OnPasswordChanged();
                _appShellViewModel.AddNotification("Success".Translate(), "Password has been changed.".Translate(), NotificationType.Success);
            }
            catch (Exception ex)
            {
                settings.OnPasswordVerificationFailed();
                _appShellViewModel.AddNotification("Error".Translate(), "Failed to change password: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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
                // Mark as having changes so SavedFeedback shows "Saved" not "No changes found"
                _appShellViewModel.HeaderViewModel.HasUnsavedChanges = true;
                await CompanyManager.ChangePasswordAsync(null);
                settings.OnPasswordRemoved();
                _appShellViewModel.AddNotification("Success".Translate(), "Password has been removed.".Translate(), NotificationType.Success);
            }
            catch (Exception ex)
            {
                settings.OnPasswordVerificationFailed();
                _appShellViewModel.AddNotification("Error".Translate(), "Failed to remove password: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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
                // Backup export - not implemented yet
                _appShellViewModel.AddNotification("Info".Translate(), "Backup export will be available in a future update.".Translate());
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
                _appShellViewModel.AddNotification("Error".Translate(), "No company is currently open.".Translate(), NotificationType.Error);
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

                _mainWindowViewModel?.HideLoading();

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
                catch
                {
                    // Ignore errors opening folder
                }
            }
            catch (Exception ex)
            {
                _mainWindowViewModel?.HideLoading();
                _appShellViewModel.AddNotification("Export Failed".Translate(), "Failed to export data: {0}".TranslateFormat(ex.Message), NotificationType.Error);
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
                _appShellViewModel.AddNotification("Error".Translate(), "No company is currently open.".Translate(), NotificationType.Error);
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
                var importService = new SpreadsheetImportService();

                // First validate the import file
                var validationResult = await importService.ValidateImportAsync(filePath, companyData);

                _mainWindowViewModel?.HideLoading();

                // Check for critical errors - show in modal
                if (validationResult.Errors.Count > 0)
                {
                    var errorDialog = ConfirmationDialog;
                    if (errorDialog != null)
                    {
                        await errorDialog.ShowAsync(new ConfirmationDialogOptions
                        {
                            Title = "Import Failed".Translate(),
                            Message = "Import validation failed:\n\n{0}".TranslateFormat(string.Join("\n", validationResult.Errors)),
                            PrimaryButtonText = "OK".Translate(),
                            CancelButtonText = ""
                        });
                    }
                    return;
                }

                // Check for missing references and ask user
                var importOptions = new ImportOptions();

                if (validationResult.HasMissingReferences)
                {
                    var missingCount = validationResult.TotalMissingReferences;
                    var missingSummary = validationResult.GetMissingReferencesSummary();

                    var dialog = ConfirmationDialog;
                    if (dialog != null)
                    {
                        var result = await dialog.ShowAsync(new ConfirmationDialogOptions
                        {
                            Title = "Missing References Found".Translate(),
                            Message = "The import file references {0} item(s) that don't exist:\n\n{1}\n\nWould you like to create placeholder entries for these missing items?".TranslateFormat(missingCount, missingSummary),
                            PrimaryButtonText = "Create & Import".Translate(),
                            CancelButtonText = "Cancel".Translate()
                        });

                        if (result != ConfirmationResult.Primary)
                        {
                            return;
                        }

                        importOptions.AutoCreateMissingReferences = true;
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

                _appShellViewModel.AddNotification("Import Complete".Translate(), successMessage, NotificationType.Success);
            }
            catch (Exception ex)
            {
                _mainWindowViewModel?.HideLoading();
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
            data.Sales,
            data.Purchases,
            data.Invoices,
            data.Payments,
            data.RecurringInvoices,
            data.Inventory,
            data.StockAdjustments,
            data.PurchaseOrders,
            data.RentalInventory,
            data.Rentals
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
                data.IdCounters.Sale = restoredCounters.Sale;
                data.IdCounters.Purchase = restoredCounters.Purchase;
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
        RestoreList(data.Sales, "Sales");
        RestoreList(data.Purchases, "Purchases");
        RestoreList(data.Invoices, "Invoices");
        RestoreList(data.Payments, "Payments");
        RestoreList(data.RecurringInvoices, "RecurringInvoices");
        RestoreList(data.Inventory, "Inventory");
        RestoreList(data.StockAdjustments, "StockAdjustments");
        RestoreList(data.PurchaseOrders, "PurchaseOrders");
        RestoreList(data.RentalInventory, "RentalInventory");
        RestoreList(data.Rentals, "Rentals");
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

                // Check for unsaved changes
                if (CompanyManager.HasUnsavedChanges)
                {
                    // Auto-save before locking
                    try
                    {
                        _mainWindowViewModel?.ShowLoading("Auto-saving before lock...".Translate());
                        await CompanyManager.SaveCompanyAsync();
                        _mainWindowViewModel?.HideLoading();
                    }
                    catch
                    {
                        _mainWindowViewModel?.HideLoading();
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
    private static async Task<bool> OpenCompanyWithRetryAsync(string filePath)
    {
        if (CompanyManager == null || _mainWindowViewModel == null || _appShellViewModel == null)
            return false;

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
                return true;
            }
            else
            {
                // User cancelled password prompt
                _mainWindowViewModel.HideLoading();
                return false;
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
                return false;
            }

            // Retry with the new password
            return await OpenCompanyWithPasswordRetryAsync(filePath, newPassword);
        }
        catch (FileNotFoundException)
        {
            _mainWindowViewModel.HideLoading();
            passwordModal.Close();
            _appShellViewModel.AddNotification("File Not Found".Translate(), "The company file no longer exists.".Translate(), NotificationType.Error);
            SettingsService?.RemoveRecentCompany(filePath);
            await LoadRecentCompaniesAsync();
            return false;
        }
        catch (Exception ex)
        {
            _mainWindowViewModel.HideLoading();
            passwordModal.Close();
            _appShellViewModel.AddNotification("Error".Translate(), "Failed to open file: {0}".TranslateFormat(ex.Message), NotificationType.Error);
            return false;
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
            _appShellViewModel.AddNotification("Error".Translate(), "Failed to open file: {0}".TranslateFormat(ex.Message), NotificationType.Error);
            return false;
        }
    }

    /// <summary>
    /// Opens the save dialog for Save As.
    /// </summary>
    private static async Task SaveCompanyAsDialogAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var file = await ShowSaveFileDialogAsync(desktop, CompanyManager?.CurrentCompanyName ?? "Company");
        if (file == null) return;

        var filePath = file.Path.LocalPath;

        try
        {
            await CompanyManager!.SaveCompanyAsAsync(filePath);
            await LoadRecentCompaniesAsync();
        }
        catch (Exception ex)
        {
            _appShellViewModel?.AddNotification("Error".Translate(), "Failed to save file: {0}".TranslateFormat(ex.Message), NotificationType.Error);
        }
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
                foreach (var company in recentCompanies.Take(10))
                {
                    _welcomeScreenViewModel.RecentCompanies.Add(new RecentCompanyItem
                    {
                        Name = company.CompanyName,
                        FilePath = company.FilePath,
                        LastOpened = company.ModifiedAt,
                        Icon = company.IsEncrypted ? "Lock" : "Building"
                    });
                }
                _welcomeScreenViewModel.HasRecentCompanies = _welcomeScreenViewModel.RecentCompanies.Count > 0;
            }
        }
        catch
        {
            // Ignore errors loading recent companies
        }
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
    /// Builds an address string from the create company args.
    /// </summary>
    private static string BuildAddress(CompanyCreatedEventArgs args)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(args.StreetAddress))
            parts.Add(args.StreetAddress);
        if (!string.IsNullOrWhiteSpace(args.City))
            parts.Add(args.City);
        if (!string.IsNullOrWhiteSpace(args.StateProvince))
            parts.Add(args.StateProvince);
        if (!string.IsNullOrWhiteSpace(args.PostalCode))
            parts.Add(args.PostalCode);
        if (!string.IsNullOrWhiteSpace(args.Country))
            parts.Add(args.Country);
        return string.Join(", ", parts);
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
            var viewModel = new DashboardPageViewModel();
            if (CompanyManager?.IsCompanyOpen == true)
            {
                viewModel.Initialize(CompanyManager);
            }

            // Wire up Google Sheets export notifications
            viewModel.GoogleSheetsExportStatusChanged += (_, args) =>
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
                    appShellViewModel.AddNotification("Export Failed".Translate(), args.ErrorMessage, NotificationType.Error);
                }
            };

            return new DashboardPage { DataContext = viewModel };
        });
        navigationService.RegisterPage("Analytics", _ =>
        {
            var viewModel = new AnalyticsPageViewModel();
            if (CompanyManager?.IsCompanyOpen == true)
            {
                viewModel.Initialize(CompanyManager);
            }
            return new AnalyticsPage { DataContext = viewModel };
        });
        navigationService.RegisterPage("Insights", _ => new InsightsPage { DataContext = new InsightsPageViewModel() });
        navigationService.RegisterPage("Reports", _ => new ReportsPage { DataContext = new ReportsPageViewModel() });

        // Transactions Section
        navigationService.RegisterPage("Revenue", param =>
        {
            var viewModel = new RevenuePageViewModel();
            if (param is TransactionNavigationParameter navParam)
            {
                viewModel.HighlightTransactionId = navParam.TransactionId;
                viewModel.ApplyHighlight();
            }
            return new RevenuePage { DataContext = viewModel };
        });
        navigationService.RegisterPage("Expenses", param =>
        {
            var viewModel = new ExpensesPageViewModel();
            if (param is TransactionNavigationParameter navParam)
            {
                viewModel.HighlightTransactionId = navParam.TransactionId;
                viewModel.ApplyHighlight();
            }
            return new ExpensesPage { DataContext = viewModel };
        });
        navigationService.RegisterPage("Invoices", _ => new InvoicesPage { DataContext = new InvoicesPageViewModel() });
        navigationService.RegisterPage("Payments", _ => new PaymentsPage { DataContext = new PaymentsPageViewModel() });

        // Inventory Section
        navigationService.RegisterPage("Products", param =>
        {
            var viewModel = new ProductsPageViewModel
            {
                // Set plan status from app shell
                HasStandard = _appShellViewModel?.SidebarViewModel.HasStandard ?? false
            };
            // Wire up upgrade request to open upgrade modal
            viewModel.UpgradeRequested += (_, _) => _appShellViewModel?.UpgradeModalViewModel.OpenCommand.Execute(null);
            if (param is Dictionary<string, object?> dict)
            {
                // Check if we should select a specific tab (0 = Expenses, 1 = Revenue)
                if (dict.TryGetValue("selectedTabIndex", out var tabIndex) && tabIndex is int index)
                {
                    viewModel.SelectedTabIndex = index;
                }
                // Check if we should open the add modal
                if (dict.TryGetValue("openAddModal", out var openAdd) && openAdd is true)
                {
                    viewModel.IsAddModalOpen = true;
                }
            }
            return new ProductsPage { DataContext = viewModel };
        });
        navigationService.RegisterPage("StockLevels", _ => new StockLevelsPage { DataContext = new StockLevelsPageViewModel() });
        navigationService.RegisterPage("Locations", param =>
        {
            var viewModel = new LocationsPageViewModel();
            // Check if we should open the add modal
            if (param is Dictionary<string, object?> dict && dict.TryGetValue("openAddModal", out var openAdd) && openAdd is true)
            {
                LocationsModalsViewModel?.OpenAddModal();
            }
            return new LocationsPage { DataContext = viewModel };
        });
        navigationService.RegisterPage("StockAdjustments", _ => new StockAdjustmentsPage { DataContext = new StockAdjustmentsPageViewModel() });
        navigationService.RegisterPage("PurchaseOrders", _ => new PurchaseOrdersPage { DataContext = new PurchaseOrdersPageViewModel() });
        navigationService.RegisterPage("Categories", param =>
        {
            var viewModel = new CategoriesPageViewModel();
            if (param is Dictionary<string, object?> dict)
            {
                // Check if we should select a specific tab (0 = Expenses, 1 = Revenue)
                if (dict.TryGetValue("selectedTabIndex", out var tabIndex) && tabIndex is int index)
                {
                    viewModel.SelectedTabIndex = index;
                }
                // Check if we should open the add modal
                if (dict.TryGetValue("openAddModal", out var openAdd) && openAdd is true)
                {
                    viewModel.IsAddModalOpen = true;
                }
            }
            return new CategoriesPage { DataContext = viewModel };
        });

        // Contacts Section
        navigationService.RegisterPage("Customers", _ => new CustomersPage { DataContext = new CustomersPageViewModel() });
        navigationService.RegisterPage("Suppliers", _ => new SuppliersPage { DataContext = new SuppliersPageViewModel() });
        navigationService.RegisterPage("Employees", _ => CreatePlaceholderPage("Employees", "Manage employee records"));
        navigationService.RegisterPage("Departments", _ => new DepartmentsPage { DataContext = new DepartmentsPageViewModel() });
        navigationService.RegisterPage("Accountants", _ => CreatePlaceholderPage("Accountants", "Manage accountant information"));

        // Rentals Section
        navigationService.RegisterPage("RentalInventory", _ => new RentalInventoryPage { DataContext = new RentalInventoryPageViewModel() });
        navigationService.RegisterPage("RentalRecords", _ => new RentalRecordsPage { DataContext = new RentalRecordsPageViewModel() });

        // Tracking Section
        navigationService.RegisterPage("Returns", _ => new ReturnsPage { DataContext = new ReturnsPageViewModel() });
        navigationService.RegisterPage("LostDamaged", _ => new LostDamagedPage { DataContext = new LostDamagedPageViewModel() });
        navigationService.RegisterPage("Receipts", _ =>
        {
            var viewModel = new ReceiptsPageViewModel
            {
                // Set plan status from app shell
                HasPremium = _appShellViewModel?.SidebarViewModel.HasPremium ?? false
            };
            return new ReceiptsPage { DataContext = viewModel };
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
        catch
        {
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
public class PlanStatusChangedEventArgs(bool hasStandard, bool hasPremium) : EventArgs
{
    public bool HasStandard { get; } = hasStandard;
    public bool HasPremium { get; } = hasPremium;
}
