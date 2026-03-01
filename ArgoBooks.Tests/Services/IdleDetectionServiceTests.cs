using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the IdleDetectionService class.
/// </summary>
public class IdleDetectionServiceTests : IDisposable
{
    private readonly IdleDetectionService _service = new();

    public void Dispose()
    {
        _service.Dispose();
    }

    #region Configure Tests

    [Fact]
    public void Configure_EnabledWithTimeout_SetsIsEnabledTrue()
    {
        _service.Configure(enabled: true, timeoutMinutes: 10);

        Assert.True(_service.IsEnabled);
        Assert.Equal(10, _service.TimeoutMinutes);
    }

    [Fact]
    public void Configure_Disabled_SetsIsEnabledFalse()
    {
        _service.Configure(enabled: true, timeoutMinutes: 10);

        _service.Configure(enabled: false, timeoutMinutes: 10);

        Assert.False(_service.IsEnabled);
    }

    [Fact]
    public void Configure_EnabledWithZeroTimeout_SetsIsEnabledFalse()
    {
        _service.Configure(enabled: true, timeoutMinutes: 0);

        Assert.False(_service.IsEnabled);
    }

    #endregion

    #region ParseTimeoutString Tests

    [Theory]
    [InlineData("5 minutes", 5)]
    [InlineData("10 minutes", 10)]
    [InlineData("15 minutes", 15)]
    [InlineData("30 minutes", 30)]
    [InlineData("1 hour", 60)]
    [InlineData("Never", 0)]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    public void ParseTimeoutString_VariousInputs_ReturnsExpectedMinutes(string? input, int expected)
    {
        var result = IdleDetectionService.ParseTimeoutString(input);

        Assert.Equal(expected, result);
    }

    #endregion

}
