using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Services;
using ArgoBooks.ViewModels;
using ArgoBooks.Views;

namespace ArgoBooks;

public partial class App : Application
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

    // View models stored for event wiring
    private static MainWindowViewModel? _mainWindowViewModel;
    private static AppShellViewModel? _appShellViewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            DisableAvaloniaDataAnnotationValidation();

            // Initialize services
            var compressionService = new CompressionService();
            var footerService = new FooterService();
            var encryptionService = new EncryptionService();
            var fileService = new FileService(compressionService, footerService, encryptionService);
            SettingsService = new GlobalSettingsService();
            CompanyManager = new CompanyManager(fileService, encryptionService, SettingsService, footerService);

            // Load global settings
            await SettingsService.LoadGlobalSettingsAsync();

            // Create navigation service
            NavigationService = new NavigationService();

            _mainWindowViewModel = new MainWindowViewModel();

            // Create app shell with navigation service
            _appShellViewModel = new AppShellViewModel(NavigationService, SettingsService);
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

            // Wire up file menu events
            WireFileMenuEvents(desktop);

            // Wire up create company wizard events
            WireCreateCompanyEvents(desktop);

            // Wire up welcome screen events
            WireWelcomeScreenEvents(desktop);

            // Wire up company switcher events
            WireCompanySwitcherEvents(desktop);

            // Navigate to Dashboard by default
            NavigationService.NavigateTo("Dashboard");

            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel
            };

            // Load and display recent companies
            await LoadRecentCompaniesAsync();
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
    /// Wires up CompanyManager events to update UI.
    /// </summary>
    private static void WireCompanyManagerEvents()
    {
        if (CompanyManager == null || _mainWindowViewModel == null || _appShellViewModel == null)
            return;

        CompanyManager.CompanyOpened += (_, args) =>
        {
            _mainWindowViewModel.OpenCompany(args.CompanyName);
            _appShellViewModel.SetCompanyInfo(args.CompanyName);
            _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany(args.CompanyName, args.FilePath);
            _mainWindowViewModel.HideLoading();
        };

        CompanyManager.CompanyClosed += (_, _) =>
        {
            _mainWindowViewModel.CloseCompany();
            _appShellViewModel.SetCompanyInfo(null);
            _mainWindowViewModel.HideLoading();
        };

        CompanyManager.CompanySaved += (_, _) =>
        {
            _mainWindowViewModel.HideLoading();
            _appShellViewModel.AddNotification("Saved", "Company saved successfully.", NotificationType.Success);
        };

        CompanyManager.PasswordRequired += async (_, args) =>
        {
            if (_appShellViewModel?.PasswordPromptModalViewModel == null) return;

            // Get company name from footer if possible
            var footer = await CompanyManager.GetFileInfoAsync(args.FilePath);
            var companyName = footer?.CompanyName ?? Path.GetFileNameWithoutExtension(args.FilePath);

            var password = await _appShellViewModel.PasswordPromptModalViewModel.ShowAsync(companyName, args.FilePath);
            args.Password = password;
            args.IsCancelled = password == null;
        };
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
                _mainWindowViewModel?.ShowLoading("Saving...");
                try
                {
                    await CompanyManager.SaveCompanyAsync();
                }
                catch (Exception ex)
                {
                    _mainWindowViewModel?.HideLoading();
                    _appShellViewModel?.AddNotification("Error", $"Failed to save: {ex.Message}", NotificationType.Error);
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
                if (CompanyManager.HasUnsavedChanges)
                {
                    // TODO: Show save prompt dialog
                    // For now, just save and close
                    await CompanyManager.SaveCompanyAsync();
                }
                await CompanyManager.CloseCompanyAsync();
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

            _mainWindowViewModel?.ShowLoading("Opening company...");
            try
            {
                var success = await CompanyManager!.OpenCompanyAsync(company.FilePath);
                if (success)
                {
                    await LoadRecentCompaniesAsync();
                }
                else
                {
                    _mainWindowViewModel?.HideLoading();
                }
            }
            catch (FileNotFoundException)
            {
                _mainWindowViewModel?.HideLoading();
                _appShellViewModel?.AddNotification("File Not Found", "The company file no longer exists.", NotificationType.Error);
                SettingsService?.RemoveRecentCompany(company.FilePath);
                await LoadRecentCompaniesAsync();
            }
            catch (Exception ex)
            {
                _mainWindowViewModel?.HideLoading();
                _appShellViewModel?.AddNotification("Error", $"Failed to open file: {ex.Message}", NotificationType.Error);
            }
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

            _mainWindowViewModel?.ShowLoading("Creating company...");
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
                _appShellViewModel?.AddNotification("Error", $"Failed to create company: {ex.Message}", NotificationType.Error);
            }
        };

        createCompany.BrowseLogoRequested += async (_, _) =>
        {
            var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select Company Logo",
                AllowMultiple = false,
                FileTypeFilter = new[]
                {
                    new FilePickerFileType("Images")
                    {
                        Patterns = new[] { "*.png", "*.jpg", "*.jpeg" }
                    }
                }
            });

            if (files.Count > 0)
            {
                var path = files[0].Path.LocalPath;
                try
                {
                    var bitmap = new Avalonia.Media.Imaging.Bitmap(path);
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
        // The welcome screen is navigated to via the navigation service
        // We'll wire it up when/if we show it
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

            _mainWindowViewModel?.ShowLoading("Opening company...");
            try
            {
                var success = await CompanyManager!.OpenCompanyAsync(company.FilePath);
                if (success)
                {
                    await LoadRecentCompaniesAsync();
                }
                else
                {
                    _mainWindowViewModel?.HideLoading();
                }
            }
            catch (FileNotFoundException)
            {
                _mainWindowViewModel?.HideLoading();
                _appShellViewModel?.AddNotification("File Not Found", "The company file no longer exists.", NotificationType.Error);
                SettingsService?.RemoveRecentCompany(company.FilePath);
                await LoadRecentCompaniesAsync();
            }
            catch (Exception ex)
            {
                _mainWindowViewModel?.HideLoading();
                _appShellViewModel?.AddNotification("Error", $"Failed to open file: {ex.Message}", NotificationType.Error);
            }
        };

        // Open company from file dialog
        companySwitcher.OpenCompanyRequested += async (_, _) =>
        {
            await OpenCompanyFileDialogAsync(desktop);
        };
    }

    /// <summary>
    /// Opens the file dialog to select a company file.
    /// </summary>
    private static async Task OpenCompanyFileDialogAsync(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var files = await desktop.MainWindow!.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Company",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Argo Books Files")
                {
                    Patterns = new[] { "*.argo" }
                },
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }
                }
            }
        });

        if (files.Count > 0)
        {
            var filePath = files[0].Path.LocalPath;
            _mainWindowViewModel?.ShowLoading("Opening company...");

            try
            {
                var success = await CompanyManager!.OpenCompanyAsync(filePath);
                if (success)
                {
                    await LoadRecentCompaniesAsync();
                }
                else
                {
                    _mainWindowViewModel?.HideLoading();
                }
            }
            catch (UnauthorizedAccessException)
            {
                _mainWindowViewModel?.HideLoading();
                _appShellViewModel?.PasswordPromptModalViewModel?.ShowError("Invalid password. Please try again.");
            }
            catch (Exception ex)
            {
                _mainWindowViewModel?.HideLoading();
                _appShellViewModel?.AddNotification("Error", $"Failed to open file: {ex.Message}", NotificationType.Error);
            }
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
        _mainWindowViewModel?.ShowLoading("Saving...");

        try
        {
            await CompanyManager!.SaveCompanyAsAsync(filePath);
            await LoadRecentCompaniesAsync();
        }
        catch (Exception ex)
        {
            _mainWindowViewModel?.HideLoading();
            _appShellViewModel?.AddNotification("Error", $"Failed to save file: {ex.Message}", NotificationType.Error);
        }
    }

    /// <summary>
    /// Shows a save file dialog.
    /// </summary>
    private static async Task<IStorageFile?> ShowSaveFileDialogAsync(IClassicDesktopStyleApplicationLifetime desktop, string suggestedFileName)
    {
        return await desktop.MainWindow!.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Company",
            SuggestedFileName = $"{suggestedFileName}.argo",
            DefaultExtension = "argo",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Argo Books Files")
                {
                    Patterns = new[] { "*.argo" }
                }
            }
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

            // Wire up opening recent companies
            foreach (var item in _appShellViewModel.FileMenuPanelViewModel.RecentCompanies)
            {
                // Items are opened via command in the view model
            }
        }
        catch
        {
            // Ignore errors loading recent companies
        }
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

        // Main Section
        navigationService.RegisterPage("Dashboard", _ => CreatePlaceholderPage("Dashboard", "Welcome to the Dashboard"));
        navigationService.RegisterPage("Analytics", _ => CreatePlaceholderPage("Analytics", "Analytics and insights"));
        navigationService.RegisterPage("Reports", _ => CreatePlaceholderPage("Reports", "Generate and view reports"));

        // Transactions Section
        navigationService.RegisterPage("Revenue", _ => CreatePlaceholderPage("Revenue", "Track income and sales"));
        navigationService.RegisterPage("Expenses", _ => CreatePlaceholderPage("Expenses", "Record and manage expenses"));
        navigationService.RegisterPage("Invoices", _ => CreatePlaceholderPage("Invoices", "Create and manage invoices"));
        navigationService.RegisterPage("Payments", _ => CreatePlaceholderPage("Payments", "Record payments"));

        // Inventory Section
        navigationService.RegisterPage("Products", _ => CreatePlaceholderPage("Products", "Manage products and services"));
        navigationService.RegisterPage("StockLevels", _ => CreatePlaceholderPage("Stock Levels", "Monitor inventory levels"));
        navigationService.RegisterPage("PurchaseOrders", _ => CreatePlaceholderPage("Purchase Orders", "Create and track purchase orders"));
        navigationService.RegisterPage("Categories", _ => CreatePlaceholderPage("Categories", "Organize items by category"));

        // Contacts Section
        navigationService.RegisterPage("Customers", _ => CreatePlaceholderPage("Customers", "Manage customer information"));
        navigationService.RegisterPage("Suppliers", _ => CreatePlaceholderPage("Suppliers", "Manage supplier information"));
        navigationService.RegisterPage("Employees", _ => CreatePlaceholderPage("Employees", "Manage employee records"));
        navigationService.RegisterPage("Accountants", _ => CreatePlaceholderPage("Accountants", "Manage accountant information"));

        // Rentals Section
        navigationService.RegisterPage("RentalInventory", _ => CreatePlaceholderPage("Rental Inventory", "Manage rental items"));
        navigationService.RegisterPage("RentalRecords", _ => CreatePlaceholderPage("Rental Records", "Track rental transactions"));

        // Settings and Help
        navigationService.RegisterPage("Settings", _ => CreatePlaceholderPage("Settings", "Configure application settings"));
        navigationService.RegisterPage("Help", _ => CreatePlaceholderPage("Help", "Get help and documentation"));

        // Search (with parameter support)
        navigationService.RegisterPage("Search", param =>
        {
            var query = param is Dictionary<string, object?> dict && dict.TryGetValue("query", out var q)
                ? q?.ToString() ?? ""
                : "";
            return CreatePlaceholderPage("Search Results", $"Searching for: {query}");
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
