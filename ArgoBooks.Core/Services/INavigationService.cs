namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for navigating between pages in the application.
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Gets the name of the current page.
    /// </summary>
    string CurrentPageName { get; }

    /// <summary>
    /// Navigates to the specified page.
    /// </summary>
    /// <param name="pageName">Name of the page to navigate to.</param>
    void NavigateTo(string pageName);

    /// <summary>
    /// Navigates to the specified page with a parameter.
    /// </summary>
    /// <param name="pageName">Name of the page to navigate to.</param>
    /// <param name="parameter">Parameter to pass to the page.</param>
    void NavigateTo(string pageName, object parameter);

    /// <summary>
    /// Navigates back to the previous page.
    /// </summary>
    /// <returns>True if navigation occurred, false if at the start.</returns>
    bool GoBack();

    /// <summary>
    /// Navigates forward to the next page (if available).
    /// </summary>
    /// <returns>True if navigation occurred, false if at the end.</returns>
    bool GoForward();

    /// <summary>
    /// Gets whether back navigation is available.
    /// </summary>
    bool CanGoBack { get; }

    /// <summary>
    /// Gets whether forward navigation is available.
    /// </summary>
    bool CanGoForward { get; }

    /// <summary>
    /// Clears the navigation history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Event raised when navigation occurs.
    /// </summary>
    event EventHandler<NavigationEventArgs>? Navigated;
}

/// <summary>
/// Event arguments for navigation events.
/// </summary>
public class NavigationEventArgs : EventArgs
{
    /// <summary>
    /// The page being navigated to.
    /// </summary>
    public string PageName { get; }

    /// <summary>
    /// The page being navigated from.
    /// </summary>
    public string? PreviousPageName { get; }

    /// <summary>
    /// Optional parameter passed during navigation.
    /// </summary>
    public object? Parameter { get; }

    /// <summary>
    /// Creates a new NavigationEventArgs instance.
    /// </summary>
    public NavigationEventArgs(string pageName, string? previousPageName = null, object? parameter = null)
    {
        PageName = pageName;
        PreviousPageName = previousPageName;
        Parameter = parameter;
    }
}
