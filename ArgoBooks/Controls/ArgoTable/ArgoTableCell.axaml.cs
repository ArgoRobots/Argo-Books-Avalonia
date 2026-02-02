using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls.ArgoTable;

/// <summary>
/// A reusable table cell with width binding and border separator.
/// Supports both text display and custom content.
/// </summary>
public partial class ArgoTableCell : UserControl, INotifyPropertyChanged
{
    #region INotifyPropertyChanged

    public new event PropertyChangedEventHandler? PropertyChanged;

    private void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion

    #region Styled Properties

    public static readonly StyledProperty<double> ColumnWidthProperty =
        AvaloniaProperty.Register<ArgoTableCell, double>(nameof(ColumnWidth), 120);

    public static readonly StyledProperty<bool> IsColumnVisibleProperty =
        AvaloniaProperty.Register<ArgoTableCell, bool>(nameof(IsColumnVisible), true);

    public static readonly StyledProperty<bool> ShowSeparatorProperty =
        AvaloniaProperty.Register<ArgoTableCell, bool>(nameof(ShowSeparator), true);

    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<ArgoTableCell, string?>(nameof(Text));

    public static readonly StyledProperty<string> NullValueProperty =
        AvaloniaProperty.Register<ArgoTableCell, string>(nameof(NullValue), "-");

    public static readonly StyledProperty<object?> CellContentProperty =
        AvaloniaProperty.Register<ArgoTableCell, object?>(nameof(CellContent));

    #endregion

    #region Properties

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
    /// Gets or sets whether to show the right border separator.
    /// </summary>
    public bool ShowSeparator
    {
        get => GetValue(ShowSeparatorProperty);
        set => SetValue(ShowSeparatorProperty, value);
    }

    /// <summary>
    /// Gets or sets the text to display.
    /// </summary>
    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    /// <summary>
    /// Gets or sets the value to display when Text is null or empty.
    /// </summary>
    public string NullValue
    {
        get => GetValue(NullValueProperty);
        set => SetValue(NullValueProperty, value);
    }

    /// <summary>
    /// Gets or sets custom content to display instead of text.
    /// </summary>
    public object? CellContent
    {
        get => GetValue(CellContentProperty);
        set => SetValue(CellContentProperty, value);
    }

    /// <summary>
    /// Gets the text to display, using NullValue when Text is empty.
    /// </summary>
    public string DisplayText => string.IsNullOrWhiteSpace(Text) ? NullValue : Text;

    /// <summary>
    /// Gets whether this cell has custom content.
    /// </summary>
    public bool HasCustomContent => CellContent != null;

    #endregion

    public ArgoTableCell()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == TextProperty || change.Property == NullValueProperty)
        {
            RaisePropertyChanged(nameof(DisplayText));
        }
        else if (change.Property == CellContentProperty)
        {
            RaisePropertyChanged(nameof(HasCustomContent));
        }
    }
}
