using ArgoBooks.ViewModels;

namespace ArgoBooks.Services;

/// <summary>
/// Service that aggregates changes from multiple sources and provides
/// a unified view of all unsaved changes in the application.
/// </summary>
public class ChangeTrackingService
{
    /// <summary>
    /// Event raised when the overall change state changes.
    /// </summary>
    public event EventHandler? ChangeStateChanged;

    /// <summary>
    /// Clears all tracked changes.
    /// </summary>
    public void ClearAllChanges()
    {
        ChangeStateChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Gets all change categories with their changes.
    /// </summary>
    public IEnumerable<ChangeCategory> GetAllChangeCategories()
    {
        return [];
    }
}
