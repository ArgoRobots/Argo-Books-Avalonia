using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// The email step that follows a redeemed key already says "Premium is active" in its own
/// panel, so dismissing it has to close the modal. Sending the customer on to the success
/// animation shows them the same news twice.
/// </summary>
public class UpgradeModalViewModelTests
{
    private static UpgradeModalViewModel AtEmailStep()
    {
        var vm = new UpgradeModalViewModel
        {
            IsEnterKeyModalOpen = true,
            IsEmailCaptureStep = true,
            LicenseKey = "ABCD-EFGH-IJKL-MNOP-QRST",
            CustomerEmail = "typed@example.com",
        };
        return vm;
    }

    [Fact]
    public void SkippingTheEmailStep_ClosesTheModal()
    {
        var vm = AtEmailStep();

        vm.SkipEmailCaptureCommand.Execute(null);

        Assert.False(vm.IsEnterKeyModalOpen);
        Assert.False(vm.IsEmailCaptureStep);
        Assert.False(vm.IsVerificationSuccess);
    }

    /// <summary>
    /// The success panel's Continue button is what turns premium on in the running app, so
    /// a dismiss that bypasses that panel has to raise the same event or the customer stays
    /// on the free tier until they restart.
    /// </summary>
    [Fact]
    public void SkippingTheEmailStep_StillReportsTheKeyAsVerified()
    {
        var vm = AtEmailStep();
        string? verified = null;
        vm.KeyVerified += (_, key) => verified = key;

        vm.SkipEmailCaptureCommand.Execute(null);

        Assert.Equal("ABCD-EFGH-IJKL-MNOP-QRST", verified);
    }
}
