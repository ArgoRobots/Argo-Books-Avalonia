using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the file menu panel.
/// </summary>
public partial class FileMenuPanelViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;

    [ObservableProperty]
    private bool _isOpen;

    [ObservableProperty]
    private bool _isRecentSubmenuOpen;

    /// <summary>
    /// Recent companies for quick access.
    /// </summary>
    public ObservableCollection<RecentCompanyItem> RecentCompanies { get; } = [];

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public FileMenuPanelViewModel()
    {
        // Design-time defaults
        RecentCompanies.Add(new RecentCompanyItem { Name = "My Company Inc.", Icon = "Building" });
        RecentCompanies.Add(new RecentCompanyItem { Name = "Side Business LLC", Icon = "Store" });
        RecentCompanies.Add(new RecentCompanyItem { Name = "Consulting Services", Icon = "Briefcase" });
    }

    /// <summary>
    /// Constructor with dependencies.
    /// </summary>
    public FileMenuPanelViewModel(INavigationService? navigationService)
    {
        _navigationService = navigationService;
    }

    #region Commands

    /// <summary>
    /// Opens the file menu panel.
    /// </summary>
    [RelayCommand]
    private void Open()
    {
        IsOpen = true;
    }

    /// <summary>
    /// Closes the file menu panel.
    /// </summary>
    [RelayCommand]
    private void Close()
    {
        IsOpen = false;
    }

    /// <summary>
    /// Toggles the file menu panel.
    /// </summary>
    [RelayCommand]
    private void Toggle()
    {
        IsOpen = !IsOpen;
    }

    /// <summary>
    /// Creates a new company.
    /// </summary>
    [RelayCommand]
    private void NewCompany()
    {
        Close();
        // TODO: Show create company wizard
        CreateNewCompanyRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens a company file.
    /// </summary>
    [RelayCommand]
    private void OpenCompany()
    {
        Close();
        // TODO: Show file picker
        OpenCompanyRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens a recent company.
    /// </summary>
    [RelayCommand]
    private void OpenRecentCompany(RecentCompanyItem? company)
    {
        if (company == null) return;
        Close();
        OpenRecentCompanyRequested?.Invoke(this, company);
    }

    /// <summary>
    /// Clears the recent companies list.
    /// </summary>
    [RelayCommand]
    private void ClearRecent()
    {
        RecentCompanies.Clear();
        Close();
    }

    /// <summary>
    /// Saves the current company.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        Close();
        SaveRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Saves the current company to a new file.
    /// </summary>
    [RelayCommand]
    private void SaveAs()
    {
        Close();
        SaveAsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Closes the current company.
    /// </summary>
    [RelayCommand]
    private void CloseCompany()
    {
        Close();
        CloseCompanyRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens the import dialog.
    /// </summary>
    [RelayCommand]
    private void Import()
    {
        Close();
        ImportRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens the export dialog.
    /// </summary>
    [RelayCommand]
    private void ExportAs()
    {
        Close();
        ExportAsRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Shows the company file in the file explorer.
    /// </summary>
    [RelayCommand]
    private void ShowInFolder()
    {
        Close();
        ShowInFolderRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events

    public event EventHandler? CreateNewCompanyRequested;
    public event EventHandler? OpenCompanyRequested;
    public event EventHandler<RecentCompanyItem>? OpenRecentCompanyRequested;
    public event EventHandler? SaveRequested;
    public event EventHandler? SaveAsRequested;
    public event EventHandler? CloseCompanyRequested;
    public event EventHandler? ImportRequested;
    public event EventHandler? ExportAsRequested;
    public event EventHandler? ShowInFolderRequested;

    #endregion
}

/// <summary>
/// Represents a recent company item.
/// </summary>
public class RecentCompanyItem
{
    public string? Name { get; set; }
    public string? FilePath { get; set; }
    public string? Icon { get; set; }
    public DateTime LastOpened { get; set; } = DateTime.Now;
}
