using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ArgoBooks.Utilities;

/// <summary>
/// Helper class for common modal operations.
/// </summary>
public static class ModalHelper
{
    /// <summary>
    /// Returns focus to the AppShell so keyboard shortcuts (like Ctrl+K) work again.
    /// Call this when a modal closes.
    /// </summary>
    /// <param name="control">Any control in the visual tree (typically the modal itself).</param>
    public static void ReturnFocusToAppShell(Control control)
    {
        Dispatcher.UIThread.Post(() =>
        {
            var topLevel = TopLevel.GetTopLevel(control);
            if (topLevel != null)
            {
                var appShell = topLevel.GetVisualDescendants()
                    .OfType<UserControl>()
                    .FirstOrDefault(x => x.GetType().Name == "AppShell");
                appShell?.Focus();
            }
        }, DispatcherPriority.Background);
    }
}
