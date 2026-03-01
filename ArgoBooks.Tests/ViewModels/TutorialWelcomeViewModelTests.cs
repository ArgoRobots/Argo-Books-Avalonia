using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Tests for the TutorialWelcomeViewModel class.
/// </summary>
public class TutorialWelcomeViewModelTests
{
    #region Show Tests

    [Fact]
    public void Show_SetsIsOpenToTrue()
    {
        var vm = new TutorialWelcomeViewModel();

        vm.Show();

        Assert.True(vm.IsOpen);
    }

    #endregion

    #region Property Changed Tests

    [Fact]
    public void IsOpen_Set_RaisesPropertyChanged()
    {
        var vm = new TutorialWelcomeViewModel();
        var raised = false;
        vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(TutorialWelcomeViewModel.IsOpen))
                raised = true;
        };

        vm.IsOpen = true;

        Assert.True(raised);
    }

    #endregion
}
