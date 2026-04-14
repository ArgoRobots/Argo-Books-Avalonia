using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ArgoBooks.Services;

/// <summary>
/// Renders PDF first pages as JPEG thumbnails using a hidden NativeWebView with pdf.js.
/// Replaces the PDFtoImage library with the cross-platform Avalonia WebView.
/// Uses postMessage callbacks for reliable async communication between JS and C#.
/// </summary>
public sealed class PdfThumbnailService
{
    private static readonly Lazy<PdfThumbnailService> _instance = new(() => new PdfThumbnailService());
    public static PdfThumbnailService Instance => _instance.Value;

    private NativeWebView? _webView;
    private Window? _offscreenWindow;
    private readonly SemaphoreSlim _renderLock = new(1, 1);
    private bool _pdfJsReady;
    private TaskCompletionSource<string>? _renderTcs;

    private PdfThumbnailService() { }

    public async Task<byte[]?> RenderPdfFirstPageAsync(byte[] pdfData)
    {
        if (!PlatformSupportsWebView)
            return null;

        // Always marshal to UI thread
        if (!Dispatcher.UIThread.CheckAccess())
        {
            return await Dispatcher.UIThread.InvokeAsync(
                () => RenderPdfFirstPageCoreAsync(pdfData),
                DispatcherPriority.Background);
        }

        return await RenderPdfFirstPageCoreAsync(pdfData);
    }

    private static bool PlatformSupportsWebView =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS();

    private async Task<byte[]?> RenderPdfFirstPageCoreAsync(byte[] pdfData)
    {
        await _renderLock.WaitAsync();
        try
        {
            await EnsureWebViewReadyAsync();

            System.Diagnostics.Debug.WriteLine($"PdfThumbnail: ready={_pdfJsReady}, webView={_webView != null}");

            if (_webView == null || !_pdfJsReady)
                return null;

            _renderTcs = new TaskCompletionSource<string>();

            var pdfBase64 = Convert.ToBase64String(pdfData);
            System.Diagnostics.Debug.WriteLine($"PdfThumbnail: invoking render for {pdfBase64.Length} chars of base64");

            await _webView.InvokeScript($"window.__renderAndPost('{pdfBase64}')");

            System.Diagnostics.Debug.WriteLine("PdfThumbnail: waiting for postMessage result...");
            var completed = await Task.WhenAny(_renderTcs.Task, Task.Delay(15000));
            if (completed != _renderTcs.Task)
            {
                System.Diagnostics.Debug.WriteLine("PdfThumbnail: TIMEOUT waiting for render result");
                return null;
            }

            var result = _renderTcs.Task.Result;
            System.Diagnostics.Debug.WriteLine($"PdfThumbnail: got result, length={result?.Length ?? 0}");
            _renderTcs = null;

            const string dataPrefix = "data:image/jpeg;base64,";
            if (!string.IsNullOrEmpty(result) && result.StartsWith(dataPrefix))
            {
                return Convert.FromBase64String(result[dataPrefix.Length..]);
            }

            return null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PdfThumbnail error: {ex.Message}");
            return null;
        }
        finally
        {
            _renderTcs = null;
            _renderLock.Release();
        }
    }

