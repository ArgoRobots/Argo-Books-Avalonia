using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable column header control with sorting support.
/// Displays column text with ascending/descending sort indicators.
/// </summary>
public partial class SortableColumnHeader : UserControl
{
    #region Styled Properties

    /// <summary>
    /// The display text for the column header.
    /// </summary>
    public static readonly StyledProperty<string> TextProperty =
        AvaloniaProperty.Register<SortableColumnHeader, string>(nameof(Text), string.Empty);

    /// <summary>
    /// The column name used for sorting (passed to SortByCommand).
    /// </summary>
    public static readonly StyledProperty<string> ColumnNameProperty =
        AvaloniaProperty.Register<SortableColumnHeader, string>(nameof(ColumnName), string.Empty);

    /// <summary>
    /// The command to execute when the header is clicked.
    /// </summary>
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<SortableColumnHeader, ICommand?>(nameof(Command));

    /// <summary>
    /// The current sort column from the ViewModel.
    /// </summary>
    public static readonly StyledProperty<string?> SortColumnProperty =
        AvaloniaProperty.Register<SortableColumnHeader, string?>(nameof(SortColumn));

    /// <summary>
    /// The current sort direction from the ViewModel.
    /// </summary>
    public static readonly StyledProperty<SortDirection> SortDirectionProperty =
        AvaloniaProperty.Register<SortableColumnHeader, SortDirection>(nameof(SortDirection), SortDirection.None);

    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets whether this column is sorted ascending.
    /// </summary>
    public static readonly DirectProperty<SortableColumnHeader, bool> IsAscendingProperty =
        AvaloniaProperty.RegisterDirect<SortableColumnHeader, bool>(nameof(IsAscending), o => o.IsAscending);

    /// <summary>
    /// Gets whether this column is sorted descending.
    /// </summary>
    public static readonly DirectProperty<SortableColumnHeader, bool> IsDescendingProperty =
        AvaloniaProperty.RegisterDirect<SortableColumnHeader, bool>(nameof(IsDescending), o => o.IsDescending);

    /// <summary>
    /// Gets whether the ascending sort indicator should be visible.
    /// </summary>
    public bool IsAscending => SortColumn == ColumnName && SortDirection == SortDirection.Ascending;

    /// <summary>
    /// Gets whether the descending sort indicator should be visible.
    /// </summary>
    public bool IsDescending => SortColumn == ColumnName && SortDirection == SortDirection.Descending;

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the display text for the column header.
    /// </summary>
    public string Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the column name used for sorting.
    /// </summary>
    public string ColumnName
    {
        get => GetValue(ColumnNameProperty);
        set => SetValue(ColumnNameProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to execute when clicked.
    /// </summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the current sort column.
    /// </summary>
    public string? SortColumn
    {
        get => GetValue(SortColumnProperty);
        set => SetValue(SortColumnProperty, value);
    }

    /// <summary>
    /// Gets or sets the current sort direction.
    /// </summary>
    public SortDirection SortDirection
    {
        get => GetValue(SortDirectionProperty);
        set => SetValue(SortDirectionProperty, value);
    }

    #endregion

    public SortableColumnHeader()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Update computed properties when sort state changes
        if (change.Property == SortColumnProperty ||
            change.Property == SortDirectionProperty ||
            change.Property == ColumnNameProperty)
        {
            RaisePropertyChanged(IsAscendingProperty, !IsAscending, IsAscending);
            RaisePropertyChanged(IsDescendingProperty, !IsDescending, IsDescending);
        }
    }
}
