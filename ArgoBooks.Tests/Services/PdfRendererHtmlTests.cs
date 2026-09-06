using System.Text;
using System.Text.RegularExpressions;
using ArgoBooks.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// The renderer page pdf.js runs in.
///
/// It used to pull pdf.js and its worker from cdnjs, which put 1.4MB of downloads between
/// opening a document and seeing it, and meant PDFs did not render at all offline: the ready
/// handshake just timed out. Both files are embedded now.
///
/// Deliberately not timing tests. A stopwatch would measure the machine rather than the fix,
/// pass on a fast network whether or not the files are embedded, and get muted the first time
/// it flaked. The speed came from removing network calls, so that is what is asserted.
/// </summary>
public class PdfRendererHtmlTests
{
    [Fact]
    public void RendererHtml_ReferencesNothingOverTheNetwork()
    {
        var html = PdfThumbnailService.RendererHtml;

        // Two shapes to catch. The library was a <script src>, and the worker was a URL
        // assigned to workerSrc in JavaScript, which no attribute search would see.
        var fetching = Regex.Matches(
            html,
            @"(?:src|href)\s*=\s*[""'](https?:)?//[^""']+[""']",
            RegexOptions.IgnoreCase);

        Assert.True(fetching.Count == 0,
            "The renderer page must not fetch anything: " +
            string.Join(", ", fetching.Select(m => m.Value)));

        // pdf.js carries URLs in its own licence header, so a blanket "https" search would
        // fail on text nothing ever requests. The CDN it used to come from appears nowhere
        // in the vendored files, which makes it a safe thing to forbid outright.
        Assert.DoesNotContain("cdnjs", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RendererHtml_ActuallyContainsTheLibraryAndWorker()
    {
        var html = PdfThumbnailService.RendererHtml;

        // A renamed placeholder would leave the page loading, reporting no error, and never
        // rendering, which is worse to diagnose than a missing file. A missing file throws.
        Assert.DoesNotContain("__PDFJS_", html);

        // Both files together are ~1.4MB. Substantially less means something was read empty.
        Assert.True(html.Length > 1_000_000, $"Renderer page is only {html.Length} chars.");
    }

    [Fact]
    public void RendererHtml_FitsInsideTheNavigateToStringLimit()
    {
        // WebView2 rejects anything larger, and the page is now 1.4MB of inlined JavaScript.
        // A pdf.js upgrade is the plausible way this gets breached, and it would fail at
        // runtime, on Windows only, with the viewer simply never becoming ready.
        var bytes = Encoding.UTF8.GetByteCount(PdfThumbnailService.RendererHtml);

        Assert.True(bytes < PdfThumbnailService.NavigateToStringLimitBytes,
            $"Renderer page is {bytes / 1024}KB, over the " +
            $"{PdfThumbnailService.NavigateToStringLimitBytes / 1024}KB NavigateToString limit.");
    }
}
