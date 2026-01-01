using ArgoBooks.Utilities;
using Xunit;

namespace ArgoBooks.Tests.Utilities;

/// <summary>
/// Tests for the LevenshteinDistance utility class.
/// </summary>
public class LevenshteinDistanceTests
{
    #region Compute Tests

    [Fact]
    public void Compute_IdenticalStrings_ReturnsZero()
    {
        var result = LevenshteinDistance.Compute("hello", "hello");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Compute_EmptyStrings_ReturnsZero()
    {
        var result = LevenshteinDistance.Compute("", "");

        Assert.Equal(0, result);
    }

    [Fact]
    public void Compute_SourceEmpty_ReturnsTargetLength()
    {
        var result = LevenshteinDistance.Compute("", "hello");

        Assert.Equal(5, result);
    }

    [Fact]
    public void Compute_TargetEmpty_ReturnsSourceLength()
    {
        var result = LevenshteinDistance.Compute("hello", "");

        Assert.Equal(5, result);
    }

    [Fact]
    public void Compute_NullSource_ReturnsTargetLength()
    {
        var result = LevenshteinDistance.Compute(null!, "hello");

        Assert.Equal(5, result);
    }

    [Fact]
    public void Compute_NullTarget_ReturnsSourceLength()
    {
        var result = LevenshteinDistance.Compute("hello", null!);

        Assert.Equal(5, result);
    }

    [Fact]
    public void Compute_BothNull_ReturnsZero()
    {
        var result = LevenshteinDistance.Compute(null!, null!);

        Assert.Equal(0, result);
    }

    [Fact]
    public void Compute_SingleSubstitution_ReturnsOne()
    {
        var result = LevenshteinDistance.Compute("cat", "bat");

        Assert.Equal(1, result);
    }

    [Fact]
    public void Compute_SingleInsertion_ReturnsOne()
    {
        var result = LevenshteinDistance.Compute("cat", "cats");

        Assert.Equal(1, result);
    }

    [Fact]
    public void Compute_SingleDeletion_ReturnsOne()
    {
        var result = LevenshteinDistance.Compute("cats", "cat");

        Assert.Equal(1, result);
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("saturday", "sunday", 3)]
    [InlineData("abc", "def", 3)]
    public void Compute_KnownDistances_ReturnsExpected(string source, string target, int expected)
    {
        var result = LevenshteinDistance.Compute(source, target);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void Compute_CaseSensitive_DistinguishesCases()
    {
        var result = LevenshteinDistance.Compute("Hello", "hello");

        Assert.Equal(1, result);
    }

    #endregion

    #region NormalizedSimilarity Tests

    [Fact]
    public void NormalizedSimilarity_IdenticalStrings_ReturnsOne()
    {
        var result = LevenshteinDistance.NormalizedSimilarity("hello", "hello");

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void NormalizedSimilarity_BothEmpty_ReturnsOne()
    {
        var result = LevenshteinDistance.NormalizedSimilarity("", "");

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void NormalizedSimilarity_SourceEmpty_ReturnsZero()
    {
        var result = LevenshteinDistance.NormalizedSimilarity("", "hello");

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void NormalizedSimilarity_TargetEmpty_ReturnsZero()
    {
        var result = LevenshteinDistance.NormalizedSimilarity("hello", "");

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void NormalizedSimilarity_CompletelyDifferent_ReturnsZero()
    {
        var result = LevenshteinDistance.NormalizedSimilarity("abc", "xyz");

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void NormalizedSimilarity_CaseInsensitive_ReturnsOne()
    {
        var result = LevenshteinDistance.NormalizedSimilarity("Hello", "HELLO");

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void NormalizedSimilarity_SimilarStrings_ReturnsBetweenZeroAndOne()
    {
        var result = LevenshteinDistance.NormalizedSimilarity("hello", "hallo");

        Assert.True(result > 0 && result < 1);
        Assert.Equal(0.8, result, 2);
    }

    [Theory]
    [InlineData("test", "test", 1.0)]
    [InlineData("test", "Test", 1.0)]
    [InlineData("testing", "test", 0.571)]
    public void NormalizedSimilarity_VariousInputs_ReturnsExpected(string source, string target, double expected)
    {
        var result = LevenshteinDistance.NormalizedSimilarity(source, target);

        Assert.Equal(expected, result, 2);
    }

    #endregion

    #region ContainsSubstring Tests

    [Fact]
    public void ContainsSubstring_ExactMatch_ReturnsTrue()
    {
        var result = LevenshteinDistance.ContainsSubstring("hello", "hello world");

        Assert.True(result);
    }

    [Fact]
    public void ContainsSubstring_SubstringAtStart_ReturnsTrue()
    {
        var result = LevenshteinDistance.ContainsSubstring("hello", "hello world");

        Assert.True(result);
    }

    [Fact]
    public void ContainsSubstring_SubstringAtEnd_ReturnsTrue()
    {
        var result = LevenshteinDistance.ContainsSubstring("world", "hello world");

        Assert.True(result);
    }

    [Fact]
    public void ContainsSubstring_SubstringInMiddle_ReturnsTrue()
    {
        var result = LevenshteinDistance.ContainsSubstring("lo wo", "hello world");

        Assert.True(result);
    }

    [Fact]
    public void ContainsSubstring_CaseInsensitive_ReturnsTrue()
    {
        var result = LevenshteinDistance.ContainsSubstring("HELLO", "hello world");

        Assert.True(result);
    }

    [Fact]
    public void ContainsSubstring_NotContained_ReturnsFalse()
    {
        var result = LevenshteinDistance.ContainsSubstring("xyz", "hello world");

        Assert.False(result);
    }

    [Fact]
    public void ContainsSubstring_EmptySource_ReturnsTrue()
    {
        var result = LevenshteinDistance.ContainsSubstring("", "hello world");

        Assert.True(result);
    }

    [Fact]
    public void ContainsSubstring_EmptyTarget_ReturnsFalse()
    {
        var result = LevenshteinDistance.ContainsSubstring("hello", "");

        Assert.False(result);
    }

    [Fact]
    public void ContainsSubstring_BothEmpty_ReturnsTrue()
    {
        var result = LevenshteinDistance.ContainsSubstring("", "");

        Assert.True(result);
    }

    #endregion

    #region ComputeSearchScore Tests

    [Fact]
    public void ComputeSearchScore_ExactMatch_ReturnsOne()
    {
        var result = LevenshteinDistance.ComputeSearchScore("hello", "hello");

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ComputeSearchScore_ExactMatchCaseInsensitive_ReturnsOne()
    {
        var result = LevenshteinDistance.ComputeSearchScore("hello", "HELLO");

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ComputeSearchScore_PrefixMatch_Returns095()
    {
        var result = LevenshteinDistance.ComputeSearchScore("hel", "hello");

        Assert.Equal(0.95, result);
    }

    [Fact]
    public void ComputeSearchScore_WordStartMatch_Returns090()
    {
        var result = LevenshteinDistance.ComputeSearchScore("wor", "hello world");

        Assert.Equal(0.9, result);
    }

    [Fact]
    public void ComputeSearchScore_SubstringMatch_Returns080()
    {
        var result = LevenshteinDistance.ComputeSearchScore("llo", "hello");

        Assert.Equal(0.8, result);
    }

    [Fact]
    public void ComputeSearchScore_EmptySearch_ReturnsOne()
    {
        var result = LevenshteinDistance.ComputeSearchScore("", "hello");

        Assert.Equal(1.0, result);
    }

    [Fact]
    public void ComputeSearchScore_EmptyTarget_ReturnsNegativeOne()
    {
        var result = LevenshteinDistance.ComputeSearchScore("hello", "");

        Assert.Equal(-1, result);
    }

    [Fact]
    public void ComputeSearchScore_NoMatch_ReturnsNegativeOne()
    {
        var result = LevenshteinDistance.ComputeSearchScore("xyz", "hello");

        Assert.Equal(-1, result);
    }

    [Fact]
    public void ComputeSearchScore_FuzzyMatch_ReturnsScaledSimilarity()
    {
        var result = LevenshteinDistance.ComputeSearchScore("helo", "hello");

        // Fuzzy matches are scaled by 0.7
        Assert.True(result > 0 && result < 0.8);
    }

    [Fact]
    public void ComputeSearchScore_LowSimilarityBelowThreshold_ReturnsNegativeOne()
    {
        var result = LevenshteinDistance.ComputeSearchScore("xyz", "abc", 0.5);

        Assert.Equal(-1, result);
    }

    [Fact]
    public void ComputeSearchScore_CustomThreshold_RespectedForFuzzyMatches()
    {
        // "helo" and "hello" have ~80% similarity
        var resultWithHighThreshold = LevenshteinDistance.ComputeSearchScore("helo", "hello", 0.9);
        var resultWithLowThreshold = LevenshteinDistance.ComputeSearchScore("helo", "hello", 0.5);

        Assert.Equal(-1, resultWithHighThreshold);
        Assert.True(resultWithLowThreshold > 0);
    }

    [Theory]
    [InlineData("inv", "Add Inventory", 0.9)]  // Word start match
    [InlineData("Add", "Add Inventory", 0.95)] // Prefix match
    [InlineData("inventory", "Add Inventory", 0.9)] // Word start match
    public void ComputeSearchScore_MenuItems_ReturnsExpectedPriority(string search, string target, double expected)
    {
        var result = LevenshteinDistance.ComputeSearchScore(search, target);

        Assert.Equal(expected, result, 2);
    }

    [Fact]
    public void ComputeSearchScore_WordSimilarity_ConsidersIndividualWords()
    {
        // "invntory" is similar to "Inventory" word
        var result = LevenshteinDistance.ComputeSearchScore("invntory", "Add Inventory");

        // Should get a fuzzy match score (scaled by 0.7)
        Assert.True(result > 0);
    }

    [Fact]
    public void ComputeSearchScore_MultipleWords_ChecksAllWords()
    {
        var result = LevenshteinDistance.ComputeSearchScore("cust", "Manage Customers");

        // Should match "Customers" word start
        Assert.Equal(0.9, result);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void Compute_VeryLongStrings_CalculatesCorrectly()
    {
        var source = new string('a', 1000);
        var target = new string('b', 1000);

        var result = LevenshteinDistance.Compute(source, target);

        Assert.Equal(1000, result);
    }

    [Fact]
    public void Compute_UnicodeCharacters_HandlesCorrectly()
    {
        var result = LevenshteinDistance.Compute("café", "cafe");

        Assert.Equal(1, result);
    }

    [Fact]
    public void Compute_WhitespaceStrings_HandlesCorrectly()
    {
        var result = LevenshteinDistance.Compute("hello world", "helloworld");

        Assert.Equal(1, result);
    }

    [Fact]
    public void NormalizedSimilarity_SingleCharacterDifference_CalculatesCorrectly()
    {
        var result = LevenshteinDistance.NormalizedSimilarity("a", "b");

        Assert.Equal(0.0, result);
    }

    [Fact]
    public void ComputeSearchScore_SpecialCharacters_HandlesCorrectly()
    {
        var result = LevenshteinDistance.ComputeSearchScore("test", "test-item");

        Assert.Equal(0.95, result); // Prefix match
    }

    #endregion
}
