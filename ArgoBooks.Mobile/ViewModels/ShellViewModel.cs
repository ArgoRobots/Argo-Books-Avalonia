using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ArgoBooks.Core.Services;
using ArgoBooks.Core.Services.Sync;
using ArgoBooks.Mobile.Services;
using ArgoBooks.Shared.Mobile;
using ArgoBooks.Shared.Sync;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Networking;

namespace ArgoBooks.Mobile.ViewModels;

/// <summary>Which bottom-nav icon is highlighted.</summary>
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
    private readonly ISecureStore _secureStore;
    private readonly IApiAuth _deviceApiAuth;
    private readonly CapturePushCoordinator _capturePushCoordinator;
    private readonly PendingScanOutbox _pendingScanOutbox;
    private readonly Stack<(object Page, string Title, AppTab Tab)> _backStack = new();

    // When the user is reviewing a receipt captured while offline, this holds that image's stable
    // outbox queue id: it's reused as the pushed transaction's ScanUid (idempotency) and identifies
    // which queued image to drop once the review is confirmed. Null during a normal online capture.
    private string? _activeOfflineQueueId;

    // Shared across every scan (rather than one HttpClient per GeminiReceiptScannerService
    // instance) so repeated scans reuse connections instead of leaking a fresh HttpClient each time.
    private readonly HttpClient _scanHttpClient = new();

    private readonly DashboardViewModel _dashboard;
    private readonly DataHubViewModel _dataHub;
    private readonly AnalyticsViewModel _analytics;
    private readonly SettingsViewModel _settings;
    private readonly CaptureViewModel _capture;

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

    /// <summary>The active paired company's label, shown in the chip at the top of the root pages.</summary>
    [ObservableProperty]
    private string _activeCompanyLabel = string.Empty;

    /// <summary>
    /// True on the root Dashboard/Data/Analytics pages once a company is paired - hidden on
    /// Settings (which has its own Companies section) and on any pushed detail/switcher/pairing
    /// page, so the chip never floats on top of unrelated content.
    /// </summary>
    [ObservableProperty]
    private bool _isCompanyChipVisible;

    /// <summary>Raised when the active company was unpaired and no paired companies remain, so the
    /// host (App.axaml.cs) should drop back to the full pairing screen.</summary>
    public event Action? RequestPairing;

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
        UpdateCompanyChipVisibility();
    }

    partial void OnCanGoBackChanged(bool value) => UpdateCompanyChipVisibility();

    partial void OnIsNotPairedChanged(bool value) => UpdateCompanyChipVisibility();

    private void UpdateCompanyChipVisibility()
    {
        var isRootViewingTab = ActiveTab is AppTab.Home or AppTab.Data or AppTab.Analytics;
        IsCompanyChipVisible = isRootViewingTab && !CanGoBack && !IsNotPaired;
    }

    public ShellViewModel(SnapshotStore snapshotStore, PairedCompanyStore pairedCompanyStore, ISecureStore secureStore, IApiAuth deviceApiAuth, MobileSyncClient syncClient, PendingScanOutbox pendingScanOutbox)
    {
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
        _pairedCompanyStore = pairedCompanyStore ?? throw new ArgumentNullException(nameof(pairedCompanyStore));
        _deviceApiAuth = deviceApiAuth ?? throw new ArgumentNullException(nameof(deviceApiAuth));
        _secureStore = secureStore ?? throw new ArgumentNullException(nameof(secureStore));
        _pendingScanOutbox = pendingScanOutbox ?? throw new ArgumentNullException(nameof(pendingScanOutbox));
        if (syncClient == null) throw new ArgumentNullException(nameof(syncClient));
        _capturePushCoordinator = new CapturePushCoordinator(syncClient, _pairedCompanyStore);

        _dashboard = new DashboardViewModel(OpenItemDetail);
        _dataHub = new DataHubViewModel(OpenSection);
        _analytics = new AnalyticsViewModel(OpenItemDetail);
        _settings = new SettingsViewModel(this);
        _capture = new CaptureViewModel(secureStore, StartScanFlowAsync, _pendingScanOutbox, StartOfflineReviewAsync);

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
            await LoadSnapshotAsync();
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    /// <summary>
    /// Fetches and applies the active company's snapshot. If the server reports this device was
    /// revoked, drops the paired company instead (see <see cref="DropRevokedActiveCompanyAsync"/>).
    /// </summary>
    private async Task LoadSnapshotAsync()
    {
        var state = await _snapshotStore.RefreshAsync(CancellationToken.None);
        if (state.Status == SnapshotStatus.Revoked)
        {
            await DropRevokedActiveCompanyAsync();
            return;
        }

        await ApplyAsync(state);
    }

    /// <summary>
    /// Handles a desktop-side revocation of the active company: removes it locally, then activates
    /// another paired company if one remains (loading its snapshot), or raises
    /// <see cref="RequestPairing"/> so the host returns to the pairing screen.
    /// </summary>
    private async Task DropRevokedActiveCompanyAsync()
    {
        var active = await _pairedCompanyStore.GetActiveAsync();
        if (active != null)
        {
            await _pairedCompanyStore.RemoveAsync(active.CompanyUid);
        }

        var remaining = await _pairedCompanyStore.GetAllAsync();
        if (remaining.Count == 0)
        {
            RequestPairing?.Invoke();
            return;
        }

        await _pairedCompanyStore.SetActiveAsync(remaining[0].CompanyUid);
        await LoadSnapshotAsync();
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
        ActiveCompanyLabel = record?.CompanyLabel ?? string.Empty;
        _settings.Update(ActiveCompanyLabel, LastSyncedText);
        _capture.SetActiveCompanyLabel(ActiveCompanyLabel);

        // Surface any receipts captured while offline as a "ready to review" prompt on the Capture
        // tab. They are reviewed one at a time (see StartOfflineReviewAsync), never auto-posted.
        await _capture.RefreshPendingOfflineCountAsync();
    }

    /// <summary>
    /// Task 6's offline-capture follow-up, review-gated: walks the receipts captured while offline
    /// through the SAME scan -> review -> confirm flow as an online capture, one at a time, so no
    /// unreviewed AI extraction is ever posted to the books. Invoked from the Capture tab's "Review
    /// now" prompt once connectivity is back (CaptureViewModel gates on network first).
    /// </summary>
    public async Task StartOfflineReviewAsync()
    {
        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return;
        }

        await StartNextOfflineReviewAsync();
    }

    /// <summary>
    /// Pulls the next queued offline image and drives it into the shared review flow, tagging it with
    /// its stable outbox id (reused as the ScanUid). Returns true if a review screen was shown, false
    /// if the queue is empty (in which case the active offline id is cleared).
    /// </summary>
    private async Task<bool> StartNextOfflineReviewAsync()
    {
        var next = await _pendingScanOutbox.PeekNextAsync();
        if (next == null)
        {
            _activeOfflineQueueId = null;
            return false;
        }

        _activeOfflineQueueId = next.Id;
        await StartScanFlowAsync(next.Image);
        return true;
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
        ResetToRoot(_capture, "Scan receipt", AppTab.Capture);
        _ = _capture.RefreshScanUsageAsync();
        _ = _capture.RefreshPendingOfflineCountAsync();
    }

    /// <summary>Refreshes the Capture tab's "captured while offline" review prompt. Called from the
    /// app's foreground hook (App.axaml.cs) so the prompt reflects the queue without a manual refresh.</summary>
    public Task RefreshOfflineQueueAsync() => _capture.RefreshPendingOfflineCountAsync();

    /// <summary>
    /// CaptureViewModel's onImageCaptured callback: pushes the Scanning screen and kicks off the AI
    /// scan against the moved GeminiReceiptScannerService, authenticated with this device's own
    /// X-Device-Id (Option A - see DeviceApiAuth). Guards against a stale ScanningViewModel still
    /// completing in the background after the user has already navigated away from it.
    /// </summary>
    private Task StartScanFlowAsync(byte[] imageBytes)
    {
        var scanner = new GeminiReceiptScannerService(MobileApiConfig.BaseUrl, _deviceApiAuth, httpClient: _scanHttpClient);
        var coordinator = new ReceiptScanCoordinator(scanner);

        ScanningViewModel? scanningViewModel = null;
        scanningViewModel = new ScanningViewModel(
            imageBytes,
            coordinator,
            onSuccess: result =>
            {
                if (ReferenceEquals(CurrentPage, scanningViewModel))
                {
                    OnScanSucceeded(result, imageBytes);
                }
            },
            onRetry: () =>
            {
                if (ReferenceEquals(CurrentPage, scanningViewModel))
                {
                    ReturnToCaptureRoot();
                }
            });

        _backStack.Push((CurrentPage, HeaderTitle, ActiveTab));
        CurrentPage = scanningViewModel;
        HeaderTitle = "Scanning";
        CanGoBack = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Pushes the editable review screen (supplier/customer, date, total/tax, and each line item's
    /// auto-suggested product, all editable) built from the scan result plus the active company's
    /// current snapshot (for supplier/product auto-suggest). Replaces (rather than pushes onto) the
    /// back stack entry the transient Scanning screen used, so GoBack from here returns straight to
    /// Capture.
    /// </summary>
    private void OnScanSucceeded(ReceiptScanResult result, byte[] imageBytes)
    {
        var snapshot = _snapshotStore.Current?.Snapshot;
        CurrentPage = new ReviewViewModel(result, imageBytes, snapshot, OnReviewConfirmedAsync, ReturnToCaptureRoot);
        HeaderTitle = "Review scan";
    }

    /// <summary>
    /// "Add to my books" callback from the review screen: encrypts the confirmed
    /// <see cref="CapturedTransaction"/> with the active company's sync key and pushes it onto the
    /// desktop's capture queue via <see cref="_capturePushCoordinator"/>, records it in Capture's
    /// "Recent scans" list either way (push failure - no active company, offline, server error -
    /// doesn't lose the confirmed scan, it just shows as not-yet-sent), bumps the local monthly
    /// scan counter (<see cref="ScanUsageStore"/>), then returns to the Capture root.
    /// </summary>
    private async Task OnReviewConfirmedAsync(CapturedTransaction transaction)
    {
        // If this scan came from the offline queue, reuse its stable queue id as the ScanUid so a
        // lost push response can't later produce a duplicate (the desktop de-duplicates on ScanUid).
        var offlineId = _activeOfflineQueueId;
        if (offlineId != null)
        {
            transaction.ScanUid = offlineId;
        }

        var pushed = await _capturePushCoordinator.PushAsync(transaction, CancellationToken.None);
        await ScanUsageStore.IncrementAsync(_secureStore);

        _capture.AddRecentScan(transaction, pushed);
        await _capture.RefreshScanUsageAsync();

        if (offlineId != null && pushed)
        {
            // Consumed: drop it from the offline queue, then keep reviewing any that remain so the
            // user clears the whole backlog in one pass.
            await _pendingScanOutbox.RemoveAsync(offlineId);
            await _capture.RefreshPendingOfflineCountAsync();
            if (await StartNextOfflineReviewAsync())
            {
                return;
            }
        }

        // Push failed (offline item stays queued for another attempt) or nothing left to review.
        ReturnToCaptureRoot();
    }

    private void ReturnToCaptureRoot()
    {
        _activeOfflineQueueId = null;
        ResetToRoot(_capture, "Scan receipt", AppTab.Capture);
        _ = _capture.RefreshPendingOfflineCountAsync();
    }

    [RelayCommand]
    private void OpenCompanySwitcher()
    {
        _backStack.Push((CurrentPage, HeaderTitle, ActiveTab));
        CurrentPage = new CompanySwitcherViewModel(_pairedCompanyStore, SwitchCompanyFromSwitcherAsync, OpenPairingFlow);
        HeaderTitle = "Switch company";
        CanGoBack = true;
    }

    private async Task SwitchCompanyFromSwitcherAsync(string companyUid)
    {
        await SwitchCompanyAsync(companyUid);
        ResetToRoot(_dashboard, "Dashboard", AppTab.Home);
    }

    /// <summary>
    /// Sets <paramref name="companyUid"/> active and refreshes the snapshot, unless it's already
    /// the active company (see <see cref="CompanySwitchDecision"/>). Shared by the company
    /// switcher page and the Settings "Companies" section.
    /// </summary>
    public async Task SwitchCompanyAsync(string companyUid)
    {
        var active = await _pairedCompanyStore.GetActiveAsync();
        if (!CompanySwitchDecision.ShouldSwitch(active?.CompanyUid, companyUid))
        {
            return;
        }

        await _pairedCompanyStore.SetActiveAsync(companyUid);
        await RefreshCommand.ExecuteAsync(null);
    }

    /// <summary>Pushes the pairing flow (same screen used for the very first pairing) so the user
    /// can connect a second company. On success, refreshes and returns to the Dashboard.</summary>
    public void OpenPairingFlow()
    {
        var pairingViewModel = new PairingViewModel();
        pairingViewModel.Paired += companyLabel => Dispatcher.UIThread.Post(() => { _ = OnPairedAnotherAsync(); });

        _backStack.Push((CurrentPage, HeaderTitle, ActiveTab));
        CurrentPage = pairingViewModel;
        HeaderTitle = "Pair another company";
        CanGoBack = true;
    }

    private async Task OnPairedAnotherAsync()
    {
        await RefreshCommand.ExecuteAsync(null);
        ResetToRoot(_dashboard, "Dashboard", AppTab.Home);
    }

    /// <summary>All paired companies, for the Settings "Companies" section.</summary>
    public Task<List<PairedCompanyRecord>> GetCompaniesAsync() => _pairedCompanyStore.GetAllAsync();

    /// <summary>The active company's UID, or null if none is active.</summary>
    public async Task<string?> GetActiveCompanyUidAsync() => (await _pairedCompanyStore.GetActiveAsync())?.CompanyUid;

    /// <summary>
    /// Removes the active paired company (local removal only - the server-side device revoke is
    /// handled desktop-side). If another paired company remains, it becomes active and the
    /// snapshot refreshes. If none remain, raises <see cref="RequestPairing"/> so the host drops
    /// back to the full pairing screen. Returns true if the shell is still usable (a company is
    /// still active), false if the caller should stop touching this shell instance.
    /// </summary>
    public async Task<bool> UnpairActiveCompanyAsync()
    {
        var active = await _pairedCompanyStore.GetActiveAsync();
        if (active == null)
        {
            return true;
        }

        await _pairedCompanyStore.RemoveAsync(active.CompanyUid);

        var remaining = await _pairedCompanyStore.GetAllAsync();
        if (remaining.Count == 0)
        {
            RequestPairing?.Invoke();
            return false;
        }

        await _pairedCompanyStore.SetActiveAsync(remaining[0].CompanyUid);
        await RefreshCommand.ExecuteAsync(null);
        return true;
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
