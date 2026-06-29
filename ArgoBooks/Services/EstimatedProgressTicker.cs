using System.Diagnostics;
using ArgoBooks.Core.Services;
using Avalonia.Threading;

namespace ArgoBooks.Services;

/// <summary>
/// Drives a smooth, honest progress percentage on the UI thread for an opaque operation, such as a
/// single AI call whose true progress can't be observed. It builds a <see cref="SmoothProgressDriver"/>
/// from the operation's learned p50/p90 (via <see cref="OperationTimingService"/>) and ticks it with a
/// <see cref="DispatcherTimer"/>, reporting 0-100 to the supplied callback. The curve eases toward the
/// estimate and asymptotes near the ceiling, so it never sits at a false value or stalls.
///
/// When a real signal becomes available (bytes uploaded, rows processed), call
/// <see cref="SetRealFraction"/> and the bar follows it instead. Call <see cref="Stop"/> when the
/// operation finishes and hands off to the next phase, or <see cref="Complete"/> to snap to 100.
/// Must be created and used on the UI thread.
/// </summary>
public sealed class EstimatedProgressTicker : IDisposable
{
    private readonly Action<double> _reportPercent;
    private readonly SmoothProgressDriver _driver;
    private readonly Stopwatch _stopwatch = new();
    private DispatcherTimer? _timer;

    public EstimatedProgressTicker(
        OperationKind operation,
        Action<double> reportPercent,
        double? sizeFeature = null,
        long uploadBytes = 0,
        int? pageCount = null)
    {
        _reportPercent = reportPercent;
        var estimate = OperationTimingService.Instance?.Estimate(operation, sizeFeature, uploadBytes, pageCount);
        double p50 = estimate is { P50Ms: > 0 } ? estimate.P50Ms : 8000;
        double p90 = estimate is { P90Ms: > 0 } ? estimate.P90Ms : 18000;
        _driver = new SmoothProgressDriver(p50, p90);
    }

    /// <summary>Begins ticking (default every 100ms) and reports the initial value immediately.</summary>
    public void Start(TimeSpan? interval = null)
    {
        if (_timer != null)
            return;
        _stopwatch.Start();
        _timer = new DispatcherTimer { Interval = interval ?? TimeSpan.FromMilliseconds(100) };
        _timer.Tick += OnTick;
        _timer.Start();
        Report();
    }

    /// <summary>Feeds a real progress fraction (0..1); the bar follows it instead of the estimate.</summary>
    public void SetRealFraction(double fraction)
    {
        _driver.SetRealFraction(fraction);
        if (_timer != null)
            Report();
    }

    /// <summary>Stops ticking at the current value (use when handing off to the next phase).</summary>
    public void Stop()
    {
        if (_timer != null)
        {
            _timer.Tick -= OnTick;
            _timer.Stop();
            _timer = null;
        }
        _stopwatch.Stop();
    }

    /// <summary>Snaps the bar to 100% and stops.</summary>
    public void Complete()
    {
        _driver.Complete();
        _reportPercent(100.0);
        Stop();
    }

    public void Dispose() => Stop();

    private void OnTick(object? sender, EventArgs e) => Report();

    private void Report() => _reportPercent(_driver.ValueAt(_stopwatch.Elapsed.TotalMilliseconds) * 100.0);
}
