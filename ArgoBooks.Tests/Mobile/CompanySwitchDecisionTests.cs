using ArgoBooks.Shared.Mobile;
using Xunit;

namespace ArgoBooks.Tests.Mobile;

/// <summary>Unit tests for the pure switch-then-refresh decision used by the company switcher.</summary>
public class CompanySwitchDecisionTests
{
    [Fact]
    public void ShouldSwitch_ReturnsTrue_WhenTargetDiffersFromActive()
    {
        Assert.True(CompanySwitchDecision.ShouldSwitch("company-1", "company-2"));
    }

    [Fact]
    public void ShouldSwitch_ReturnsFalse_WhenTargetIsAlreadyActive()
    {
        Assert.False(CompanySwitchDecision.ShouldSwitch("company-1", "company-1"));
    }

    [Fact]
    public void ShouldSwitch_ReturnsTrue_WhenNoCompanyCurrentlyActive()
    {
        Assert.True(CompanySwitchDecision.ShouldSwitch(null, "company-1"));
    }

    [Fact]
    public void ShouldSwitch_ReturnsFalse_WhenTargetUidIsEmpty()
    {
        Assert.False(CompanySwitchDecision.ShouldSwitch("company-1", ""));
    }
}
