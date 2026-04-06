// ArgoBooks/ViewModels/BulkScanItem.cs
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Status of a single receipt in the bulk scan pipeline.
/// </summary>
public enum BulkScanStatus
{
    Queued,
    Scanning,
    Succeeded,
    Failed
}

/// <summary>
/// Tracks the state of a single receipt through bulk scanning: file data, scan status,
/// result, approval state, and preview image path.
/// </summary>
public partial class BulkScanItem : ObservableObject
{
    /// <summary>
    /// Original file path selected by the user.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Display file name (e.g., "receipt1.jpg").
    /// </summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>
    /// Raw file bytes read from disk.
    /// </summary>
    public byte[]? FileData { get; set; }

    /// <summary>
    /// Preprocessed image bytes (EXIF-fixed, contrast-boosted) ready for API.
    /// For PDFs this is the raw PDF bytes unchanged.
    /// </summary>
    public byte[]? PreprocessedData { get; set; }

    /// <summary>
    /// The file name to send to the scanner (may be changed to .jpg after preprocessing).
    /// </summary>
    public string ScanFileName { get; set; } = string.Empty;

    /// <summary>
    /// Current scan status.
    /// </summary>
    [ObservableProperty]
    private BulkScanStatus _status = BulkScanStatus.Queued;

    /// <summary>
    /// Scan result if status is Succeeded.
    /// </summary>
    public ReceiptScanResult? ScanResult { get; set; }

    /// <summary>
    /// Error message if status is Failed.
    /// </summary>
    [ObservableProperty]
    private string _errorMessage = string.Empty;

    /// <summary>
    /// Path to preview image on disk (JPEG for both images and PDFs).
    /// </summary>
    [ObservableProperty]
    private string? _previewImagePath;

    /// <summary>
    /// Whether the user has approved this receipt for transaction creation.
    /// </summary>
    [ObservableProperty]
    private bool _isApproved;

    /// <summary>
    /// Whether this item is currently being viewed in the carousel.
    /// </summary>
    [ObservableProperty]
    private bool _isActive;

    /// <summary>
    /// Whether the user has reviewed this item (approved or skipped past it).
    /// </summary>
    [ObservableProperty]
    private bool _isReviewed;

    /// <summary>
    /// Whether the user explicitly skipped this item.
    /// </summary>
    [ObservableProperty]
    private bool _isSkipped;

    /// <summary>
    /// Quick summary text shown on progress screen and carousel thumbnail.
    /// e.g., "Costco · $142.57"
    /// </summary>
    [ObservableProperty]
    private string _summaryText = string.Empty;

    /// <summary>
    /// Confidence level text for display.
    /// </summary>
    [ObservableProperty]
    private string _confidenceText = string.Empty;

    /// <summary>
    /// Whether this is a PDF file.
    /// </summary>
    public bool IsPdf => Path.GetExtension(FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase);

    // Computed status booleans for UI visibility bindings
    public bool IsQueued => Status == BulkScanStatus.Queued;
    public bool IsScanning => Status == BulkScanStatus.Scanning;
    public bool IsSucceeded => Status == BulkScanStatus.Succeeded;
    public bool IsFailed => Status == BulkScanStatus.Failed;

    partial void OnStatusChanged(BulkScanStatus value)
    {
        OnPropertyChanged(nameof(IsQueued));
        OnPropertyChanged(nameof(IsScanning));
        OnPropertyChanged(nameof(IsSucceeded));
        OnPropertyChanged(nameof(IsFailed));
    }
}
