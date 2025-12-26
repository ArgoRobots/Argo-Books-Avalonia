using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ArgoBooks.Controls;

/// <summary>
/// A data grid control with resizable columns that auto-fit to available space.
/// </summary>
public partial class ResizableDataGrid : UserControl, INotifyPropertyChanged
{
    public new event PropertyChangedEventHandler? PropertyChanged;

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private Canvas? _headerCanvas;
    private ItemsControl? _rowsContainer;
    private readonly List<Border> _resizeHandles = new();
    private readonly List<TextBlock> _headerTexts = new();
    private Border? _activeResizeHandle;
    private int _resizingColumnIndex = -1;
    private double _resizeStartX;
    private double _resizeStartWidth;
    private bool _isInitialized;

    #region Styled Properties

    public static readonly StyledProperty<IEnumerable?> ItemsSourceProperty =
        AvaloniaProperty.Register<ResizableDataGrid, IEnumerable?>(nameof(ItemsSource));

    public static readonly StyledProperty<ObservableCollection<ResizableColumn>> ColumnsProperty =
        AvaloniaProperty.Register<ResizableDataGrid, ObservableCollection<ResizableColumn>>(
            nameof(Columns), new ObservableCollection<ResizableColumn>());

    public static readonly StyledProperty<DataTemplate?> CellTemplateProperty =
        AvaloniaProperty.Register<ResizableDataGrid, DataTemplate?>(nameof(CellTemplate));

    public static readonly StyledProperty<DataTemplate?> RowTemplateProperty =
        AvaloniaProperty.Register<ResizableDataGrid, DataTemplate?>(nameof(RowTemplate));

    public static readonly StyledProperty<string> EmptyMessageProperty =
        AvaloniaProperty.Register<ResizableDataGrid, string>(nameof(EmptyMessage), "No data to display");

    public static readonly StyledProperty<Thickness> HeaderPaddingProperty =
        AvaloniaProperty.Register<ResizableDataGrid, Thickness>(nameof(HeaderPadding), new Thickness(24, 12));

    public static readonly StyledProperty<Thickness> RowPaddingProperty =
        AvaloniaProperty.Register<ResizableDataGrid, Thickness>(nameof(RowPadding), new Thickness(24, 16));

