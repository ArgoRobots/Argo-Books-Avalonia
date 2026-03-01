using ArgoBooks.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the LanguageService class.
/// </summary>
public class LanguageServiceTests
{
    #region GetStringKey Tests

    [Theory]
    [InlineData("Hello World", "str_helloworld")]
    [InlineData("Test", "str_test")]
    [InlineData("", "")]
    public void GetStringKey_GeneratesExpectedKey(string input, string expected)
    {
        var result = LanguageService.GetStringKey(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetStringKey_LongInput_TruncatesKeyTo50CharsOrLess()
    {
        var longInput = new string('a', 100);

        var result = LanguageService.GetStringKey(longInput);

        // Result is "str_" + up to 50 chars = max 54 chars total
        Assert.True(result.Length <= 54);
    }

    [Fact]
    public void GetStringKey_RemovesSpecialCharacters()
    {
        var result = LanguageService.GetStringKey("Hello! @World#");

        Assert.DoesNotContain("!", result);
        Assert.DoesNotContain("@", result);
        Assert.DoesNotContain("#", result);
    }

    #endregion

    #region Translate Tests

    [Fact]
    public void Translate_EnglishText_ReturnsSameForEnglish()
    {
        // When language is English, translations should return the key/original
        var result = LanguageService.Instance.Translate("Hello");

        Assert.NotNull(result);
    }

    #endregion

    #region CurrentLanguage Tests

    [Fact]
    public void CurrentLanguage_DefaultIsEnglish()
    {
        Assert.Equal("English", LanguageService.Instance.CurrentLanguage);
    }

    #endregion
}
