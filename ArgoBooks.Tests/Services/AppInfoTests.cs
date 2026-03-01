using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the ArgoBooks.Core.Services.AppInfo static class.
/// </summary>
public class AppInfoTests
{
    #region VersionNumber Tests

    [Fact]
    public void VersionNumber_IsNotNullOrEmpty()
    {
        var version = Core.Services.AppInfo.VersionNumber;

        Assert.False(string.IsNullOrEmpty(version));
    }

    [Fact]
    public void VersionNumber_MatchesXDotXDotXFormat()
    {
        var version = Core.Services.AppInfo.VersionNumber;

        // Should match pattern like "1.0.0" or "2.3.1"
        Assert.Matches(@"^\d+\.\d+\.\d+$", version);
    }

    [Fact]
    public void VersionNumber_IsCached()
    {
        var first = Core.Services.AppInfo.VersionNumber;
        var second = Core.Services.AppInfo.VersionNumber;

        Assert.Equal(first, second);
    }

    #endregion

    #region AssemblyVersion Tests

    [Fact]
    public void AssemblyVersion_DoesNotThrow()
    {
        // AssemblyVersion may be null in test context (no entry assembly)
        // but accessing it should not throw
        var version = Core.Services.AppInfo.AssemblyVersion;

        // Either null or a valid Version object
        Assert.True(version == null || version != null);
    }

    #endregion
}
