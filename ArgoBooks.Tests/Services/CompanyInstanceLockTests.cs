using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests the cross-instance company lock. Two <see cref="CompanyInstanceLock"/> objects stand in
/// for two running app instances: an exclusive OS handle conflicts across handles even within one
/// process, so a second lock on the same path is blocked exactly as a second process would be.
/// </summary>
public class CompanyInstanceLockTests
{
    private static string TempCompanyPath() =>
        Path.Combine(Path.GetTempPath(), $"argo-lock-test-{Guid.NewGuid():N}.argo");

    [Fact]
    public void TryAcquire_SucceedsForFreshPath()
    {
        using var lock1 = new CompanyInstanceLock();
        Assert.True(lock1.TryAcquire(TempCompanyPath()));
    }

    [Fact]
    public void SecondLock_OnSamePath_IsBlocked()
    {
        var path = TempCompanyPath();
        using var first = new CompanyInstanceLock();
        using var second = new CompanyInstanceLock();

        Assert.True(first.TryAcquire(path));    // first "instance" holds it
        Assert.False(second.TryAcquire(path));  // second "instance" is blocked
    }

    [Fact]
    public void ReleasingFirst_AllowsSecondToAcquire()
    {
        var path = TempCompanyPath();
        using var first = new CompanyInstanceLock();
        using var second = new CompanyInstanceLock();

        Assert.True(first.TryAcquire(path));
        Assert.False(second.TryAcquire(path));

        first.Release();
        Assert.True(second.TryAcquire(path));   // freed, second can now open it
    }

    [Fact]
    public void DifferentPaths_DoNotConflict()
    {
        using var first = new CompanyInstanceLock();
        using var second = new CompanyInstanceLock();

        Assert.True(first.TryAcquire(TempCompanyPath()));
        Assert.True(second.TryAcquire(TempCompanyPath()));
    }

    [Fact]
    public void ReAcquire_SamePath_SameInstance_Succeeds()
    {
        var path = TempCompanyPath();
        using var only = new CompanyInstanceLock();

        Assert.True(only.TryAcquire(path));
        // Re-acquiring its own lock releases then retakes it, so it must still succeed (this is what
        // Save-As / rename onto an unchanged path relies on).
        Assert.True(only.TryAcquire(path));
    }

    [Fact]
    public void SamePath_DifferingCase_ConflictsOnCaseInsensitiveFileSystems()
    {
        // Windows and macOS are case-insensitive, so differing case must map to one lock. Linux is
        // case-sensitive, so this collapse doesn't apply there.
        if (OperatingSystem.IsLinux())
            return;

        var path = TempCompanyPath();
        using var first = new CompanyInstanceLock();
        using var second = new CompanyInstanceLock();

        Assert.True(first.TryAcquire(path.ToUpperInvariant()));
        Assert.False(second.TryAcquire(path.ToLowerInvariant()));
    }

    [Fact]
    public void IsHeldByAnotherInstance_TrueWhenHeldElsewhere_FalseWhenFree()
    {
        var path = TempCompanyPath();
        using var holder = new CompanyInstanceLock();
        using var probe = new CompanyInstanceLock();

        // Free before anyone holds it.
        Assert.False(probe.IsHeldByAnotherInstance(path));

        holder.TryAcquire(path);
        // Now another instance holds it → the probe must report it as taken, without acquiring.
        Assert.True(probe.IsHeldByAnotherInstance(path));
        Assert.Null(probe.LockedPath); // probing must not claim the lock

        holder.Release();
        Assert.False(probe.IsHeldByAnotherInstance(path)); // freed again
    }

    [Fact]
    public void IsHeldByAnotherInstance_FalseForPathThisInstanceHolds()
    {
        var path = TempCompanyPath();
        using var self = new CompanyInstanceLock();

        self.TryAcquire(path);
        // A company this instance already holds is not "another instance" — this is what lets the
        // same company be re-opened/switched without a false "already open" block.
        Assert.False(self.IsHeldByAnotherInstance(path));
    }

    [Fact]
    public void LockedPath_ReflectsCurrentState()
    {
        var path = TempCompanyPath();
        using var only = new CompanyInstanceLock();

        Assert.Null(only.LockedPath);
        Assert.True(only.TryAcquire(path));
        Assert.NotNull(only.LockedPath);

        only.Release();
        Assert.Null(only.LockedPath);
    }
}
