using System.Runtime.InteropServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace ArgoBooks.Controls;

/// <summary>
/// Cross-platform invoice preview control.
/// On Windows: Uses WebView2 for high-quality HTML rendering.
/// On Mac/Linux: Shows a fallback message with option to view in browser.
/// </summary>
public partial class InvoicePreviewControl : UserControl
{
    /// <summary>
    /// Defines the Html property for binding HTML content.
    /// </summary>
    public static readonly StyledProperty<string?> HtmlProperty =
        AvaloniaProperty.Register<InvoicePreviewControl, string?>(nameof(Html));

    /// <summary>
    /// Defines the OpenInBrowserCommand property.
    /// </summary>
    public static readonly StyledProperty<ICommand?> OpenInBrowserCommandProperty =
        AvaloniaProperty.Register<InvoicePreviewControl, ICommand?>(nameof(OpenInBrowserCommand));

    /// <summary>
    /// Gets or sets the HTML content to display.
    /// </summary>
    public string? Html
    {
        get => GetValue(HtmlProperty);
        set => SetValue(HtmlProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when "Open in Browser" is clicked.
    /// </summary>
    public ICommand? OpenInBrowserCommand
    {
        get => GetValue(OpenInBrowserCommandProperty);
        set => SetValue(OpenInBrowserCommandProperty, value);
    }

    private Panel? _rootPanel;
    private Border? _fallbackPanel;
    private Border? _zoomToolbar;
    private TextBlock? _zoomPercentageText;
    private bool _isInitialized;

    private const double ZoomStep = 0.1;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 5.0;

#if WINDOWS
    private Microsoft.Web.WebView2.WinForms.WebView2? _webView;
    private WebView2Host? _webViewHost;
    private bool _isWebViewInitialized;
    private bool _isHandlingZoom;
#endif

    public InvoicePreviewControl()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_isInitialized)
            return;

        _rootPanel = this.FindControl<Panel>("RootPanel");
        _fallbackPanel = this.FindControl<Border>("FallbackPanel");
        _zoomToolbar = this.FindControl<Border>("ZoomToolbar");
        _zoomPercentageText = this.FindControl<TextBlock>("ZoomPercentageText");

        _isInitialized = true;

        // IMPORTANT: Do NOT create WebView here!
        // OnLoaded fires before bindings evaluate, so IsVisible may incorrectly be true.
        // NativeControlHost windows don't respect Avalonia visibility, so we must only
        // create the WebView when IsVisible explicitly changes to true.
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HtmlProperty && _isInitialized)
        {
            // If Html is being set and we're visible but WebView doesn't exist, create it.
            // This handles cases where the control is always visible (no IsVisible binding)
            // like in the template designer modal.
            EnsureWebViewCreatedIfVisible();
            UpdateWebViewContent();
        }

