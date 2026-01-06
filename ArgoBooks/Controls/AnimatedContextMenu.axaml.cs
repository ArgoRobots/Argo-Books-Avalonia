using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable animated context menu with fade-in animation.
/// Provides overlay backdrop, positioning, and common styling.
/// </summary>
public partial class AnimatedContextMenu : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<AnimatedContextMenu, bool>(nameof(IsOpen));

    public static readonly StyledProperty<double> MenuXProperty =
        AvaloniaProperty.Register<AnimatedContextMenu, double>(nameof(MenuX));

    public static readonly StyledProperty<double> MenuYProperty =
        AvaloniaProperty.Register<AnimatedContextMenu, double>(nameof(MenuY));

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<AnimatedContextMenu, ICommand?>(nameof(CloseCommand));

    public static readonly StyledProperty<object?> MenuContentProperty =
        AvaloniaProperty.Register<AnimatedContextMenu, object?>(nameof(MenuContent));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the context menu is visible.
    /// </summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets the X position of the context menu.
    /// </summary>
    public double MenuX
    {
        get => GetValue(MenuXProperty);
        set => SetValue(MenuXProperty, value);
    }

    /// <summary>
    /// Gets or sets the Y position of the context menu.
    /// </summary>
    public double MenuY
    {
        get => GetValue(MenuYProperty);
        set => SetValue(MenuYProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to close the context menu.
    /// </summary>
    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the content to display in the context menu.
    /// </summary>
    public object? MenuContent
    {
        get => GetValue(MenuContentProperty);
        set => SetValue(MenuContentProperty, value);
    }

    #endregion

    public AnimatedContextMenu()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            var animationBorder = this.FindControl<Border>("AnimationBorder");
            if (animationBorder == null) return;

            if (IsOpen)
            {
                // Animate in
                Dispatcher.UIThread.Post(() =>
                {
                    animationBorder.Opacity = 1;
                    animationBorder.RenderTransform = new TranslateTransform(0, 0);
                }, DispatcherPriority.Render);
            }
            else
            {
                // Reset for next open
                Dispatcher.UIThread.Post(() =>
                {
                    animationBorder.Opacity = 0;
                    animationBorder.RenderTransform = new TranslateTransform(0, -8);
                }, DispatcherPriority.Background);
            }
        }
    }

    /// <summary>
    /// Handles pointer pressed on the overlay to close the context menu.
    /// </summary>
    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (CloseCommand?.CanExecute(null) == true)
        {
            CloseCommand.Execute(null);
        }
        else
        {
            // Fallback: directly set IsOpen to false
            IsOpen = false;
        }
        e.Handled = true;
    }
}
