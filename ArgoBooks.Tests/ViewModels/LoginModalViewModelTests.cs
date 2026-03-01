using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for the LoginModalViewModel.
/// </summary>
public class LoginModalViewModelTests
{
    private readonly LoginModalViewModel _viewModel;

    public LoginModalViewModelTests()
    {
        _viewModel = new LoginModalViewModel();
    }

    #region OpenForAccount Tests

    [Fact]
    public void OpenForAccount_WithValidAccount_SetsAccountInfo()
    {
        var account = new AccountItem
        {
            Id = "acc-123",
            Name = "Test User",
            Description = "Test Description",
            Initials = "TU",
            Color = "#FF5733"
        };

        _viewModel.OpenForAccount(account);

        Assert.Equal("acc-123", _viewModel.AccountId);
        Assert.Equal("Test User", _viewModel.AccountName);
        Assert.Equal("Test Description", _viewModel.AccountDescription);
        Assert.Equal("TU", _viewModel.AccountInitials);
    }

    [Fact]
    public void OpenForAccount_WithValidAccount_SetsIsOpenToTrue()
    {
        var account = new AccountItem
        {
            Id = "acc-123",
            Name = "Test",
            Description = "Desc",
            Initials = "T",
            Color = "#3B82F6"
        };

        _viewModel.OpenForAccount(account);

        Assert.True(_viewModel.IsOpen);
    }

    [Fact]
    public void OpenForAccount_WithValidAccount_ClearsPasswordAndError()
    {
        _viewModel.Password = "old-password";
        _viewModel.ErrorMessage = "Old error";
        _viewModel.HasError = true;

        var account = new AccountItem
        {
            Id = "acc-123",
            Name = "Test",
            Description = "Desc",
            Initials = "T",
            Color = "#3B82F6"
        };

        _viewModel.OpenForAccount(account);

        Assert.Equal(string.Empty, _viewModel.Password);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.False(_viewModel.HasError);
    }

    [Fact]
    public void OpenForAccount_WithValidAccount_ResetsLoadingAndPasswordVisibility()
    {
        var account = new AccountItem
        {
            Id = "acc-123",
            Name = "Test",
            Description = "Desc",
            Initials = "T",
            Color = "#3B82F6"
        };

        _viewModel.OpenForAccount(account);

        Assert.False(_viewModel.IsLoading);
        Assert.False(_viewModel.IsPasswordVisible);
    }

    #endregion

    #region IsPasswordVisible Tests

    [Fact]
    public void IsPasswordVisible_WhenFalse_ReturnsEyeOpenIcon()
    {
        _viewModel.IsPasswordVisible = false;

        // Eye open icon (show password action) - starts with the visible eye path
        Assert.Contains("M12 4.5C7 4.5", _viewModel.PasswordVisibilityIcon);
    }

    [Fact]
    public void IsPasswordVisible_WhenTrue_ReturnsEyeClosedIcon()
    {
        _viewModel.IsPasswordVisible = true;

        // Eye closed icon (hide password action) - starts with the crossed-out eye path
        Assert.Contains("M12 7c2.76", _viewModel.PasswordVisibilityIcon);
    }

    [Fact]
    public void IsPasswordVisible_Toggle_ChangesIcon()
    {
        var iconBefore = _viewModel.PasswordVisibilityIcon;

        _viewModel.IsPasswordVisible = true;

        Assert.NotEqual(iconBefore, _viewModel.PasswordVisibilityIcon);
    }

    #endregion

    #region IsLoading Tests

    [Fact]
    public void IsLoading_WhenFalse_LoginButtonTextIsSignIn()
    {
        _viewModel.IsLoading = false;

        Assert.Equal("Sign In", _viewModel.LoginButtonText);
    }

    [Fact]
    public void IsLoading_WhenTrue_LoginButtonTextIsSigningIn()
    {
        _viewModel.IsLoading = true;

        Assert.Equal("Signing in...", _viewModel.LoginButtonText);
    }

    #endregion

    #region HasError Tests

    [Fact]
    public void HasError_WhenErrorMessageSet_ReflectsErrorState()
    {
        _viewModel.ErrorMessage = "Invalid password";
        _viewModel.HasError = true;

        Assert.True(_viewModel.HasError);
        Assert.Equal("Invalid password", _viewModel.ErrorMessage);
    }

    [Fact]
    public void HasError_WhenErrorMessageEmpty_NoError()
    {
        _viewModel.ErrorMessage = string.Empty;
        _viewModel.HasError = false;

        Assert.False(_viewModel.HasError);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
    }

    #endregion

    #region Command Tests

    [Fact]
    public void TogglePasswordVisibilityCommand_WhenExecuted_TogglesVisibility()
    {
        Assert.False(_viewModel.IsPasswordVisible);

        _viewModel.TogglePasswordVisibilityCommand.Execute(null);

        Assert.True(_viewModel.IsPasswordVisible);

        _viewModel.TogglePasswordVisibilityCommand.Execute(null);

        Assert.False(_viewModel.IsPasswordVisible);
    }

    [Fact]
    public void CloseCommand_WhenExecuted_SetsIsOpenToFalse()
    {
        _viewModel.OpenCommand.Execute(null);
        Assert.True(_viewModel.IsOpen);

        _viewModel.CloseCommand.Execute(null);

        Assert.False(_viewModel.IsOpen);
    }

    [Fact]
    public void CloseCommand_WhenExecuted_ClearsPassword()
    {
        _viewModel.Password = "secret";

        _viewModel.CloseCommand.Execute(null);

        Assert.Equal(string.Empty, _viewModel.Password);
    }

    [Fact]
    public void OpenCommand_WhenExecuted_ResetsState()
    {
        _viewModel.Password = "old-password";
        _viewModel.ErrorMessage = "Some error";
        _viewModel.HasError = true;
        _viewModel.IsLoading = true;

        _viewModel.OpenCommand.Execute(null);

        Assert.True(_viewModel.IsOpen);
        Assert.Equal(string.Empty, _viewModel.Password);
        Assert.Equal(string.Empty, _viewModel.ErrorMessage);
        Assert.False(_viewModel.HasError);
        Assert.False(_viewModel.IsLoading);
    }

    #endregion
}
