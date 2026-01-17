using ArgoBooks.Core.Enums;
using Xunit;

namespace ArgoBooks.Tests.Enums;

/// <summary>
/// Tests for modal-related enums.
/// </summary>
public class ModalEnumsTests
{
    #region ModalSize Tests

    [Fact]
    public void ModalSize_ContainsAllExpectedValues()
    {
        var values = Enum.GetValues<ModalSize>();

        Assert.Equal(6, values.Length);
        Assert.Contains(ModalSize.Small, values);
        Assert.Contains(ModalSize.Medium, values);
        Assert.Contains(ModalSize.Large, values);
        Assert.Contains(ModalSize.ExtraLarge, values);
        Assert.Contains(ModalSize.Full, values);
        Assert.Contains(ModalSize.Custom, values);
    }

    [Theory]
    [InlineData(ModalSize.Small, 0)]
    [InlineData(ModalSize.Medium, 1)]
    [InlineData(ModalSize.Large, 2)]
    [InlineData(ModalSize.ExtraLarge, 3)]
    [InlineData(ModalSize.Full, 4)]
    [InlineData(ModalSize.Custom, 5)]
    public void ModalSize_HasExpectedOrdinalValues(ModalSize size, int expectedOrdinal)
    {
        Assert.Equal(expectedOrdinal, (int)size);
    }

    [Fact]
    public void ModalSize_DefaultValue_IsSmall()
    {
        var defaultSize = default(ModalSize);

        Assert.Equal(ModalSize.Small, defaultSize);
    }

    [Theory]
    [InlineData("Small")]
    [InlineData("Medium")]
    [InlineData("Large")]
    [InlineData("ExtraLarge")]
    [InlineData("Full")]
    [InlineData("Custom")]
    public void ModalSize_CanBeParsedFromString(string sizeName)
    {
        var parsed = Enum.Parse<ModalSize>(sizeName);

        Assert.True(Enum.IsDefined(parsed));
    }

    [Fact]
    public void ModalSize_ToString_ReturnsName()
    {
        Assert.Equal("Small", ModalSize.Small.ToString());
        Assert.Equal("Medium", ModalSize.Medium.ToString());
        Assert.Equal("Large", ModalSize.Large.ToString());
        Assert.Equal("ExtraLarge", ModalSize.ExtraLarge.ToString());
        Assert.Equal("Full", ModalSize.Full.ToString());
        Assert.Equal("Custom", ModalSize.Custom.ToString());
    }

    #endregion

    #region ModalResult Tests

    [Fact]
    public void ModalResult_ContainsAllExpectedValues()
    {
        var values = Enum.GetValues<ModalResult>();

        Assert.Equal(5, values.Length);
        Assert.Contains(ModalResult.None, values);
        Assert.Contains(ModalResult.Ok, values);
        Assert.Contains(ModalResult.Cancel, values);
        Assert.Contains(ModalResult.Yes, values);
        Assert.Contains(ModalResult.No, values);
    }

    [Theory]
    [InlineData(ModalResult.None, 0)]
    [InlineData(ModalResult.Ok, 1)]
    [InlineData(ModalResult.Cancel, 2)]
    [InlineData(ModalResult.Yes, 3)]
    [InlineData(ModalResult.No, 4)]
    public void ModalResult_HasExpectedOrdinalValues(ModalResult result, int expectedOrdinal)
    {
        Assert.Equal(expectedOrdinal, (int)result);
    }

