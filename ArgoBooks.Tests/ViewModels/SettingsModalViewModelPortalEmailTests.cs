using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Guards the "server wins, but only when clean" rule that decides whether the
/// desktop mirrors the server's portal owner email onto this device. This is
/// what lets a server-side change (e.g. the email-change revert link) reach the
/// app, without clobbering a local edit that is still in flight.
/// </summary>
public class SettingsModalViewModelPortalEmailTests
{
    [Fact]
    public void Reconciles_when_server_email_is_present_different_and_not_editing()
    {
        Assert.True(SettingsModalViewModel.ShouldReconcilePortalEmail(
            serverEmail: "old@example.com",
            localEmail: "new@example.com",
            isEmailBeingEdited: false));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Skips_when_server_email_is_blank(string? serverEmail)
    {
        // Never wipe a good local value with a blank; covers the pre-set and
        // pending-change windows where the server has no owner email yet.
        Assert.False(SettingsModalViewModel.ShouldReconcilePortalEmail(
            serverEmail,
            localEmail: "new@example.com",
            isEmailBeingEdited: false));
    }

    [Theory]
    [InlineData("same@example.com", "same@example.com")]
    [InlineData("Same@Example.com", "same@example.com")]
    [InlineData("  same@example.com  ", "same@example.com")]
    public void Skips_when_values_match_ignoring_case_and_whitespace(string serverEmail, string localEmail)
    {
        // Avoid dirtying the file with a no-op write.
        Assert.False(SettingsModalViewModel.ShouldReconcilePortalEmail(
            serverEmail, localEmail, isEmailBeingEdited: false));
    }

    [Fact]
    public void Skips_when_email_is_being_hand_edited()
    {
        // A different server value would normally reconcile, but an open
        // Company-details editor means the user is mid-edit; don't clobber it.
        Assert.False(SettingsModalViewModel.ShouldReconcilePortalEmail(
            serverEmail: "old@example.com",
            localEmail: "typing@example.com",
            isEmailBeingEdited: true));
    }

    [Fact]
    public void Reconciles_against_an_empty_local_email()
    {
        // Empty local + real server value should sync (e.g. self-heal a device
        // whose local email got lost).
        Assert.True(SettingsModalViewModel.ShouldReconcilePortalEmail(
            serverEmail: "owner@example.com",
            localEmail: "",
            isEmailBeingEdited: false));
    }
}
