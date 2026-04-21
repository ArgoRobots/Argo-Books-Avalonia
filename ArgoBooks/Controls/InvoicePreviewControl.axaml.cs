using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ArgoBooks.Controls;

/// <summary>
/// Cross-platform invoice preview control.
/// On Windows/macOS: Uses NativeWebView for high-quality HTML rendering with zoom/pan.
/// On Linux: Shows a fallback with option to view in browser (NativeWebView doesn't embed inline on Linux).
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

    private NativeWebView? _webView;
    private Panel? _rootPanel;
    private Border? _fallbackPanel;
    private Border? _zoomToolbar;
    private TextBlock? _zoomPercentageText;
    private bool _isInitialized;
    private bool _webViewReady;
    private double _currentZoom = 1.0;
    private double _pendingScrollX;
    private double _pendingScrollY;
    private bool _hasPendingScroll;

    private const double ZoomStep = 0.1;
    private const double MinZoom = 0.25;
    private const double MaxZoom = 5.0;

    /// <summary>
    /// Whether the current platform supports inline WebView embedding.
    /// Avalonia 12 NativeWebView supports Windows (WebView2), macOS (WKWebView), and Linux (WebKitGTK).
    /// </summary>
    private static bool PlatformSupportsInlineWebView =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

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
        _webView = this.FindControl<NativeWebView>("WebView");

        _isInitialized = true;

        // IMPORTANT: Do NOT create/show WebView here!
        // OnLoaded fires before bindings evaluate, so IsVisible may incorrectly be true.
        // We must only activate the WebView when IsVisible explicitly changes to true.
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == HtmlProperty && _isInitialized)
        {
            EnsureWebViewActiveIfVisible();
            _ = UpdateWebViewContent();
        }

        if (change.Property == IsVisibleProperty && _isInitialized)
        {
            bool wasVisible = change.OldValue is bool oldVal && oldVal;
            bool isNowVisible = change.NewValue is bool newVal && newVal;

            if (isNowVisible && !wasVisible)
            {
                InitializePlatformPreview();
            }
            else if (!isNowVisible && wasVisible)
            {
                DeactivateWebView();
            }
        }
    }

    private void InitializePlatformPreview()
    {
        if (PlatformSupportsInlineWebView)
        {
            ActivateWebView();
        }
        else
        {
            ShowFallback();
        }
    }

    private void EnsureWebViewActiveIfVisible()
    {
        if (PlatformSupportsInlineWebView && _webView != null && !_webView.IsVisible
            && IsEffectivelyVisible && !string.IsNullOrEmpty(Html))
        {
            InitializePlatformPreview();
        }
        else if (!PlatformSupportsInlineWebView && IsEffectivelyVisible)
        {
            ShowFallback();
        }
    }

    private void ActivateWebView()
    {
        if (_webView == null || _webViewReady)
            return;

        _webView.IsVisible = true;
        _webView.NavigationCompleted += OnNavigationCompleted;
        _webView.WebMessageReceived += OnWebMessageReceived;

        if (_zoomToolbar != null)
            _zoomToolbar.IsVisible = true;

        _webViewReady = true;
        _ = UpdateWebViewContent();
    }

    private void DeactivateWebView()
    {
        if (_webView != null)
        {
            _webView.NavigationCompleted -= OnNavigationCompleted;
            _webView.WebMessageReceived -= OnWebMessageReceived;
            _webView.IsVisible = false;
        }

        _webViewReady = false;

        if (_zoomToolbar != null)
            _zoomToolbar.IsVisible = false;
    }

    private void ShowFallback()
    {
        if (_fallbackPanel != null)
            _fallbackPanel.IsVisible = true;
    }

    private void OnNavigationCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
    {
        if (_hasPendingScroll && _webView != null)
        {
            var sx = _pendingScrollX.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var sy = _pendingScrollY.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _ = _webView.InvokeScript($"window.scrollTo({sx}, {sy})");
            _hasPendingScroll = false;
        }
    }

    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        try
        {
            var message = e.Body;
            if (string.IsNullOrEmpty(message))
                return;

            var json = System.Text.Json.JsonDocument.Parse(message);
            var root = json.RootElement;

            if (!root.TryGetProperty("type", out var typeElement))
                return;

            var messageType = typeElement.GetString();

            if (messageType == "zoomUpdate" && root.TryGetProperty("zoom", out var zoomElement))
            {
                _currentZoom = zoomElement.GetDouble();
                Avalonia.Threading.Dispatcher.UIThread.Post(UpdateZoomDisplay);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"InvoicePreview message error: {ex.Message}");
        }
    }

    private void UpdateZoomDisplay()
    {
        if (_zoomPercentageText != null)
        {
            var percentage = (int)Math.Round(_currentZoom * 100);
            _zoomPercentageText.Text = $"{percentage}%";
        }
    }

    private async Task UpdateWebViewContent()
    {
        if (!_webViewReady || _webView == null || string.IsNullOrEmpty(Html))
            return;

        // Capture current scroll position so NavigationCompleted can restore it
        // after NavigateToString resets the page. Skip re-capture while a prior
        // navigation is still in flight — the live page is mid-reload and would
        // report scroll=0, clobbering the position we're trying to preserve.
        if (!_hasPendingScroll)
        {
            try
            {
                var result = await _webView.InvokeScript("JSON.stringify([window.scrollX||0,window.scrollY||0])");
                if (TryParseScrollResult(result, out var sx, out var sy))
                {
                    _pendingScrollX = sx;
                    _pendingScrollY = sy;
                    _hasPendingScroll = true;
                }
            }
            catch
            {
                // No saved scroll; reload will keep scroll at 0
            }
        }

        // Inject interaction scripts for zoom and pan handling
        var interactionScript = @"
<script>
(function() {
    if (window.__interactionHandlersInstalled) return;
    window.__interactionHandlersInstalled = true;

    // Create zoom wrapper
    var zoomWrapper = document.getElementById('__zoomWrapper');
    if (!zoomWrapper) {
        zoomWrapper = document.createElement('div');
        zoomWrapper.id = '__zoomWrapper';
        zoomWrapper.style.cssText = 'transform-origin: 0 0; min-height: 100%; will-change: transform;';
        zoomWrapper.dataset.scale = '1';
        while (document.body.firstChild) {
            zoomWrapper.appendChild(document.body.firstChild);
        }
        document.body.appendChild(zoomWrapper);
        document.body.style.overflow = 'auto';
    }

    function updateZoom(newScale, originX, originY) {
        var wrapper = document.getElementById('__zoomWrapper');
        var oldScale = parseFloat(wrapper.dataset.scale || '1');
        newScale = Math.max(0.25, Math.min(5.0, newScale));
        wrapper.dataset.scale = newScale;

        // Calculate scroll adjustment to keep the point under cursor
        var scrollX = window.scrollX || 0;
        var scrollY = window.scrollY || 0;

        // Document position under the origin point at old scale
        var docX = scrollX + (originX / oldScale);
        var docY = scrollY + (originY / oldScale);

        wrapper.style.transform = 'scale(' + newScale + ')';

        // New scroll position to keep the same document point under cursor
        var newScrollX = docX - (originX / newScale);
        var newScrollY = docY - (originY / newScale);
        window.scrollTo(newScrollX, newScrollY);

        // Notify C# of zoom level
        try {
            window.chrome.webview.postMessage(JSON.stringify({ type: 'zoomUpdate', zoom: newScale }));
        } catch(e) {
            try {
                window.webkit.messageHandlers.webview.postMessage(JSON.stringify({ type: 'zoomUpdate', zoom: newScale }));
            } catch(e2) {}
        }
    }

    // Expose for C# InvokeScript calls
    window.__setZoom = function(newScale) {
        var vw = window.innerWidth / 2;
        var vh = window.innerHeight / 2;
        updateZoom(newScale, vw, vh);
    };

    window.__fitToWindow = function() {
        var wrapper = document.getElementById('__zoomWrapper');
        // Reset to 1 first to measure natural size
        wrapper.style.transform = 'scale(1)';
        wrapper.dataset.scale = '1';
        var contentWidth = wrapper.scrollWidth;
        var contentHeight = wrapper.scrollHeight;
        var viewportWidth = window.innerWidth;
        var viewportHeight = window.innerHeight;
        if (contentWidth <= 0 || contentHeight <= 0) return;
        var fitScale = Math.min(viewportWidth / contentWidth, viewportHeight / contentHeight);
        fitScale = Math.max(0.25, Math.min(5.0, fitScale));
        updateZoom(fitScale, viewportWidth / 2, viewportHeight / 2);
    };

    window.__getZoom = function() {
        var wrapper = document.getElementById('__zoomWrapper');
        return parseFloat(wrapper.dataset.scale || '1');
    };

    // Zoom handling (Ctrl+Scroll)
    document.addEventListener('wheel', function(e) {
        if (e.ctrlKey) {
            e.preventDefault();
            var wrapper = document.getElementById('__zoomWrapper');
            var currentScale = parseFloat(wrapper.dataset.scale || '1');
            var delta = e.deltaY < 0 ? 0.1 : -0.1;
            updateZoom(currentScale + delta, e.clientX, e.clientY);
        }
    }, { passive: false });

    // Pan handling (Right-click drag) with rubber band effect
    var isPanning = false;
    var startX = 0, startY = 0, startScrollX = 0, startScrollY = 0;
    var overscrollX = 0, overscrollY = 0;
    var resistance = 0.3, maxOverscroll = 80;

    document.addEventListener('mousedown', function(e) {
        if (e.button === 2) {
            isPanning = true;
            startX = e.clientX;
            startY = e.clientY;
            startScrollX = window.scrollX || 0;
            startScrollY = window.scrollY || 0;
            overscrollX = 0;
            overscrollY = 0;
            var zw = document.getElementById('__zoomWrapper');
            if (zw) zw.style.transition = 'none';
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
        var targetScrollX = startScrollX - deltaX;
        var targetScrollY = startScrollY - deltaY;
        var clampedX = Math.max(0, Math.min(maxScrollX, targetScrollX));
        var clampedY = Math.max(0, Math.min(maxScrollY, targetScrollY));

        if (targetScrollX < 0) overscrollX = Math.min(maxOverscroll, -targetScrollX * resistance);
        else if (targetScrollX > maxScrollX) overscrollX = Math.max(-maxOverscroll, -(targetScrollX - maxScrollX) * resistance);
        else overscrollX = 0;

        if (targetScrollY < 0) overscrollY = Math.min(maxOverscroll, -targetScrollY * resistance);
        else if (targetScrollY > maxScrollY) overscrollY = Math.max(-maxOverscroll, -(targetScrollY - maxScrollY) * resistance);
        else overscrollY = 0;

        window.scrollTo(clampedX, clampedY);
        var zw = document.getElementById('__zoomWrapper');
        if (zw) {
            var scale = parseFloat(zw.dataset.scale || '1');
            if (overscrollX !== 0 || overscrollY !== 0) {
                zw.style.transform = 'scale(' + scale + ') translate(' + (overscrollX/scale) + 'px, ' + (overscrollY/scale) + 'px)';
            } else {
                zw.style.transform = 'scale(' + scale + ')';
            }
        }
    });

    document.addEventListener('mouseup', function(e) {
        if (e.button === 2 && isPanning) {
            isPanning = false;
            document.body.style.cursor = '';
            document.body.style.userSelect = '';
            var zw = document.getElementById('__zoomWrapper');
            if (zw) {
                var scale = parseFloat(zw.dataset.scale || '1');
                if (overscrollX !== 0 || overscrollY !== 0) {
                    zw.style.transition = 'transform 0.25s cubic-bezier(0.25, 0.46, 0.45, 0.94)';
                    zw.style.transform = 'scale(' + scale + ')';
                }
            }
            overscrollX = 0;
            overscrollY = 0;
        }
    });

    document.addEventListener('contextmenu', function(e) { e.preventDefault(); });
})();
</script>";

        // Insert script before closing body tag, or at end if no body tag
        var html = Html!;
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
            html += interactionScript;
        }

        try
        {
            _webView!.NavigateToString(html, new Uri("https://localhost/"));
        }
        catch (Exception ex)
        {
            // NavigateToString never delivered the navigation, so OnNavigationCompleted
            // won't fire to reset _hasPendingScroll. Clear it here so the next
            // UpdateWebViewContent can recapture a fresh scroll position instead of
            // being blocked indefinitely by a stale pending capture.
            _hasPendingScroll = false;
            System.Diagnostics.Debug.WriteLine($"InvoicePreview error: {ex.Message}");
        }
    }

    /// <summary>
    /// Captures the current WebView content as a PNG screenshot and returns it as base64.
    /// Returns null on platforms where the WebView is not active.
    /// </summary>
    public async Task<string?> CaptureScreenshotBase64Async()
    {
        if (!_webViewReady || _webView == null)
            return null;

        try
        {
            // Use html2canvas via JavaScript to capture the rendered content
            var captureScript = @"
(function() {
    return new Promise(function(resolve) {
        var wrapper = document.getElementById('__zoomWrapper') || document.body;
        // Reset transform temporarily for clean capture
        var origTransform = wrapper.style.transform;
        var origTransformOrigin = wrapper.style.transformOrigin;
        wrapper.style.transform = 'none';
        wrapper.style.transformOrigin = '';

        // Use canvas to capture
        var canvas = document.createElement('canvas');
        var rect = wrapper.getBoundingClientRect();
        canvas.width = Math.min(rect.width, 1200);
        canvas.height = Math.min(rect.height, 1600);

        // Simple capture: render visible area to canvas via foreignObject
        var svg = '<svg xmlns=""http://www.w3.org/2000/svg"" width=""' + canvas.width + '"" height=""' + canvas.height + '"">' +
            '<foreignObject width=""100%"" height=""100%"">' +
            '<div xmlns=""http://www.w3.org/1999/xhtml"">' + wrapper.innerHTML + '</div>' +
            '</foreignObject></svg>';

        var img = new Image();
        img.onload = function() {
            var ctx = canvas.getContext('2d');
            ctx.drawImage(img, 0, 0);
            wrapper.style.transform = origTransform;
            wrapper.style.transformOrigin = origTransformOrigin;
            resolve(canvas.toDataURL('image/png'));
        };
        img.onerror = function() {
            wrapper.style.transform = origTransform;
            wrapper.style.transformOrigin = origTransformOrigin;
            resolve('');
        };
        img.src = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg);
    });
})()";

            var result = await _webView.InvokeScript(captureScript);
            if (!string.IsNullOrEmpty(result) && result.StartsWith("data:image/png;base64,"))
            {
                return result.Substring("data:image/png;base64,".Length);
            }

            // Strip surrounding quotes if the result is a JSON string
            if (result != null && result.StartsWith('"') && result.EndsWith('"'))
            {
                result = result[1..^1].Replace("\\\"", "\"");
                if (result.StartsWith("data:image/png;base64,"))
                {
                    return result.Substring("data:image/png;base64,".Length);
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool TryParseScrollResult(string? result, out double x, out double y)
    {
        x = 0;
        y = 0;
        if (string.IsNullOrEmpty(result))
            return false;

        // InvokeScript may return the JSON string wrapped in quotes with escaped inner quotes.
        var trimmed = result.Trim();
        if (trimmed.StartsWith('"') && trimmed.EndsWith('"'))
            trimmed = trimmed[1..^1].Replace("\\\"", "\"");

        trimmed = trimmed.Trim('[', ']');
        var parts = trimmed.Split(',');
        return parts.Length == 2
            && double.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out x)
            && double.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out y);
    }

    private void OpenInBrowserButton_Click(object? sender, RoutedEventArgs e)
    {
        OpenInBrowserCommand?.Execute(null);
    }

    private void ZoomIn_Click(object? sender, RoutedEventArgs e)
    {
        if (_webView == null || !_webViewReady)
            return;

        var newZoom = Math.Min(_currentZoom + ZoomStep, MaxZoom);
        _ = _webView.InvokeScript($"window.__setZoom({newZoom.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
    }

    private void ZoomOut_Click(object? sender, RoutedEventArgs e)
    {
        if (_webView == null || !_webViewReady)
            return;

        var newZoom = Math.Max(_currentZoom - ZoomStep, MinZoom);
        _ = _webView.InvokeScript($"window.__setZoom({newZoom.ToString(System.Globalization.CultureInfo.InvariantCulture)})");
    }

    private void ResetZoom_Click(object? sender, RoutedEventArgs e)
    {
        if (_webView == null || !_webViewReady)
            return;

        _ = _webView.InvokeScript("window.__setZoom(1)");
    }

    private void FitToWindow_Click(object? sender, RoutedEventArgs e)
    {
        if (_webView == null || !_webViewReady)
            return;

        _ = _webView.InvokeScript("window.__fitToWindow()");
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        DeactivateWebView();
    }
}
