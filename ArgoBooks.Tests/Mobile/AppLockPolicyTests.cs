using ArgoBooks.Shared.Mobile;
using Xunit;

namespace ArgoBooks.Tests.Mobile;

/// <summary>
/// Unit tests for AppLockPolicy.ShouldLock - the pure grace-period/cold-start decision that
/// drives the biometric app lock. The actual BiometricPrompt call and timers are device/Android
/// specific and are not unit-tested (see ArgoBooks.Mobile/Services/AppLockService.cs).
/// </summary>
public class AppLockPolicyTests
{
    private static readonly DateTime Now = new(2026, 7, 13, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ShouldLock_LockDisabled_ReturnsFalse()
    {
        Assert.False(AppLockPolicy.ShouldLock(isLockEnabled: false, isPaired: true, lastBackgroundedUtc: null, Now, AppLockPolicy.DefaultGracePeriod));
    }

    [Fact]
    public void ShouldLock_NotPaired_ReturnsFalse()
    {
        Assert.False(AppLockPolicy.ShouldLock(isLockEnabled: true, isPaired: false, lastBackgroundedUtc: null, Now, AppLockPolicy.DefaultGracePeriod));
    }

    [Fact]
    public void ShouldLock_ColdStart_NoBackgroundedTimestamp_ReturnsTrue()
    {
        // Never backgrounded this process lifetime => cold start => always lock when enabled+paired.
        Assert.True(AppLockPolicy.ShouldLock(isLockEnabled: true, isPaired: true, lastBackgroundedUtc: null, Now, AppLockPolicy.DefaultGracePeriod));
    }

    [Fact]
    public void ShouldLock_ResumedWithinGracePeriod_ReturnsFalse()
    {
        var lastBackgrounded = Now - TimeSpan.FromSeconds(30);
        Assert.False(AppLockPolicy.ShouldLock(isLockEnabled: true, isPaired: true, lastBackgrounded, Now, AppLockPolicy.DefaultGracePeriod));
    }

    [Fact]
    public void ShouldLock_ResumedExactlyAtGracePeriod_ReturnsTrue()
    {
        var lastBackgrounded = Now - AppLockPolicy.DefaultGracePeriod;
        Assert.True(AppLockPolicy.ShouldLock(isLockEnabled: true, isPaired: true, lastBackgrounded, Now, AppLockPolicy.DefaultGracePeriod));
    }

    [Fact]
    public void ShouldLock_ResumedPastGracePeriod_ReturnsTrue()
    {
        var lastBackgrounded = Now - TimeSpan.FromMinutes(5);
        Assert.True(AppLockPolicy.ShouldLock(isLockEnabled: true, isPaired: true, lastBackgrounded, Now, AppLockPolicy.DefaultGracePeriod));
    }
}
