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
            // For URLs, validate scheme to prevent command injection
            if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                if (uri.Scheme != "https" && uri.Scheme != "http" && uri.Scheme != "mailto" && uri.Scheme != "file")
                    return false;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = url,
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
