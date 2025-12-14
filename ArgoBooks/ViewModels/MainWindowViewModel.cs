using ArgoBooks.Core.Models;
using Avalonia.Controls;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the main application window. Manages window state,
/// current view, and company state.
/// </summary>
public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IGlobalSettingsService? _globalSettingsService;

    #region Window State Properties

    [ObservableProperty]
    private double _windowWidth = 1280;

    [ObservableProperty]
    private double _windowHeight = 800;

    [ObservableProperty]
    private double _windowLeft = -1;

    [ObservableProperty]
    private double _windowTop = -1;

    [ObservableProperty]
    private WindowState _windowState = WindowState.Normal;

    [ObservableProperty]
    private string _windowTitle = "Argo Books";

    #endregion

    #region Application State Properties

    [ObservableProperty]
    private bool _hasCompanyOpen;

    [ObservableProperty]
    private string? _currentCompanyName;

    [ObservableProperty]
    private object? _currentView;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _loadingMessage;

    #endregion

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public MainWindowViewModel() : this(null)
    {
    }

    /// <summary>
    /// Constructor with dependency injection.
    /// </summary>
    /// <param name="globalSettingsService">Service for persisting window state.</param>
    public MainWindowViewModel(IGlobalSettingsService? globalSettingsService)
    {
        _globalSettingsService = globalSettingsService;
    }

    /// <summary>
    /// Updates the window title when company name changes.
    /// </summary>
    partial void OnCurrentCompanyNameChanged(string? value)
    {
        HasCompanyOpen = !string.IsNullOrEmpty(value);
        WindowTitle = string.IsNullOrEmpty(value)
            ? "Argo Books"
            : $"{value} - Argo Books";
    }

    /// <summary>
    /// Loads the saved window state from settings.
    /// </summary>
    public void LoadWindowState()
    {
        if (_globalSettingsService == null)
            return;

        try
        {
            var settings = _globalSettingsService.GetSettings();
            if (settings?.WindowState != null)
            {
                var ws = settings.WindowState;

                if (ws.Width > 0)
                    WindowWidth = ws.Width;
                if (ws.Height > 0)
                    WindowHeight = ws.Height;
                if (ws.Left >= 0)
                    WindowLeft = ws.Left;
                if (ws.Top >= 0)
                    WindowTop = ws.Top;

                WindowState = ws.IsMaximized ? WindowState.Maximized : WindowState.Normal;
            }
        }
        catch
        {
            // Ignore errors loading window state - use defaults
        }
    }

    /// <summary>
    /// Saves the current window state to settings.
    /// </summary>
    public void SaveWindowState()
    {
        if (_globalSettingsService == null)
            return;

        try
        {
            var settings = _globalSettingsService.GetSettings() ?? new GlobalSettings();
            settings.WindowState = new WindowStateSettings
            {
                Width = WindowWidth,
                Height = WindowHeight,
                Left = WindowLeft,
                Top = WindowTop,
                IsMaximized = WindowState == WindowState.Maximized
            };

            _globalSettingsService.SaveSettings(settings);
        }
        catch
        {
            // Ignore errors saving window state
        }
    }

    /// <summary>
    /// Navigates to a view by setting it as the current view.
    /// </summary>
    /// <param name="view">The view to display.</param>
    public void NavigateTo(object view)
    {
        CurrentView = view;
    }

    /// <summary>
    /// Shows a loading overlay with optional message.
    /// </summary>
    /// <param name="message">Loading message to display.</param>
    public void ShowLoading(string? message = null)
    {
        LoadingMessage = message ?? "Loading...";
        IsLoading = true;
    }

    /// <summary>
    /// Hides the loading overlay.
    /// </summary>
    public void HideLoading()
    {
        IsLoading = false;
        LoadingMessage = null;
    }

    /// <summary>
    /// Opens a company with the given name.
    /// </summary>
    /// <param name="companyName">Name of the company to display.</param>
    public void OpenCompany(string companyName)
    {
        CurrentCompanyName = companyName;
    }

    /// <summary>
    /// Closes the current company.
    /// </summary>
    public void CloseCompany()
    {
        CurrentCompanyName = null;
        // Don't reset CurrentView - keep showing the AppShell
        // The navigation will handle showing the appropriate page
    }
}
