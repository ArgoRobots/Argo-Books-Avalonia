using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable context menu for toggling column visibility in tables.
/// Uses AnimatedContextMenu for overlay, positioning, and animation.
/// </summary>
public partial class ColumnVisibilityMenu : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ColumnVisibilityMenu, bool>(nameof(IsOpen), defaultBindingMode: Avalonia.Data.BindingMode.TwoWay);

    public static readonly StyledProperty<double> MenuXProperty =
        AvaloniaProperty.Register<ColumnVisibilityMenu, double>(nameof(MenuX));

    public static readonly StyledProperty<double> MenuYProperty =
        AvaloniaProperty.Register<ColumnVisibilityMenu, double>(nameof(MenuY));

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<ColumnVisibilityMenu, ICommand?>(nameof(CloseCommand));

    public static readonly StyledProperty<object?> ColumnsContentProperty =
        AvaloniaProperty.Register<ColumnVisibilityMenu, object?>(nameof(ColumnsContent));

    public static readonly StyledProperty<ICommand?> ResetCommandProperty =
        AvaloniaProperty.Register<ColumnVisibilityMenu, ICommand?>(nameof(ResetCommand));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the column visibility menu is visible.
    /// </summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets the X position of the menu.
    /// </summary>
    public double MenuX
    {
        get => GetValue(MenuXProperty);
        set => SetValue(MenuXProperty, value);
    }

    /// <summary>
    /// Gets or sets the Y position of the menu.
    /// </summary>
    public double MenuY
    {
        get => GetValue(MenuYProperty);
        set => SetValue(MenuYProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to close the menu.
    /// </summary>
    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the column checkbox content for this menu.
    /// </summary>
    public object? ColumnsContent
    {
        get => GetValue(ColumnsContentProperty);
        set => SetValue(ColumnsContentProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to reset column visibility to defaults.
    /// </summary>
    public ICommand? ResetCommand
    {
        get => GetValue(ResetCommandProperty);
        set => SetValue(ResetCommandProperty, value);
    }

    #endregion

    public ColumnVisibilityMenu()
    {
        InitializeComponent();

        // Set default close command if not provided
        CloseCommand = new RelayCommand(() => IsOpen = false);
    }

    /// <summary>
    /// Shows the column visibility menu at the specified pointer position.
    /// Call this from the table header's PointerPressed event.
    /// </summary>
    public void ShowAt(PointerPressedEventArgs e, Control relativeTo)
    {
        var position = e.GetPosition(relativeTo);
        MenuX = position.X;
        MenuY = position.Y;
        IsOpen = true;
    }

    /// <summary>
    /// Shows the column visibility menu at the specified position.
    /// </summary>
    public void ShowAt(double x, double y)
    {
        MenuX = x;
        MenuY = y;
        IsOpen = true;
    }

}