        // Handle visibility changes - NativeControlHost doesn't respect IsVisible,
        // so we must manually create/destroy the WebView when visibility changes.
        // We check for an explicit change from false to true to avoid the initial
        // binding evaluation race condition.
        if (change.Property == IsVisibleProperty && _isInitialized)
        {
            bool wasVisible = change.OldValue is bool oldVal && oldVal;
            bool isNowVisible = change.NewValue is bool newVal && newVal;

            if (isNowVisible && !wasVisible)
            {
                // Visibility changed from false to true - create WebView
                InitializePlatformPreview();
            }
            else if (!isNowVisible && wasVisible)
            {
                // Visibility changed from true to false - destroy WebView
                DestroyWebView();
            }
        }
    }

    private void InitializePlatformPreview()
    {
        if (OperatingSystem.IsWindows())
        {
            InitializeWindowsWebView();
        }
        else
        {
            ShowFallback();
        }
    }

    private void EnsureWebViewCreatedIfVisible()
    {
#if WINDOWS
        // Only create if we don't have a WebView yet and the control is effectively visible
        if (_webView == null && IsEffectivelyVisible && !string.IsNullOrEmpty(Html))
        {
            InitializePlatformPreview();
        }
#else
        // On non-Windows, ensure fallback is shown
        if (IsEffectivelyVisible)
        {
            ShowFallback();
        }
#endif
    }

    private void DestroyWebView()
    {
#if WINDOWS
        // Unsubscribe from events
        if (_webView != null)
        {
            _webView.ZoomFactorChanged -= OnZoomFactorChanged;
            _webView.MouseWheel -= OnWebViewMouseWheel;
        }

        if (_webViewHost != null && _rootPanel != null)
        {
            _rootPanel.Children.Remove(_webViewHost);
        }

        if (_webView != null)
        {
            _webView.Dispose();
            _webView = null;
            _webViewHost = null;
            _isWebViewInitialized = false;
        }

        // Hide zoom toolbar
        if (_zoomToolbar != null)
        {
            _zoomToolbar.IsVisible = false;
        }
#endif
    }

    private void ShowFallback()
    {
        if (_fallbackPanel != null)
        {
            _fallbackPanel.IsVisible = true;
        }
    }

    private void InitializeWindowsWebView()
    {
#if WINDOWS
        // Don't re-initialize if already exists
        if (_webView != null)
            return;

        try
        {
            _webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            _webViewHost = new WebView2Host(_webView);

            // Set the host to fill the panel
            _webViewHost.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch;
            _webViewHost.VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch;

            if (_rootPanel != null)
            {
                _rootPanel.Children.Insert(0, _webViewHost);
            }

            // Initialize WebView2 asynchronously
            InitializeWebView2Async();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to create WebView2: {ex.Message}");
            ShowFallback();
        }
#else
        ShowFallback();
#endif
    }

