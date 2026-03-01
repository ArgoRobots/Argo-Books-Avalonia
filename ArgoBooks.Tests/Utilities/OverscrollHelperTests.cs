using Avalonia;
using ArgoBooks.Helpers;
using Avalonia.Controls;
using Xunit;

namespace ArgoBooks.Tests.Utilities;

/// <summary>
/// Tests for the OverscrollHelper class.
/// </summary>
public class OverscrollHelperTests
{
    #region Constants Tests

    [Fact]
    public void DefaultResistance_Is0Point3()
    {
        Assert.Equal(0.3, OverscrollHelper.DefaultResistance);
    }

    [Fact]
    public void DefaultMaxDistance_Is100()
    {
        Assert.Equal(100.0, OverscrollHelper.DefaultMaxDistance);
    }

    #endregion

    #region CalculateOverscroll Tests

    [Fact]
    public void CalculateOverscroll_WithinBounds_NoOverscroll()
    {
        var control = new LayoutTransformControl();
        var helper = new OverscrollHelper(control);

        var result = helper.CalculateOverscroll(50, 50, 100, 100);

        Assert.Equal(50, result.ClampedX);
        Assert.Equal(50, result.ClampedY);
        Assert.Equal(0, result.OverscrollX);
        Assert.Equal(0, result.OverscrollY);
    }

    [Fact]
    public void CalculateOverscroll_BeyondMaxX_ClampsAndReportsOverscroll()
    {
        var control = new LayoutTransformControl();
        var helper = new OverscrollHelper(control);

        var result = helper.CalculateOverscroll(150, 50, 100, 100);

        Assert.Equal(100, result.ClampedX);
        Assert.True(result.OverscrollX > 0);
    }

    [Fact]
    public void CalculateOverscroll_BelowZeroX_ClampsAndReportsNegativeOverscroll()
    {
        var control = new LayoutTransformControl();
        var helper = new OverscrollHelper(control);

        var result = helper.CalculateOverscroll(-50, 50, 100, 100);

        Assert.Equal(0, result.ClampedX);
        Assert.True(result.OverscrollX < 0);
    }

    #endregion

    #region HasOverscroll Tests

    [Fact]
    public void HasOverscroll_DefaultIsFalse()
    {
        var control = new LayoutTransformControl();
        var helper = new OverscrollHelper(control);

        Assert.False(helper.HasOverscroll);
    }

    [Fact]
    public void HasOverscroll_AfterApplyOverscroll_IsTrue()
    {
        var control = new LayoutTransformControl();
        var helper = new OverscrollHelper(control);

        helper.ApplyOverscroll(10, 0);

        Assert.True(helper.HasOverscroll);
    }

    #endregion

    #region Reset Tests

    [Fact]
    public void Reset_ClearsOverscroll()
    {
        var control = new LayoutTransformControl();
        var helper = new OverscrollHelper(control);

        helper.ApplyOverscroll(10, 5);
        helper.Reset();

        Assert.False(helper.HasOverscroll);
        Assert.Equal(new Vector(0, 0), helper.Overscroll);
    }

    #endregion

    #region ApplyOverscroll Tests

    [Fact]
    public void ApplyOverscroll_SetsOverscrollVector()
    {
        var control = new LayoutTransformControl();
        var helper = new OverscrollHelper(control);

        helper.ApplyOverscroll(15, 20);

        Assert.Equal(new Vector(15, 20), helper.Overscroll);
    }

    [Fact]
    public void ApplyOverscroll_Zero_NoOverscroll()
    {
        var control = new LayoutTransformControl();
        var helper = new OverscrollHelper(control);

        helper.ApplyOverscroll(0, 0);

        Assert.False(helper.HasOverscroll);
    }

    #endregion

    #region Custom Resistance Tests

    [Fact]
    public void Constructor_CustomResistance_AffectsOverscrollAmount()
    {
        var control = new LayoutTransformControl();
        var lowResistance = new OverscrollHelper(control, 0.1);
        var highResistance = new OverscrollHelper(control, 0.9);

        var lowResult = lowResistance.CalculateOverscroll(150, 0, 100, 100);
        var highResult = highResistance.CalculateOverscroll(150, 0, 100, 100);

        // Higher resistance multiplier means more overscroll movement
        Assert.True(Math.Abs(highResult.OverscrollX) > Math.Abs(lowResult.OverscrollX));
    }

    #endregion
}
