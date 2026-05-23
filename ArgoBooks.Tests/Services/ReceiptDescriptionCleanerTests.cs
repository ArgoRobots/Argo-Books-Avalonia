using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

public class ReceiptDescriptionCleanerTests
{
    [Theory]
    [InlineData("6010-0272-0259-0062 Co Palm Refill", "Co Palm Refill")]
    [InlineData("6014-0415-0000-0007 Fo Coco Butter", "Fo Coco Butter")]
    [InlineData("6012-0206-0287-0068 Eo Lemon Side", "Eo Lemon Side")]
    [InlineData("  6025-0306-0267-0005   Sb Relax Roll  ", "Sb Relax Roll")]
    public void Clean_strips_leading_hyphenated_code(string input, string expected)
    {
        Assert.Equal(expected, ReceiptDescriptionCleaner.Clean(input));
    }

    [Theory]
    [InlineData("0123456789 Organic Bananas", "Organic Bananas")]
    public void Clean_strips_long_leading_barcode(string input, string expected)
    {
        Assert.Equal(expected, ReceiptDescriptionCleaner.Clean(input));
    }

    [Theory]
    [InlineData("2 Coconut Cups", "2 Coconut Cups")]   // quantity, not a code
    [InlineData("2024 Vintage Wine", "2024 Vintage Wine")] // year, not a code
    [InlineData("Coca-Cola 2L", "Coca-Cola 2L")]       // hyphen but not leading digit code
    [InlineData("Palm Refill", "Palm Refill")]          // already clean
    public void Clean_keeps_real_names(string input, string expected)
    {
        Assert.Equal(expected, ReceiptDescriptionCleaner.Clean(input));
    }

    [Theory]
    [InlineData("6010-0272-0259-0062", "6010-0272-0259-0062")] // code only, no name to keep
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData(null, "")]
    public void Clean_handles_edge_cases(string? input, string expected)
    {
        Assert.Equal(expected, ReceiptDescriptionCleaner.Clean(input));
    }
}
