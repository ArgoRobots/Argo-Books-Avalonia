using System.Diagnostics;

namespace ArgoBooks.Core.Platform.Linux;

/// <summary>
/// Provides secure credential storage on Linux via the Secret Service API (libsecret).
/// Uses the `secret-tool` CLI which integrates with GNOME Keyring and KDE Wallet.
/// </summary>
internal static class LinuxSecretStorage
{
    private const string SecretToolPath = "secret-tool";
    private const string AttributeApp = "application";
    private const string AttributeAppValue = "ArgoBooks";
    private const string AttributeFileId = "fileId";

    /// <summary>
    /// Checks if secret-tool is available on this system.
    /// </summary>
    public static bool IsAvailable()
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = "which",
                Arguments = SecretToolPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) return false;
            process.WaitForExit(3000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Stores a password in the system keyring.
    /// </summary>
    public static void Store(string fileId, string password)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = SecretToolPath,
                Arguments = $"store --label=\"Argo Books Password\" {AttributeApp} {AttributeAppValue} {AttributeFileId} {fileId}",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) return;

            process.StandardInput.Write(password);
            process.StandardInput.Close();
            process.WaitForExit(10000);
        }
        catch
        {
            // Silently fail — user will need to enter password manually
        }
    }

    /// <summary>
    /// Looks up a stored password from the system keyring.
    /// </summary>
    public static string? Lookup(string fileId)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = SecretToolPath,
                Arguments = $"lookup {AttributeApp} {AttributeAppValue} {AttributeFileId} {fileId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            if (process == null) return null;
            process.WaitForExit(10000);

            if (process.ExitCode != 0) return null;

            var result = process.StandardOutput.ReadToEnd().TrimEnd('\n', '\r');
            return string.IsNullOrEmpty(result) ? null : result;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Clears a stored password from the system keyring.
    /// </summary>
    public static void Clear(string fileId)
    {
        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = SecretToolPath,
                Arguments = $"clear {AttributeApp} {AttributeAppValue} {AttributeFileId} {fileId}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            process?.WaitForExit(5000);
        }
        catch
        {
            // Silently fail
        }
    }
}
