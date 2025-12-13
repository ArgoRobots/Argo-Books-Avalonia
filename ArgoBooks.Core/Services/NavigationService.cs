namespace ArgoBooks.Core.Services;

/// <summary>
/// Implementation of INavigationService providing page navigation with history support.
/// </summary>
public class NavigationService : INavigationService
{
    private readonly Stack<NavigationEntry> _backStack = new();
    private readonly Stack<NavigationEntry> _forwardStack = new();
    private NavigationEntry? _currentEntry;

    private readonly Dictionary<string, Func<object?, object>> _pageFactories = new();
    private Action<object>? _navigationCallback;

    /// <inheritdoc />
    public string CurrentPageName => _currentEntry?.PageName ?? string.Empty;

    /// <inheritdoc />
    public bool CanGoBack => _backStack.Count > 0;

    /// <inheritdoc />
    public bool CanGoForward => _forwardStack.Count > 0;

    /// <inheritdoc />
    public event EventHandler<NavigationEventArgs>? Navigated;

    /// <summary>
    /// Registers a page factory for creating views/view models by page name.
    /// </summary>
    /// <param name="pageName">The page name identifier.</param>
    /// <param name="factory">Factory function that creates the page (receives optional parameter).</param>
    public void RegisterPage(string pageName, Func<object?, object> factory)
    {
        _pageFactories[pageName] = factory;
    }

    /// <summary>
    /// Registers a simple page factory without parameter support.
    /// </summary>
    /// <param name="pageName">The page name identifier.</param>
    /// <param name="factory">Factory function that creates the page.</param>
    public void RegisterPage(string pageName, Func<object> factory)
    {
        _pageFactories[pageName] = _ => factory();
    }

    /// <summary>
    /// Sets the callback to invoke when navigation occurs (to update the current view).
    /// </summary>
    /// <param name="callback">Callback that receives the new page view/viewmodel.</param>
    public void SetNavigationCallback(Action<object> callback)
    {
        _navigationCallback = callback;
    }

    /// <inheritdoc />
    public void NavigateTo(string pageName)
    {
        NavigateTo(pageName, null);
    }

    /// <inheritdoc />
    public void NavigateTo(string pageName, object? parameter)
    {
        if (string.IsNullOrEmpty(pageName))
            return;

        var previousPageName = _currentEntry?.PageName;

        // Push current entry to back stack
        if (_currentEntry != null)
        {
            _backStack.Push(_currentEntry);
        }

        // Clear forward stack on new navigation
        _forwardStack.Clear();

        // Create new entry
        _currentEntry = new NavigationEntry(pageName, parameter);

        // Navigate and notify
        PerformNavigation(previousPageName);
    }

    /// <inheritdoc />
    public bool GoBack()
    {
        if (!CanGoBack)
            return false;

        var previousPageName = _currentEntry?.PageName;

        // Push current to forward stack
        if (_currentEntry != null)
        {
            _forwardStack.Push(_currentEntry);
        }

        // Pop from back stack
        _currentEntry = _backStack.Pop();

        // Navigate and notify
        PerformNavigation(previousPageName);
        return true;
    }

    /// <inheritdoc />
    public bool GoForward()
    {
        if (!CanGoForward)
            return false;

        var previousPageName = _currentEntry?.PageName;

        // Push current to back stack
        if (_currentEntry != null)
        {
            _backStack.Push(_currentEntry);
        }

        // Pop from forward stack
        _currentEntry = _forwardStack.Pop();

        // Navigate and notify
        PerformNavigation(previousPageName);
        return true;
    }

    /// <inheritdoc />
    public void ClearHistory()
    {
        _backStack.Clear();
        _forwardStack.Clear();
    }

    /// <summary>
    /// Performs the actual navigation by invoking the factory and callback.
    /// </summary>
    private void PerformNavigation(string? previousPageName)
    {
        if (_currentEntry == null)
            return;

        // Create the page using the registered factory
        if (_pageFactories.TryGetValue(_currentEntry.PageName, out var factory))
        {
            var page = factory(_currentEntry.Parameter);
            _navigationCallback?.Invoke(page);
        }

        // Raise navigation event
        Navigated?.Invoke(this, new NavigationEventArgs(
            _currentEntry.PageName,
            previousPageName,
            _currentEntry.Parameter));
    }

    /// <summary>
    /// Gets the parameter for the current page (if any).
    /// </summary>
    public object? GetCurrentParameter() => _currentEntry?.Parameter;

    /// <summary>
    /// Gets the navigation history (page names only).
    /// </summary>
    public IReadOnlyList<string> GetHistory()
    {
        var history = new List<string>();
        foreach (var entry in _backStack.Reverse())
        {
            history.Add(entry.PageName);
        }
        if (_currentEntry != null)
        {
            history.Add(_currentEntry.PageName);
        }
        return history;
    }
}

/// <summary>
/// Represents a navigation history entry.
/// </summary>
internal class NavigationEntry
{
    public string PageName { get; }
    public object? Parameter { get; }

    public NavigationEntry(string pageName, object? parameter = null)
    {
        PageName = pageName;
        Parameter = parameter;
    }
}