    private async Task EnsureWebViewReadyAsync()
    {
        if (_pdfJsReady && _webView != null)
            return;

        _offscreenWindow = new Window
        {
            Width = 800,
            Height = 600,
            ShowInTaskbar = false,
            WindowDecorations = WindowDecorations.None,
            ShowActivated = false,
            CanResize = false,
        };

        _webView = new NativeWebView();
        _offscreenWindow.Content = _webView;

        _offscreenWindow.Opened += (_, _) =>
        {
            _offscreenWindow.Position = new PixelPoint(-30000, -30000);
        };
        _offscreenWindow.Show();

        var navTcs = new TaskCompletionSource<bool>();
        var readyTcs = new TaskCompletionSource<bool>();

        void OnNavCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
        {
            _webView.NavigationCompleted -= OnNavCompleted;
            navTcs.TrySetResult(e.IsSuccess);
        }

        // Handle ALL messages from JS in a single handler
        _webView.WebMessageReceived += OnWebMessageReceived;

        void OnReadyMessage(object? sender, WebMessageReceivedEventArgs e)
        {
            if (e.Body?.Contains("pdfjs-ready") == true)
            {
                readyTcs.TrySetResult(true);
            }
        }

        // Temporarily listen for the ready signal
        _webView.WebMessageReceived += OnReadyMessage;
        _webView.NavigationCompleted += OnNavCompleted;

        _webView.NavigateToString(PdfJsRendererHtml, new Uri("https://localhost/pdfrender"));

        var navCompleted = await Task.WhenAny(navTcs.Task, Task.Delay(15000));
        if (navCompleted != navTcs.Task || !navTcs.Task.Result)
        {
            System.Diagnostics.Debug.WriteLine($"PdfThumbnail INIT: navigation failed or timed out");
            return;
        }
        System.Diagnostics.Debug.WriteLine("PdfThumbnail INIT: navigation succeeded, waiting for pdf.js...");

        var readyCompleted = await Task.WhenAny(readyTcs.Task, Task.Delay(15000));
        _webView.WebMessageReceived -= OnReadyMessage;
        _pdfJsReady = readyCompleted == readyTcs.Task;
        System.Diagnostics.Debug.WriteLine($"PdfThumbnail INIT: pdfJsReady={_pdfJsReady}");
    }

    private void OnWebMessageReceived(object? sender, WebMessageReceivedEventArgs e)
    {
        var body = e.Body;
        if (string.IsNullOrEmpty(body))
            return;

        // Render result messages start with "render-result:" prefix
        if (body.StartsWith("render-result:"))
        {
            var dataUrl = body["render-result:".Length..];
            _renderTcs?.TrySetResult(dataUrl);
        }
    }

    public void Dispose()
    {
        if (_webView != null)
            _webView.WebMessageReceived -= OnWebMessageReceived;
        _offscreenWindow?.Close();
        _offscreenWindow = null;
        _webView = null;
        _pdfJsReady = false;
    }

    private const string PdfJsRendererHtml = """
<!DOCTYPE html>
<html>
<head>
<script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js"></script>
<script>
    function postMsg(msg) {
        try { window.chrome.webview.postMessage(msg); } catch(e) {}
        try { window.webkit.messageHandlers.webview.postMessage(msg); } catch(e) {}
    }

    function initPdfJs() {
        if (typeof pdfjsLib !== 'undefined') {
            pdfjsLib.GlobalWorkerOptions.workerSrc =
                'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';
            postMsg('pdfjs-ready');
        } else {
            setTimeout(initPdfJs, 200);
        }
    }
    window.onload = initPdfJs;

    // Called from C# via InvokeScript — renders PDF and posts result via postMessage
    window.__renderAndPost = function(base64Data) {
        try {
            var binaryString = atob(base64Data);
            var bytes = new Uint8Array(binaryString.length);
            for (var i = 0; i < binaryString.length; i++) {
                bytes[i] = binaryString.charCodeAt(i);
            }
            pdfjsLib.getDocument({ data: bytes }).promise.then(function(pdf) {
                return pdf.getPage(1);
            }).then(function(page) {
                var scale = 1.5;
                var viewport = page.getViewport({ scale: scale });
                var canvas = document.createElement('canvas');
                canvas.width = viewport.width;
                canvas.height = viewport.height;
                var ctx = canvas.getContext('2d');
                page.render({ canvasContext: ctx, viewport: viewport }).promise.then(function() {
                    postMsg('render-result:' + canvas.toDataURL('image/jpeg', 0.85));
                }).catch(function(e) {
                    postMsg('render-result:');
                });
            }).catch(function(e) {
                postMsg('render-result:');
            });
        } catch (e) {
            postMsg('render-result:');
        }
    };
</script>
</head>
<body style="margin:0;background:#fff;"></body>
</html>
""";
}
