using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Importer;

[Trait("Category", "LiveEval")]
public class LiveEvaluationTests
{
    [Fact]
    public async Task Scorecard_OverCorpus()
    {
        var key = Environment.GetEnvironmentVariable("ARGO_LIVE_EVAL");
        if (string.IsNullOrEmpty(key))
        {
            Console.WriteLine("skipped: set ARGO_LIVE_EVAL=1 (and API config) to run live eval.");
            return;
        }

        var live = new GeminiService(null, null);
        if (!live.IsConfigured)
        {
            Console.WriteLine("skipped: GeminiService is not configured in this environment.");
            return;
        }

        int sheets = 0, type = 0, tier = 0;
        foreach (var dir in ImporterHarness.EnumerateFixtureDirectories())
        {
            var s = await ImporterHarness.ScoreAsync(dir, live);
            sheets += s.Sheets;
            type += s.CorrectType;
            tier += s.CorrectTier;
        }

        Console.WriteLine($"LIVE EVAL: classification {type}/{sheets}, tier {tier}/{sheets}");
        Assert.True(sheets > 0);
    }
}
