using ArgoBooks.Core.Enums;
using Xunit;

namespace ArgoBooks.Tests.Enums;

/// <summary>
/// Tests for modal-related enums.
/// These tests verify the enum definitions haven't accidentally changed.
/// </summary>
public class ModalEnumsTests
{
    [Fact]
    public void ModalSize_HasExpectedValueCount()
    {
        // Ensure no values were accidentally added or removed
        Assert.Equal(6, Enum.GetValues<ModalSize>().Length);
    }

    [Fact]
    public void ModalResult_HasExpectedValueCount()
    {
        Assert.Equal(5, Enum.GetValues<ModalResult>().Length);
    }

    [Fact]
    public void ConfirmationResult_HasExpectedValueCount()
    {
        Assert.Equal(4, Enum.GetValues<ConfirmationResult>().Length);
    }

    [Fact]
    public void ModalResult_DefaultIsNone()
    {
        // Important: default should be None (no action taken)
        Assert.Equal(ModalResult.None, default(ModalResult));
    }

    [Fact]
    public void ConfirmationResult_DefaultIsNone()
    {
        // Important: default should be None (dialog closed without action)
        Assert.Equal(ConfirmationResult.None, default(ConfirmationResult));
    }
}
