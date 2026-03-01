using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the TelemetryManager and related services.
/// </summary>
public class TelemetryManagerTests
{
    #region TelemetryEvent Tests

    [Fact]
    public void TelemetryEvent_GeneratesUniqueDataId()
    {
        var event1 = new SessionEvent();
        var event2 = new SessionEvent();

        Assert.NotEqual(event1.DataId, event2.DataId);
        Assert.Equal(16, event1.DataId.Length);
    }

    #endregion
}