    public static readonly StyledProperty<Func<object, int, Control>?> CellFactoryProperty =
        AvaloniaProperty.Register<ResizableDataGrid, Func<object, int, Control>?>(nameof(CellFactory));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the items source for the data grid.
    /// </summary>
    public IEnumerable? ItemsSource
    {
        get => GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    /// <summary>
    /// Gets or sets the column definitions.
    /// </summary>
    public ObservableCollection<ResizableColumn> Columns
    {
        get => GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    /// <summary>
    /// Gets or sets the cell template.
    /// </summary>
    public DataTemplate? CellTemplate
    {
        get => GetValue(CellTemplateProperty);
        set => SetValue(CellTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the row template.
    /// </summary>
    public DataTemplate? RowTemplate
    {
        get => GetValue(RowTemplateProperty);
        set => SetValue(RowTemplateProperty, value);
    }

    /// <summary>
    /// Gets or sets the empty state message.
    /// </summary>
    public string EmptyMessage
    {
        get => GetValue(EmptyMessageProperty);
        set => SetValue(EmptyMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding for the header row.
    /// </summary>
    public Thickness HeaderPadding
    {
        get => GetValue(HeaderPaddingProperty);
        set => SetValue(HeaderPaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets the padding for data rows.
    /// </summary>
    public Thickness RowPadding
    {
        get => GetValue(RowPaddingProperty);
        set => SetValue(RowPaddingProperty, value);
    }

    /// <summary>
    /// Gets or sets a factory function to create cell controls.
    /// The function receives the data item and column index, and returns a Control.
    /// </summary>
    public Func<object, int, Control>? CellFactory
    {
        get => GetValue(CellFactoryProperty);
        set => SetValue(CellFactoryProperty, value);
    }

    /// <summary>
    /// Gets whether there are items to display.
    /// </summary>
    public bool HasItems => ItemsSource?.Cast<object>().Any() == true;

    #endregion

    public ResizableDataGrid()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        _headerCanvas = this.FindControl<Canvas>("HeaderCanvas");
        _rowsContainer = this.FindControl<ItemsControl>("RowsContainer");

        if (_headerCanvas != null)
        {
            _isInitialized = true;
            RebuildHeaders();
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ItemsSourceProperty)
        {
            OnItemsSourceChanged();
            RaisePropertyChanged(nameof(HasItems));
        }
        else if (change.Property == ColumnsProperty)
        {
            OnColumnsChanged();
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);

        if (_isInitialized && e.WidthChanged)
        {
            AutoSizeColumns();
        }
    }

    private void OnItemsSourceChanged()
    {
        if (ItemsSource is INotifyCollectionChanged observable)
        {
            observable.CollectionChanged += OnItemsCollectionChanged;
        }

        UpdateAllRowCells();
    }

    private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RaisePropertyChanged(nameof(HasItems));
        UpdateAllRowCells();
    }

    private void OnColumnsChanged()
    {
        foreach (var column in Columns)
        {
            column.PropertyChanged += OnColumnPropertyChanged;
        }

        Columns.CollectionChanged += OnColumnsCollectionChanged;

        if (_isInitialized)
        {
            RebuildHeaders();
            AutoSizeColumns();
        }
    }

    private void OnColumnsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (ResizableColumn column in e.NewItems)
            {
                column.PropertyChanged += OnColumnPropertyChanged;
            }
        }

        if (_isInitialized)
        {
            RebuildHeaders();
            AutoSizeColumns();
        }
    }

    private void OnColumnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ResizableColumn.Width))
        {
            UpdateColumnPositions();
            UpdateAllRowCells();
        }
        else if (e.PropertyName == nameof(ResizableColumn.IsVisible))
        {
            RebuildHeaders();
            AutoSizeColumns();
        }
    }

    /// <summary>
    /// Rebuilds the header controls.
    /// </summary>
    private void RebuildHeaders()
    {
        if (_headerCanvas == null) return;

        _headerCanvas.Children.Clear();
        _resizeHandles.Clear();
        _headerTexts.Clear();

        double x = 0;
        var visibleColumns = Columns.Where(c => c.IsVisible).ToList();

        for (int i = 0; i < visibleColumns.Count; i++)
        {
            var column = visibleColumns[i];
            var colIndex = Columns.IndexOf(column);

            // Create header text
            var headerText = new TextBlock
            {
                Text = column.Header,
                Classes = { "column-header" },
                HorizontalAlignment = column.Alignment,
                Width = column.Width,
                ClipToBounds = true
            };

            Canvas.SetLeft(headerText, x);
            Canvas.SetTop(headerText, 0);
            _headerCanvas.Children.Add(headerText);
            _headerTexts.Add(headerText);

            // Create resize handle (except for last column)
            if (i < visibleColumns.Count - 1 && column.CanResize)
            {
                var handle = new Border
                {
                    Classes = { "resize-handle" },
                    Height = 24,
                    Tag = colIndex // Store actual column index
                };

                Canvas.SetLeft(handle, x + column.Width - 4);
                Canvas.SetTop(handle, 0);

                handle.PointerPressed += OnResizeHandlePointerPressed;
                handle.PointerMoved += OnResizeHandlePointerMoved;
                handle.PointerReleased += OnResizeHandlePointerReleased;

                _headerCanvas.Children.Add(handle);
                _resizeHandles.Add(handle);
            }

            x += column.Width;
        }
    }

    /// <summary>
    /// Updates column positions after a resize.
    /// </summary>
    private void UpdateColumnPositions()
    {
        if (_headerCanvas == null) return;

        double x = 0;
        var visibleColumns = Columns.Where(c => c.IsVisible).ToList();
        int headerIndex = 0;
        int handleIndex = 0;

        for (int i = 0; i < visibleColumns.Count; i++)
        {
            var column = visibleColumns[i];

            // Update header text position and width
            if (headerIndex < _headerTexts.Count)
            {
                var headerText = _headerTexts[headerIndex];
                Canvas.SetLeft(headerText, x);
                headerText.Width = column.Width;
                headerIndex++;
            }

            // Update resize handle position
            if (i < visibleColumns.Count - 1 && column.CanResize && handleIndex < _resizeHandles.Count)
            {
                var handle = _resizeHandles[handleIndex];
                Canvas.SetLeft(handle, x + column.Width - 4);
                handleIndex++;
            }

            x += column.Width;
        }
    }

    /// <summary>
    /// Automatically sizes columns to fill available width.
    /// </summary>
    public void AutoSizeColumns()
    {
        var visibleColumns = Columns.Where(c => c.IsVisible).ToList();
        if (visibleColumns.Count == 0) return;

        // Calculate available width (minus padding)
        var padding = HeaderPadding;
        double availableWidth = Bounds.Width - padding.Left - padding.Right;

        if (availableWidth <= 0) return;

        // Calculate total star value for proportional columns
        double totalStarValue = 0;
        double fixedWidth = 0;

        foreach (var column in visibleColumns)
        {
            if (column.IsFixed)
            {
                fixedWidth += column.Width;
            }
            else
            {
                totalStarValue += column.StarWidth;
            }
        }

        // Calculate width per star unit
        double remainingWidth = availableWidth - fixedWidth;
        double widthPerStar = totalStarValue > 0 ? remainingWidth / totalStarValue : 0;

        // Apply widths
        foreach (var column in visibleColumns)
        {
            if (!column.IsFixed)
            {
                double targetWidth = column.StarWidth * widthPerStar;
                // Clamp to min/max constraints
                targetWidth = Math.Max(column.MinWidth, Math.Min(column.MaxWidth, targetWidth));
                column.Width = targetWidth;
            }
        }

        UpdateColumnPositions();
        UpdateAllRowCells();
    }

    #region Resize Handling

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border handle) return;

