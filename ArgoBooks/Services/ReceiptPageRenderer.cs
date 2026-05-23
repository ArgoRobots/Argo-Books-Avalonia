using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Services;

namespace ArgoBooks.Services;

/// <summary>
/// Resolves a receipt's displayable page image paths, rendering PDFs to one JPEG per page
/// (cached in temp) and orienting raster images. Shared by every full-size receipt viewer so
/// the PDF-vs-image branching and temp caching live in one place.
/// </summary>
public static class ReceiptPageRenderer
{
    private static string TempDir => Path.Combine(Path.GetTempPath(), "ArgoBooks", "Receipts");

    /// <summary>
    /// Returns the page image paths for a receipt, in order. PDFs render all pages; raster
    /// images return a single oriented file. Returns an empty list when there is no data or
    /// rendering fails. Results are cached on disk and reused on subsequent calls.
    /// </summary>
    public static async Task<IReadOnlyList<string>> GetPagePathsAsync(Receipt receipt)
    {
        if (string.IsNullOrEmpty(receipt.FileData))
            return [];

        try
        {
            Directory.CreateDirectory(TempDir);
            var bytes = Convert.FromBase64String(receipt.FileData);
            var isPdf = receipt.FileType?.Contains("pdf", StringComparison.OrdinalIgnoreCase) == true
                        || receipt.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase);

            if (!isPdf)
            {
                var imgPath = Path.Combine(TempDir, receipt.FileName);
                if (!File.Exists(imgPath))
                    await File.WriteAllBytesAsync(imgPath, ReceiptImageHelper.FixOrientation(bytes));
                return [imgPath];
            }

            var nameNoExt = Path.GetFileNameWithoutExtension(receipt.FileName);

            // Reuse cached pages if page 1 already exists.
            var cached = CachedPagePaths(nameNoExt);
            if (cached.Count > 0)
                return cached;

            var rendered = await PdfThumbnailService.Instance.RenderPdfAllPagesAsync(bytes);
            if (rendered == null || rendered.Length == 0)
                return [];

            var paths = new List<string>(rendered.Length);
            for (var i = 0; i < rendered.Length; i++)
            {
                var p = Path.Combine(TempDir, $"{nameNoExt}_p{i + 1}.jpg");
                await File.WriteAllBytesAsync(p, rendered[i]);
                paths.Add(p);
            }
            return paths;
        }
        catch
        {
            return [];
        }
    }

    private static List<string> CachedPagePaths(string nameNoExt)
    {
        var result = new List<string>();
        for (var i = 1; ; i++)
        {
            var p = Path.Combine(TempDir, $"{nameNoExt}_p{i}.jpg");
            if (!File.Exists(p))
                break;
            result.Add(p);
        }
        return result;
    }
}
