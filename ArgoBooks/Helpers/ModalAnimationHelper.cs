using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace ArgoBooks.Helpers;

/// <summary>
/// Helper class for animating modal dialogs.
/// Provides consistent fade-in/scale animations for all modals.
/// </summary>
public static class ModalAnimationHelper
{
    /// <summary>
    /// Animates a modal into view with a fade + scale effect.
    /// Call this when the modal opens (IsOpen becomes true).
    /// </summary>
    /// <param name="modalBorder">The Border element containing the modal content (should have modal-content class)</param>
    public static void AnimateIn(Border? modalBorder)
    {
        if (modalBorder == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            modalBorder.Opacity = 1;
            modalBorder.RenderTransform = new ScaleTransform(1, 1);
            modalBorder.Focus();
        }, DispatcherPriority.Render);
    }

    /// <summary>
    /// Resets a modal to its initial hidden state.
    /// Call this when the modal closes (IsOpen becomes false).
    /// </summary>
    /// <param name="modalBorder">The Border element containing the modal content</param>
    public static void AnimateOut(Border? modalBorder)
    {
        if (modalBorder == null) return;

        Dispatcher.UIThread.Post(() =>
        {
            modalBorder.Opacity = 0;
            modalBorder.RenderTransform = new ScaleTransform(0.95, 0.95);
        }, DispatcherPriority.Background);
    }
}
