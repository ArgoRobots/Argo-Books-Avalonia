using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
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
    private static WelcomeScreenViewModel? _welcomeScreenViewModel;

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
            CompanyManager = new CompanyManager(fileService, encryptionService, SettingsService, footerService);

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

            // Wire up header save request
            _appShellViewModel.HeaderViewModel.SaveRequested += async (_, _) =>
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

            // Share CreateCompanyViewModel with MainWindow for full-screen overlay
            _mainWindowViewModel.CreateCompanyViewModel = _appShellViewModel.CreateCompanyViewModel;

            // Share WelcomeScreenViewModel with MainWindow for full-screen overlay
            _mainWindowViewModel.WelcomeScreenViewModel = _welcomeScreenViewModel;

            desktop.MainWindow = new MainWindow
            {
                DataContext = _mainWindowViewModel
            };

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
            // Load global settings
            if (SettingsService != null)
            {
                await SettingsService.LoadGlobalSettingsAsync();

                // Initialize theme service with settings
                ThemeService.Instance.SetGlobalSettingsService(SettingsService);
                ThemeService.Instance.Initialize();
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
    /// Wires up CompanyManager events to update UI.
    /// </summary>
    private static void WireCompanyManagerEvents()
    {
        if (CompanyManager == null || _mainWindowViewModel == null || _appShellViewModel == null)
            return;

        CompanyManager.CompanyOpened += (_, args) =>
        {
            _mainWindowViewModel.OpenCompany(args.CompanyName);
            var logo = LoadBitmapFromPath(CompanyManager.CurrentCompanyLogoPath);
            _appShellViewModel.SetCompanyInfo(args.CompanyName, logo);
            _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany(args.CompanyName, args.FilePath, logo);
            _appShellViewModel.FileMenuPanelViewModel.SetCurrentCompany(args.FilePath);
            _mainWindowViewModel.HideLoading();

            // Navigate to Dashboard when company is opened
            NavigationService?.NavigateTo("Dashboard");
        };

        CompanyManager.CompanyClosed += (_, _) =>
        {
            _mainWindowViewModel.CloseCompany();
            _appShellViewModel.SetCompanyInfo(null);
            _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany("", null);
            _appShellViewModel.FileMenuPanelViewModel.SetCurrentCompany(null);
            _mainWindowViewModel.HideLoading();

            // Navigate back to Welcome screen when company is closed
            NavigationService?.NavigateTo("Welcome");
        };

        CompanyManager.CompanySaved += (_, _) =>
        {
            _mainWindowViewModel.HideLoading();
            _mainWindowViewModel.HasUnsavedChanges = false;
            _appShellViewModel.HeaderViewModel.ShowSavedFeedback();
        };

        CompanyManager.CompanyDataChanged += (_, _) =>
        {
            _mainWindowViewModel.HasUnsavedChanges = true;
            _appShellViewModel.HeaderViewModel.HasUnsavedChanges = true;
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

        // Edit current company
        companySwitcher.EditCompanyRequested += (_, _) =>
        {
            if (CompanyManager?.IsCompanyOpen != true || _appShellViewModel == null) return;

            var settings = CompanyManager.CurrentCompanySettings;
            var logoPath = CompanyManager.CurrentCompanyLogoPath;
            var logo = LoadBitmapFromPath(logoPath);
            _appShellViewModel.EditCompanyModalViewModel.Open(
                settings?.Company.Name ?? "",
                settings?.Company.Email,
                settings?.Company.Phone,
                settings?.Company.Address,
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
                    settings.Company.Name = args.CompanyName;
                    settings.Company.Email = args.Email;
                    settings.Company.Phone = args.Phone;
                    settings.Company.Address = args.Address;

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

                    // Update UI
                    _mainWindowViewModel?.OpenCompany(args.CompanyName);
                    _appShellViewModel.SetCompanyInfo(args.CompanyName);
                    _appShellViewModel.CompanySwitcherPanelViewModel.SetCurrentCompany(
                        args.CompanyName,
                        CompanyManager.CurrentFilePath,
                        LoadBitmapFromPath(CompanyManager.CurrentCompanyLogoPath));
                }

                _appShellViewModel?.AddNotification("Updated", "Company information updated.", NotificationType.Success);
            }
            catch (Exception ex)
            {
                _appShellViewModel?.AddNotification("Error", $"Failed to update company: {ex.Message}", NotificationType.Error);
            }
        };

        // Browse logo
        editCompany.BrowseLogoRequested += async (_, _) =>
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
        _welcomeScreenViewModel = new WelcomeScreenViewModel(navigationService);
        navigationService.RegisterPage("Welcome", _ => new WelcomeScreen { DataContext = _welcomeScreenViewModel });

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
