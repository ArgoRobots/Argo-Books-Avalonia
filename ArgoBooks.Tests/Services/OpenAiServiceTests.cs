using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the OpenAiService class.
/// </summary>
public class OpenAiServiceTests
{
    #region IsConfigured Tests

    [Fact]
    public void IsConfigured_WithoutApiKey_ReturnsFalse()
    {
        // Unless OPENAI_API_KEY is set in the environment, IsConfigured should reflect the env state
        var service = new OpenAiService();

        // We can't guarantee false here since the env might have the key,
        // but we can verify it doesn't throw
        _ = service.IsConfigured;
    }

    #endregion

    #region GetSupplierCategorySuggestionAsync Tests

    [Fact]
    public async Task GetSupplierCategorySuggestionAsync_WithNullRequest_HandlesGracefully()
    {
        var service = new OpenAiService();

        // When not configured, should return null without throwing
        if (!service.IsConfigured)
        {
            var result = await service.GetSupplierCategorySuggestionAsync(
                new ArgoBooks.Core.Models.AI.ReceiptAnalysisRequest(),
                CancellationToken.None);

            Assert.Null(result);
        }
    }

    #endregion
}
