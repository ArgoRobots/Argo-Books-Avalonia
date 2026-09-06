using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the command-line company file path used by the .argo file association.
/// </summary>
public class StartupFileArgsTests
{
    private static readonly Func<string, bool> AllExist = _ => true;
    private static readonly Func<string, bool> NoneExist = _ => false;

    [Fact]
    public void GetCompanyFilePath_ReturnsFullPath_ForArgoFile()
    {
        var result = StartupFileArgs.GetCompanyFilePath([@"C:\Books\Acme.argo"], AllExist);

        Assert.Equal(Path.GetFullPath(@"C:\Books\Acme.argo"), result);
    }

    [Fact]
    public void GetCompanyFilePath_IsCaseInsensitiveOnExtension()
    {
        var result = StartupFileArgs.GetCompanyFilePath([@"C:\Books\Acme.ARGO"], AllExist);

        Assert.NotNull(result);
    }

    [Fact]
    public void GetCompanyFilePath_StripsSurroundingQuotes()
    {
        var result = StartupFileArgs.GetCompanyFilePath([@"""C:\My Books\Acme.argo"""], AllExist);

        Assert.Equal(Path.GetFullPath(@"C:\My Books\Acme.argo"), result);
    }

    [Fact]
    public void GetCompanyFilePath_SkipsSwitches()
    {
        var result = StartupFileArgs.GetCompanyFilePath(["--debug", "/silent", @"C:\Books\Acme.argo"], AllExist);

        Assert.Equal(Path.GetFullPath(@"C:\Books\Acme.argo"), result);
    }

    [Fact]
    public void GetCompanyFilePath_ReturnsNull_ForOtherExtensions()
    {
        var result = StartupFileArgs.GetCompanyFilePath([@"C:\Books\Acme.argobk", @"C:\Books\notes.txt"], AllExist);

        Assert.Null(result);
    }

    [Fact]
    public void GetCompanyFilePath_ReturnsNull_WhenFileMissing()
    {
        var result = StartupFileArgs.GetCompanyFilePath([@"C:\Books\Acme.argo"], NoneExist);

        Assert.Null(result);
    }

    [Fact]
    public void GetCompanyFilePath_ReturnsNull_ForEmptyOrNullArgs()
    {
        Assert.Null(StartupFileArgs.GetCompanyFilePath(null, AllExist));
        Assert.Null(StartupFileArgs.GetCompanyFilePath([], AllExist));
        Assert.Null(StartupFileArgs.GetCompanyFilePath(["   "], AllExist));
    }

    /// <summary>
    /// The AppImage registers application/x-argo and the desktop entry passes %f, so on Linux the
    /// file manager hands over an absolute POSIX path. Treating a leading '/' as a switch, which is
    /// only true on Windows, discarded every one of them and left double-click doing nothing.
    /// </summary>
    [Fact]
    public void GetCompanyFilePath_AcceptsAPosixAbsolutePath()
    {
        const string path = "/home/evan/Books/Acme.argo";

        var result = StartupFileArgs.GetCompanyFilePath([path], AllExist);

        Assert.Equal(Path.GetFullPath(path), result);
    }

    /// <summary>
    /// The companion to the case above: dropping the '/' check must not let a Windows-style switch
    /// through. The extension test is what turns it away.
    /// </summary>
    [Fact]
    public void GetCompanyFilePath_StillIgnoresASlashSwitch()
    {
        Assert.Null(StartupFileArgs.GetCompanyFilePath(["/silent", "/S"], AllExist));
    }

    [Fact]
    public void GetCompanyFilePath_FindsRealFileOnDisk()
    {
        var path = Path.Combine(Path.GetTempPath(), $"argo-startup-{Guid.NewGuid():N}.argo");
        File.WriteAllText(path, "test");
        try
        {
            Assert.Equal(Path.GetFullPath(path), StartupFileArgs.GetCompanyFilePath([path]));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
