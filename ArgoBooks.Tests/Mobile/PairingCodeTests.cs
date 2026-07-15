using ArgoBooks.Shared.Sync;
using Xunit;

namespace ArgoBooks.Tests.Mobile;

/// <summary>
/// Unit tests for PairingCode: normalizing user-typed short codes and formatting them for display.
/// </summary>
public class PairingCodeTests
{
    [Fact]
    public void Normalize_LowercasesInputAndStripsSpacesAndDashes()
    {
        Assert.Equal("K7RM9XQF", PairingCode.Normalize("k7rm-9xqf "));
    }

    [Fact]
    public void Normalize_DropsCharsNotInAlphabet()
    {
        // '0', '1', 'I', 'L', 'O', 'U' are not in the alphabet and should be dropped.
        Assert.Equal("K7RM9XQF", PairingCode.Normalize("K7RM-01ILOU-9XQF"));
    }

    [Fact]
    public void Normalize_EmptyInput_ReturnsEmptyString()
    {
        Assert.Equal(string.Empty, PairingCode.Normalize(""));
    }

    [Fact]
    public void Format_EightCharCode_InsertsDashAfterFourChars()
    {
        Assert.Equal("K7RM-9XQF", PairingCode.Format("K7RM9XQF"));
    }

    [Fact]
    public void Format_NonEightCharCode_ReturnsInputUnchanged()
    {
        Assert.Equal("K7RM9X", PairingCode.Format("K7RM9X"));
        Assert.Equal(string.Empty, PairingCode.Format(""));
    }
}
