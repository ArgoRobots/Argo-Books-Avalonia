using System.Diagnostics;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Measures a launch from the moment the OS created the process, not from the moment our
/// code started running.
///
/// <para>
/// The distinction is the entire reason this exists. A stopwatch started in <c>Main</c> or
/// in <c>OnFrameworkInitializationCompleted</c> cannot see the runtime being loaded, the
/// assemblies being mapped, or antivirus scanning a freshly installed binary the first time
/// it is touched, and on a slow disk that is most of the wait. It is also the part a splash
/// window can never cover, because the splash is drawn by the very code that is waiting to
/// start. A user staring at nothing and clicking the shortcut again is reacting to exactly
/// this window, so measuring anything else answers the wrong question.
/// </para>
///
/// <para>
/// <see cref="Process.StartTime"/> is the OS's own record and needs no cooperation from us.
/// It can throw where the platform will not hand over process details, so every read is
/// guarded and the whole thing degrades to null rather than failing a launch. A null here
/// means "we could not time this run", never zero, which would quietly read as instant.
/// </para>
/// </summary>
public static class StartupTimeline
{
    private static readonly DateTime? ProcessStartUtc = ReadProcessStartUtc();

    private static long? _toFirstPaintMs;
    private static bool _reported;

    /// <summary>
    /// True when no sibling instance was already running as this one started. A relaunch
    /// while the first instance is live reads everything from the OS file cache, so its
    /// timings are not comparable with a genuine cold start and averaging the two together
    /// would hide the slow launches we are looking for.
    /// </summary>
    public static bool IsColdStart { get; } = ReadIsColdStart();

    /// <summary>
    /// Call the instant the splash window is actually on screen. Records the dead time the
    /// user spent looking at nothing. Ignores repeat calls.
    /// </summary>
    public static void MarkFirstPaint()
    {
        _toFirstPaintMs ??= ElapsedSinceProcessStartMs();
    }

    /// <summary>
    /// Milliseconds from process start to the splash appearing, or null if it was never
    /// marked (the splash failed to open) or the process start time was unreadable.
    /// </summary>
    public static long? ToFirstPaintMs => _toFirstPaintMs;

    /// <summary>
    /// Milliseconds from process start to now. Called when the main window opens, so it
    /// spans the whole launch and includes <see cref="ToFirstPaintMs"/> rather than
    /// continuing from it.
    /// </summary>
    public static long? ToReadyMs() => ElapsedSinceProcessStartMs();

    /// <summary>
    /// True the first time it is called and false afterwards, so the one-event-per-launch
    /// rule holds even if the main window's Opened handler fires more than once.
    /// </summary>
    public static bool TryClaimReport()
    {
        if (_reported)
        {
            return false;
        }

        _reported = true;
        return true;
    }

    private static long? ElapsedSinceProcessStartMs()
    {
        if (ProcessStartUtc is not { } start)
        {
            return null;
        }

        var elapsed = (DateTime.UtcNow - start).TotalMilliseconds;

        // A machine suspended mid-launch, or a clock corrected by NTP between process start
        // and now, produces a figure that describes the clock rather than the app. Drop
        // those instead of letting them drag an average around.
        if (elapsed < 0 || elapsed > TimeSpan.FromMinutes(10).TotalMilliseconds)
        {
            return null;
        }

        return (long)elapsed;
    }

    private static DateTime? ReadProcessStartUtc()
    {
        try
        {
            return Process.GetCurrentProcess().StartTime.ToUniversalTime();
        }
        catch (Exception)
        {
            // Some sandboxed and containerised environments refuse process details.
            // Timing is a diagnostic, never a requirement.
            return null;
        }
    }

    private static bool ReadIsColdStart()
    {
        try
        {
            var current = Process.GetCurrentProcess();
            return !Process.GetProcessesByName(current.ProcessName)
                .Any(p => p.Id != current.Id);
        }
        catch (Exception)
        {
            // Unreadable, so don't claim either way. Reporting this as cold would put
            // warm relaunches into the cold bucket, which is the comparison we care about.
            return false;
        }
    }
}