    [Fact]
    public void ModalResult_DefaultValue_IsNone()
    {
        var defaultResult = default(ModalResult);

        Assert.Equal(ModalResult.None, defaultResult);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Ok")]
    [InlineData("Cancel")]
    [InlineData("Yes")]
    [InlineData("No")]
    public void ModalResult_CanBeParsedFromString(string resultName)
    {
        var parsed = Enum.Parse<ModalResult>(resultName);

        Assert.True(Enum.IsDefined(parsed));
    }

    [Fact]
    public void ModalResult_ToString_ReturnsName()
    {
        Assert.Equal("None", ModalResult.None.ToString());
        Assert.Equal("Ok", ModalResult.Ok.ToString());
        Assert.Equal("Cancel", ModalResult.Cancel.ToString());
        Assert.Equal("Yes", ModalResult.Yes.ToString());
        Assert.Equal("No", ModalResult.No.ToString());
    }

    #endregion

    #region ConfirmationResult Tests

    [Fact]
    public void ConfirmationResult_ContainsAllExpectedValues()
    {
        var values = Enum.GetValues<ConfirmationResult>();

        Assert.Equal(4, values.Length);
        Assert.Contains(ConfirmationResult.None, values);
        Assert.Contains(ConfirmationResult.Primary, values);
        Assert.Contains(ConfirmationResult.Secondary, values);
        Assert.Contains(ConfirmationResult.Cancel, values);
    }

    [Theory]
    [InlineData(ConfirmationResult.None, 0)]
    [InlineData(ConfirmationResult.Primary, 1)]
    [InlineData(ConfirmationResult.Secondary, 2)]
    [InlineData(ConfirmationResult.Cancel, 3)]
    public void ConfirmationResult_HasExpectedOrdinalValues(ConfirmationResult result, int expectedOrdinal)
    {
        Assert.Equal(expectedOrdinal, (int)result);
    }

    [Fact]
    public void ConfirmationResult_DefaultValue_IsNone()
    {
        var defaultResult = default(ConfirmationResult);

        Assert.Equal(ConfirmationResult.None, defaultResult);
    }

    [Theory]
    [InlineData("None")]
    [InlineData("Primary")]
    [InlineData("Secondary")]
    [InlineData("Cancel")]
    public void ConfirmationResult_CanBeParsedFromString(string resultName)
    {
        var parsed = Enum.Parse<ConfirmationResult>(resultName);

        Assert.True(Enum.IsDefined(parsed));
    }

    [Fact]
    public void ConfirmationResult_ToString_ReturnsName()
    {
        Assert.Equal("None", ConfirmationResult.None.ToString());
        Assert.Equal("Primary", ConfirmationResult.Primary.ToString());
        Assert.Equal("Secondary", ConfirmationResult.Secondary.ToString());
        Assert.Equal("Cancel", ConfirmationResult.Cancel.ToString());
    }

    #endregion

    #region Enum Comparison Tests

    [Fact]
    public void ModalResult_CanBeCompared()
    {
        Assert.True(ModalResult.Ok > ModalResult.None);
        Assert.True(ModalResult.Cancel > ModalResult.Ok);
        Assert.False(ModalResult.None > ModalResult.Ok);
    }

    [Fact]
    public void ConfirmationResult_CanBeCompared()
    {
        Assert.True(ConfirmationResult.Primary > ConfirmationResult.None);
        Assert.True(ConfirmationResult.Cancel > ConfirmationResult.Secondary);
        Assert.False(ConfirmationResult.None > ConfirmationResult.Primary);
    }

    [Fact]
    public void ModalSize_CanBeCompared()
    {
        Assert.True(ModalSize.Large > ModalSize.Medium);
        Assert.True(ModalSize.Full > ModalSize.ExtraLarge);
        Assert.False(ModalSize.Small > ModalSize.Medium);
    }

    #endregion

    #region Usage Pattern Tests

    [Fact]
    public void ModalResult_CanBeUsedInSwitchExpression()
    {
        var result = ModalResult.Ok;

        var message = result switch
        {
            ModalResult.None => "No action",
            ModalResult.Ok => "Confirmed",
            ModalResult.Cancel => "Cancelled",
            ModalResult.Yes => "Agreed",
            ModalResult.No => "Declined",
            _ => "Unknown"
        };

        Assert.Equal("Confirmed", message);
    }

    [Fact]
    public void ConfirmationResult_CanBeUsedInSwitchExpression()
    {
        var result = ConfirmationResult.Primary;

        var shouldProceed = result switch
        {
            ConfirmationResult.Primary => true,
            ConfirmationResult.Secondary => true,
            ConfirmationResult.Cancel => false,
            ConfirmationResult.None => false,
            _ => false
        };

        Assert.True(shouldProceed);
    }

    [Theory]
    [InlineData(ConfirmationResult.Primary, true)]
    [InlineData(ConfirmationResult.Secondary, false)]
    [InlineData(ConfirmationResult.Cancel, false)]
    [InlineData(ConfirmationResult.None, false)]
    public void ConfirmationResult_IsPrimaryAction(ConfirmationResult result, bool expectedIsPrimary)
    {
        var isPrimary = result == ConfirmationResult.Primary;

        Assert.Equal(expectedIsPrimary, isPrimary);
    }

    [Theory]
    [InlineData(ModalResult.Ok, true)]
    [InlineData(ModalResult.Yes, true)]
    [InlineData(ModalResult.Cancel, false)]
    [InlineData(ModalResult.No, false)]
    [InlineData(ModalResult.None, false)]
    public void ModalResult_IsPositiveAction(ModalResult result, bool expectedIsPositive)
    {
        var isPositive = result == ModalResult.Ok || result == ModalResult.Yes;

        Assert.Equal(expectedIsPositive, isPositive);
    }

    #endregion
}
