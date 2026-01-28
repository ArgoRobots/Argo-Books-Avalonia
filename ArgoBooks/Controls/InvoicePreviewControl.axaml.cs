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
            if (_webView.CoreWebView2 != null)
            {
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }
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
        catch (Exception)
        {
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

                // Subscribe to messages from JavaScript for zoom-to-cursor
                _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

                _isWebViewInitialized = true;

                // Show zoom toolbar
                if (_zoomToolbar != null)
                {
                    _zoomToolbar.IsVisible = true;
                }

                // Set initial zoom to 0.98 to prevent horizontal scrollbar
                _webView.ZoomFactor = 0.98;
                UpdateZoomDisplay();

                // Set initial content if available
                UpdateWebViewContent();
            }
        }
        catch (Exception)
        {
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
            var zoom = _webView.ZoomFactor;
            // Show 100% when at default zoom (0.98) to hide the slight adjustment
            var percentage = Math.Abs(zoom - 0.98) < 0.001 ? 100 : (int)Math.Round(zoom * 100);
            _zoomPercentageText.Text = $"{percentage}%";
        }
    }

    private void OnWebMessageReceived(object? sender, Microsoft.Web.WebView2.Core.CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.WebMessageAsJson;
            var json = System.Text.Json.JsonDocument.Parse(message);
            var root = json.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var messageType = typeElement.GetString();

            // Only zoom messages need C# handling - pan is handled entirely in JavaScript
            if (messageType == "zoom")
            {
                HandleZoomMessage(root);
            }
        }
        catch (Exception)
        {
            // Ignore malformed messages
        }
    }

    private void HandleZoomMessage(System.Text.Json.JsonElement root)
    {
        if (_webView?.CoreWebView2 == null || _isHandlingZoom)
            return;

        _isHandlingZoom = true;

        try
        {
            var deltaY = root.GetProperty("deltaY").GetDouble();
            var clientX = root.GetProperty("clientX").GetInt32();
            var clientY = root.GetProperty("clientY").GetInt32();
            var scrollX = root.GetProperty("scrollX").GetDouble();
            var scrollY = root.GetProperty("scrollY").GetDouble();

            // Calculate zoom direction (negative deltaY = zoom in)
            double zoomDelta = deltaY < 0 ? ZoomStep : -ZoomStep;
            double oldZoom = _webView.ZoomFactor;
            double newZoom = Math.Max(MinZoom, Math.Min(MaxZoom, oldZoom + zoomDelta));

            if (Math.Abs(newZoom - oldZoom) > 0.001)
            {
                // Calculate document position under cursor
                double docX = scrollX + (clientX / oldZoom);
                double docY = scrollY + (clientY / oldZoom);

                // Apply new zoom
                _webView.ZoomFactor = newZoom;

                // Calculate new scroll position to keep the point under cursor
                double newScrollX = docX - (clientX / newZoom);
                double newScrollY = docY - (clientY / newZoom);

                // Scroll to new position
                var scrollScript = $@"window.scrollTo({newScrollX.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {newScrollY.ToString(System.Globalization.CultureInfo.InvariantCulture)});";
                _ = _webView.CoreWebView2.ExecuteScriptAsync(scrollScript);
            }
        }
        finally
        {
            _isHandlingZoom = false;
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
            var centerX = viewportWidth / 2.0;
            var centerY = viewportHeight / 2.0;

            // JavaScript to get current scroll position
            var script = @"(function() {
                var scrollX = window.scrollX || document.documentElement.scrollLeft || 0;
                var scrollY = window.scrollY || document.documentElement.scrollTop || 0;
                return [scrollX, scrollY];
            })()";

            var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);

            if (string.IsNullOrEmpty(result) || result == "null")
            {
                _webView.ZoomFactor = newZoom;
                UpdateZoomDisplay();
                return;
            }

            var scrollValues = System.Text.Json.JsonSerializer.Deserialize<double[]>(result);
            if (scrollValues == null || scrollValues.Length < 2)
            {
                _webView.ZoomFactor = newZoom;
                UpdateZoomDisplay();
                return;
            }

            double scrollX = scrollValues[0];
            double scrollY = scrollValues[1];

            // Calculate the document point at viewport center
            var docCenterX = scrollX + (centerX / oldZoom);
            var docCenterY = scrollY + (centerY / oldZoom);

            // Apply new zoom
            _webView.ZoomFactor = newZoom;
            UpdateZoomDisplay();

            // Calculate new scroll position to keep center in view
            var newScrollX = docCenterX - (centerX / newZoom);
            var newScrollY = docCenterY - (centerY / newZoom);

            // Scroll to new position
            var scrollScript = $@"window.scrollTo({newScrollX.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {newScrollY.ToString(System.Globalization.CultureInfo.InvariantCulture)});";
            await _webView.CoreWebView2.ExecuteScriptAsync(scrollScript);
        }
        catch (Exception)
        {
            _webView.ZoomFactor = newZoom;
            UpdateZoomDisplay();
        }
    }
