using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Controls.Reports;

/// <summary>
/// Control for displaying a preview of the generated report.
/// Supports zooming, paging, and refreshing.
/// </summary>
public partial class ReportPreviewControl : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<ReportConfiguration?> ConfigurationProperty =
        AvaloniaProperty.Register<ReportPreviewControl, ReportConfiguration?>(nameof(Configuration));

    public static readonly StyledProperty<double> ZoomLevelProperty =
        AvaloniaProperty.Register<ReportPreviewControl, double>(nameof(ZoomLevel), 1.0);

    public static readonly StyledProperty<int> CurrentPageProperty =
        AvaloniaProperty.Register<ReportPreviewControl, int>(nameof(CurrentPage), 1);

    public static readonly StyledProperty<int> TotalPagesProperty =
        AvaloniaProperty.Register<ReportPreviewControl, int>(nameof(TotalPages), 1);

    public static readonly StyledProperty<bool> IsLoadingProperty =
        AvaloniaProperty.Register<ReportPreviewControl, bool>(nameof(IsLoading));

    public static readonly StyledProperty<bool> HasErrorProperty =
        AvaloniaProperty.Register<ReportPreviewControl, bool>(nameof(HasError));

    public static readonly StyledProperty<string?> ErrorMessageProperty =
        AvaloniaProperty.Register<ReportPreviewControl, string?>(nameof(ErrorMessage));

    #endregion

    #region Properties

    /// <summary>
    /// The report configuration to preview.
    /// </summary>
    public ReportConfiguration? Configuration
    {
        get => GetValue(ConfigurationProperty);
        set => SetValue(ConfigurationProperty, value);
    }

    /// <summary>
    /// Current zoom level (1.0 = 100%).
    /// </summary>
    public double ZoomLevel
    {
        get => GetValue(ZoomLevelProperty);
        set => SetValue(ZoomLevelProperty, Math.Clamp(value, 0.25, 4.0));
    }

    /// <summary>
    /// Current page number (1-based).
    /// </summary>
    public int CurrentPage
    {
        get => GetValue(CurrentPageProperty);
        set => SetValue(CurrentPageProperty, Math.Clamp(value, 1, TotalPages));
    }

    /// <summary>
    /// Total number of pages.
    /// </summary>
    public int TotalPages
    {
        get => GetValue(TotalPagesProperty);
        set => SetValue(TotalPagesProperty, Math.Max(1, value));
    }

    /// <summary>
    /// Whether a preview is being generated.
    /// </summary>
    public bool IsLoading
    {
        get => GetValue(IsLoadingProperty);
        set => SetValue(IsLoadingProperty, value);
    }

    /// <summary>
    /// Whether an error occurred during preview generation.
    /// </summary>
    public bool HasError
    {
        get => GetValue(HasErrorProperty);
        set => SetValue(HasErrorProperty, value);
    }

    /// <summary>
    /// Error message if preview generation failed.
    /// </summary>
    public string? ErrorMessage
    {
        get => GetValue(ErrorMessageProperty);
        set => SetValue(ErrorMessageProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the preview needs to be refreshed.
    /// </summary>
    public event EventHandler? RefreshRequested;

    #endregion

    #region Private Fields

    private Image? _previewImage;
    private Border? _pageBorder;
    private Border? _zoomContainer;
    private Border? _loadingOverlay;
    private Border? _errorOverlay;
    private TextBlock? _errorText;
    private TextBlock? _totalPagesText;
    private NumericUpDown? _pageNumberInput;
    private ComboBox? _zoomComboBox;
    private ScrollViewer? _previewScrollViewer;

    private Bitmap? _currentBitmap;

    private double _pageWidth;
    private double _pageHeight;

    private bool _pendingZoomToFit;

    #endregion

    public ReportPreviewControl()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _previewImage = this.FindControl<Image>("PreviewImage");
        _pageBorder = this.FindControl<Border>("PageBorder");
        _zoomContainer = this.FindControl<Border>("ZoomContainer");
        _loadingOverlay = this.FindControl<Border>("LoadingOverlay");
        _errorOverlay = this.FindControl<Border>("ErrorOverlay");
        _errorText = this.FindControl<TextBlock>("ErrorText");
        _totalPagesText = this.FindControl<TextBlock>("TotalPagesText");
        _pageNumberInput = this.FindControl<NumericUpDown>("PageNumberInput");
        _zoomComboBox = this.FindControl<ComboBox>("ZoomComboBox");
        _previewScrollViewer = this.FindControl<ScrollViewer>("PreviewScrollViewer");

        // Subscribe to layout updated to handle deferred zoom
        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.LayoutUpdated += OnScrollViewerLayoutUpdated;
        }
    }

    private void OnScrollViewerLayoutUpdated(object? sender, EventArgs e)
    {
        if (_pendingZoomToFit && _previewScrollViewer != null &&
            _previewScrollViewer.Bounds.Width > 0 && _previewScrollViewer.Bounds.Height > 0)
        {
            _pendingZoomToFit = false;
            ZoomToFitPage();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ConfigurationProperty)
        {
            OnConfigurationChanged();
        }
        else if (change.Property == ZoomLevelProperty)
        {
            ApplyZoom();
        }
        else if (change.Property == CurrentPageProperty)
        {
            UpdatePageDisplay();
        }
        else if (change.Property == TotalPagesProperty)
        {
            UpdatePageInfo();
        }
        else if (change.Property == IsLoadingProperty)
        {
            UpdateLoadingState();
        }
        else if (change.Property == HasErrorProperty || change.Property == ErrorMessageProperty)
        {
            UpdateErrorState();
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (Configuration != null)
        {
            GeneratePreviewAsync();
        }
    }

    #region Configuration Handling

    private void OnConfigurationChanged()
    {
        if (Configuration == null)
        {
            ClearPreview();
            return;
        }

        // Update page dimensions
        var (width, height) = PageDimensions.GetDimensions(
            Configuration.PageSize,
            Configuration.PageOrientation);
        _pageWidth = width;
        _pageHeight = height;

        UpdatePageSize();
        GeneratePreviewAsync();
    }

    private void UpdatePageSize()
    {
        if (_pageBorder == null) return;

        _pageBorder.Width = _pageWidth;
        _pageBorder.Height = _pageHeight;
    }

    #endregion

    #region Preview Generation

    /// <summary>
    /// Generates the preview asynchronously.
    /// </summary>
    public async void GeneratePreviewAsync()
    {
        if (Configuration == null) return;

        IsLoading = true;
        HasError = false;
        ErrorMessage = null;

        try
        {
            var config = Configuration;
            var width = (int)_pageWidth;
            var height = (int)_pageHeight;

            await Task.Run(() =>
            {
                // Generate preview bitmap using SkiaSharp
                using var renderer = new ReportRenderer(config!, null);
                using var skBitmap = renderer.CreatePreview(width, height);

                // Convert SKBitmap to Avalonia Bitmap
                using var stream = new System.IO.MemoryStream();
                using var image = SkiaSharp.SKImage.FromBitmap(skBitmap);
                using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
                data.SaveTo(stream);
                stream.Position = 0;

                var avaloniaBitmap = new Bitmap(stream);

                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    SetPreviewBitmap(avaloniaBitmap);
                    IsLoading = false;
                    // Fit to page after preview is generated
                    ZoomToFitPage();
                });
            });
        }
        catch (Exception ex)
        {
            HasError = true;
            ErrorMessage = ex.Message;
            IsLoading = false;
        }
    }

    private void SetPreviewBitmap(Bitmap? bitmap)
    {
        // Dispose of previous bitmap
        _currentBitmap?.Dispose();
        _currentBitmap = bitmap;

        if (_previewImage != null)
        {
            _previewImage.Source = bitmap;
        }
    }

    private void ClearPreview()
    {
        _currentBitmap?.Dispose();
        _currentBitmap = null;

        if (_previewImage != null)
        {
            _previewImage.Source = null;
        }
    }

    #endregion

    #region Zoom

    private void ApplyZoom()
    {
        if (_zoomContainer == null) return;

        _zoomContainer.RenderTransform = new ScaleTransform(ZoomLevel, ZoomLevel);
        _zoomContainer.RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative);

        UpdateZoomComboBox();
    }

    private void UpdateZoomComboBox()
    {
        if (_zoomComboBox == null) return;

        // Update selection based on zoom level
        var index = ZoomLevel switch
        {
            0.5 => 0,
            0.75 => 1,
            1.0 => 2,
            1.5 => 5,
            2.0 => 6,
            _ => -1
        };

        if (index >= 0)
        {
            _zoomComboBox.SelectedIndex = index;
        }
    }

    private void OnZoomInClick(object? sender, RoutedEventArgs e)
    {
        ZoomLevel = Math.Min(ZoomLevel + 0.25, 4.0);
    }

    private void OnZoomOutClick(object? sender, RoutedEventArgs e)
    {
        ZoomLevel = Math.Max(ZoomLevel - 0.25, 0.25);
    }

    public void OnZoomSelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        if (_zoomComboBox?.SelectedIndex is not int index) return;

        switch (index)
        {
            case 0: ZoomLevel = 0.5; break;
            case 1: ZoomLevel = 0.75; break;
            case 2: ZoomLevel = 1.0; break;
            case 3: ZoomToFitWidth(); break;
            case 4: ZoomToFitPage(); break;
            case 5: ZoomLevel = 1.5; break;
            case 6: ZoomLevel = 2.0; break;
        }
    }

    /// <summary>
    /// Zooms to fit the page width in the viewport.
    /// </summary>
    public void ZoomToFitWidth()
    {
        if (_previewScrollViewer == null || _pageWidth <= 0) return;

        var viewportWidth = _previewScrollViewer.Bounds.Width - 80; // Account for padding
        ZoomLevel = viewportWidth / _pageWidth;
    }

    /// <summary>
    /// Zooms to fit the entire page in the viewport.
    /// </summary>
    public void ZoomToFitPage()
    {
        if (_previewScrollViewer == null || _pageWidth <= 0 || _pageHeight <= 0) return;

        var viewportWidth = _previewScrollViewer.Bounds.Width - 80;
        var viewportHeight = _previewScrollViewer.Bounds.Height - 80;

        // If bounds aren't ready yet, defer the zoom
        if (viewportWidth <= 0 || viewportHeight <= 0)
        {
            _pendingZoomToFit = true;
            return;
        }

        var scaleX = viewportWidth / _pageWidth;
        var scaleY = viewportHeight / _pageHeight;

        ZoomLevel = Math.Min(scaleX, scaleY);
    }

    #endregion

    #region Paging

    private void OnPreviousPageClick(object? sender, RoutedEventArgs e)
    {
        if (CurrentPage > 1)
        {
            CurrentPage--;
        }
    }

    private void OnNextPageClick(object? sender, RoutedEventArgs e)
    {
        if (CurrentPage < TotalPages)
        {
            CurrentPage++;
        }
    }

    private void OnPageNumberChanged(object? sender, NumericUpDownValueChangedEventArgs e)
    {
        if (e.NewValue.HasValue)
        {
            CurrentPage = (int)e.NewValue.Value;
        }
    }

    private void UpdatePageDisplay()
    {
        if (_pageNumberInput != null)
        {
            _pageNumberInput.Value = CurrentPage;
        }

        // In the future, this would render a different page
        // For now, we only support single-page reports
    }

    private void UpdatePageInfo()
    {
        if (_totalPagesText != null)
        {
            _totalPagesText.Text = $"of {TotalPages}";
        }

        if (_pageNumberInput != null)
        {
            _pageNumberInput.Maximum = TotalPages;
        }
    }

    #endregion

    #region State Management

    private void UpdateLoadingState()
    {
        if (_loadingOverlay != null)
        {
            _loadingOverlay.IsVisible = IsLoading;
        }

        if (_errorOverlay != null && IsLoading)
        {
            _errorOverlay.IsVisible = false;
        }
    }

    private void UpdateErrorState()
    {
        if (_errorOverlay != null)
        {
            _errorOverlay.IsVisible = HasError && !IsLoading;
        }

        if (_errorText != null)
        {
            _errorText.Text = ErrorMessage ?? "Failed to generate preview";
        }
    }

    #endregion

    #region Event Handlers

    private void OnRefreshClick(object? sender, RoutedEventArgs e)
    {
        RefreshRequested?.Invoke(this, EventArgs.Empty);
        GeneratePreviewAsync();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Refreshes the preview.
    /// </summary>
    public void Refresh()
    {
        GeneratePreviewAsync();
    }

    /// <summary>
    /// Gets the current preview bitmap.
    /// </summary>
    public Bitmap? GetPreviewBitmap()
    {
        return _currentBitmap;
    }

    #endregion

    #region Cleanup

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        ClearPreview();

        // Unsubscribe from layout updated
        if (_previewScrollViewer != null)
        {
            _previewScrollViewer.LayoutUpdated -= OnScrollViewerLayoutUpdated;
        }
    }

    #endregion
}
