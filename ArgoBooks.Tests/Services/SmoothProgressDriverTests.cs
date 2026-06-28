using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for <see cref="SmoothProgressDriver"/>: the spread-aware easing curve, the real-signal
/// override, monotonicity, and the snap-to-100 on completion.
/// </summary>
public class SmoothProgressDriverTests
{
    [Fact]
    public void StartsAtZero()
    {
        var d = new SmoothProgressDriver(8000, 20000);
        Assert.Equal(0.0, d.ValueAt(0), 3);
    }

    [Fact]
    public void ReadsAboutHalfAtP50()
    {
        var d = new SmoothProgressDriver(8000, 20000);
        Assert.InRange(d.ValueAt(8000), 0.49, 0.51);
    }

    [Fact]
    public void ReadsAboutNinetyAtP90()
    {
        var d = new SmoothProgressDriver(8000, 20000);
        Assert.InRange(d.ValueAt(20000), 0.89, 0.91);
    }

    [Fact]
    public void NeverReachesCeilingOrOneBeforeComplete()
    {
        var d = new SmoothProgressDriver(8000, 20000);
        var v = d.ValueAt(10_000_000); // far beyond p90
        Assert.True(v < 1.0);
        Assert.True(v <= SmoothProgressDriver.Ceiling + 1e-9);
        Assert.True(v > 0.9);
    }

    [Fact]
    public void IsMonotonic_DoesNotDecreaseWhenTimeGoesBackward()
    {
        var d = new SmoothProgressDriver(8000, 20000);
        var later = d.ValueAt(15000);
        var earlier = d.ValueAt(5000);
        Assert.True(earlier >= later);
    }

    [Fact]
    public void Complete_SnapsToOneAndStays()
    {
        var d = new SmoothProgressDriver(8000, 20000);
        d.ValueAt(4000);
        Assert.Equal(1.0, d.Complete(), 6);
        Assert.Equal(1.0, d.ValueAt(1), 6);
    }

    [Fact]
    public void RealFraction_OverridesTimeEstimate()
    {
        var d = new SmoothProgressDriver(8000, 20000);
        d.SetRealFraction(0.25);
        Assert.InRange(d.ValueAt(1), 0.249, 0.251);
    }

    [Fact]
    public void WideSpread_ApproachesMoreSlowlyThanNarrow()
    {
        var narrow = new SmoothProgressDriver(8000, 10000);
        var wide = new SmoothProgressDriver(8000, 40000);
        // Same elapsed time past p50: the wider-spread driver should report less progress,
        // because its 50%->90% segment is stretched over a longer window.
        Assert.True(wide.ValueAt(12000) < narrow.ValueAt(12000));
    }
}
