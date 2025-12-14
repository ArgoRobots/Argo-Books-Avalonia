using System.Collections.ObjectModel;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the welcome/startup screen.
/// </summary>
public partial class WelcomeScreenViewModel : ViewModelBase
{
    private readonly INavigationService? _navigationService;

    /// <summary>
    /// Recent companies for quick access.
    /// </summary>
    public ObservableCollection<RecentCompanyItem> RecentCompanies { get; } = [];

    [ObservableProperty]
    private bool _hasRecentCompanies;

    [ObservableProperty]
    private string _appVersion = "1.0.0";

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public WelcomeScreenViewModel()
    {
        // Design-time defaults
        RecentCompanies.Add(new RecentCompanyItem
        {
            Name = "My Company Inc.",
            FilePath = "C:\\ArgoBooks\\MyCompany.argobk",
            Icon = "Building",
            LastOpened = DateTime.Now.AddDays(-1)
        });
        RecentCompanies.Add(new RecentCompanyItem
        {
            Name = "Side Business LLC",
            FilePath = "C:\\ArgoBooks\\SideBusiness.argobk",
            Icon = "Store",
            LastOpened = DateTime.Now.AddDays(-3)
        });
        RecentCompanies.Add(new RecentCompanyItem
        {
            Name = "Consulting Services",
            FilePath = "C:\\ArgoBooks\\Consulting.argobk",
            Icon = "Briefcase",
            LastOpened = DateTime.Now.AddDays(-7)
        });
        HasRecentCompanies = RecentCompanies.Count > 0;
    }

    /// <summary>
    /// Constructor with dependencies.
    /// </summary>
    public WelcomeScreenViewModel(INavigationService? navigationService)
    {
        _navigationService = navigationService;
        // Real data will be populated by LoadRecentCompaniesAsync
        // Start with no recent companies
        HasRecentCompanies = false;
    }

    #region Commands

    /// <summary>
    /// Creates a new company.
    /// </summary>
    [RelayCommand]
    private void CreateNewCompany()
    {
        // TODO: Show create company wizard
        CreateNewCompanyRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens an existing company file.
    /// </summary>
    [RelayCommand]
    private void OpenCompany()
    {
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
        OpenRecentCompanyRequested?.Invoke(this, company);
    }

    /// <summary>
    /// Opens the sample company for exploration.
    /// </summary>
    [RelayCommand]
    private void OpenSampleCompany()
    {
        OpenSampleCompanyRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Removes a company from recent list.
    /// </summary>
    [RelayCommand]
    private void RemoveFromRecent(RecentCompanyItem? company)
    {
        if (company == null) return;
        RecentCompanies.Remove(company);
        HasRecentCompanies = RecentCompanies.Count > 0;
    }

    /// <summary>
    /// Clears all recent companies.
    /// </summary>
    [RelayCommand]
    private void ClearRecent()
    {
        RecentCompanies.Clear();
        HasRecentCompanies = false;
    }

    /// <summary>
    /// Opens help documentation.
    /// </summary>
    [RelayCommand]
    private void OpenHelp()
    {
        OpenHelpRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens what's new / release notes.
    /// </summary>
    [RelayCommand]
    private void OpenWhatsNew()
    {
        OpenWhatsNewRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events

    public event EventHandler? CreateNewCompanyRequested;
    public event EventHandler? OpenCompanyRequested;
    public event EventHandler<RecentCompanyItem>? OpenRecentCompanyRequested;
    public event EventHandler? OpenSampleCompanyRequested;
    public event EventHandler? OpenHelpRequested;
    public event EventHandler? OpenWhatsNewRequested;

    #endregion

    #region Public Methods

    /// <summary>
    /// Adds a company to the recent list.
    /// </summary>
    public void AddRecentCompany(string name, string filePath, string? icon = null)
    {
        // Remove if already exists
        var existing = RecentCompanies.FirstOrDefault(c => c.FilePath == filePath);
        if (existing != null)
        {
            RecentCompanies.Remove(existing);
        }

        // Add to top
        RecentCompanies.Insert(0, new RecentCompanyItem
        {
            Name = name,
            FilePath = filePath,
            Icon = icon ?? "Building",
            LastOpened = DateTime.Now
        });

        // Keep max 10 recent
        while (RecentCompanies.Count > 10)
        {
            RecentCompanies.RemoveAt(RecentCompanies.Count - 1);
        }

        HasRecentCompanies = RecentCompanies.Count > 0;
    }

    #endregion
}
