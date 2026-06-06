using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for <see cref="FileAccessHelper.IsLikelySecurityBlock"/>. The classifier must
/// catch the antivirus / ransomware-protection cases (access denied, sharing/lock
/// violations) while leaving genuine bugs (missing file, disk full) to normal handling.
/// </summary>
public class FileAccessHelperTests
{
    [Fact]
    public void UnauthorizedAccess_IsSecurityBlock()
    {
        Assert.True(FileAccessHelper.IsLikelySecurityBlock(new UnauthorizedAccessException()));
    }

    [Fact]
    public void SharingViolation_IsSecurityBlock()
    {
        // 0x80070020 == ERROR_SHARING_VIOLATION: a file held open by AV/indexer/backup.
        var ex = new IOException("locked") { HResult = unchecked((int)0x80070020) };
        Assert.True(FileAccessHelper.IsLikelySecurityBlock(ex));
    }

    [Fact]
    public void AccessDeniedIO_IsSecurityBlock()
    {
        // 0x80070005 == ERROR_ACCESS_DENIED surfaced as an IOException.
        var ex = new IOException("denied") { HResult = unchecked((int)0x80070005) };
        Assert.True(FileAccessHelper.IsLikelySecurityBlock(ex));
    }

    [Fact]
    public void DiskFull_IsNotSecurityBlock()
    {
        // 0x80070070 == ERROR_DISK_FULL: a real out-of-space condition, not security.
        var ex = new IOException("full") { HResult = unchecked((int)0x80070070) };
        Assert.False(FileAccessHelper.IsLikelySecurityBlock(ex));
    }

    [Fact]
    public void FileNotFound_IsNotSecurityBlock()
    {
        Assert.False(FileAccessHelper.IsLikelySecurityBlock(new FileNotFoundException()));
    }

    [Fact]
    public void UnrelatedException_IsNotSecurityBlock()
    {
        Assert.False(FileAccessHelper.IsLikelySecurityBlock(new InvalidOperationException()));
        Assert.False(FileAccessHelper.IsLikelySecurityBlock(null));
    }
}
