using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using ArgoBooks.Core.Services.Sync;
using ArgoBooks.Mobile.Services;
using ArgoBooks.Shared.Mobile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Networking;

namespace ArgoBooks.Mobile.ViewModels;

/// <summary>
/// The Capture tab's root screen: shows which paired company the scan will land in and a
/// best-effort local scan counter, then a shutter plus an "Import from photos" affordance. Both
/// buttons launch the same ML Kit DocumentScanner - its own UI exposes gallery import once
/// SetGalleryImportAllowed is set (see DocumentScanner's doc comment), so there's no separate
/// gallery-only entry point to call. A captured image hands off to ShellViewModel's
/// StartScanFlowAsync callback, which pushes the ScanningView and drives the AI call.
/// Also owns the "Recent scans" list (see <see cref="AddRecentScan"/>): once a scan is confirmed
/// on the review screen and Task 5's CapturePushCoordinator has (or hasn't) pushed it to the
/// desktop queue, ShellViewModel records it here so the user has some visible confirmation - the
/// phone has no ledger of its own and can't poll for the desktop's ingest, so this is local-only
/// and never reconciled against what actually landed on the desktop.
/// Task 6 edge states: once the local free-scan counter (<see cref="ScanUsageStore"/>/
/// <see cref="ScanQuota"/>) reaches the monthly limit, <see cref="IsOverLimit"/> flips and the view
/// swaps to an upgrade prompt (<see cref="UpgradeCommand"/> opens the marketing site in the system
/// browser - no in-app purchase). If a capture happens with no network, the cropped image is
/// queued in <see cref="PendingScanOutbox"/> instead of starting the AI scan flow; ShellViewModel's
/// DrainPendingScansAsync re-runs the scan for anything still queued once connectivity returns.
/// </summary>
public partial class CaptureViewModel : ViewModelBase
{
    private static readonly Uri UpgradeUri = new("https://argorobots.com");

    private readonly ISecureStore _secureStore;
    private readonly Func<byte[], Task> _onImageCaptured;
    private readonly PendingScanOutbox _pendingScanOutbox;

    /// <summary>Set by ShellViewModel (via <see cref="SetActiveCompanyLabel"/>) whenever the
    /// active company changes, so the "Scanning into X" bar always reflects it.</summary>
    [ObservableProperty]
    private string _activeCompanyLabel = string.Empty;

    [ObservableProperty]
    private int _scansUsedThisMonth;

    /// <summary>True once <see cref="ScansUsedThisMonth"/> has reached the free monthly limit
    /// (<see cref="ScanQuota.FreeMonthlyLimit"/>). Swaps the Capture screen from the viewfinder to
    /// the "used your N free scans" upgrade prompt.</summary>
    [ObservableProperty]
    private bool _isOverLimit;

    /// <summary>The free-tier monthly scan limit, for the upgrade prompt's message.</summary>
    public int FreeScanLimit => ScanQuota.FreeMonthlyLimit;

    [ObservableProperty]
    private bool _isBusy;

    /// <summary>Most recent scan first. Populated by <see cref="AddRecentScan"/>.</summary>
    public ObservableCollection<RecentScanViewModel> RecentScans { get; } = new();

    /// <summary>True once at least one scan has been confirmed this session, so the "Recent
    /// scans" section only shows up once there's something to show.</summary>
    public bool HasRecentScans => RecentScans.Count > 0;

    [ObservableProperty]
    private bool _isConfirmationVisible;

    [ObservableProperty]
    private string _confirmationMessage = string.Empty;

    public CaptureViewModel(ISecureStore secureStore, Func<byte[], Task> onImageCaptured, PendingScanOutbox pendingScanOutbox)
    {
        _secureStore = secureStore ?? throw new ArgumentNullException(nameof(secureStore));
        _onImageCaptured = onImageCaptured ?? throw new ArgumentNullException(nameof(onImageCaptured));
        _pendingScanOutbox = pendingScanOutbox ?? throw new ArgumentNullException(nameof(pendingScanOutbox));
        _ = RefreshScanUsageAsync();
    }

