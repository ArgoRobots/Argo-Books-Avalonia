namespace ArgoBooks.Core.Enums;

/// <summary>
/// Modal size presets for consistent sizing across the application.
/// </summary>
public enum ModalSize
{
    /// <summary>
    /// Small modal (400px).
    /// </summary>
    Small,

    /// <summary>
    /// Medium modal (500px). Default.
    /// </summary>
    Medium,

    /// <summary>
    /// Large modal (700px).
    /// </summary>
    Large,

    /// <summary>
    /// Extra large modal (900px).
    /// </summary>
    ExtraLarge,

    /// <summary>
    /// Full screen modal with margin.
    /// </summary>
    Full,

    /// <summary>
    /// Custom size - uses ModalWidth property.
    /// </summary>
    Custom
}

/// <summary>
/// Modal result indicating how the modal was closed.
/// </summary>
public enum ModalResult
{
    /// <summary>
    /// Modal was closed without action.
    /// </summary>
    None,

    /// <summary>
    /// Modal was closed with OK/Primary action.
    /// </summary>
    Ok,

    /// <summary>
    /// Modal was closed with Cancel/Secondary action.
    /// </summary>
    Cancel,

    /// <summary>
    /// Modal was closed with Yes action.
    /// </summary>
    Yes,

    /// <summary>
    /// Modal was closed with No action.
    /// </summary>
    No
}

/// <summary>
/// Result of a confirmation dialog.
/// </summary>
public enum ConfirmationResult
{
    /// <summary>
    /// Dialog was closed without a result (e.g., backdrop click).
    /// </summary>
    None,

    /// <summary>
    /// Primary action was selected (e.g., "Save", "Yes", "OK", "Delete").
    /// </summary>
    Primary,

    /// <summary>
    /// Secondary action was selected (e.g., "Don't Save", "No").
    /// </summary>
    Secondary,

    /// <summary>
    /// Cancel action was selected.
    /// </summary>
    Cancel
}
