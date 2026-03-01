using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for the CreateCompanyViewModel.
/// </summary>
public class CreateCompanyViewModelTests
{
    private readonly CreateCompanyViewModel _viewModel;

    public CreateCompanyViewModelTests()
    {
        _viewModel = new CreateCompanyViewModel();
    }

    #region Step Computed Properties Tests

    [Fact]
    public void IsStep1_WhenCurrentStepIsOne_ReturnsTrue()
    {
        _viewModel.CurrentStep = 1;

        Assert.True(_viewModel.IsStep1);
    }

    [Fact]
    public void IsStep1_WhenCurrentStepIsTwo_ReturnsFalse()
    {
        _viewModel.CurrentStep = 2;

        Assert.False(_viewModel.IsStep1);
    }

    [Fact]
    public void IsStep2_WhenCurrentStepIsTwo_ReturnsTrue()
    {
        _viewModel.CurrentStep = 2;

        Assert.True(_viewModel.IsStep2);
    }

    [Fact]
    public void IsStep2_WhenCurrentStepIsOne_ReturnsFalse()
    {
        _viewModel.CurrentStep = 1;

        Assert.False(_viewModel.IsStep2);
    }

    #endregion

    #region Navigation Properties Tests

    [Fact]
    public void CanGoBack_WhenOnStep1_ReturnsFalse()
    {
        _viewModel.CurrentStep = 1;

        Assert.False(_viewModel.CanGoBack);
    }

    [Fact]
    public void CanGoBack_WhenOnStep2_ReturnsTrue()
    {
        _viewModel.CurrentStep = 2;

        Assert.True(_viewModel.CanGoBack);
    }

    [Fact]
    public void CanGoNext_WhenOnStep1_ReturnsTrue()
    {
        _viewModel.CurrentStep = 1;

        Assert.True(_viewModel.CanGoNext);
    }

    [Fact]
    public void CanGoNext_WhenOnStep2_ReturnsFalse()
    {
        _viewModel.CurrentStep = 2;

        Assert.False(_viewModel.CanGoNext);
    }

    [Fact]
    public void IsLastStep_WhenOnStep1_ReturnsFalse()
    {
        _viewModel.CurrentStep = 1;

        Assert.False(_viewModel.IsLastStep);
    }

    [Fact]
    public void IsLastStep_WhenOnStep2_ReturnsTrue()
    {
        _viewModel.CurrentStep = 2;

        Assert.True(_viewModel.IsLastStep);
    }

    #endregion

    #region Data Lists Tests

    [Fact]
    public void BusinessTypes_ListIsNotEmpty()
    {
        Assert.NotEmpty(_viewModel.BusinessTypes);
    }

    [Fact]
    public void BusinessTypes_ContainsExpectedTypes()
    {
        Assert.Contains("Sole Proprietorship", _viewModel.BusinessTypes);
        Assert.Contains("Corporation", _viewModel.BusinessTypes);
        Assert.Contains("LLC", _viewModel.BusinessTypes);
    }

    [Fact]
    public void Industries_ListIsNotEmpty()
    {
        Assert.NotEmpty(_viewModel.Industries);
    }

    [Fact]
    public void Industries_ContainsExpectedIndustries()
    {
        Assert.Contains("Retail", _viewModel.Industries);
        Assert.Contains("Technology", _viewModel.Industries);
        Assert.Contains("Healthcare", _viewModel.Industries);
    }

    [Fact]
    public void Currencies_ListIsPopulated()
    {
        Assert.NotNull(_viewModel.Currencies);
        Assert.NotEmpty(_viewModel.Currencies);
    }

    [Fact]
    public void PriorityCurrencies_ListIsPopulated()
    {
        Assert.NotNull(_viewModel.PriorityCurrencies);
        Assert.NotEmpty(_viewModel.PriorityCurrencies);
    }

    #endregion

    #region Navigation Command Tests

    [Fact]
    public void NextStepCommand_WhenOnStep1_AdvancesToStep2()
    {
        _viewModel.CurrentStep = 1;

        _viewModel.NextStepCommand.Execute(null);

        Assert.Equal(2, _viewModel.CurrentStep);
    }

    [Fact]
    public void PreviousStepCommand_WhenOnStep2_GoesBackToStep1()
    {
        _viewModel.CurrentStep = 2;

        _viewModel.PreviousStepCommand.Execute(null);

        Assert.Equal(1, _viewModel.CurrentStep);
    }

    [Fact]
    public void PreviousStepCommand_WhenOnStep1_StaysOnStep1()
    {
        _viewModel.CurrentStep = 1;

        _viewModel.PreviousStepCommand.Execute(null);

        Assert.Equal(1, _viewModel.CurrentStep);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void IsStep1Valid_WhenCompanyNameAndCountrySet_ReturnsTrue()
    {
        _viewModel.CompanyName = "Test Company";
        _viewModel.Country = "United States";

        Assert.True(_viewModel.IsStep1Valid);
    }

    [Fact]
    public void IsStep1Valid_WhenCompanyNameEmpty_ReturnsFalse()
    {
        _viewModel.CompanyName = "";
        _viewModel.Country = "United States";

        Assert.False(_viewModel.IsStep1Valid);
    }

    [Fact]
    public void IsStep2Valid_WhenPasswordDisabled_ReturnsTrue()
    {
        _viewModel.EnablePassword = false;

        Assert.True(_viewModel.IsStep2Valid);
    }

    [Fact]
    public void PasswordsMatch_WhenBothMatch_ReturnsTrue()
    {
        _viewModel.Password = "test123";
        _viewModel.ConfirmPassword = "test123";

        Assert.True(_viewModel.PasswordsMatch);
    }

    [Fact]
    public void PasswordsMatch_WhenDifferent_ReturnsFalse()
    {
        _viewModel.Password = "test123";
        _viewModel.ConfirmPassword = "different";

        Assert.False(_viewModel.PasswordsMatch);
    }

    #endregion

    #region Open/Close Tests

    [Fact]
    public void OpenCommand_WhenExecuted_SetsIsOpenToTrue()
    {
        _viewModel.OpenCommand.Execute(null);

        Assert.True(_viewModel.IsOpen);
    }

    [Fact]
    public void OpenCommand_WhenExecuted_ResetsCurrentStepToOne()
    {
        _viewModel.CurrentStep = 2;

        _viewModel.OpenCommand.Execute(null);

        Assert.Equal(1, _viewModel.CurrentStep);
    }

    [Fact]
    public void CloseCommand_WhenExecuted_SetsIsOpenToFalse()
    {
        _viewModel.OpenCommand.Execute(null);
        Assert.True(_viewModel.IsOpen);

        _viewModel.CloseCommand.Execute(null);

        Assert.False(_viewModel.IsOpen);
    }

    #endregion
}
