using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for first-visit hint tooltips shown when visiting a page for the first time.
/// </summary>
public partial class FirstVisitHintViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isVisible;

    [ObservableProperty]
    private string _hintText = "";

    [ObservableProperty]
    private string _pageName = "";

    private string _currentPageId = "";

    /// <summary>
    /// Shows a hint for the specified page if it's the first visit.
    /// </summary>
    /// <param name="pageId">The page identifier.</param>
    /// <param name="pageName">The display name of the page.</param>
    public void ShowForPage(string pageId, string pageName)
    {
        // Don't show if hints are disabled or page was already visited
        if (!TutorialService.Instance.ShowFirstVisitHints ||
            TutorialService.Instance.HasVisitedPage(pageId))
        {
            IsVisible = false;
            return;
        }

        var hint = TutorialService.GetFirstVisitHint(pageId);
        if (string.IsNullOrEmpty(hint))
        {
            IsVisible = false;
            return;
        }

        _currentPageId = pageId;
        PageName = pageName;
        HintText = hint;
        IsVisible = true;
    }

    /// <summary>
    /// Hides the current hint.
    /// </summary>
    public void Hide()
    {
        IsVisible = false;
    }

    [RelayCommand]
    private void Dismiss()
    {
        if (!string.IsNullOrEmpty(_currentPageId))
        {
            TutorialService.Instance.MarkPageVisited(_currentPageId);
        }
        IsVisible = false;
    }

    [RelayCommand]
    private void DontShowAgain()
    {
        TutorialService.Instance.DisableFirstVisitHints();
        IsVisible = false;
    }
}