#if WINDOWS
    private async void InitializeWebView2Async()
    {
        try
        {
            if (_webView == null) return;

            await _webView.EnsureCoreWebView2Async();

            // Disable context menu and other browser features for clean preview
            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.Settings.IsZoomControlEnabled = false; // We handle zoom ourselves for zoom-to-cursor

                // Subscribe to zoom changes to update the percentage display
                _webView.ZoomFactorChanged += OnZoomFactorChanged;

                // Handle mouse wheel for zoom-to-cursor
                _webView.MouseWheel += OnWebViewMouseWheel;

                _isWebViewInitialized = true;

                // Show zoom toolbar
                if (_zoomToolbar != null)
                {
                    _zoomToolbar.IsVisible = true;
                }

                // Update zoom display
                UpdateZoomDisplay();

                // Set initial content if available
                UpdateWebViewContent();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView2 initialization failed: {ex.Message}");
            // Show fallback on UI thread
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(ShowFallback);
        }
    }

    private void OnZoomFactorChanged(object? sender, EventArgs e)
    {
        // Update zoom display on UI thread
        Avalonia.Threading.Dispatcher.UIThread.Post(UpdateZoomDisplay);
    }

    private void UpdateZoomDisplay()
    {
        if (_zoomPercentageText != null && _webView != null)
        {
            var percentage = (int)Math.Round(_webView.ZoomFactor * 100);
            _zoomPercentageText.Text = $"{percentage}%";
        }
    }

    private void OnWebViewMouseWheel(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        // Only zoom if Ctrl is pressed
        if ((System.Windows.Forms.Control.ModifierKeys & System.Windows.Forms.Keys.Control) == 0)
            return;

        if (_webView?.CoreWebView2 == null || _isHandlingZoom)
            return;

        _isHandlingZoom = true;

        try
        {
            // Calculate zoom direction
            double zoomDelta = e.Delta > 0 ? ZoomStep : -ZoomStep;
            double oldZoom = _webView.ZoomFactor;
            double newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, oldZoom + zoomDelta));

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                // Zoom to cursor position
                ZoomToPoint(newZoom, e.X, e.Y);
            }
        }
        finally
        {
            _isHandlingZoom = false;
        }
    }

    private async void ZoomToPoint(double newZoom, int pointX, int pointY)
    {
        if (_webView?.CoreWebView2 == null)
            return;

        try
        {
            double oldZoom = _webView.ZoomFactor;

            // JavaScript to get current scroll position and perform zoom-to-point
            var script = $@"
                (function() {{
                    var scrollX = window.scrollX || document.documentElement.scrollLeft;
                    var scrollY = window.scrollY || document.documentElement.scrollTop;

                    // Point in document coordinates (accounting for current zoom)
                    var docX = scrollX + {pointX} / {oldZoom.ToString(System.Globalization.CultureInfo.InvariantCulture)};
                    var docY = scrollY + {pointY} / {oldZoom.ToString(System.Globalization.CultureInfo.InvariantCulture)};

                    return JSON.stringify({{ scrollX: scrollX, scrollY: scrollY, docX: docX, docY: docY }});
                }})()";

            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);

            if (string.IsNullOrEmpty(result) || result == "null")
            {
                _webView.ZoomFactor = newZoom;
                return;
            }

            // Parse result
            var json = System.Text.Json.JsonDocument.Parse(result.Trim('"').Replace("\\\"", "\""));
            var docX = json.RootElement.GetProperty("docX").GetDouble();
            var docY = json.RootElement.GetProperty("docY").GetDouble();

            // Apply new zoom
            _webView.ZoomFactor = newZoom;

            // Calculate new scroll position to keep the point under cursor
            var newScrollX = docX - (pointX / newZoom);
            var newScrollY = docY - (pointY / newZoom);

            // Scroll to new position
            var scrollScript = $@"window.scrollTo({newScrollX.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {newScrollY.ToString(System.Globalization.CultureInfo.InvariantCulture)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(scrollScript);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ZoomToPoint failed: {ex.Message}");
            _webView.ZoomFactor = newZoom;
        }
    }

    private async void ZoomToCenter(double newZoom)
    {
        if (_webView?.CoreWebView2 == null)
            return;

        try
        {
            double oldZoom = _webView.ZoomFactor;

            // Get viewport center
            var viewportWidth = _webView.Width;
            var viewportHeight = _webView.Height;
            var centerX = viewportWidth / 2;
            var centerY = viewportHeight / 2;

            // JavaScript to get current scroll position
            var script = @"
                (function() {
                    var scrollX = window.scrollX || document.documentElement.scrollLeft;
                    var scrollY = window.scrollY || document.documentElement.scrollTop;
                    return JSON.stringify({ scrollX: scrollX, scrollY: scrollY });
                })()";

            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);

            if (string.IsNullOrEmpty(result) || result == "null")
            {
                _webView.ZoomFactor = newZoom;
                return;
            }

            // Parse result
            var json = System.Text.Json.JsonDocument.Parse(result.Trim('"').Replace("\\\"", "\""));
            var scrollX = json.RootElement.GetProperty("scrollX").GetDouble();
            var scrollY = json.RootElement.GetProperty("scrollY").GetDouble();

            // Calculate the document point at viewport center
            var docCenterX = scrollX + (centerX / oldZoom);
            var docCenterY = scrollY + (centerY / oldZoom);

            // Apply new zoom
            _webView.ZoomFactor = newZoom;

            // Calculate new scroll position to keep center in view
            var newScrollX = docCenterX - (centerX / newZoom);
            var newScrollY = docCenterY - (centerY / newZoom);

            // Scroll to new position
            var scrollScript = $@"window.scrollTo({newScrollX.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {newScrollY.ToString(System.Globalization.CultureInfo.InvariantCulture)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(scrollScript);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ZoomToCenter failed: {ex.Message}");
            _webView.ZoomFactor = newZoom;
        }
    }
