namespace ArgoBooks.Mobile.ViewModels;

/// <summary>
/// One row in the Capture tab's "Recent scans" list (see <see cref="CaptureViewModel.RecentScans"/>).
/// Immutable display snapshot of a confirmed scan - vendor/customer, amount, when it was confirmed,
/// and whether Task 5's CapturePushCoordinator managed to push it to the desktop queue.
/// </summary>
public sealed class RecentScanViewModel
{
    public string VendorText { get; }

    public string AmountText { get; }

    public string TimeText { get; }

    public string StatusText { get; }

    public RecentScanViewModel(string vendorText, string amountText, string timeText, string statusText)
    {
        VendorText = vendorText;
        AmountText = amountText;
        TimeText = timeText;
        StatusText = statusText;
    }
}
