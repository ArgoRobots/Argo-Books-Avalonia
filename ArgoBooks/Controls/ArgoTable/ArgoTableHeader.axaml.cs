using System.Windows.Input;
using ArgoBooks.Controls.ColumnWidths;
using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls.ArgoTable;

/// <summary>
/// A reusable table column header with sort indicators and resize gripper.
/// </summary>
public partial class ArgoTableHeader : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<string> HeaderProperty =
        AvaloniaProperty.Register<ArgoTableHeader, string>(nameof(Header), string.Empty);

    public static readonly StyledProperty<string> ColumnNameProperty =
        AvaloniaProperty.Register<ArgoTableHeader, string>(nameof(ColumnName), string.Empty);

    public static readonly StyledProperty<string> SortPropertyProperty =
        AvaloniaProperty.Register<ArgoTableHeader, string>(nameof(SortProperty), string.Empty);

    public static readonly StyledProperty<double> ColumnWidthProperty =
        AvaloniaProperty.Register<ArgoTableHeader, double>(nameof(ColumnWidth), 120);

    public static readonly StyledProperty<bool> IsColumnVisibleProperty =
        AvaloniaProperty.Register<ArgoTableHeader, bool>(nameof(IsColumnVisible), true);

    public static readonly StyledProperty<bool> IsSortableProperty =
        AvaloniaProperty.Register<ArgoTableHeader, bool>(nameof(IsSortable), true);

    public static readonly StyledProperty<bool> IsResizableProperty =
        AvaloniaProperty.Register<ArgoTableHeader, bool>(nameof(IsResizable), true);

    public static readonly StyledProperty<bool> ShowSeparatorProperty =
        AvaloniaProperty.Register<ArgoTableHeader, bool>(nameof(ShowSeparator), true);

    public static readonly StyledProperty<string?> SortColumnProperty =
        AvaloniaProperty.Register<ArgoTableHeader, string?>(nameof(SortColumn));

    public static readonly StyledProperty<SortDirection> SortDirectionProperty =
        AvaloniaProperty.Register<ArgoTableHeader, SortDirection>(nameof(SortDirection));

    public static readonly StyledProperty<ICommand?> SortCommandProperty =
        AvaloniaProperty.Register<ArgoTableHeader, ICommand?>(nameof(SortCommand));

    public static readonly StyledProperty<ITableColumnWidths?> ColumnWidthsProperty =
        AvaloniaProperty.Register<ArgoTableHeader, ITableColumnWidths?>(nameof(ColumnWidths));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the header text to display.
    /// </summary>
    public string Header
    {
        get => GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }

    /// <summary>
    /// Gets or sets the column name for resize gripper.
    /// </summary>
    public string ColumnName
    {
        get => GetValue(ColumnNameProperty);
        set => SetValue(ColumnNameProperty, value);
    }

    /// <summary>
    /// Gets or sets the property name used for sorting (passed to SortCommand).
    /// </summary>
    public string SortProperty
    {
        get => GetValue(SortPropertyProperty);
        set => SetValue(SortPropertyProperty, value);
    }

    /// <summary>
    /// Gets or sets the column width.
    /// </summary>
    public double ColumnWidth
    {
        get => GetValue(ColumnWidthProperty);
        set => SetValue(ColumnWidthProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the column is visible.
    /// </summary>
    public bool IsColumnVisible
    {
        get => GetValue(IsColumnVisibleProperty);
        set => SetValue(IsColumnVisibleProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the column is sortable.
    /// </summary>
    public bool IsSortable
    {
        get => GetValue(IsSortableProperty);
        set => SetValue(IsSortableProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the column is resizable.
    /// </summary>
    public bool IsResizable
    {
        get => GetValue(IsResizableProperty);
        set => SetValue(IsResizableProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the right border separator.
    /// </summary>
    public bool ShowSeparator
    {
        get => GetValue(ShowSeparatorProperty);
        set => SetValue(ShowSeparatorProperty, value);
    }

    /// <summary>
    /// Gets or sets the current sort column (for indicator display).
    /// </summary>
    public string? SortColumn
    {
        get => GetValue(SortColumnProperty);
        set => SetValue(SortColumnProperty, value);
    }

    /// <summary>
    /// Gets or sets the current sort direction (for indicator display).
    /// </summary>
    public SortDirection SortDirection
    {
        get => GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when sorting.
    /// </summary>
    public ICommand? SortCommand
    {
        get => GetValue(SortCommandProperty);
        set => SetValue(SortCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the column widths manager for resize gripper.
    /// </summary>
    public ITableColumnWidths? ColumnWidths
    {
        get => GetValue(ColumnWidthsProperty);
        set => SetValue(ColumnWidthsProperty, value);
    }

    #endregion

    public ArgoTableHeader()
    {
        InitializeComponent();
    }
}
