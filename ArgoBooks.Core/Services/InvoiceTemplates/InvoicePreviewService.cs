using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ArgoBooks.Core.Services.InvoiceTemplates;

/// <summary>
/// Service for previewing invoice HTML in the system browser.
/// </summary>
public static class InvoicePreviewService
{
    private static readonly string TempDirectory = Path.Combine(Path.GetTempPath(), "ArgoBooks", "InvoicePreviews");

    /// <summary>
    /// Opens the invoice HTML in the default browser for preview.
    /// </summary>
    /// <param name="html">The HTML content to preview.</param>
    /// <param name="invoiceId">Optional invoice ID for the filename.</param>
    /// <returns>True if the browser was opened successfully.</returns>
    public static async Task<bool> PreviewInBrowserAsync(string html, string? invoiceId = null)
    {
        try
        {
            // Ensure the temp directory exists
            Directory.CreateDirectory(TempDirectory);

            // Generate a filename
            var filename = $"invoice-preview-{invoiceId ?? Guid.NewGuid().ToString("N")[..8]}.html";
            var filePath = Path.Combine(TempDirectory, filename);

            // Write the HTML to the file
            await File.WriteAllTextAsync(filePath, html);

            // Open in default browser
            OpenUrl($"file://{filePath}");

            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Cleans up old preview files.
    /// </summary>
    public static void CleanupOldPreviews(TimeSpan maxAge)
    {
        try
        {
            if (!Directory.Exists(TempDirectory))
                return;

            var cutoff = DateTime.UtcNow - maxAge;
            var files = Directory.GetFiles(TempDirectory, "invoice-preview-*.html");

            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (info.CreationTimeUtc < cutoff)
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // Ignore deletion errors
                    }
                }
            }
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    /// <summary>
    /// Opens a URL in the default browser (cross-platform).
    /// </summary>
    private static void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch
        {
            // Try platform-specific approaches
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url.Replace("&", "^&")}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }
}
