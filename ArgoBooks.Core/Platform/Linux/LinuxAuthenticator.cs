using System.Diagnostics;

namespace ArgoBooks.Core.Platform.Linux;

/// <summary>
/// Provides user authentication on Linux via polkit (pkexec).
/// Shows the system authentication dialog which supports fingerprint readers
/// if fprintd is configured.
/// </summary>
internal static class LinuxAuthenticator
{
    private const string PkexecPath = "/usr/bin/pkexec";

    /// <summary>
    /// Checks if pkexec is available on this system.
    /// </summary>
    public static bool IsAvailable()
    {
        try
        {
            return File.Exists(PkexecPath);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Authenticates the user via the system polkit agent.
    /// Shows the native authentication dialog (supports password and fingerprint).
    /// </summary>
    /// <returns>True if the user authenticated successfully.</returns>
    public static async Task<bool> AuthenticateAsync()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = PkexecPath,
                Arguments = "/bin/true",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            });

            if (process == null) return false;

            await process.WaitForExitAsync();

            // Exit code 0 = authenticated, 126 = dismissed, 127 = not authorized
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
