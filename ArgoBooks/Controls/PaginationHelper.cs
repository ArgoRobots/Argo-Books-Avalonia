using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// Attached properties for pagination button styling.
/// </summary>
public static class PaginationHelper
{
    /// <summary>
    /// Attached property indicating whether a pagination button is for the currently active page.
    /// When set to true, adds the ":active" pseudo-class to the control.
    /// </summary>
    public static readonly AttachedProperty<bool> IsActivePageProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, bool>(
            "IsActivePage",
            defaultValue: false,
            inherits: false);

    static PaginationHelper()
    {
        IsActivePageProperty.Changed.AddClassHandler<Control>(OnIsActivePageChanged);
    }

    public static bool GetIsActivePage(Control control) => control.GetValue(IsActivePageProperty);
    public static void SetIsActivePage(Control control, bool value) => control.SetValue(IsActivePageProperty, value);

    private static void OnIsActivePageChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is bool isActive)
        {
            ((IPseudoClasses)control.Classes).Set(":active", isActive);
        }
    }
}
