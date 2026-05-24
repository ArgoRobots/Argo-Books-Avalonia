using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Services;

/// <summary>
/// Resolves a receipt's displayable page image paths, rendering PDFs to one JPEG per page
/// (cached in temp) and orienting raster images. Shared by every full-size receipt viewer so the
/// PDF-vs-image branching, page naming, and temp caching live in one place. Page files are named
/// <c>&lt;name&gt;_p{n}.jpg</c> (1-based) and the page count is cached in a
/// <c>&lt;name&gt;.pagecount</c> marker so the viewer can reuse pages already rendered for the
/// card thumbnail and skip re-rendering them.
/// </summary>
public static class ReceiptPageRenderer
{
    private static string TempDir => Path.Combine(Path.GetTempPath(), "ArgoBooks", "Receipts");

    /// <summary>Path of the JPEG for a given 0-based page index of a receipt file.</summary>
    public static string PagePath(string fileName, int zeroBasedIndex)
        => Path.Combine(TempDir, $"{Path.GetFileNameWithoutExtension(fileName)}_p{zeroBasedIndex + 1}.jpg");

    /// <summary>Path of the cached image for a non-PDF receipt.</summary>
    public static string ImagePath(string fileName) => Path.Combine(TempDir, fileName);

    /// <summary>Ensures the receipt temp directory exists.</summary>
    public static void EnsureTempDir() => Directory.CreateDirectory(TempDir);

    private static string PageCountMarkerPath(string fileName)
        => Path.Combine(TempDir, $"{Path.GetFileNameWithoutExtension(fileName)}.pagecount");

    /// <summary>Records how many pages a PDF receipt has, so viewers know the full set.</summary>
    public static void WritePageCount(string fileName, int count)
    {
        try { File.WriteAllText(PageCountMarkerPath(fileName), count.ToString()); }
        catch { /* non-critical cache marker */ }
    }

    /// <summary>Cached page count for a receipt file, or 1 when unknown (no marker yet).</summary>
    public static int CachedPageCount(string fileName) => ReadPageCount(fileName) ?? 1;

    private static int? ReadPageCount(string fileName)
    {
        try
        {
            var path = PageCountMarkerPath(fileName);
            if (File.Exists(path) && int.TryParse(File.ReadAllText(path), out var count) && count > 0)
                return count;
        }
        catch { /* ignore */ }
        return null;
    }

    /// <summary>
    /// Returns the page image paths for a receipt, in order. PDFs render all pages; raster images
    /// return a single oriented file. Returns an empty list when there is no data or rendering
    /// fails. Pages already on disk (e.g. the card's first-page thumbnail) are reused, and only
    /// missing pages are rendered.
    /// </summary>
    /// <param name="receipt">The receipt to render.</param>
    /// <param name="onPage">
    /// Optional progress callback invoked once per page (0-based index + path) as each becomes
    /// available, so the UI can stream pages in instead of waiting for the whole document.
    /// </param>
    public static async Task<IReadOnlyList<string>> GetPagePathsAsync(
        Receipt receipt,
        IProgress<(int Index, string Path)>? onPage = null)
    {
        if (string.IsNullOrEmpty(receipt.FileData))
            return [];

        try
        {
            EnsureTempDir();
            var bytes = Convert.FromBase64String(receipt.FileData);
            var isPdf = receipt.FileType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true
                        || receipt.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            if (!isPdf)
            {
                var imgPath = ImagePath(receipt.FileName);
                if (!File.Exists(imgPath))
                    await File.WriteAllBytesAsync(imgPath, ReceiptImageHelper.FixOrientation(bytes));
                onPage?.Report((0, imgPath));
                return [imgPath];
            }

            return await GetPdfPagePathsAsync(receipt.FileName, bytes, onPage);
        }
        catch
        {
            return [];
        }
    }

    private static async Task<IReadOnlyList<string>> GetPdfPagePathsAsync(
        string fileName, byte[] bytes, IProgress<(int Index, string Path)>? onPage)
    {
        // Writes a rendered page to disk and reports it. Runs on the render callback thread.
        void WriteAndReport(int index, byte[] jpeg)
        {
            var p = PagePath(fileName, index);
            try { File.WriteAllBytes(p, jpeg); } catch { return; }
            onPage?.Report((index, p));
        }

        var known = ReadPageCount(fileName);

        if (known is int total)
        {
            // We know how many pages exist. Reuse cached pages, render only the missing ones.
            var cached = new List<int>();
            for (var i = 0; i < total; i++)
                if (File.Exists(PagePath(fileName, i)))
                    cached.Add(i);

            // Report cached pages first, in order.
            foreach (var i in cached)
                onPage?.Report((i, PagePath(fileName, i)));

            var missing = Enumerable.Range(0, total).Except(cached).ToList();
            if (missing.Count > 0)
            {
                var rendered = await PdfThumbnailService.Instance.RenderPdfAllPagesAsync(
                    bytes, onPage: WriteAndReport, skipZeroBasedPages: cached);
                if (rendered == null && cached.Count == 0)
                    return [];
            }

            return BuildExistingPaths(fileName, total);
        }

        // No marker yet: render the whole document, then record the count.
        var all = await PdfThumbnailService.Instance.RenderPdfAllPagesAsync(bytes, onPage: WriteAndReport);
        if (all == null || all.Length == 0)
            return [];

        WritePageCount(fileName, all.Length);
        return BuildExistingPaths(fileName, all.Length);
    }

    private static List<string> BuildExistingPaths(string fileName, int total)
    {
        var paths = new List<string>(total);
        for (var i = 0; i < total; i++)
        {
            var p = PagePath(fileName, i);
            if (File.Exists(p))
                paths.Add(p);
        }
        return paths;
    }
}
