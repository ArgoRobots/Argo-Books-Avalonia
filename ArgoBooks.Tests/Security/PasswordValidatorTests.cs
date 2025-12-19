using ArgoBooks.Core.Security;
using Xunit;

namespace ArgoBooks.Tests.Security;

/// <summary>
/// Tests for the PasswordValidator class.
/// </summary>
public class PasswordValidatorTests
{
    #region Constants Tests

    [Fact]
    public void Constants_HaveReasonableValues()
    {
        // Minimum length should be at least 8 for security
        Assert.True(PasswordValidator.MinLength >= 8);

        // Maximum length should allow for passphrase-style passwords
        Assert.True(PasswordValidator.MaxLength >= 64);
    }

    #endregion

    #region IsValid Tests

    [Fact]
    public void IsValid_ReturnsTrueForValidPassword()
    {
        Assert.True(PasswordValidator.IsValid("Password123"));
    }

    [Fact]
    public void IsValid_ReturnsFalseForNull()
    {
        Assert.False(PasswordValidator.IsValid(null));
    }

    [Fact]
    public void IsValid_ReturnsFalseForEmpty()
    {
        Assert.False(PasswordValidator.IsValid(""));
    }

    [Fact]
    public void IsValid_ReturnsFalseForTooShort()
    {
        Assert.False(PasswordValidator.IsValid("Pass1")); // 5 chars, less than MinLength
    }

    [Fact]
    public void IsValid_ReturnsFalseForTooLong()
    {
        var tooLong = new string('a', PasswordValidator.MaxLength + 1) + "1";
        Assert.False(PasswordValidator.IsValid(tooLong));
    }

    [Fact]
    public void IsValid_ReturnsFalseForNoLetters()
    {
        Assert.False(PasswordValidator.IsValid("12345678"));
    }

    [Fact]
    public void IsValid_ReturnsFalseForNoDigits()
    {
        Assert.False(PasswordValidator.IsValid("Password"));
    }

    [Theory]
    [InlineData("Password1")]       // Basic valid
    [InlineData("mypassword123")]   // Lowercase with numbers
    [InlineData("MYPASSWORD123")]   // Uppercase with numbers
    [InlineData("MyP@ssw0rd!")]     // Mixed with special chars
    [InlineData("12345678a")]       // Numbers first, then letter
    public void IsValid_AcceptsVariousValidPasswords(string password)
    {
        Assert.True(PasswordValidator.IsValid(password));
    }

    #endregion

    #region GetValidationError Tests

    [Fact]
    public void GetValidationError_ReturnsNullForValidPassword()
    {
        Assert.Null(PasswordValidator.GetValidationError("Password123"));
    }

