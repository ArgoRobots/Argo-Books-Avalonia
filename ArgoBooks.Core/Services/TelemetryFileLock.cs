namespace ArgoBooks.Core.Services;

/// <summary>
/// Serialises access to the device-global telemetry files across every running instance.
///
/// <para>
/// Multiple instances are supported deliberately (see <see cref="AtomicFile.TempPathFor"/>),
/// and they all read and write one events file under AppData. The
/// <see cref="SemaphoreSlim"/> inside <see cref="TelemetryStorageService"/> only orders
/// callers within a single process, so without this two instances interleave their
/// read-modify-write cycles freely. That produced two distinct symptoms in the field:
/// an <see cref="IOException"/> when one instance opened the file while another was
/// renaming a new copy over it, and duplicate uploads when an instance wrote a stale
/// copy back over the "already uploaded" flags another had just recorded, so the same
/// events were sent again and counted twice on the dashboard.
/// </para>
///
/// <para>
/// The lock is an exclusive OS handle on a small file next to the data it guards, the same
/// mechanism <see cref="CompanyInstanceLock"/> uses, so it is released by the OS if a
/// process dies while holding it and can never deadlock across launches. Unlike that class
/// this one waits: contention here lasts a few milliseconds and the caller wants the data,
/// not a "busy" answer. Deliberately NOT <see cref="FileOptions.DeleteOnClose"/>: this lock
/// is taken constantly, and a delete pending on Windows makes concurrent openers fail with
/// <see cref="UnauthorizedAccessException"/> rather than queue politely.
/// </para>
/// </summary>
public sealed class TelemetryFileLock : IDisposable
{
    private const string LockFileName = ".storage.lock";

    /// <summary>
    /// How long to keep retrying before giving up. Generous next to the few milliseconds a
    /// real write takes, because giving up means proceeding unguarded, which is the
    /// behaviour we are trying to get rid of.
    /// </summary>
    private static readonly TimeSpan AcquireTimeout = TimeSpan.FromSeconds(3);

    private FileStream? _handle;

    private TelemetryFileLock(FileStream handle) => _handle = handle;

    /// <summary>
    /// Waits for the lock and returns a handle to release it, or null if it could not be
    /// taken in time or the lock file itself is unusable.
    ///
    /// <para>
    /// A null return means "carry on without the lock", never "abandon the write". Telemetry
    /// is best-effort, and a permissions problem on the lock file must not stop the app
    /// recording what the user did. The caller does not need to null-check before disposing:
    /// <c>using var _ = await AcquireAsync(...)</c> handles null fine.
    /// </para>
    /// </summary>
    public static async Task<TelemetryFileLock?> AcquireAsync(
        string directory,
        IErrorLogger? errorLogger = null,
        CancellationToken cancellationToken = default)
    {
        string lockPath;
        try
        {
            Directory.CreateDirectory(directory);
            lockPath = Path.Combine(directory, LockFileName);
        }
        catch (Exception ex)
        {
            errorLogger?.LogDebug($"Telemetry lock directory unavailable: {ex.Message}");
            return null;
        }

        var deadline = DateTime.UtcNow + AcquireTimeout;
        var delayMs = 5;

        while (true)
        {
            try
            {
                var handle = new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.WriteThrough);

                return new TelemetryFileLock(handle);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                // Held by another instance, or briefly unopenable while Windows finishes
                // closing someone else's handle. Both are worth waiting out.
                if (DateTime.UtcNow >= deadline)
                {
                    errorLogger?.LogDebug(
                        $"Telemetry lock busy for {AcquireTimeout.TotalSeconds:0}s; continuing unguarded.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                errorLogger?.LogDebug($"Telemetry lock unavailable: {ex.Message}");
                return null;
            }

            try
            {
                await Task.Delay(delayMs, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                return null;
            }

            // Back off gently up to a ceiling, so a long hold doesn't spin the CPU and a
            // short one is still picked up almost immediately.
            delayMs = Math.Min(delayMs * 2, 100);
        }
    }

    public void Dispose()
    {
        try
        {
            _handle?.Dispose();
        }
        catch
        {
            // The OS releases the handle on process exit regardless.
        }

        _handle = null;
    }
}
