using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

public class AppShellViewModelTests
{
    private readonly AppShellViewModel _viewModel = new();

    [Fact]
    public void ShowUpdateBanner_SetsProperties()
    {
        _viewModel.ShowUpdateBanner("2.0");

        Assert.True(_viewModel.ShowUpdateAvailableBanner);
        Assert.False(string.IsNullOrEmpty(_viewModel.UpdateBannerMessage));
        Assert.Contains("2.0", _viewModel.UpdateBannerMessage);
    }

    [Fact]
    public void DismissUpdateBanner_HidesBanner()
    {
        _viewModel.ShowUpdateBanner("Test");
        _viewModel.DismissUpdateBannerCommand.Execute(null);

        Assert.False(_viewModel.ShowUpdateAvailableBanner);
    }
}