    [Fact]
    public void GetValidationError_ReturnsErrorForNull()
    {
        var error = PasswordValidator.GetValidationError(null);
        Assert.NotNull(error);
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetValidationError_ReturnsErrorForEmpty()
    {
        var error = PasswordValidator.GetValidationError("");
        Assert.NotNull(error);
        Assert.Contains("required", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetValidationError_ReturnsErrorForTooShort()
    {
        var error = PasswordValidator.GetValidationError("Pass1");
        Assert.NotNull(error);
        Assert.Contains("at least", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetValidationError_ReturnsErrorForTooLong()
    {
        var tooLong = new string('a', PasswordValidator.MaxLength + 1) + "1";
        var error = PasswordValidator.GetValidationError(tooLong);
        Assert.NotNull(error);
        Assert.Contains("no more than", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetValidationError_ReturnsErrorForNoLetters()
    {
        var error = PasswordValidator.GetValidationError("12345678");
        Assert.NotNull(error);
        Assert.Contains("letter", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetValidationError_ReturnsErrorForNoDigits()
    {
        var error = PasswordValidator.GetValidationError("Password");
        Assert.NotNull(error);
        Assert.Contains("number", error, StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetAllValidationErrors Tests

    [Fact]
    public void GetAllValidationErrors_ReturnsEmptyForValidPassword()
    {
        var errors = PasswordValidator.GetAllValidationErrors("Password123");
        Assert.Empty(errors);
    }

    [Fact]
    public void GetAllValidationErrors_ReturnsMultipleErrorsWhenApplicable()
    {
        // A password that is too short and has no digits
        var errors = PasswordValidator.GetAllValidationErrors("abc");
        Assert.True(errors.Count >= 2);
    }

    [Fact]
    public void GetAllValidationErrors_ReturnsOnlyRequiredErrorForNull()
    {
        var errors = PasswordValidator.GetAllValidationErrors(null);
        Assert.Single(errors);
        Assert.Contains("required", errors[0], StringComparison.OrdinalIgnoreCase);
    }

    #endregion

    #region GetStrengthScore Tests

    [Fact]
    public void GetStrengthScore_ReturnsZeroForNull()
    {
        Assert.Equal(0, PasswordValidator.GetStrengthScore(null));
    }

    [Fact]
    public void GetStrengthScore_ReturnsZeroForEmpty()
    {
        Assert.Equal(0, PasswordValidator.GetStrengthScore(""));
    }

    [Fact]
    public void GetStrengthScore_ReturnsLowScoreForWeakPassword()
    {
        var score = PasswordValidator.GetStrengthScore("a1");
        Assert.True(score < 40);
    }

    [Fact]
    public void GetStrengthScore_ReturnsHighScoreForStrongPassword()
    {
        var score = PasswordValidator.GetStrengthScore("MyStr0ng!P@ssw0rd");
        Assert.True(score >= 60);
    }

    [Fact]
    public void GetStrengthScore_LongerPasswordsScoreHigher()
    {
        var shortScore = PasswordValidator.GetStrengthScore("Aa1!");
        var longScore = PasswordValidator.GetStrengthScore("Aa1!Bb2@Cc3#");

        Assert.True(longScore > shortScore);
    }

    [Fact]
    public void GetStrengthScore_MixedCaseScoresHigher()
    {
        var lowerScore = PasswordValidator.GetStrengthScore("password123!");
        var mixedScore = PasswordValidator.GetStrengthScore("Password123!");

        Assert.True(mixedScore > lowerScore);
    }

    [Fact]
    public void GetStrengthScore_SpecialCharsScoreHigher()
    {
        var noSpecialScore = PasswordValidator.GetStrengthScore("Password123");
        var withSpecialScore = PasswordValidator.GetStrengthScore("Password123!");

        Assert.True(withSpecialScore > noSpecialScore);
    }

    [Fact]
    public void GetStrengthScore_RepeatingCharsPenalized()
    {
        var normalScore = PasswordValidator.GetStrengthScore("Password123!");
        var repeatingScore = PasswordValidator.GetStrengthScore("Passwoooord123!");

        Assert.True(repeatingScore < normalScore);
    }

    [Fact]
    public void GetStrengthScore_SequentialCharsPenalized()
    {
        // Use passwords without "123" since that's also sequential
        var normalScore = PasswordValidator.GetStrengthScore("Pmqz597!");
        var sequentialScore = PasswordValidator.GetStrengthScore("Pabc597!");

        Assert.True(sequentialScore < normalScore);
    }

    [Fact]
    public void GetStrengthScore_ClampedBetween0And100()
    {
        // Very weak password
        var weakScore = PasswordValidator.GetStrengthScore("a");
        Assert.True(weakScore >= 0 && weakScore <= 100);

        // Very strong password
        var strongScore = PasswordValidator.GetStrengthScore("MyV3ryStr0ng!P@ssw0rd#2024");
        Assert.True(strongScore >= 0 && strongScore <= 100);
    }

    #endregion

    #region GetStrengthDescription Tests

    [Theory]
    [InlineData(0, "Very Weak")]
    [InlineData(10, "Very Weak")]
    [InlineData(19, "Very Weak")]
    [InlineData(20, "Weak")]
    [InlineData(39, "Weak")]
    [InlineData(40, "Fair")]
    [InlineData(59, "Fair")]
    [InlineData(60, "Strong")]
    [InlineData(79, "Strong")]
    [InlineData(80, "Very Strong")]
    [InlineData(100, "Very Strong")]
    public void GetStrengthDescription_ReturnsCorrectDescription(int score, string expected)
    {
        Assert.Equal(expected, PasswordValidator.GetStrengthDescription(score));
    }

    #endregion
}
