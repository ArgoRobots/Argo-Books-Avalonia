namespace ArgoBooks.Core.Services;

/// <summary>
/// Creates the per-company temp directories that hold a company's decrypted files. Centralizes the
/// creation and owner-only permission hardening so <see cref="CompanyManager"/> and
/// <see cref="FileService"/> can't drift apart.
/// </summary>
internal static class SecureTempDirectory
{
    /// <summary>
    /// Creates a unique ArgoBooks temp directory under the OS temp path. On Unix the directory is
    /// restricted to the owner so other local users on a shared machine can't read the decrypted
    /// company files; this is a no-op on Windows (temp is already per-user).
    /// </summary>
    public static string Create()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "ArgoBooks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        if (!OperatingSystem.IsWindows())
        {
            try { File.SetUnixFileMode(tempPath, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute); }
            catch { /* best effort; permissions hardening only */ }
        }
        return tempPath;
    }
}
