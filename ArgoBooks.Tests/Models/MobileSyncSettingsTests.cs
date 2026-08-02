using ArgoBooks.Core.Models;
using Xunit;
namespace ArgoBooks.Tests.Models;
public class MobileSyncSettingsTests
{
    [Fact]
    public void Defaults_are_disabled_with_notify_on()
    {
        var s = new MobileSyncSettings();
        Assert.False(s.Enabled);
        Assert.Null(s.CompanyUid);
        Assert.True(s.NotifyOnCapture);
    }

    [Fact]
    public void CompanySettings_exposes_MobileSync()
    {
        var cs = new CompanySettings();
        Assert.NotNull(cs.MobileSync);
        Assert.False(cs.MobileSync.Enabled);
    }
}
