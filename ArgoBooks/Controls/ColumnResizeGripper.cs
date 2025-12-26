using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;

namespace ArgoBooks.Controls;

/// <summary>
/// A thumb control for resizing table columns via drag.
/// </summary>
public class ColumnResizeGripper : Thumb
{
    private double _startX;
    private double _originalWidth;

    public static readonly StyledProperty<string> ColumnNameProperty =
        AvaloniaProperty.Register<ColumnResizeGripper, string>(nameof(ColumnName));

    public static readonly StyledProperty<TableColumnWidths?> ColumnWidthsProperty =
        AvaloniaProperty.Register<ColumnResizeGripper, TableColumnWidths?>(nameof(ColumnWidths));

    /// <summary>
    /// Gets or sets the name of the column this gripper controls.
    /// </summary>
    public string ColumnName
    {
        get => GetValue(ColumnNameProperty);
        set => SetValue(ColumnNameProperty, value);
    }

    /// <summary>
    /// Gets or sets the TableColumnWidths instance.
    /// </summary>
    public TableColumnWidths? ColumnWidths
    {
        get => GetValue(ColumnWidthsProperty);
        set => SetValue(ColumnWidthsProperty, value);
    }

    /// <summary>
    /// Event raised when a resize operation starts.
    /// </summary>
    public event EventHandler<string>? ResizeStarted;

    /// <summary>
    /// Event raised during resize with the delta value.
    /// </summary>
    public event EventHandler<(string Column, double Delta)>? Resizing;

    /// <summary>
    /// Event raised when a resize operation completes.
    /// </summary>
    public event EventHandler<string>? ResizeCompleted;

    public ColumnResizeGripper()
    {
        Width = 8;
        MinHeight = 20;
        Cursor = new Cursor(StandardCursorType.SizeWestEast);
        Background = Brushes.Transparent;
        Margin = new Thickness(-4, 0, -4, 0);

        DragStarted += OnDragStarted;
        DragDelta += OnDragDelta;
        DragCompleted += OnDragCompleted;
    }

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        Background = new SolidColorBrush(Color.FromArgb(80, 59, 130, 246)); // Semi-transparent primary blue
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        Background = Brushes.Transparent;
    }

    private void OnDragStarted(object? sender, VectorEventArgs e)
    {
        _startX = 0;
        _originalWidth = GetCurrentColumnWidth();
        Background = new SolidColorBrush(Color.FromArgb(120, 59, 130, 246)); // More opaque during drag
        ResizeStarted?.Invoke(this, ColumnName);
    }

    private void OnDragDelta(object? sender, VectorEventArgs e)
    {
        var delta = e.Vector.X;
        ColumnWidths?.ResizeColumn(ColumnName, delta);
        Resizing?.Invoke(this, (ColumnName, delta));
    }

    private void OnDragCompleted(object? sender, VectorEventArgs e)
    {
        Background = Brushes.Transparent;
        ResizeCompleted?.Invoke(this, ColumnName);
    }

    private double GetCurrentColumnWidth()
    {
        if (ColumnWidths == null) return 100;

        return ColumnName switch
        {
            "Id" => ColumnWidths.IdColumnWidth,
            "Accountant" => ColumnWidths.AccountantColumnWidth,
            "Product" => ColumnWidths.ProductColumnWidth,
            "Supplier" => ColumnWidths.SupplierColumnWidth,
            "Date" => ColumnWidths.DateColumnWidth,
            "Quantity" => ColumnWidths.QuantityColumnWidth,
            "UnitPrice" => ColumnWidths.UnitPriceColumnWidth,
            "Amount" => ColumnWidths.AmountColumnWidth,
            "Tax" => ColumnWidths.TaxColumnWidth,
            "Shipping" => ColumnWidths.ShippingColumnWidth,
            "Discount" => ColumnWidths.DiscountColumnWidth,
            "Total" => ColumnWidths.TotalColumnWidth,
            "Receipt" => ColumnWidths.ReceiptColumnWidth,
            "Status" => ColumnWidths.StatusColumnWidth,
            "Actions" => ColumnWidths.ActionsColumnWidth,
            _ => 100
        };
    }
}
