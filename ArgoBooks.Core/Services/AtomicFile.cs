using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Atomic file replacement that survives transient interference from antivirus,
/// search indexers, and backup agents. These tools briefly lock or scan a freshly
/// written file, which makes a plain <see cref="File.Move(string, string, bool)"/>
/// fail intermittently, most visibly on a brand-new install the AV hasn't whitelisted
/// yet. Callers write their data to a temp path first, then call <see cref="ReplaceAsync"/>
/// to swap it onto the final path.
/// </summary>
public static class AtomicFile
{
    private const int MaxAttempts = 4;

    /// <summary>
    /// Moves <paramref name="tempPath"/> onto <paramref name="finalPath"/>, retrying with a
    /// short async backoff when the destination is transiently locked, then giving up and
    /// rethrowing if the lock persists.
    /// <para>
    /// The rename is the only swap used: on the same volume <see cref="File.Move(string, string, bool)"/>
    /// is atomic, so <paramref name="finalPath"/> is always either the old file or the new one,
    /// never a half-written mix. A non-atomic copy fallback is deliberately NOT used: it would
    /// truncate the destination and risk corrupting a company file if interrupted, and it rarely
    /// helps anyway because a lock strong enough to fail the rename also fails a copy's open-for-write.
    /// Callers that hold real user data should let the rethrow surface as a "couldn't save" error.
    /// </para>
    /// </summary>
    public static async Task ReplaceAsync(
        string tempPath,
        string finalPath,
        bool overwrite = true,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                File.Move(tempPath, finalPath, overwrite);
                return;
            }
            catch (FileNotFoundException)
            {
                // The temp source is gone (e.g. antivirus quarantined it between the
                // write and the move). Retrying can't bring it back; surface to the caller.
                throw;
            }
            catch (Exception ex) when ((ex is IOException || ex is UnauthorizedAccessException) && attempt < MaxAttempts)
            {
                // Destination briefly locked by AV / indexer / backup. Back off (without
                // blocking the calling thread) and retry. The last attempt's failure is not
                // caught here, so it propagates to the caller.
                await Task.Delay(50 * attempt, cancellationToken);
            }
        }
    }
}
