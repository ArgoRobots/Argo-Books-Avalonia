namespace ArgoBooks.Core.Services;

/// <summary>
/// Computes a smooth, honest 0..1 progress value for one operation phase. Two modes:
/// <list type="bullet">
///   <item><b>Real signal</b>: when a true fraction is known (bytes uploaded, rows or
///   chunks processed), call <see cref="SetRealFraction"/> continuously and the value
///   follows it exactly.</item>
///   <item><b>Time estimate</b>: otherwise the value eases along a spread-aware curve
///   built from the operation's scaled p50/p90 anchors: it reads ~50% at p50 and ~90%
///   at p90, then crawls toward a ~97% ceiling and never reaches 100% until
///   <see cref="Complete"/>. A wide p50->p90 spread (e.g. receipts that vary 5-50s)
///   automatically yields a slower, flatter approach, so a long outlier shows the bar
///   still creeping in the 90s rather than parked at a false "almost done".</item>
/// </list>
/// The value is monotonic (never decreases). This class is pure: it holds no timer.
/// The UI layer ticks it with the elapsed time.
/// </summary>
public sealed class SmoothProgressDriver
{
    /// <summary>Highest value the time-estimate curve will reach before completion.</summary>
    public const double Ceiling = 0.97;

    private readonly double _p50Ms;
    private readonly double _p90Ms;
    private double _last;
    private double? _realFraction;
    private bool _complete;

    public SmoothProgressDriver(double p50Ms, double p90Ms)
    {
        _p50Ms = Math.Max(1, p50Ms);
        _p90Ms = Math.Max(_p50Ms * 1.2, p90Ms);
    }

    /// <summary>
    /// Supplies a true progress fraction (0..1) from a real signal. Once set, the value
    /// follows it instead of the time estimate. Intended to be called continuously as the
    /// real signal advances (e.g. rows processed / total).
    /// </summary>
    public void SetRealFraction(double fraction)
    {
        _realFraction = Math.Clamp(fraction, 0.0, 1.0);
    }

    /// <summary>Marks the operation finished. The value snaps to 1.0 and stays there.</summary>
    public double Complete()
    {
        _complete = true;
        _last = 1.0;
        return 1.0;
    }

    /// <summary>
    /// Returns the current value for the given elapsed time (ms). Monotonic and capped at
    /// <see cref="Ceiling"/> until <see cref="Complete"/> is called.
    /// </summary>
    public double ValueAt(double elapsedMs)
    {
        if (_complete)
            return 1.0;

        double target = _realFraction ?? TimeFraction(elapsedMs);
        target = Math.Min(target, Ceiling);
        if (target > _last)
            _last = target;
        return _last;
    }

    /// <summary>
    /// Spread-aware ease: 0 at t=0, 0.5 at p50, 0.9 at p90, then asymptotic toward the ceiling.
    /// </summary>
    private double TimeFraction(double t)
    {
        if (t <= 0)
            return 0;
        if (t <= _p50Ms)
            return 0.5 * (t / _p50Ms);
        if (t <= _p90Ms)
            return 0.5 + 0.4 * ((t - _p50Ms) / (_p90Ms - _p50Ms));

        double over = (t - _p90Ms) / _p90Ms;
        return 0.9 + (Ceiling - 0.9) * (1 - Math.Exp(-over));
    }
}
