using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for placeholder pages that haven't been implemented yet.
/// </summary>
public partial class PlaceholderPageViewModel : ViewModelBase
{
    /// <summary>
    /// Gets the page title.
    /// </summary>
    [ObservableProperty]
    private string _title;

    /// <summary>
    /// Gets the page description.
    /// </summary>
    [ObservableProperty]
    private string _description;

    /// <summary>
    /// Creates a new placeholder page view model.
    /// </summary>
    /// <param name="title">Page title.</param>
    /// <param name="description">Page description.</param>
    public PlaceholderPageViewModel(string title, string description)
    {
        _title = title;
        _description = description;
    }
}
