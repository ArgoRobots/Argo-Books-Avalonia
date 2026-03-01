using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for the UserPanelViewModel.
/// </summary>
public class UserPanelViewModelTests
{
    #region Default State Tests

    [Fact]
    public void Constructor_Default_HasDesignTimeUserInfo()
    {
        // Default constructor creates a HeaderViewModel with design-time defaults
        var vm = new UserPanelViewModel();

        Assert.NotNull(vm.UserDisplayName);
        Assert.NotNull(vm.UserInitials);
    }

    #endregion

    #region Constructor with HeaderViewModel Tests

    [Fact]
    public void Constructor_WithHeaderViewModel_ReflectsHeaderUserInfo()
    {
        var headerVm = new HeaderViewModel(null);
        headerVm.SetUserInfo("Alice Smith", email: "alice@example.com", role: "Manager");
        var vm = new UserPanelViewModel(headerVm);

        Assert.Equal("Alice Smith", vm.UserDisplayName);
        Assert.Equal("alice@example.com", vm.UserEmail);
        Assert.Equal("Manager", vm.UserRole);
        Assert.Equal("AS", vm.UserInitials);
    }

    #endregion

    #region User Info Delegation Tests

    [Fact]
    public void UserDisplayName_ReflectsHeaderViewModel()
    {
        var headerVm = new HeaderViewModel(null);
        var vm = new UserPanelViewModel(headerVm);

        headerVm.SetUserInfo("Bob Jones");

        Assert.Equal("Bob Jones", vm.UserDisplayName);
    }

    [Fact]
    public void UserEmail_ReflectsHeaderViewModel()
    {
        var headerVm = new HeaderViewModel(null);
        var vm = new UserPanelViewModel(headerVm);

        headerVm.SetUserInfo("Bob", email: "bob@test.com");

        Assert.Equal("bob@test.com", vm.UserEmail);
    }

    [Fact]
    public void UserRole_ReflectsHeaderViewModel()
    {
        var headerVm = new HeaderViewModel(null);
        var vm = new UserPanelViewModel(headerVm);

        headerVm.SetUserInfo("Bob", role: "Administrator");

        Assert.Equal("Administrator", vm.UserRole);
    }

    [Fact]
    public void UserInitials_ReflectsHeaderViewModel()
    {
        var headerVm = new HeaderViewModel(null);
        var vm = new UserPanelViewModel(headerVm);

        headerVm.SetUserInfo("Charlie Brown");

        Assert.Equal("CB", vm.UserInitials);
    }

    [Fact]
    public void HasUserAvatar_WhenNoAvatar_IsFalse()
    {
        var headerVm = new HeaderViewModel(null);
        var vm = new UserPanelViewModel(headerVm);

        headerVm.SetUserInfo("Alice");

        Assert.False(vm.HasUserAvatar);
    }

    [Fact]
    public void UserAvatarSource_WhenNoAvatar_IsNull()
    {
        var headerVm = new HeaderViewModel(null);
        var vm = new UserPanelViewModel(headerVm);

        headerVm.SetUserInfo("Alice");

        Assert.Null(vm.UserAvatarSource);
    }

    #endregion

    #region Open/Close/Toggle Commands Tests

    [Fact]
    public void OpenCommand_WhenExecuted_SetsIsOpenToTrue()
    {
        var vm = new UserPanelViewModel();

        vm.OpenCommand.Execute(null);

        Assert.True(vm.IsOpen);
    }

    [Fact]
    public void CloseCommand_WhenOpen_SetsIsOpenToFalse()
    {
        var vm = new UserPanelViewModel();
        vm.IsOpen = true;

        vm.CloseCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void ToggleCommand_WhenClosed_SetsIsOpenToTrue()
    {
        var vm = new UserPanelViewModel();

        vm.ToggleCommand.Execute(null);

        Assert.True(vm.IsOpen);
    }

    [Fact]
    public void ToggleCommand_WhenOpen_SetsIsOpenToFalse()
    {
        var vm = new UserPanelViewModel();
        vm.IsOpen = true;

        vm.ToggleCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void ToggleCommand_DoubleTap_RestoresOriginalState()
    {
        var vm = new UserPanelViewModel();

        vm.ToggleCommand.Execute(null);
        vm.ToggleCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    #endregion

    #region Panel Action Commands Tests

    [Fact]
    public void OpenProfileCommand_WhenExecuted_ClosesPanel()
    {
        var vm = new UserPanelViewModel();
        vm.IsOpen = true;

        vm.OpenProfileCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void OpenMyPlanCommand_WhenExecuted_ClosesPanel()
    {
        var vm = new UserPanelViewModel();
        vm.IsOpen = true;

        vm.OpenMyPlanCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void OpenSettingsCommand_WhenExecuted_ClosesPanel()
    {
        var vm = new UserPanelViewModel();
        vm.IsOpen = true;

        vm.OpenSettingsCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void OpenHelpCommand_WhenExecuted_ClosesPanel()
    {
        var vm = new UserPanelViewModel();
        vm.IsOpen = true;

        vm.OpenHelpCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void SwitchAccountCommand_WhenExecuted_ClosesPanel()
    {
        var vm = new UserPanelViewModel();
        vm.IsOpen = true;

        vm.SwitchAccountCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    [Fact]
    public void SignOutCommand_WhenExecuted_ClosesPanel()
    {
        var vm = new UserPanelViewModel();
        vm.IsOpen = true;

        vm.SignOutCommand.Execute(null);

        Assert.False(vm.IsOpen);
    }

    #endregion

    #region Event Raising Tests

    [Fact]
    public void OpenProfileCommand_RaisesOpenProfileRequestedEvent()
    {
        var vm = new UserPanelViewModel();
        var eventRaised = false;
        vm.OpenProfileRequested += (_, _) => eventRaised = true;

        vm.OpenProfileCommand.Execute(null);

        Assert.True(eventRaised);
    }

    [Fact]
    public void OpenMyPlanCommand_RaisesOpenMyPlanRequestedEvent()
    {
        var vm = new UserPanelViewModel();
        var eventRaised = false;
        vm.OpenMyPlanRequested += (_, _) => eventRaised = true;

        vm.OpenMyPlanCommand.Execute(null);

        Assert.True(eventRaised);
    }

    [Fact]
    public void SignOutCommand_RaisesSignOutRequestedEvent()
    {
        var vm = new UserPanelViewModel();
        var eventRaised = false;
        vm.SignOutRequested += (_, _) => eventRaised = true;

        vm.SignOutCommand.Execute(null);

        Assert.True(eventRaised);
    }

    #endregion

}
