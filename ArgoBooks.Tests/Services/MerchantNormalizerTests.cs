using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class MerchantNormalizerTests
{
    [Theory]
    [InlineData("AMZN MKTP US*2H8KL", "amzn mktp us")]
    [InlineData("SQ *BLUE BOTTLE COFFEE #4412", "sq blue bottle coffee")]
    [InlineData("POS DEBIT 0405 SHELL OIL 574123", "pos debit shell oil")]
    [InlineData("   Multiple   Spaces  ", "multiple spaces")]
    public void Normalize_StripsNoiseAndLowercases(string input, string expected)
    {
        Assert.Equal(expected, MerchantNormalizer.Normalize(input));
    }

    [Fact]
    public void Normalize_EmptyOrNull_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, MerchantNormalizer.Normalize(""));
        Assert.Equal(string.Empty, MerchantNormalizer.Normalize(null!));
    }
}
