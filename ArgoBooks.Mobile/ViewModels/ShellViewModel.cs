using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ArgoBooks.Core.Services.Sync;
using ArgoBooks.Shared.Mobile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Mobile.ViewModels;

/// <summary>Which bottom-nav icon is highlighted. Capture is a placeholder until Plan 5.</summary>
public enum AppTab
{
    Home,
    Data,
    Capture,
    Analytics,
    Settings,
}

/// <summary>
/// App shell: owns the <see cref="SnapshotStore"/> refresh cycle, the icon-only bottom nav, and a
/// tiny single-level "push a detail page, then back" navigation stack (mirrors the prototype's
/// nav()/back pattern) for Data hub section -> item detail. Home/Data/Analytics/Settings are
/// persistent root pages; opening a section or an item detail pushes onto the back stack.
/// </summary>
public partial class ShellViewModel : ViewModelBase
{
    private readonly SnapshotStore _snapshotStore;
    private readonly PairedCompanyStore _pairedCompanyStore;
    private readonly Stack<(object Page, string Title, AppTab Tab)> _backStack = new();

    private readonly DashboardViewModel _dashboard;
    private readonly DataHubViewModel _dataHub;
    private readonly AnalyticsViewModel _analytics;
    private readonly SettingsViewModel _settings;

    [ObservableProperty]
    private object _currentPage;

    [ObservableProperty]
    private AppTab _activeTab = AppTab.Home;

    [ObservableProperty]
    private string _headerTitle = "Dashboard";

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _isRefreshing;

    [ObservableProperty]
    private bool _isNotPaired;

    [ObservableProperty]
    private bool _isWaitingForSync;

    [ObservableProperty]
    private bool _isStale;

    [ObservableProperty]
    private string _lastSyncedText = "Not synced yet";

    /// <summary>True once a snapshot (fresh or cached) is loaded, so the root pages can render.</summary>
    [ObservableProperty]
    private bool _isContentVisible;

    // Bottom-nav highlight flags, kept in sync with ActiveTab (see OnActiveTabChanged) so the
    // XAML doesn't need an enum-comparison converter.
    [ObservableProperty]
    private bool _isHomeActive = true;

    [ObservableProperty]
    private bool _isDataActive;

    [ObservableProperty]
    private bool _isCaptureActive;

    [ObservableProperty]
    private bool _isAnalyticsActive;

    [ObservableProperty]
    private bool _isSettingsActive;

    partial void OnActiveTabChanged(AppTab value)
    {
        IsHomeActive = value == AppTab.Home;
        IsDataActive = value == AppTab.Data;
        IsCaptureActive = value == AppTab.Capture;
        IsAnalyticsActive = value == AppTab.Analytics;
        IsSettingsActive = value == AppTab.Settings;
    }

    public ShellViewModel(SnapshotStore snapshotStore, PairedCompanyStore pairedCompanyStore)
    {
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _pairedCompanyStore = pairedCompanyStore ?? throw new ArgumentNullException(nameof(pairedCompanyStore));

        _dashboard = new DashboardViewModel(OpenItemDetail);
        _dataHub = new DataHubViewModel(OpenSection);
        _analytics = new AnalyticsViewModel(OpenItemDetail);
        _settings = new SettingsViewModel(this);

        _currentPage = _dashboard;
    }

    /// <summary>Fetches the initial snapshot. Call once after constructing the shell.</summary>
    public async Task InitializeAsync() => await RefreshAsync();

    [RelayCommand]
    private async Task RefreshAsync()
    {
        IsRefreshing = true;
        try
        {
            var state = await _snapshotStore.RefreshAsync(CancellationToken.None);
            await ApplyAsync(state);
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    private async Task ApplyAsync(SnapshotState state)
    {
        IsNotPaired = state.Status == SnapshotStatus.NotPaired;
        IsWaitingForSync = state.Status == SnapshotStatus.WaitingForFirstSync;
        IsContentVisible = state.Status == SnapshotStatus.Loaded;
        IsStale = state.IsStale;
        LastSyncedText = FormatLastSynced(state.LastSyncedAt, state.IsStale);

        _dashboard.UpdateSnapshot(state.Snapshot);
        _dataHub.UpdateSnapshot(state.Snapshot);
        _analytics.UpdateSnapshot(state.Snapshot);

        var record = await _pairedCompanyStore.GetActiveAsync();
        _settings.Update(record?.CompanyLabel ?? string.Empty, LastSyncedText);
    }

    private static string FormatLastSynced(DateTime? lastSyncedAt, bool isStale)
    {
        if (lastSyncedAt == null)
        {
            return "Not synced yet";
        }

        var age = DateTime.UtcNow - lastSyncedAt.Value;
        var when = age.TotalSeconds < 60 ? "just now"
            : age.TotalMinutes < 60 ? $"{(int)age.TotalMinutes} min ago"
            : age.TotalHours < 24 ? $"{(int)age.TotalHours} hr ago"
            : $"{(int)age.TotalDays} d ago";

        return isStale ? $"Synced {when} (offline)" : $"Synced {when}";
    }

    [RelayCommand]
    private void NavigateHome() => ResetToRoot(_dashboard, "Dashboard", AppTab.Home);

    [RelayCommand]
    private void NavigateData() => ResetToRoot(_dataHub, "Your data", AppTab.Data);

    [RelayCommand]
    private void NavigateAnalytics() => ResetToRoot(_analytics, "Analytics", AppTab.Analytics);

    [RelayCommand]
    private void NavigateSettings() => ResetToRoot(_settings, "Settings", AppTab.Settings);

    [RelayCommand]
    private void NavigateCapture()
    {
        // Placeholder: receipt capture (scan -> review -> add to books) is Plan 5's job.
        // Tapping this tab today just highlights it; no screen is wired up yet.
        ActiveTab = AppTab.Capture;
    }

    private void ResetToRoot(object page, string title, AppTab tab)
    {
        _backStack.Clear();
        CurrentPage = page;
        HeaderTitle = title;
        ActiveTab = tab;
        CanGoBack = false;
    }

    private void OpenSection(string sectionKey, string label, IReadOnlyList<RowDto> rows)
    {
        _backStack.Push((CurrentPage, HeaderTitle, ActiveTab));
        CurrentPage = new DataSectionListViewModel(label, rows, OpenItemDetail);
        HeaderTitle = label;
        CanGoBack = true;
    }

    private void OpenItemDetail(RowDto row)
    {
        _backStack.Push((CurrentPage, HeaderTitle, ActiveTab));
        CurrentPage = new ItemDetailViewModel(row);
        HeaderTitle = "Details";
        CanGoBack = true;
    }

    [RelayCommand]
    private void GoBack()
    {
        if (_backStack.Count == 0)
        {
            return;
        }

        var (page, title, tab) = _backStack.Pop();
        CurrentPage = page;
        HeaderTitle = title;
        ActiveTab = tab;
        CanGoBack = _backStack.Count > 0;
    }
}
