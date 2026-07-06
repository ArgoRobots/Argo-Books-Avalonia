namespace ArgoBooks.ViewModels;

/// <summary>
/// Implemented by cached page ViewModels that subscribe to long-lived singleton service events and
/// must tear those subscriptions down when the company is switched. <c>ClearPageCaches</c> calls
/// <see cref="Cleanup"/> on every cached VM that implements this, so a VM is covered regardless of
/// which base class it derives from (some pages extend SortablePageViewModelBase, others extend
/// ChartContextMenuViewModelBase or ViewModelBase directly).
/// </summary>
public interface ICleanupViewModel
{
    /// <summary>
    /// Unsubscribes from any long-lived events this ViewModel wired up, so it can be garbage
    /// collected and stops reacting to events after its page is dropped.
    /// </summary>
    void Cleanup();
}
