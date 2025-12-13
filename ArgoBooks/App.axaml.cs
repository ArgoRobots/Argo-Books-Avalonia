using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
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

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // Create navigation service
            NavigationService = new NavigationService();

            var mainWindowViewModel = new MainWindowViewModel();

            // Create app shell with navigation service
            var appShellViewModel = new AppShellViewModel(NavigationService, null);
            var appShell = new AppShell
            {
                DataContext = appShellViewModel
            };

            // Register pages with navigation service
            RegisterPages(NavigationService, appShellViewModel);

            // Set navigation callback to update current page in AppShell
            NavigationService.SetNavigationCallback(page => appShellViewModel.CurrentPage = page);

            // Set initial view
            mainWindowViewModel.NavigateTo(appShell);

            // Navigate to Dashboard by default
            NavigationService.NavigateTo("Dashboard");

            desktop.MainWindow = new MainWindow
            {
                DataContext = mainWindowViewModel
            };
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