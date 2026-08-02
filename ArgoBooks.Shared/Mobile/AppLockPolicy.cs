namespace ArgoBooks.Shared.Mobile;

/// <summary>
/// Pure decision logic for the biometric app lock: should the lock screen be shown right now?
/// Framework-agnostic (no MAUI/Android dependency) so it is fully unit-testable; the Android head
/// (ArgoBooks.Mobile) supplies the actual clock and the "last backgrounded" timestamp and calls
/// into this on cold start and on resume from background.
/// </summary>
public static class AppLockPolicy
{
    /// <summary>Default grace period: resuming within this window after backgrounding does not re-lock.</summary>
    public static readonly TimeSpan DefaultGracePeriod = TimeSpan.FromMinutes(1);

    /// <summary>
    /// Decides whether the lock screen should be shown.
    /// </summary>
    /// <param name="isLockEnabled">The user's "biometric app lock" setting (default on).</param>
    /// <param name="isPaired">Whether a company is currently paired (nothing to protect otherwise).</param>
    /// <param name="lastBackgroundedUtc">
    /// When the app was last sent to the background, or null if it hasn't been backgrounded yet
    /// this process lifetime (i.e. this is a cold start).
    /// </param>
    /// <param name="nowUtc">The current time.</param>
    /// <param name="gracePeriod">How long after backgrounding the app can resume without re-locking.</param>
    public static bool ShouldLock(bool isLockEnabled, bool isPaired, DateTime? lastBackgroundedUtc, DateTime nowUtc, TimeSpan gracePeriod)
    {
        if (!isLockEnabled || !isPaired)
        {
            return false;
        }

        // No backgrounding recorded yet this process lifetime: cold start, always lock.
        if (lastBackgroundedUtc == null)
        {
            return true;
        }

        return nowUtc - lastBackgroundedUtc.Value >= gracePeriod;
    }
}
