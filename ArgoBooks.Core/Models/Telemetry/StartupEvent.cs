namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// How long a launch actually took, split at the first moment the app could draw anything.
///
/// <para>
/// The split is the whole point. Users who click the shortcut a second time are reacting to
/// a screen with nothing on it, and a splash window can only appear once managed code is
/// already running. Everything before that (loading the runtime, mapping assemblies, the
/// first-run antivirus scan of a fresh install) is invisible to any in-app timer that starts
/// at Main. Measuring from the OS process start time is what separates "our startup is slow"
/// from "the machine took eight seconds to get to our first line", and those need completely
/// different fixes.
/// </para>
/// </summary>
public class StartupEvent : TelemetryEvent
{
    /// <inheritdoc />
    public override TelemetryDataType DataType => TelemetryDataType.Startup;

    /// <summary>
    /// Process start to the splash window being on screen. This is the dead time a splash
    /// cannot cover, and the number to look at before optimising anything else.
    /// </summary>
    public long? ToFirstPaintMs { get; set; }

    /// <summary>
    /// Process start to the main window opening, so it includes
    /// <see cref="ToFirstPaintMs"/> rather than continuing from it.
    /// </summary>
    public long? ToReadyMs { get; set; }

    /// <summary>
    /// Milliseconds from process start to the service graph being built, before any view
    /// model exists. Sits between <see cref="ToFirstPaintMs"/> and
    /// <see cref="ToViewModelsReadyMs"/>; like every mark here it is measured from process
    /// start, so the segments are differences rather than sums.
    /// </summary>
    public long? ToServicesReadyMs { get; set; }

    /// <summary>
    /// Milliseconds from process start to the view models being built, immediately before
    /// the main window is constructed.
    /// </summary>
    public long? ToViewModelsReadyMs { get; set; }

    /// <summary>
    /// True when no other instance was already running. A second instance starting while the
    /// first is live reads its files from the OS cache and is not comparable to a cold launch,
    /// so mixing the two would flatter the average exactly when we are investigating relaunches.
    /// </summary>
    public bool ColdStart { get; set; }
}