#endif

    private void UpdateWebViewContent()
    {
#if WINDOWS
        if (_isWebViewInitialized && _webView?.CoreWebView2 != null && !string.IsNullOrEmpty(Html))
        {
            _webView.CoreWebView2.NavigateToString(Html);
        }
#endif
    }

    private void OpenInBrowserButton_Click(object? sender, RoutedEventArgs e)
    {
        OpenInBrowserCommand?.Execute(null);
    }

    private void ZoomIn_Click(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        if (_webView != null)
        {
            var newZoom = Math.Min(_webView.ZoomFactor + ZoomStep, MaxZoom);
            ZoomToCenter(newZoom);
        }
#endif
    }

    private void ZoomOut_Click(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        if (_webView != null)
        {
            var newZoom = Math.Max(_webView.ZoomFactor - ZoomStep, MinZoom);
            ZoomToCenter(newZoom);
        }
#endif
    }

    private void ResetZoom_Click(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        if (_webView != null)
        {
            _webView.ZoomFactor = 1.0;
        }
#endif
    }

    private async void FitToWindow_Click(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        if (_webView?.CoreWebView2 == null || _rootPanel == null)
            return;

        try
        {
            // Get the document dimensions via JavaScript
            var result = await _webView.CoreWebView2.ExecuteScriptAsync(
                "JSON.stringify({ width: document.documentElement.scrollWidth, height: document.documentElement.scrollHeight })");

            if (string.IsNullOrEmpty(result) || result == "null")
                return;

            // Parse the JSON result (it comes back as a JSON string)
            var json = System.Text.Json.JsonDocument.Parse(result.Trim('"').Replace("\\\"", "\""));
            var docWidth = json.RootElement.GetProperty("width").GetDouble();
            var docHeight = json.RootElement.GetProperty("height").GetDouble();

            if (docWidth <= 0 || docHeight <= 0)
                return;

            // Get the available viewport size
            var viewportWidth = _rootPanel.Bounds.Width;
            var viewportHeight = _rootPanel.Bounds.Height;

            if (viewportWidth <= 0 || viewportHeight <= 0)
                return;

            // Calculate the zoom factor to fit the content
            var zoomX = viewportWidth / docWidth;
            var zoomY = viewportHeight / docHeight;
            var fitZoom = Math.Min(zoomX, zoomY);

            // Clamp to valid zoom range
            fitZoom = Math.Max(MinZoom, Math.Min(MaxZoom, fitZoom));

            _webView.ZoomFactor = fitZoom;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fit to window failed: {ex.Message}");
            // Fallback to reset zoom
            _webView.ZoomFactor = 1.0;
        }
#endif
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        DestroyWebView();
    }
}

#if WINDOWS
/// <summary>
/// NativeControlHost that wraps a WebView2 control for embedding in Avalonia.
/// This allows the Windows WebView2 control to be rendered within the Avalonia visual tree.
/// </summary>
internal class WebView2Host : NativeControlHost
{
    private readonly Microsoft.Web.WebView2.WinForms.WebView2 _webView;

    public WebView2Host(Microsoft.Web.WebView2.WinForms.WebView2 webView)
    {
        _webView = webView;
    }

    protected override IPlatformHandle CreateNativeControlCore(IPlatformHandle parent)
    {
        // Ensure the WinForms control is created
        if (!_webView.IsHandleCreated)
        {
            _webView.CreateControl();
        }

        // Set the parent window handle for proper embedding
        if (parent.Handle != IntPtr.Zero)
        {
            SetParent(_webView.Handle, parent.Handle);
        }

        return new WinPlatformHandle(_webView.Handle, "HWND");
    }

    protected override void DestroyNativeControlCore(IPlatformHandle control)
    {
        // WebView2 disposal is handled by the parent InvoicePreviewControl
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);
}

/// <summary>
/// Simple platform handle implementation for Windows HWND.
/// </summary>
internal class WinPlatformHandle : IPlatformHandle
{
    public WinPlatformHandle(IntPtr handle, string descriptor)
    {
        Handle = handle;
        HandleDescriptor = descriptor;
    }

    public IntPtr Handle { get; }
    public string HandleDescriptor { get; }
}
#endif
