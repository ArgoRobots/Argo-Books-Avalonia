namespace ArgoBooks.Services;

/// <summary>
/// Removes stale receipt preview/render files from the temp directory. Rendered PDF pages,
/// receipt previews, and bulk-scan thumbnails accumulate under %TEMP%/ArgoBooks and are
/// regenerated on demand, so old ones can be safely deleted to keep the temp folder from growing
/// without bound. Recent files are kept so the on-disk cache still speeds up reopening receipts.
/// </summary>
public static class ReceiptTempCleanup
{
    private static readonly string[] Subdirectories = ["Receipts", "ScanPreview", "BulkScanPreview"];

    /// <summary>Files not modified within this window are considered stale and deleted.</summary>
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(7);

    /// <summary>
    /// Deletes cached receipt preview files older than <see cref="MaxAge"/>. Safe to call
    /// fire-and-forget at startup; never throws.
    /// </summary>
    public static Task CleanOldFilesAsync() => Task.Run(CleanOldFiles);

    private static void CleanOldFiles()
    {
        var cutoff = DateTime.UtcNow - MaxAge;
        var root = Path.Combine(Path.GetTempPath(), "ArgoBooks");

        foreach (var sub in Subdirectories)
        {
            var dir = Path.Combine(root, sub);
            if (!Directory.Exists(dir))
                continue;

            try
            {
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    try
                    {
                        if (File.GetLastWriteTimeUtc(file) < cutoff)
                            File.Delete(file);
                    }
                    catch
                    {
                        // File in use or already gone, skip it.
                    }
                }
            }
            catch
            {
                // Directory enumeration failed, non-critical, skip this subdirectory.
            }
        }
    }
}
