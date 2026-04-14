using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ArgoBooks.Services;

/// <summary>
/// Renders PDF first pages as JPEG thumbnails using a hidden NativeWebView with pdf.js.
/// Replaces the PDFtoImage library with the cross-platform Avalonia WebView.
/// </summary>
public sealed class PdfThumbnailService
{
    private static readonly Lazy<PdfThumbnailService> _instance = new(() => new PdfThumbnailService());
    public static PdfThumbnailService Instance => _instance.Value;

    private NativeWebView? _webView;
    private Window? _offscreenWindow;
    private readonly SemaphoreSlim _renderLock = new(1, 1);
    private bool _pdfJsReady;

    private PdfThumbnailService() { }

    /// <summary>
    /// Renders the first page of a PDF as a JPEG image.
    /// Can be called from any thread — marshals to UI thread internally.
    /// Returns null if the PDF cannot be rendered.
    /// </summary>
    public async Task<byte[]?> RenderPdfFirstPageAsync(byte[] pdfData)
    {
        if (!PlatformSupportsWebView)
            return null;

        if (!Dispatcher.UIThread.CheckAccess())
        {
            return await Dispatcher.UIThread.InvokeAsync(() => RenderPdfFirstPageCoreAsync(pdfData));
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

            if (_webView == null || !_pdfJsReady)
                return null;

            var pdfBase64 = Convert.ToBase64String(pdfData);

            // Call the render function. InvokeScript returns the JS result as a string.
            // Since __renderPdfToJpeg is async, we wrap it so the WebView awaits the promise.
            var script = $"window.__renderPdfToJpeg('{pdfBase64}')";
            var result = await _webView.InvokeScript(script);

            if (string.IsNullOrEmpty(result))
                return null;

            // The result may be JSON-encoded (wrapped in quotes with escapes)
            result = result.Trim();
            if (result.StartsWith('"') && result.EndsWith('"'))
            {
                result = System.Text.Json.JsonSerializer.Deserialize<string>(result) ?? "";
            }

            const string dataPrefix = "data:image/jpeg;base64,";
            if (result.StartsWith(dataPrefix))
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
            _renderLock.Release();
        }
    }

    private async Task EnsureWebViewReadyAsync()
    {
        if (_pdfJsReady && _webView != null)
            return;

        // Write the pdf.js worker and library to temp so WebView can load them via file://
        var tempDir = Path.Combine(Path.GetTempPath(), "ArgoBooks", "PdfJs");
        Directory.CreateDirectory(tempDir);
        var htmlPath = Path.Combine(tempDir, "renderer.html");
        await File.WriteAllTextAsync(htmlPath, PdfJsRendererHtml);

        // Create a hidden off-screen window to host the WebView
        _offscreenWindow = new Window
        {
            Width = 800,
            Height = 600,
            ShowInTaskbar = false,
            WindowDecorations = WindowDecorations.None,
            ShowActivated = false,
            CanResize = false,
            Topmost = false,
        };

        _webView = new NativeWebView();
        _offscreenWindow.Content = _webView;

        _offscreenWindow.Opened += (_, _) =>
        {
            // Move off-screen after the window is created
            _offscreenWindow.Position = new PixelPoint(-30000, -30000);
        };
        _offscreenWindow.Show();

        // Navigate to the local HTML file (file:// allows loading CDN scripts)
        var tcs = new TaskCompletionSource<bool>();
        var readyTcs = new TaskCompletionSource<bool>();

        void OnNavCompleted(object? sender, WebViewNavigationCompletedEventArgs e)
        {
            _webView.NavigationCompleted -= OnNavCompleted;
            tcs.TrySetResult(e.IsSuccess);
        }

        void OnMessage(object? sender, WebMessageReceivedEventArgs e)
        {
            if (e.Body == "pdfjs-ready")
            {
                _webView.WebMessageReceived -= OnMessage;
                readyTcs.TrySetResult(true);
            }
        }

        _webView.NavigationCompleted += OnNavCompleted;
        _webView.WebMessageReceived += OnMessage;

        // Navigate to the local file
        _webView.Source = new Uri(htmlPath);

        // Wait for navigation
        var navTask = await Task.WhenAny(tcs.Task, Task.Delay(15000));
        if (navTask != tcs.Task || !tcs.Task.Result)
            return;

        // Wait for pdf.js to load (signaled via postMessage)
        var readyTask = await Task.WhenAny(readyTcs.Task, Task.Delay(15000));
        _pdfJsReady = readyTask == readyTcs.Task;
    }

    /// <summary>
    /// Disposes the off-screen WebView and window.
    /// </summary>
    public void Dispose()
    {
        _offscreenWindow?.Close();
        _offscreenWindow = null;
        _webView = null;
        _pdfJsReady = false;
    }

    /// <summary>
    /// Self-contained HTML that loads pdf.js as a standard (non-module) script from CDN,
    /// and exposes a __renderPdfToJpeg function that accepts base64 PDF data
    /// and returns a JPEG data URL of the first page.
    /// </summary>
    private const string PdfJsRendererHtml = """
<!DOCTYPE html>
<html>
<head>
<script src="https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.min.js"></script>
<script>
    // Wait for pdf.js to load, then signal readiness
    function initPdfJs() {
        if (typeof pdfjsLib !== 'undefined') {
            pdfjsLib.GlobalWorkerOptions.workerSrc =
                'https://cdnjs.cloudflare.com/ajax/libs/pdf.js/3.11.174/pdf.worker.min.js';

            // Signal to C# that pdf.js is ready
            try { window.chrome.webview.postMessage('pdfjs-ready'); } catch(e) {}
            try { window.webkit.messageHandlers.webview.postMessage('pdfjs-ready'); } catch(e) {}
        } else {
            setTimeout(initPdfJs, 100);
        }
    }
    window.onload = initPdfJs;

    window.__renderPdfToJpeg = function(base64Data) {
        return new Promise(function(resolve) {
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
                        resolve(canvas.toDataURL('image/jpeg', 0.85));
                    }).catch(function() { resolve(''); });
                }).catch(function() { resolve(''); });
            } catch (e) {
                resolve('');
            }
        });
    };
</script>
</head>
<body style="margin:0;background:#fff;"></body>
</html>
""";
}