#endif

    private void UpdateWebViewContent()
    {
#if WINDOWS
        if (_isWebViewInitialized && _webView?.CoreWebView2 != null && !string.IsNullOrEmpty(Html))
        {
            // Inject styles and scripts for zoom and pan handling
            var interactionScript = @"
<script>
(function() {
    if (window.__interactionHandlersInstalled) return;
    window.__interactionHandlersInstalled = true;

    // Zoom handling (Ctrl+Scroll) - needs C# for ZoomFactor
    document.addEventListener('wheel', function(e) {
        if (e.ctrlKey) {
            e.preventDefault();
            window.chrome.webview.postMessage({
                type: 'zoom',
                deltaY: e.deltaY,
                clientX: e.clientX,
                clientY: e.clientY,
                scrollX: window.scrollX || document.documentElement.scrollLeft || 0,
                scrollY: window.scrollY || document.documentElement.scrollTop || 0
            });
        }
    }, { passive: false });

    // Pan handling (Right-click drag) - handled entirely in JavaScript
    var isPanning = false;
    var startX = 0;
    var startY = 0;
    var startScrollX = 0;
    var startScrollY = 0;
    var overscrollX = 0;
    var overscrollY = 0;
    var resistance = 0.3;
    var maxOverscroll = 80;

    // Create a wrapper for rubber band effect if needed
    var wrapper = document.getElementById('__panWrapper');
    if (!wrapper) {
        wrapper = document.createElement('div');
        wrapper.id = '__panWrapper';
        wrapper.style.cssText = 'min-height: 100%; will-change: transform;';
        while (document.body.firstChild) {
            wrapper.appendChild(document.body.firstChild);
        }
        document.body.appendChild(wrapper);
        document.body.style.overflow = 'auto';
    }

    document.addEventListener('mousedown', function(e) {
        if (e.button === 2) {
            isPanning = true;
            startX = e.clientX;
            startY = e.clientY;
            startScrollX = window.scrollX || 0;
            startScrollY = window.scrollY || 0;
            overscrollX = 0;
            overscrollY = 0;
            wrapper.style.transition = 'none';
            document.body.style.cursor = 'grabbing';
            document.body.style.userSelect = 'none';
            e.preventDefault();
        }
    });

    document.addEventListener('mousemove', function(e) {
        if (!isPanning) return;

        var deltaX = e.clientX - startX;
        var deltaY = e.clientY - startY;

        var maxScrollX = Math.max(0, document.documentElement.scrollWidth - window.innerWidth);
        var maxScrollY = Math.max(0, document.documentElement.scrollHeight - window.innerHeight);

        // Calculate target scroll (inverted for pan feel)
        var targetScrollX = startScrollX - deltaX;
        var targetScrollY = startScrollY - deltaY;

        // Clamp scroll to bounds
        var clampedX = Math.max(0, Math.min(maxScrollX, targetScrollX));
        var clampedY = Math.max(0, Math.min(maxScrollY, targetScrollY));

        // Calculate overscroll with resistance
        if (targetScrollX < 0) {
            overscrollX = Math.min(maxOverscroll, -targetScrollX * resistance);
        } else if (targetScrollX > maxScrollX) {
            overscrollX = Math.max(-maxOverscroll, -(targetScrollX - maxScrollX) * resistance);
        } else {
            overscrollX = 0;
        }

        if (targetScrollY < 0) {
            overscrollY = Math.min(maxOverscroll, -targetScrollY * resistance);
        } else if (targetScrollY > maxScrollY) {
            overscrollY = Math.max(-maxOverscroll, -(targetScrollY - maxScrollY) * resistance);
        } else {
            overscrollY = 0;
        }

        // Apply scroll
        window.scrollTo(clampedX, clampedY);

        // Apply rubber band transform
        if (overscrollX !== 0 || overscrollY !== 0) {
            wrapper.style.transform = 'translate(' + overscrollX + 'px, ' + overscrollY + 'px)';
        } else {
            wrapper.style.transform = '';
        }
    });

    document.addEventListener('mouseup', function(e) {
        if (e.button === 2 && isPanning) {
            isPanning = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';

            // Animate snap-back
            if (overscrollX !== 0 || overscrollY !== 0) {
                wrapper.style.transition = 'transform 0.25s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
                wrapper.style.transform = '';
            }

            overscrollX = 0;
            overscrollY = 0;
        }
    });

    // Prevent context menu on right-click
    document.addEventListener('contextmenu', function(e) {
        e.preventDefault();
    });
})();
</script>";

            // Insert script before closing body tag, or at end if no body tag
            var html = Html;
            if (html.Contains("</body>", StringComparison.OrdinalIgnoreCase))
            {
                html = html.Replace("</body>", interactionScript + "</body>", StringComparison.OrdinalIgnoreCase);
            }
            else if (html.Contains("</html>", StringComparison.OrdinalIgnoreCase))
            {
                html = html.Replace("</html>", interactionScript + "</html>", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                html = html + interactionScript;
            }

            _webView.CoreWebView2.NavigateToString(html);
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
            UpdateZoomDisplay();
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
            UpdateZoomDisplay();
        }
#endif
    }

    private void ResetZoom_Click(object? sender, RoutedEventArgs e)
    {
#if WINDOWS
        if (_webView != null)
        {
            // Use 0.98 to prevent horizontal scrollbar at "100%" zoom
            _webView.ZoomFactor = 0.98;
            UpdateZoomDisplay();
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
            UpdateZoomDisplay();
        }
        catch (Exception)
        {
            // Fallback to reset zoom
            _webView.ZoomFactor = 1.0;
            UpdateZoomDisplay();
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
