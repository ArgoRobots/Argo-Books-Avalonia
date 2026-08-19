using ArgoBooks.Utilities;
using Xunit;

namespace ArgoBooks.Tests.Utilities;

/// <summary>
/// Tests for where a multi-file export writes to.
///
/// The boundary is the whole point: one file goes straight into the folder the user picked,
/// and two or more get their own subfolder so a pay run does not scatter a dozen PDFs through
/// someone's Downloads.
/// </summary>
public class ExportFolderHelperTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "ArgoBooksExportTests", Guid.NewGuid().ToString("N"));

    public ExportFolderHelperTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { /* best effort */ }
        GC.SuppressFinalize(this);
    }

    [Fact]
    public void ASingleFile_GoesStraightIntoTheChosenFolder()
    {
        // Wrapping one PDF in a folder is just an extra click on the way to it.
        Assert.Equal(_root, ExportFolderHelper.Resolve(_root, "Pay stubs 2026-07-03", 1));
        Assert.Empty(Directory.GetDirectories(_root));
    }

    [Fact]
    public void NoFilesAtAll_CreatesNothing()
    {
        Assert.Equal(_root, ExportFolderHelper.Resolve(_root, "Pay stubs 2026-07-03", 0));
        Assert.Empty(Directory.GetDirectories(_root));
    }

    [Fact]
    public void TwoFiles_GetTheirOwnFolder()
    {
        string path = ExportFolderHelper.Resolve(_root, "Pay stubs 2026-07-03", 2);

        Assert.NotEqual(_root, path);
        Assert.True(Directory.Exists(path));
        Assert.Equal("Pay-stubs-2026-07-03", Path.GetFileName(path));
    }

    [Fact]
    public void ExportingTheSameRunTwice_ReusesTheFolder()
    {
        // Someone re-downloading after a correction wants to replace what is there, not to
        // accumulate "Pay stubs (2)" beside it.
        string first = ExportFolderHelper.Resolve(_root, "Pay stubs 2026-07-03", 3);
        string second = ExportFolderHelper.Resolve(_root, "Pay stubs 2026-07-03", 3);

        Assert.Equal(first, second);
        Assert.Single(Directory.GetDirectories(_root));
    }

    [Theory]
    [InlineData("T4 2026", "T4-2026")]
    [InlineData("RL-1 2026", "RL-1-2026")]
    [InlineData("Pay stubs 2026-07-03", "Pay-stubs-2026-07-03")]
    public void SpacesBecomeDashes_SoTheNameSurvivesBeingEmailed(string given, string expected) =>
        Assert.Equal(expected, ExportFolderHelper.Sanitize(given));

    [Fact]
    public void CharactersTheFilesystemRefuses_AreReplaced()
    {
        string safe = ExportFolderHelper.Sanitize("Dana / Smith: \"the third\"");

        Assert.DoesNotContain(safe, s => Path.GetInvalidFileNameChars().Contains(s));
        Assert.DoesNotContain(' ', safe);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("///")]
    public void ANameThatSanitisesToNothing_FallsBackRatherThanProducingAnEmptyPath(string given)
    {
        // An empty segment would silently write into the parent folder instead.
        Assert.Equal("export", ExportFolderHelper.Sanitize(given));
    }

    [Fact]
    public void ANullName_DoesNotThrow() => Assert.Equal("export", ExportFolderHelper.Sanitize(null!));
}