        _activeResizeHandle = handle;
        _resizingColumnIndex = (int)handle.Tag!;
        _resizeStartX = e.GetPosition(this).X;
        _resizeStartWidth = Columns[_resizingColumnIndex].Width;

        handle.Classes.Add("resizing");
        e.Pointer.Capture(handle);
        e.Handled = true;
    }

    private void OnResizeHandlePointerMoved(object? sender, PointerEventArgs e)
    {
        if (_activeResizeHandle == null || _resizingColumnIndex < 0) return;

        double currentX = e.GetPosition(this).X;
        double delta = currentX - _resizeStartX;
        double newWidth = Math.Max(Columns[_resizingColumnIndex].MinWidth, _resizeStartWidth + delta);

        // Apply max width constraint
        if (Columns[_resizingColumnIndex].MaxWidth < double.PositiveInfinity)
        {
            newWidth = Math.Min(Columns[_resizingColumnIndex].MaxWidth, newWidth);
        }

        Columns[_resizingColumnIndex].Width = newWidth;
        e.Handled = true;
    }

    private void OnResizeHandlePointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_activeResizeHandle == null) return;

        _activeResizeHandle.Classes.Remove("resizing");
        e.Pointer.Capture(null);

        _activeResizeHandle = null;
        _resizingColumnIndex = -1;
        e.Handled = true;
    }

    #endregion

    #region Row Cell Management

    /// <summary>
    /// Updates cells in all data rows.
    /// </summary>
    private void UpdateAllRowCells()
    {
        if (_rowsContainer?.ItemContainerGenerator == null) return;

        // Wait for layout to complete
        Dispatcher.UIThread.Post(() =>
        {
            if (_rowsContainer?.ItemsSource == null) return;

            foreach (var item in _rowsContainer.ItemsSource)
            {
                var container = _rowsContainer.ContainerFromItem(item) as ContentPresenter;
                if (container == null) continue;

                // Find the Canvas in the row template
                var canvas = FindDescendant<Canvas>(container);
                if (canvas == null) continue;

                var dataItem = canvas.Tag ?? item;
                UpdateRowCells(canvas, dataItem);
            }
        }, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Updates cells in a single row.
    /// </summary>
    private void UpdateRowCells(Canvas canvas, object dataItem)
    {
        canvas.Children.Clear();

        double x = 0;
        var visibleColumns = Columns.Where(c => c.IsVisible).ToList();

        for (int i = 0; i < visibleColumns.Count; i++)
        {
            var column = visibleColumns[i];
            var colIndex = Columns.IndexOf(column);

            Control cellControl;

            // Use factory if provided, otherwise create default TextBlock
            if (CellFactory != null)
            {
                cellControl = CellFactory(dataItem, colIndex);
            }
            else
            {
                // Create default cell
                var value = column.GetValue(dataItem);
                cellControl = new TextBlock
                {
                    Text = value?.ToString() ?? string.Empty,
                    Classes = { "cell" },
                    HorizontalAlignment = column.Alignment,
                    Width = column.Width,
                    ClipToBounds = true
                };
            }

            // Ensure width is set for custom cells too
            if (cellControl is Control ctrl)
            {
                ctrl.Width = column.Width;
            }

            Canvas.SetLeft(cellControl, x);
            Canvas.SetTop(cellControl, 0);
            canvas.Children.Add(cellControl);

            x += column.Width;
        }
    }

    /// <summary>
    /// Finds a descendant control of a specific type.
    /// </summary>
    private static T? FindDescendant<T>(Visual root) where T : Visual
    {
        if (root is T match)
            return match;

        foreach (var child in root.GetVisualChildren())
        {
            if (child is Visual visual)
            {
                var result = FindDescendant<T>(visual);
                if (result != null)
                    return result;
            }
        }

        return null;
    }

    #endregion
}
