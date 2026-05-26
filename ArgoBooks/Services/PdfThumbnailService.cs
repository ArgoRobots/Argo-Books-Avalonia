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
    private (Dictionary<int, byte[]> Pages, TaskCompletionSource<bool> Done, Action<int, byte[]>? OnPage)? _allPagesCollector;

    private PdfThumbnailService() { }

    /// <summary>
    /// Renders the first page of a PDF as a JPEG and reports the document's total page count.
    /// Used for fast card/thumbnail previews. Returns null on failure.
    /// </summary>
    public async Task<(byte[] Image, int PageCount)?> RenderPdfFirstPageAsync(byte[] pdfData)
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

    /// <summary>
    /// Renders pages of a PDF as JPEGs, in page order. Returns the pages that were actually
    /// rendered (excluding any in <paramref name="skipZeroBasedPages"/>), or null if none could be
    /// rendered.
    /// </summary>
    /// <param name="pdfData">The PDF bytes.</param>
    /// <param name="onPage">
    /// Optional callback invoked once per rendered page as it completes, with the zero-based page
    /// index and its JPEG bytes. Enables streaming pages into the UI as they finish.
    /// </param>
    /// <param name="skipZeroBasedPages">
    /// Optional set of zero-based page indices to skip rendering (e.g. pages already cached on
    /// disk). Skipped pages are not rendered and not reported, but still count toward the total.
    /// </param>
    public async Task<byte[][]?> RenderPdfAllPagesAsync(
        byte[] pdfData,
        Action<int, byte[]>? onPage = null,
        IReadOnlyCollection<int>? skipZeroBasedPages = null)
    {
        if (!PlatformSupportsWebView)
            return null;

        if (!Dispatcher.UIThread.CheckAccess())
        {
            return await Dispatcher.UIThread.InvokeAsync(
                () => RenderPdfAllPagesCoreAsync(pdfData, onPage, skipZeroBasedPages),
                DispatcherPriority.Background);
        }

        return await RenderPdfAllPagesCoreAsync(pdfData, onPage, skipZeroBasedPages);
    }

    private static bool PlatformSupportsWebView =>
        OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() || OperatingSystem.IsLinux();

    private async Task<(byte[] Image, int PageCount)?> RenderPdfFirstPageCoreAsync(byte[] pdfData)
    {
        await _renderLock.WaitAsync();
        try
        {
            await EnsureWebViewReadyAsync();

            System.Diagnostics.Debug.WriteLine($"PdfThumbnail: ready={_pdfJsReady}, webView={_webView != null}");

            if (_webView == null || !_pdfJsReady)
                return null;

            _renderTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

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

            // Payload form: "<count>:<dataUrl>"
            var result = await _renderTcs.Task;
            System.Diagnostics.Debug.WriteLine($"PdfThumbnail: got result, length={result?.Length ?? 0}");
            _renderTcs = null;

            if (string.IsNullOrEmpty(result))
                return null;

            var firstColon = result.IndexOf(':');
            if (firstColon <= 0)
                return null;
            if (!int.TryParse(result[..firstColon], out var pageCount))
                pageCount = 1;

            var dataUrl = result[(firstColon + 1)..];
            const string dataPrefix = "data:image/jpeg;base64,";
            if (dataUrl.StartsWith(dataPrefix))
            {
                return (Convert.FromBase64String(dataUrl[dataPrefix.Length..]), pageCount);
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

    private async Task<byte[][]?> RenderPdfAllPagesCoreAsync(
        byte[] pdfData,
        Action<int, byte[]>? onPage,
        IReadOnlyCollection<int>? skipZeroBasedPages)
    {
        await _renderLock.WaitAsync();
        try
        {
            await EnsureWebViewReadyAsync();

            if (_webView == null || !_pdfJsReady)
                return null;

            var pages = new Dictionary<int, byte[]>();
            var doneTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            _allPagesCollector = (pages, doneTcs, onPage);

            var pdfBase64 = Convert.ToBase64String(pdfData);
            // Pass skipped (0-based) page indices so the JS does not render them. The renderer
            // still reports the total page count so cached+rendered pages can be recombined.
            var skipCsv = skipZeroBasedPages is { Count: > 0 }
                ? string.Join(",", skipZeroBasedPages)
                : string.Empty;
            await _webView.InvokeScript($"window.__renderAllAndPost('{pdfBase64}', '{skipCsv}')");

            // Scale timeout with size: base 20s + 4s per 100KB, capped at 120s.
            var timeoutMs = Math.Min(120_000, 20_000 + pdfData.Length / 1024 / 100 * 4_000);
            await Task.WhenAny(doneTcs.Task, Task.Delay(timeoutMs));

            // Return whatever rendered (null only if nothing succeeded).
            var ordered = PdfRenderMessageParser.ToOrderedArray(pages);
            return ordered.Length > 0 ? ordered : null;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"PdfThumbnail all-pages error: {ex.Message}");
            return null;
        }
        finally
        {
            _allPagesCollector = null;
            _renderLock.Release();
        }
    }

    private async Task EnsureWebViewReadyAsync()
    {
        if (_pdfJsReady && _webView != null)
            return;

        // Dispose any previous failed init to avoid leaking windows
        CleanupWebView();

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

        // All-pages streaming: collect indexed pages until the done marker.
        if (_allPagesCollector is { } collector)
        {
            if (PdfRenderMessageParser.TryParsePage(body, out var idx, out var bytes))
            {
                collector.Pages[idx] = bytes;
                // OnPage callbacks can do synchronous disk I/O (writing JPEG pages),
                // which would block the WebView message handler / UI thread while
                // pages stream in. Dispatch to the thread pool so the UI stays
                // responsive during multi-page renders.
                if (collector.OnPage is { } onPage)
                    _ = Task.Run(() => onPage(idx, bytes));
                return;
            }
            if (PdfRenderMessageParser.TryParseDone(body, out _))
            {
                collector.Done.TrySetResult(true);
                return;
            }
        }

        // First-page single result: "render-result:<count>:<dataUrl>"
        if (body.StartsWith("render-result:"))
        {
            var payload = body["render-result:".Length..];
            _renderTcs?.TrySetResult(payload);
        }
    }

    private void CleanupWebView()
    {
        if (_webView != null)
            _webView.WebMessageReceived -= OnWebMessageReceived;
        _offscreenWindow?.Close();
        _offscreenWindow = null;
        _webView = null;
        _pdfJsReady = false;
    }

    public void Dispose() => CleanupWebView();

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

    function b64ToBytes(base64Data) {
        var binaryString = atob(base64Data);
        var bytes = new Uint8Array(binaryString.length);
        for (var i = 0; i < binaryString.length; i++) {
            bytes[i] = binaryString.charCodeAt(i);
        }
        return bytes;
    }

    function renderPageToJpeg(page) {
        var scale = 1.5;
        var viewport = page.getViewport({ scale: scale });
        var canvas = document.createElement('canvas');
        canvas.width = viewport.width;
        canvas.height = viewport.height;
        var ctx = canvas.getContext('2d');
        return page.render({ canvasContext: ctx, viewport: viewport }).promise.then(function() {
            return canvas.toDataURL('image/jpeg', 0.85);
        });
    }

    // Called from C# via InvokeScript, renders first page + reports page count.
    // Posts "render-result:<count>:<dataUrl>".
    window.__renderAndPost = function(base64Data) {
        try {
            var bytes = b64ToBytes(base64Data);
            pdfjsLib.getDocument({ data: bytes }).promise.then(function(pdf) {
                var count = pdf.numPages;
                return pdf.getPage(1).then(function(page) {
                    return renderPageToJpeg(page).then(function(url) {
                        postMsg('render-result:' + count + ':' + url);
                    });
                });
            }).catch(function(e) {
                postMsg('render-result:1:');
            });
        } catch (e) {
            postMsg('render-result:1:');
        }
    };

    // Renders pages sequentially. Posts "render-page:<index>:<dataUrl>" per rendered page,
    // then "render-done:<count>". Sequential to avoid spiking memory on large PDFs.
    // skipCsv is a comma-separated list of 0-based page indices to skip (e.g. already cached).
    window.__renderAllAndPost = function(base64Data, skipCsv) {
        try {
            var skip = {};
            if (skipCsv) {
                skipCsv.split(',').forEach(function(s) {
                    var n = parseInt(s, 10);
                    if (!isNaN(n)) skip[n] = true;
                });
            }
            var bytes = b64ToBytes(base64Data);
            pdfjsLib.getDocument({ data: bytes }).promise.then(function(pdf) {
                var count = pdf.numPages;
                var i = 1;
                function next() {
                    if (i > count) { postMsg('render-done:' + count); return; }
                    var pageNum = i;
                    if (skip[pageNum - 1]) { i++; next(); return; }
                    pdf.getPage(pageNum).then(function(page) {
                        return renderPageToJpeg(page);
                    }).then(function(url) {
                        postMsg('render-page:' + (pageNum - 1) + ':' + url);
                        i++; next();
                    }).catch(function() { i++; next(); });
                }
                next();
            }).catch(function(e) {
                postMsg('render-done:0');
            });
        } catch (e) {
            postMsg('render-done:0');
        }
    };
</script>
</head>
<body style="margin:0;background:#fff;"></body>
</html>
""";

    /// <summary>
    /// Pure parsing of the JS render message protocol, factored out so it can be unit tested
    /// without a live WebView.
    /// </summary>
    public static class PdfRenderMessageParser
    {
        public const string PagePrefix = "render-page:";
        public const string DonePrefix = "render-done:";
        private const string DataPrefix = "data:image/jpeg;base64,";

        /// <summary>True if the body is the done terminator; outputs the declared count.</summary>
        public static bool TryParseDone(string body, out int count)
        {
            count = 0;
            if (!body.StartsWith(DonePrefix)) return false;
            return int.TryParse(body[DonePrefix.Length..], out count);
        }

        /// <summary>
        /// Parses a page message into (index, jpegBytes). Returns false if not a page message
        /// or the payload is empty/invalid.
        /// </summary>
        public static bool TryParsePage(string body, out int index, out byte[] bytes)
        {
            index = -1;
            bytes = [];
            if (!body.StartsWith(PagePrefix)) return false;
            var rest = body[PagePrefix.Length..];
            var colon = rest.IndexOf(':');
            if (colon <= 0) return false;
            if (!int.TryParse(rest[..colon], out index)) return false;
            var dataUrl = rest[(colon + 1)..];
            if (!dataUrl.StartsWith(DataPrefix)) return false;
            var b64 = dataUrl[DataPrefix.Length..];
            if (string.IsNullOrEmpty(b64)) return false;
            try { bytes = Convert.FromBase64String(b64); return true; }
            catch { return false; }
        }

        /// <summary>Orders an (index -> bytes) map into a dense array, dropping gaps.</summary>
        public static byte[][] ToOrderedArray(IReadOnlyDictionary<int, byte[]> pages)
            => pages.OrderBy(kv => kv.Key).Select(kv => kv.Value).ToArray();
    }
}
