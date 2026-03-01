using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for the SettingsModalViewModel.
/// </summary>
public class SettingsModalViewModelTests
{
    private readonly SettingsModalViewModel _viewModel;

    public SettingsModalViewModelTests()
    {
        _viewModel = new SettingsModalViewModel();
    }

    #region Time Format Tests

    [Fact]
    public void Is12HourFormat_WhenSelectedTimeFormatIs12h_ReturnsTrue()
    {
        _viewModel.SelectedTimeFormat = "12h";

        Assert.True(_viewModel.Is12HourFormat);
        Assert.False(_viewModel.Is24HourFormat);
    }

    [Fact]
    public void Is24HourFormat_WhenSelectedTimeFormatIs24h_ReturnsTrue()
    {
        _viewModel.SelectedTimeFormat = "24h";

        Assert.True(_viewModel.Is24HourFormat);
        Assert.False(_viewModel.Is12HourFormat);
    }

    #endregion

    #region Data Lists Tests

    [Fact]
    public void DateFormats_IsPopulated()
    {
        Assert.NotEmpty(_viewModel.DateFormats);
    }

    [Fact]
    public void DateFormats_ContainsExpectedFormats()
    {
        Assert.Contains("MM/DD/YYYY", _viewModel.DateFormats);
        Assert.Contains("DD/MM/YYYY", _viewModel.DateFormats);
        Assert.Contains("YYYY-MM-DD", _viewModel.DateFormats);
    }

    [Fact]
    public void Themes_IsPopulated()
    {
        Assert.NotEmpty(_viewModel.Themes);
    }

    [Fact]
    public void Themes_ContainsExpectedThemes()
    {
        Assert.Contains("Light", _viewModel.Themes);
        Assert.Contains("Dark", _viewModel.Themes);
        Assert.Contains("System", _viewModel.Themes);
    }

    [Fact]
    public void AccentColors_IsPopulated()
    {
        Assert.NotEmpty(_viewModel.AccentColors);
    }

    [Fact]
    public void MaxPieSlicesOptions_IsPopulated()
    {
        Assert.NotEmpty(_viewModel.MaxPieSlicesOptions);
        Assert.Contains(6, _viewModel.MaxPieSlicesOptions);
    }

    [Fact]
    public void ReminderMinuteOptions_IsPopulated()
    {
        Assert.NotEmpty(_viewModel.ReminderMinuteOptions);
        Assert.Contains(5, _viewModel.ReminderMinuteOptions);
    }

    [Fact]
    public void AutoLockOptions_IsPopulated()
    {
        Assert.NotEmpty(_viewModel.AutoLockOptions);
        Assert.Contains("Never", _viewModel.AutoLockOptions);
        Assert.Contains("5 minutes", _viewModel.AutoLockOptions);
    }

    #endregion

    #region Security Settings Tests

    [Fact]
    public void CanEnableWindowsHello_WhenHasPremiumAndHasPassword_ReturnsTrue()
    {
        _viewModel.HasPremium = true;
        _viewModel.HasPassword = true;

        Assert.True(_viewModel.CanEnableWindowsHello);
    }

    [Fact]
    public void CanEnableWindowsHello_WhenNoPremium_ReturnsFalse()
    {
        _viewModel.HasPremium = false;
        _viewModel.HasPassword = true;

        Assert.False(_viewModel.CanEnableWindowsHello);
    }

    [Fact]
    public void NeedsPasswordForWindowsHello_WhenHasPremiumButNoPassword_ReturnsTrue()
    {
        _viewModel.HasPremium = true;
        _viewModel.HasPassword = false;

        Assert.True(_viewModel.NeedsPasswordForWindowsHello);
    }

    [Fact]
    public void NeedsPasswordForAutoLock_WhenNoPassword_ReturnsTrue()
    {
        _viewModel.HasPassword = false;

        Assert.True(_viewModel.NeedsPasswordForAutoLock);
    }

    #endregion

    #region SelectTimeFormat Command Tests

    [Fact]
    public void SelectTimeFormatCommand_With24h_ChangesFormat()
    {
        _viewModel.SelectTimeFormatCommand.Execute("24h");

        Assert.Equal("24h", _viewModel.SelectedTimeFormat);
        Assert.True(_viewModel.Is24HourFormat);
    }

    [Fact]
    public void SelectTimeFormatCommand_With12h_ChangesFormat()
    {
        _viewModel.SelectedTimeFormat = "24h";

        _viewModel.SelectTimeFormatCommand.Execute("12h");

        Assert.Equal("12h", _viewModel.SelectedTimeFormat);
        Assert.True(_viewModel.Is12HourFormat);
    }

    #endregion
}
