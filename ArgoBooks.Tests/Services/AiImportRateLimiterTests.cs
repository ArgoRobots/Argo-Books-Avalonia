using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class AiImportRateLimiterTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AiImportRateLimiter _limiter;

    public AiImportRateLimiterTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"argo-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _limiter = new AiImportRateLimiter(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch
        {
            // Cleanup is best-effort in tests
        }
    }

    [Fact]
    public void CanImport_WhenNoImports_ReturnsTrue()
    {
        Assert.True(_limiter.CanImport());
    }

    [Fact]
    public void GetRemainingImportsToday_WhenNoImports_ReturnsMax()
    {
        Assert.Equal(10, _limiter.GetRemainingImportsToday());
    }

    [Fact]
    public void MaxPerDay_Returns10()
    {
        Assert.Equal(10, _limiter.MaxPerDay);
    }

    [Fact]
    public void RecordImport_DecrementsRemaining()
    {
        _limiter.RecordImport("test.xlsx");

        Assert.Equal(9, _limiter.GetRemainingImportsToday());
    }

    [Fact]
    public void CanImport_AfterMaxImports_ReturnsFalse()
    {
        for (int i = 0; i < 10; i++)
        {
            _limiter.RecordImport($"test{i}.xlsx");
        }

        Assert.False(_limiter.CanImport());
        Assert.Equal(0, _limiter.GetRemainingImportsToday());
    }

    [Fact]
    public void RecordImport_PersistsToFile()
    {
        _limiter.RecordImport("persist-test.xlsx");

        // Create a new limiter pointing to the same directory
        var limiter2 = new AiImportRateLimiter(_tempDir);
        Assert.Equal(9, limiter2.GetRemainingImportsToday());
    }

    [Fact]
    public void CanImport_WithCorruptedFile_ReturnsTrue()
    {
        // Write garbage to the usage file
        File.WriteAllText(Path.Combine(_tempDir, "ai-import-usage.json"), "not valid json");

        Assert.True(_limiter.CanImport());
    }

    [Fact]
    public void RecordImport_WithEmptyFileName_Succeeds()
    {
        _limiter.RecordImport();
        Assert.Equal(9, _limiter.GetRemainingImportsToday());
    }

    [Fact]
    public void GetRemainingImportsToday_NeverReturnsNegative()
    {
        for (int i = 0; i < 15; i++)
        {
            _limiter.RecordImport($"test{i}.xlsx");
        }

        Assert.Equal(0, _limiter.GetRemainingImportsToday());
    }
}
