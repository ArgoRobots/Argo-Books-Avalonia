using System.Diagnostics;

namespace ArgoBooks.Core.Platform;

/// <summary>
/// Provides safe URL opening utilities with validation to prevent command injection.
/// </summary>
public static class UrlHelper
{
    /// <summary>
    /// Safely opens a URL or file path in the default application.
    /// Validates URLs to prevent command injection via shell metacharacters.
    /// </summary>
    public static bool SafeOpenUrl(string url)
    {
        try
        {
            // Require a valid absolute URI with an allowed scheme
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
                return false;

            if (uri.Scheme != "https" && uri.Scheme != "http" && uri.Scheme != "mailto" && uri.Scheme != "file")
                return false;

            Process.Start(new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
}
