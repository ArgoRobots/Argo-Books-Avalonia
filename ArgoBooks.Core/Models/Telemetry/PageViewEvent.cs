namespace ArgoBooks.Core.Models.Telemetry;

/// <summary>
/// One visit to one screen, recorded when the user leaves it.
///
/// <para>
/// Separate from <see cref="FeatureUsageEvent"/> rather than another FeatureName, because a
/// navigation is not a feature. Every other feature event marks something the user chose to
/// do; page views fire constantly on the way to those choices, and mixed into the same list
/// they bury the events that mean something.
/// </para>
///
/// <para>
/// Both durations are kept for the same reason a session records both: wall clock counts a
/// screen left open while someone takes a call, and active time alone cannot distinguish a
/// page nobody opened from one somebody stared at without touching. The pair separates
/// "stuck here" from "walked away here", which is the actual question.
/// </para>
/// </summary>
public class PageViewEvent : TelemetryEvent
{
    /// <inheritdoc />
    public override TelemetryDataType DataType => TelemetryDataType.PageView;

    /// <summary>Which screen, using the same names the navigation raises.</summary>
    public string? PageName { get; set; }

    /// <summary>
    /// Seconds the app was actually being driven while this page was open. Gaps longer than
    /// the idle threshold contribute nothing, so a page left open over lunch reads as zero.
    /// </summary>
    public long ActiveSeconds { get; set; }

    /// <summary>
    /// Wall clock seconds the page was open. Always greater than or equal to
    /// <see cref="ActiveSeconds"/>; a large gap between the two is someone who left.
    /// </summary>
    public long DurationSeconds { get; set; }
}
