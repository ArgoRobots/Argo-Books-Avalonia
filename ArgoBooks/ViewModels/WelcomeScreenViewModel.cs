using System.Collections.ObjectModel;
using System.Diagnostics;
using ArgoBooks.Services;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the welcome/startup screen.
/// </summary>
public partial class WelcomeScreenViewModel : ViewModelBase
{
    /// <summary>
    /// Recent companies for quick access.
    /// </summary>
    public ObservableCollection<RecentCompanyItem> RecentCompanies { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRecentCompanies))]
    private bool _hasRecentCompanies;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRecentCompanies))]
    private bool _isRecentCompaniesLoaded;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowRecentCompanies))]
    [NotifyPropertyChangedFor(nameof(ShowOpenCompany))]
    [NotifyPropertyChangedFor(nameof(ShowSampleCompany))]
    private bool _isTutorialMode;

    /// <summary>
    /// Only show recent companies section after initial load completes and there are companies.
    /// This prevents layout shift/flicker on startup.
    /// </summary>
    public bool ShowRecentCompanies => IsRecentCompaniesLoaded && HasRecentCompanies && !IsTutorialMode;

    /// <summary>
    /// Gets whether to show the Open Company option (hidden in tutorial mode).
    /// </summary>
    public bool ShowOpenCompany => !IsTutorialMode;

    /// <summary>
    /// Gets whether to show the Sample Company option (hidden in tutorial mode).
    /// </summary>
    public bool ShowSampleCompany => !IsTutorialMode;

    [ObservableProperty]
    private string _appVersion = AppInfo.Version;

    /// <summary>
    /// Default constructor for design-time.
    /// </summary>
    public WelcomeScreenViewModel()
    {
        // Only populate sample data in design mode to avoid flicker at runtime
        if (!Design.IsDesignMode)
            return;

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
        IsRecentCompaniesLoaded = true;
    }

    #region Commands

    /// <summary>
    /// Creates a new company.
    /// </summary>
    [RelayCommand]
    private void CreateNewCompany()
    {
        CreateNewCompanyRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens an existing company file.
    /// </summary>
    [RelayCommand]
    private void OpenCompany()
    {
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
        RemoveFromRecentRequested?.Invoke(this, company);
    }

    /// <summary>
    /// Clears all recent companies.
    /// </summary>
    [RelayCommand]
    private void ClearRecent()
    {
        RecentCompanies.Clear();
        HasRecentCompanies = false;
        ClearRecentRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens help documentation.
    /// </summary>
    [RelayCommand]
    private void OpenHelp()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://argorobots.com/contact-us/",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening URL
        }
        OpenHelpRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Opens what's new / release notes.
    /// </summary>
    [RelayCommand]
    private void OpenWhatsNew()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://argorobots.com/whats-new/",
                UseShellExecute = true
            });
        }
        catch
        {
            // Ignore errors opening URL
        }
        OpenWhatsNewRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Skips the tutorial and exits tutorial mode.
    /// </summary>
    [RelayCommand]
    private void SkipTutorial()
    {
        TutorialService.Instance.CompleteWelcomeTutorial();
        TutorialService.Instance.CompleteAppTour();
        IsTutorialMode = false;
        SkipTutorialRequested?.Invoke(this, EventArgs.Empty);
    }

    #endregion

    #region Events

    public event EventHandler? CreateNewCompanyRequested;
    public event EventHandler? OpenCompanyRequested;
    public event EventHandler<RecentCompanyItem>? OpenRecentCompanyRequested;
    public event EventHandler<RecentCompanyItem>? RemoveFromRecentRequested;
    public event EventHandler? ClearRecentRequested;
    public event EventHandler? OpenSampleCompanyRequested;
    public event EventHandler? OpenHelpRequested;
    public event EventHandler? OpenWhatsNewRequested;
    public event EventHandler? SkipTutorialRequested;

    #endregion

    #region Public Methods

    /// <summary>
    /// Initializes the tutorial mode state based on whether this is a first-time user.
    /// </summary>
    public void InitializeTutorialMode()
    {
        IsTutorialMode = TutorialService.Instance.IsFirstTimeUser;
    }

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
