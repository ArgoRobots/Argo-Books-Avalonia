using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;

namespace ArgoBooks.Helpers;

/// <summary>
/// Attached behavior for banner fade-in/fade-out animations.
/// Similar to ModalAnimationBehavior but supports nested property paths.
/// </summary>
public static class BannerAnimationBehavior
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<Border, bool>("IsEnabled", typeof(BannerAnimationBehavior));

    public static readonly AttachedProperty<bool> IsVisibleProperty =
        AvaloniaProperty.RegisterAttached<Border, bool>("IsVisible", typeof(BannerAnimationBehavior));

    public static bool GetIsEnabled(Border element) => element.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(Border element, bool value) => element.SetValue(IsEnabledProperty, value);

    public static bool GetIsVisible(Border element) => element.GetValue(IsVisibleProperty);
    public static void SetIsVisible(Border element, bool value) => element.SetValue(IsVisibleProperty, value);

    static BannerAnimationBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<Border>(OnIsEnabledChanged);
        IsVisibleProperty.Changed.AddClassHandler<Border>(OnIsVisibleChanged);
    }

    private static void OnIsEnabledChanged(Border border, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            // Set initial state
            border.Opacity = 0;
            border.IsHitTestVisible = false;
        }
    }

    private static void OnIsVisibleChanged(Border border, AvaloniaPropertyChangedEventArgs e)
    {
        if (!GetIsEnabled(border))
            return;

        if (e.NewValue is true)
        {
            AnimateIn(border);
        }
        else
        {
            AnimateOut(border);
        }
    }

    private static void AnimateIn(Border border)
    {
        // Use Dispatcher.Post to ensure the animation happens after render
        Dispatcher.UIThread.Post(() =>
        {
            border.Opacity = 1;
            border.IsHitTestVisible = true;
        }, DispatcherPriority.Render);
    }

    private static void AnimateOut(Border border)
    {
        Dispatcher.UIThread.Post(() =>
        {
            border.Opacity = 0;
            border.IsHitTestVisible = false;
        }, DispatcherPriority.Background);
    }
}
