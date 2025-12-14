using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the help panel.
/// </summary>
public partial class HelpPanelViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private string _appVersion = "2.4.1";

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public HelpPanelViewModel()
    {
    }

    /// <summary>
    /// Constructor with dependencies.
    /// </summary>
    public HelpPanelViewModel(INavigationService? navigationService)
    {
        _navigationService = navigationService;
    }

    #region Commands

    [RelayCommand]
    private void Open()
    {
        IsOpen = true;
    }

    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    [RelayCommand]
    private void Toggle()
    {
        IsOpen = !IsOpen;
    }

    [RelayCommand]
    private void OpenWhatsNew()
    {
        Close();
        WhatsNewRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenDocumentation()
    {
        Close();
        DocumentationRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenCommunity()
    {
        Close();
        CommunityRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenSupport()
    {
        Close();
        SupportRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void CheckForUpdates()
    {
        Close();
        CheckForUpdatesRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events

    public event EventHandler? WhatsNewRequested;
    public event EventHandler? DocumentationRequested;
    public event EventHandler? CommunityRequested;
    public event EventHandler? SupportRequested;
    public event EventHandler? CheckForUpdatesRequested;

    #endregion
}
