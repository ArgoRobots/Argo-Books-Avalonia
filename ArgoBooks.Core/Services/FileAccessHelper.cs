namespace ArgoBooks.Core.Services;

/// <summary>
/// Heuristics for recognizing file failures that are most likely caused by security
/// software, antivirus, Windows Controlled Folder Access (ransomware protection), a
/// denied ACL, or another app holding a lock, rather than by a bug or a full disk.
/// Used to give the user a clear, actionable message instead of a raw "access denied".
/// </summary>
public static class FileAccessHelper
{
    // Win32 error codes live in the low 16 bits of an HResult of the form 0x8007xxxx.
    private const int ERROR_ACCESS_DENIED = 0x5;
    private const int ERROR_SHARING_VIOLATION = 0x20;
    private const int ERROR_LOCK_VIOLATION = 0x21;

    /// <summary>
    /// Returns true when <paramref name="ex"/> looks like a save/write that was blocked
    /// by security software rather than a programming error or out-of-space condition.
    /// Deliberately conservative: only access-denied and lock/sharing violations qualify,
    /// so genuine bugs (missing file, bad path, disk full) keep their normal handling.
    /// </summary>
    public static bool IsLikelySecurityBlock(Exception? ex)
    {
        switch (ex)
        {
            // Access denied: Controlled Folder Access, an AV-quarantined folder, or a
            // denied ACL all surface as UnauthorizedAccessException.
            case UnauthorizedAccessException:
                return true;

            // FileNotFound / DirectoryNotFound derive from IOException but indicate a
            // missing target, not a security block, so exclude them explicitly.
            case FileNotFoundException:
            case DirectoryNotFoundException:
                return false;

            // A file held open by AV / an indexer / a backup agent surfaces as an
            // IOException carrying a sharing- or lock-violation (or access-denied) code.
            case IOException io:
                var code = io.HResult & 0xFFFF;
                return code is ERROR_ACCESS_DENIED or ERROR_SHARING_VIOLATION or ERROR_LOCK_VIOLATION;

            default:
                return false;
        }
    }
}
