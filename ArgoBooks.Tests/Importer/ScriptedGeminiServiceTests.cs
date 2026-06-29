using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Importer;

public class ScriptedGeminiServiceTests
{
    [Fact]
    public async Task SendChatAsync_ReturnsResponseWhoseKeyAppearsInPrompt()
    {
        var fake = new ScriptedGeminiService(new Dictionary<string, string>
        {
            ["Employees"] = "{\"sheets\":[]}",
            ["Expenses"]  = "{\"sheets\":[{\"sourceSheetName\":\"Expenses\"}]}",
        });

        var resp = await fake.SendChatAsync("sys", "Sheet: \"Expenses\" (3 rows)");
        Assert.Contains("Expenses", resp);
        Assert.Equal(1, fake.CallCount);
    }

    [Fact]
    public async Task SendChatAsync_NoMatch_ReturnsNullAndRecordsUnmatched()
    {
        var fake = new ScriptedGeminiService(new Dictionary<string, string>());
        var resp = await fake.SendChatAsync("sys", "anything");
        Assert.Null(resp);
        Assert.Single(fake.UnmatchedPrompts);
    }
}
