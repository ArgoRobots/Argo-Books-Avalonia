using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A context menu for report element right-click actions.
/// Uses AnimatedContextMenu for overlay, positioning, and animation.
/// </summary>
public partial class ReportsContextMenu : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ReportsContextMenu, bool>(nameof(IsOpen));

    public static readonly StyledProperty<double> MenuXProperty =
        AvaloniaProperty.Register<ReportsContextMenu, double>(nameof(MenuX));

    public static readonly StyledProperty<double> MenuYProperty =
        AvaloniaProperty.Register<ReportsContextMenu, double>(nameof(MenuY));

    public static readonly StyledProperty<ICommand?> BringToFrontCommandProperty =
        AvaloniaProperty.Register<ReportsContextMenu, ICommand?>(nameof(BringToFrontCommand));

    public static readonly StyledProperty<ICommand?> SendToBackCommandProperty =
        AvaloniaProperty.Register<ReportsContextMenu, ICommand?>(nameof(SendToBackCommand));

    public static readonly StyledProperty<ICommand?> DuplicateCommandProperty =
        AvaloniaProperty.Register<ReportsContextMenu, ICommand?>(nameof(DuplicateCommand));

    public static readonly StyledProperty<ICommand?> DeleteCommandProperty =
        AvaloniaProperty.Register<ReportsContextMenu, ICommand?>(nameof(DeleteCommand));

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<ReportsContextMenu, ICommand?>(nameof(CloseCommand));

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
    /// Gets or sets the command to bring element to front.
    /// </summary>
    public ICommand? BringToFrontCommand
    {
        get => GetValue(BringToFrontCommandProperty);
        set => SetValue(BringToFrontCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to send element to back.
    /// </summary>
    public ICommand? SendToBackCommand
    {
        get => GetValue(SendToBackCommandProperty);
        set => SetValue(SendToBackCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to duplicate the element.
    /// </summary>
    public ICommand? DuplicateCommand
    {
        get => GetValue(DuplicateCommandProperty);
        set => SetValue(DuplicateCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to delete the element.
    /// </summary>
    public ICommand? DeleteCommand
    {
        get => GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to close the context menu.
    /// </summary>
    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    #endregion

    public ReportsContextMenu()
    {
        InitializeComponent();
    }
}
