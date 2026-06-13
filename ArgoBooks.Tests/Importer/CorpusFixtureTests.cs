using Xunit;

namespace ArgoBooks.Tests.Importer;

public class CorpusFixtureTests
{
    public static IEnumerable<object[]> Fixtures() =>
        ImporterHarness.EnumerateFixtureDirectories().Select(d => new object[] { d });

    [Theory]
    [MemberData(nameof(Fixtures))]
    public async Task Fixture_ImportsAsExpected(string fixtureDir)
    {
        var report = await ImporterHarness.RunTrackAAsync(fixtureDir);
        Assert.True(report.Passed, report.FailureMessage);
    }
}
