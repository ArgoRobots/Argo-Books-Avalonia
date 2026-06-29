namespace ArgoBooks.Core.Services;

/// <summary>
/// Turns pooled server timing priors plus locally-measured calibration into a duration
/// estimate for one AI/import operation. The estimate splits into:
/// <list type="bullet">
///   <item>compute time = pooled p50 scaled by document size, current server load, and a
///   per-machine calibration factor;</item>
///   <item>network time = upload bytes / locally-measured upload speed.</item>
/// </list>
/// Both halves come from real measurements, not a fixed guess, and self-calibrate as
/// real runs complete via <see cref="RecordResult"/>. Safe to use before any server
/// fetch: it falls back to <see cref="TimingPriors.Seed"/>.
///
/// Learned state (<see cref="UserCalibration"/>, <see cref="UploadBytesPerMs"/>) is
/// plain properties so the persistence layer can load/save them; the math here is
/// deterministic and unit-tested.
/// </summary>
public sealed class OperationEstimator
{
    private readonly object _lock = new();
    private TimingPriors _priors;

    public OperationEstimator(TimingPriors? priors = null)
    {
        _priors = priors ?? TimingPriors.Seed;
        CurrentLoadFactor = _priors.LoadFactor > 0 ? _priors.LoadFactor : 1.0;
    }

    /// <summary>Per-machine multiplier: measured server-compute / pooled p50. Starts neutral.</summary>
    public double UserCalibration { get; set; } = 1.0;

    /// <summary>Measured upload throughput (bytes per millisecond). Default ~12 Mbps.</summary>
    public double UploadBytesPerMs { get; set; } = 1500;

    /// <summary>Freshest known server load factor (from priors or the last call's response).</summary>
    public double CurrentLoadFactor { get; set; } = 1.0;

    /// <summary>Replaces the priors (e.g. after a successful fetch) and refreshes the load factor.</summary>
    public void SetPriors(TimingPriors priors)
    {
        ArgumentNullException.ThrowIfNull(priors);
        lock (_lock)
        {
            _priors = priors;
            if (priors.LoadFactor > 0)
                CurrentLoadFactor = priors.LoadFactor;
        }
    }

    /// <summary>Updates the live load factor (e.g. from the last AI response's timing block).</summary>
    public void UpdateLoadFactor(double loadFactor)
    {
        if (loadFactor > 0)
            CurrentLoadFactor = Clamp(loadFactor, 0.3, 4.0);
    }

    /// <summary>
    /// Estimates one operation. <paramref name="sizeFeature"/> is the op-specific size hint
    /// (image bytes, line count, column count, pdf bytes); <paramref name="uploadBytes"/> is
    /// the payload uploaded (0 for text-only ops); <paramref name="pageCount"/> scales PDF ops
    /// by pages when a per-page prior is known.
    /// </summary>
    public OperationEstimate Estimate(
        OperationKind op,
        double? sizeFeature = null,
        long uploadBytes = 0,
        int? pageCount = null)
    {
        OperationPrior prior;
        double load;
        lock (_lock)
        {
            prior = _priors.For(op) ?? TimingPriors.Seed.For(op) ?? SeedFallback(op);
            load = Clamp(CurrentLoadFactor, 0.3, 4.0);
        }

        // Prefer a per-page model for PDFs when both the prior slope and a page count exist.
        bool perPage = pageCount is > 0 && prior.PerPageMs is > 0;
        double spread = prior.P50Ms > 0 ? prior.P90Ms / prior.P50Ms : 2.0;
        double baseP50 = perPage ? prior.PerPageMs!.Value * pageCount!.Value : prior.P50Ms;
        double baseP90 = perPage ? baseP50 * spread : prior.P90Ms;

        double scale = SizeCorrection(prior, sizeFeature) * load * Clamp(UserCalibration, 0.25, 4.0);
        double computeP50 = baseP50 * scale;
        double computeP90 = Math.Max(baseP90 * scale, computeP50 * 1.2);

        double networkMs = uploadBytes > 0 && UploadBytesPerMs > 0 ? uploadBytes / UploadBytesPerMs : 0;

        return new OperationEstimate
        {
            ComputeMs = computeP50,
            NetworkMs = networkMs,
            TotalMs = computeP50 + networkMs,
            P50Ms = computeP50,
            P90Ms = computeP90,
        };
    }

    /// <summary>
    /// Feeds back a completed run so future estimates self-calibrate.
    /// <paramref name="serverComputeMs"/> is the server-measured Gemini time (from the response
    /// timing block); <paramref name="totalWallClockMs"/> is the client's full stopwatch; the
    /// difference is the user's network time, which trains the upload-speed estimate.
    /// </summary>
    public void RecordResult(
        OperationKind op,
        double serverComputeMs,
        double totalWallClockMs,
        long uploadBytes = 0)
    {
        if (serverComputeMs <= 0)
            return;

        OperationPrior? prior;
        lock (_lock)
        {
            prior = _priors.For(op);
        }

        if (prior is { P50Ms: > 0 })
        {
            double ratio = Clamp(serverComputeMs / prior.P50Ms, 0.25, 4.0);
            UserCalibration = Ema(UserCalibration, ratio, 0.2);
        }

        double networkMs = totalWallClockMs - serverComputeMs;
        if (uploadBytes > 0 && networkMs > 1)
        {
            double bytesPerMs = uploadBytes / networkMs;
            // Ignore implausible values (clock skew, tiny payloads) so one bad sample can't
            // poison the running average.
            if (bytesPerMs is > 0 and < 1_000_000)
                UploadBytesPerMs = Ema(UploadBytesPerMs, bytesPerMs, 0.3);
        }
    }

    private static double SizeCorrection(OperationPrior prior, double? sizeFeature)
    {
        if (sizeFeature is > 0 && prior.AvgSizeFeature is > 0)
            return Clamp(sizeFeature.Value / prior.AvgSizeFeature.Value, 0.5, 2.5);
        return 1.0;
    }

    private static OperationPrior SeedFallback(OperationKind op) =>
        new() { Operation = op, P50Ms = 5000, P90Ms = 12000 };

    private static double Ema(double current, double sample, double alpha) =>
        alpha * sample + (1 - alpha) * current;

    private static double Clamp(double v, double lo, double hi) => Math.Max(lo, Math.Min(hi, v));
}
