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
        var (expectedKnownGap, _) = await ImporterHarness.ReadGapAsync(fixtureDir);
        var report = await ImporterHarness.RunTrackAAsync(fixtureDir);

        if (expectedKnownGap)
            Assert.False(report.Passed,
                "Fixture marked knownGap but now passes; remove the knownGap flag.");
        else
            Assert.True(report.Passed, report.FailureMessage);
    }
}