    /// <summary>Updates the "Scanning into X" label. Called by ShellViewModel after every
    /// snapshot refresh/company switch.</summary>
    public void SetActiveCompanyLabel(string label) => ActiveCompanyLabel = label;

    /// <summary>Reloads the local scan counter (and <see cref="IsOverLimit"/> with it); called on
    /// every NavigateCapture so a scan recorded while this tab wasn't visible shows up
    /// immediately.</summary>
    public async Task RefreshScanUsageAsync()
    {
        ScansUsedThisMonth = await ScanUsageStore.GetCountAsync(_secureStore);
        IsOverLimit = ScanQuota.IsOverLimit(ScansUsedThisMonth);
    }

    /// <summary>"Upgrade on the web" button on the over-limit prompt: opens the marketing site in
    /// the system browser. No in-app purchase flow exists on the phone.</summary>
    [RelayCommand]
    private async Task UpgradeAsync()
    {
        try
        {
            await Browser.Default.OpenAsync(UpgradeUri, BrowserLaunchMode.SystemPreferred);
        }
        catch (Exception)
        {
            // No browser available, or the intent couldn't be resolved - not worth surfacing an
            // error for; the user can still find the upgrade link themselves on the web.
        }
    }

    /// <summary>
    /// Called by ShellViewModel.OnReviewConfirmedAsync right after CapturePushCoordinator has
    /// tried to push the confirmed <paramref name="transaction"/>. Adds a "Recent scans" row and
    /// shows a brief confirmation banner - "sent to your desktop" if the push succeeded, or a
    /// softer "saved, will sync once connected" message if it didn't (no active company, offline,
    /// server error), since the scan itself was still captured and the review data isn't lost.
    /// </summary>
    public void AddRecentScan(CapturedTransaction transaction, bool pushed)
    {
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));

        var vendor = string.IsNullOrWhiteSpace(transaction.SupplierOrCustomer)
            ? "Unnamed scan"
            : transaction.SupplierOrCustomer;
        var amountText = transaction.Total.ToString("C", CultureInfo.CurrentCulture);
        var timeText = DateTime.Now.ToString("h:mm tt", CultureInfo.InvariantCulture);
        var statusText = pushed ? "Sent to your desktop" : "Saved - will sync once connected";

        RecentScans.Insert(0, new RecentScanViewModel(vendor, amountText, timeText, statusText));
        OnPropertyChanged(nameof(HasRecentScans));

        ConfirmationMessage = pushed ? "Added and sent to your desktop" : "Added - will sync to your desktop once connected";
        IsConfirmationVisible = true;
        _ = HideConfirmationAfterDelayAsync();
    }

    private async Task HideConfirmationAfterDelayAsync()
    {
        await Task.Delay(TimeSpan.FromSeconds(3));
        IsConfirmationVisible = false;
    }

    /// <summary>
    /// Shared by both the shutter and "Import from photos" buttons: DocumentScanner's own UI
    /// offers gallery import once SetGalleryImportAllowed is set (see DocumentScanner's doc
    /// comment), so there's no separate gallery-only code path. If there's no network once the
    /// image is cropped, the AI scan can't run yet - the cropped bytes are queued in
    /// <see cref="PendingScanOutbox"/> instead of starting the Scanning screen, and a toast tells
    /// the user it'll scan once they're back online (ShellViewModel drains the queue on the next
    /// connectivity/foreground check).
    /// </summary>
    [RelayCommand]
    private async Task ScanAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            var imageBytes = await DocumentScanner.ScanAsync();
            if (imageBytes == null || imageBytes.Length == 0)
            {
                // User cancelled, or the scanner/Play Services module isn't available - stay put.
                return;
            }

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                await _pendingScanOutbox.EnqueueAsync(imageBytes);
                ShowOfflineQueuedMessage();
                return;
            }

            await _onImageCaptured(imageBytes);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ShowOfflineQueuedMessage()
    {
        ConfirmationMessage = "Saved - will scan when you're back online";
        IsConfirmationVisible = true;
        _ = HideConfirmationAfterDelayAsync();
    }
}
