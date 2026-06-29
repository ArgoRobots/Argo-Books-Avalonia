namespace ArgoBooks.Core.Services;

/// <summary>
/// Pooled duration statistics for a single operation, served by
/// <c>/api/ai/timing-priors.php</c> for the currently configured model. Durations are
/// the SERVER-measured Gemini wall time (the user's network is excluded), so the
/// client adds its own measured network time on top.
/// </summary>
public sealed class OperationPrior
{
    public required OperationKind Operation { get; init; }

    /// <summary>Median server-compute time (ms). The bar reads ~50% here.</summary>
    public double P50Ms { get; init; }

    /// <summary>90th-percentile server-compute time (ms). The bar reads ~90% here.</summary>
    public double P90Ms { get; init; }

    /// <summary>Number of samples behind this prior (0 for bundled seeds).</summary>
    public int SampleCount { get; init; }

    /// <summary>Average size feature for the samples, used to scale by document size.</summary>
    public double? AvgSizeFeature { get; init; }

    /// <summary>Average output token count (diagnostic / future use).</summary>
    public double? AvgOutputTokens { get; init; }

    /// <summary>Milliseconds per page (PDF operations), used to scale by page count.</summary>
    public double? PerPageMs { get; init; }
}

/// <summary>
/// A full set of timing priors for one model, plus the current server load factor.
/// </summary>
public sealed class TimingPriors
{
    public string Model { get; init; } = "";

    /// <summary>
    /// How much slower (or faster) calls are running right now versus their baseline.
    /// 1.0 = normal. Biases every estimate up/down when the server is busy.
    /// </summary>
    public double LoadFactor { get; init; } = 1.0;

    public IReadOnlyList<OperationPrior> Priors { get; init; } = [];

    public OperationPrior? For(OperationKind op)
    {
        foreach (var p in Priors)
        {
            if (p.Operation == op)
                return p;
        }
        return null;
    }

    /// <summary>
    /// Bundled seed priors so the very first run (before any server fetch) is already
    /// reasonable. These self-calibrate from real runs and are replaced by the fetched
    /// pooled priors; a model swap server-side updates those with no app release.
    /// </summary>
    public static TimingPriors Seed { get; } = new()
    {
        Model = "seed",
        LoadFactor = 1.0,
        Priors = new[]
        {
            new OperationPrior { Operation = OperationKind.ReceiptScan,         P50Ms = 9000,  P90Ms = 22000 },
            new OperationPrior { Operation = OperationKind.BankPdfExtract,      P50Ms = 12000, P90Ms = 28000, PerPageMs = 3500 },
            new OperationPrior { Operation = OperationKind.BankCategorize,      P50Ms = 7000,  P90Ms = 16000 },
            new OperationPrior { Operation = OperationKind.SupplierCategory,    P50Ms = 2500,  P90Ms = 6000 },
            new OperationPrior { Operation = OperationKind.SpreadsheetAnalysis, P50Ms = 6000,  P90Ms = 15000 },
            new OperationPrior { Operation = OperationKind.SpreadsheetProcess,  P50Ms = 8000,  P90Ms = 18000 },
            new OperationPrior { Operation = OperationKind.Completion,          P50Ms = 5000,  P90Ms = 12000 },
        },
    };
}

/// <summary>
/// One operation's duration estimate. <see cref="P50Ms"/>/<see cref="P90Ms"/> are the
/// scaled compute-phase anchors that drive <see cref="SmoothProgressDriver"/>;
/// <see cref="NetworkMs"/> is the estimated upload time (real bytes drive that phase
/// when available).
/// </summary>
public sealed class OperationEstimate
{
    public double ComputeMs { get; init; }
    public double NetworkMs { get; init; }
    public double TotalMs { get; init; }
    public double P50Ms { get; init; }
    public double P90Ms { get; init; }
}
