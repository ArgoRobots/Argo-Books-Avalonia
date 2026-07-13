using System;
using System.Threading.Tasks;
using ArgoBooks.Mobile.Services;
using ArgoBooks.Shared.Mobile;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Mobile.ViewModels;

/// <summary>
/// The Capture tab's root screen: shows which paired company the scan will land in and a
/// best-effort local scan counter, then a shutter plus an "Import from photos" affordance. Both
/// buttons launch the same ML Kit DocumentScanner - its own UI exposes gallery import once
/// SetGalleryImportAllowed is set (see DocumentScanner's doc comment), so there's no separate
/// gallery-only entry point to call. A captured image hands off to ShellViewModel's
/// StartScanFlowAsync callback, which pushes the ScanningView and drives the AI call.
/// </summary>
public partial class CaptureViewModel : ViewModelBase
{
    private readonly ISecureStore _secureStore;
    private readonly Func<byte[], Task> _onImageCaptured;

    /// <summary>Set by ShellViewModel (via <see cref="SetActiveCompanyLabel"/>) whenever the
    /// active company changes, so the "Scanning into X" bar always reflects it.</summary>
    [ObservableProperty]
    private string _activeCompanyLabel = string.Empty;

    [ObservableProperty]
    private int _scansUsedThisMonth;

    [ObservableProperty]
    private bool _isBusy;

    public CaptureViewModel(ISecureStore secureStore, Func<byte[], Task> onImageCaptured)
    {
        _secureStore = secureStore ?? throw new ArgumentNullException(nameof(secureStore));
        _onImageCaptured = onImageCaptured ?? throw new ArgumentNullException(nameof(onImageCaptured));
        _ = RefreshScanUsageAsync();
    }

    /// <summary>Updates the "Scanning into X" label. Called by ShellViewModel after every
    /// snapshot refresh/company switch.</summary>
    public void SetActiveCompanyLabel(string label) => ActiveCompanyLabel = label;

    /// <summary>Reloads the local scan counter; called on every NavigateCapture so a scan
    /// recorded while this tab wasn't visible shows up immediately.</summary>
    public async Task RefreshScanUsageAsync() => ScansUsedThisMonth = await ScanUsageStore.GetCountAsync(_secureStore);

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

            await _onImageCaptured(imageBytes);
        }
        finally
        {
            IsBusy = false;
        }
    }
}
