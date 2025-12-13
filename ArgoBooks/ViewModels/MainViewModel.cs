using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Main application ViewModel that manages the overall application state.
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;

    [ObservableProperty]
    private string _title = "Argo Books";

    [ObservableProperty]
    private bool _isCompanyOpen;

    [ObservableProperty]
    private string? _currentCompanyName;

    [ObservableProperty]
    private ViewModelBase? _currentPage;

    [ObservableProperty]
    private bool _isSidebarCollapsed;

    public MainViewModel(ISettingsService settingsService)
    {
        _settingsService = settingsService;
    }

    /// <summary>
    /// Updates the window title based on the current state.
    /// </summary>
    partial void OnCurrentCompanyNameChanged(string? value)
    {
        Title = string.IsNullOrEmpty(value) ? "Argo Books" : $"{value} - Argo Books";
    }

    /// <summary>
    /// Toggles the sidebar collapsed state.
    /// </summary>
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
    }
}
