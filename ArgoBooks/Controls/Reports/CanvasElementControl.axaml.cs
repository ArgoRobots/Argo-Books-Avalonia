using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;

namespace ArgoBooks.Controls.Reports;

/// <summary>
/// Interactive control for displaying and manipulating report elements on a design canvas.
/// Supports selection, dragging, and resizing with visual handles.
/// </summary>
public partial class CanvasElementControl : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<ReportElementBase?> ElementProperty =
        AvaloniaProperty.Register<CanvasElementControl, ReportElementBase?>(nameof(Element));

    public static readonly StyledProperty<bool> IsSelectedProperty =
        AvaloniaProperty.Register<CanvasElementControl, bool>(nameof(IsSelected));

    public static readonly StyledProperty<bool> IsLockedProperty =
        AvaloniaProperty.Register<CanvasElementControl, bool>(nameof(IsLocked));

    public static readonly StyledProperty<bool> ShowTypeIndicatorProperty =
        AvaloniaProperty.Register<CanvasElementControl, bool>(nameof(ShowTypeIndicator), true);

    public static readonly StyledProperty<object?> ElementContentProperty =
        AvaloniaProperty.Register<CanvasElementControl, object?>(nameof(ElementContent));

    public static readonly StyledProperty<IBrush?> ElementBackgroundProperty =
        AvaloniaProperty.Register<CanvasElementControl, IBrush?>(nameof(ElementBackground));

    public static readonly StyledProperty<string> ElementTypeNameProperty =
        AvaloniaProperty.Register<CanvasElementControl, string>(nameof(ElementTypeName), "Element");

    public static readonly StyledProperty<double> MinElementWidthProperty =
        AvaloniaProperty.Register<CanvasElementControl, double>(nameof(MinElementWidth), 50);

    public static readonly StyledProperty<double> MinElementHeightProperty =
        AvaloniaProperty.Register<CanvasElementControl, double>(nameof(MinElementHeight), 30);

    public static readonly StyledProperty<double> SnapGridSizeProperty =
        AvaloniaProperty.Register<CanvasElementControl, double>(nameof(SnapGridSize), 0);

    #endregion

    #region Properties

    /// <summary>
    /// The report element this control represents.
    /// </summary>
    public ReportElementBase? Element
    {
        get => GetValue(ElementProperty);
        set => SetValue(ElementProperty, value);
    }

    /// <summary>
    /// Whether the element is currently selected.
    /// </summary>
    public bool IsSelected
    {
        get => GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Whether the element is locked and cannot be moved or resized.
    /// </summary>
    public bool IsLocked
    {
        get => GetValue(IsLockedProperty);
        set => SetValue(IsLockedProperty, value);
    }

    /// <summary>
    /// Whether to show the element type indicator badge.
    /// </summary>
    public bool ShowTypeIndicator
    {
        get => GetValue(ShowTypeIndicatorProperty);
        set => SetValue(ShowTypeIndicatorProperty, value);
    }

    /// <summary>
    /// The content to display inside the element.
    /// </summary>
    public object? ElementContent
    {
        get => GetValue(ElementContentProperty);
        set => SetValue(ElementContentProperty, value);
    }

    /// <summary>
    /// Background brush for the element.
    /// </summary>
    public IBrush? ElementBackground
    {
        get => GetValue(ElementBackgroundProperty);
        set => SetValue(ElementBackgroundProperty, value);
    }

    /// <summary>
    /// Display name for the element type.
    /// </summary>
    public string ElementTypeName
    {
        get => GetValue(ElementTypeNameProperty);
        set => SetValue(ElementTypeNameProperty, value);
    }

    /// <summary>
    /// Minimum width for resizing.
    /// </summary>
    public double MinElementWidth
    {
        get => GetValue(MinElementWidthProperty);
        set => SetValue(MinElementWidthProperty, value);
    }

    /// <summary>
    /// Minimum height for resizing.
    /// </summary>
    public double MinElementHeight
    {
        get => GetValue(MinElementHeightProperty);
        set => SetValue(MinElementHeightProperty, value);
    }

    /// <summary>
    /// Grid size for snapping (0 = no snapping).
    /// </summary>
    public double SnapGridSize
    {
        get => GetValue(SnapGridSizeProperty);
        set => SetValue(SnapGridSizeProperty, value);
    }

    #endregion

    #region Events

    /// <summary>
    /// Raised when the element is selected.
    /// </summary>
    public event EventHandler<ElementSelectedEventArgs>? ElementSelected;

    /// <summary>
    /// Raised when the element position changes (during or after drag).
    /// </summary>
    public event EventHandler<ElementPositionChangedEventArgs>? PositionChanged;

    /// <summary>
    /// Raised when the element size changes (during or after resize).
    /// </summary>
    public event EventHandler<ElementSizeChangedEventArgs>? SizeChanged;

    /// <summary>
    /// Raised when dragging starts.
    /// </summary>
    public event EventHandler<ElementDragEventArgs>? DragStarted;

    /// <summary>
    /// Raised when dragging ends.
    /// </summary>
    public event EventHandler<ElementDragEventArgs>? DragEnded;

    /// <summary>
    /// Raised when resizing starts.
    /// </summary>
    public event EventHandler<ElementResizeEventArgs>? ResizeStarted;

    /// <summary>
    /// Raised when resizing ends.
    /// </summary>
    public event EventHandler<ElementResizeEventArgs>? ResizeEnded;

    /// <summary>
    /// Raised when the element requests deletion (e.g., Delete key).
    /// </summary>
    public event EventHandler<ElementDeleteRequestedEventArgs>? DeleteRequested;

    #endregion

    #region Private Fields

    private bool _isDragging;
    private bool _isResizing;
    private Point _dragStartPoint;
    private Point _elementStartPosition;
    private Size _elementStartSize;
    private string? _resizeHandle;
    private bool _isHovering;

    private Border? _hoverBorder;
    private Border? _selectionBorder;
    private Canvas? _resizeHandlesCanvas;
    private Border? _typeIndicator;
    private Border? _lockIndicator;

    // Resize handle references
    private Border? _handleTopLeft;
    private Border? _handleTop;
    private Border? _handleTopRight;
    private Border? _handleLeft;
    private Border? _handleRight;
    private Border? _handleBottomLeft;
    private Border? _handleBottom;
    private Border? _handleBottomRight;

    private const double HandleSize = 10;
    private const double HandleOffset = HandleSize / 2;

    #endregion

    public CanvasElementControl()
    {
        InitializeComponent();
    }

    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);

        // Get references to template parts
        _hoverBorder = this.FindControl<Border>("HoverBorder");
        _selectionBorder = this.FindControl<Border>("SelectionBorder");
        _resizeHandlesCanvas = this.FindControl<Canvas>("ResizeHandlesCanvas");
        _typeIndicator = this.FindControl<Border>("TypeIndicator");
        _lockIndicator = this.FindControl<Border>("LockIndicator");

        _handleTopLeft = this.FindControl<Border>("HandleTopLeft");
        _handleTop = this.FindControl<Border>("HandleTop");
        _handleTopRight = this.FindControl<Border>("HandleTopRight");
        _handleLeft = this.FindControl<Border>("HandleLeft");
        _handleRight = this.FindControl<Border>("HandleRight");
        _handleBottomLeft = this.FindControl<Border>("HandleBottomLeft");
        _handleBottom = this.FindControl<Border>("HandleBottom");
        _handleBottomRight = this.FindControl<Border>("HandleBottomRight");

        UpdateVisualState();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsSelectedProperty)
        {
            UpdateVisualState();
        }
        else if (change.Property == IsLockedProperty)
        {
            UpdateVisualState();
        }
        else if (change.Property == ElementProperty)
        {
            OnElementChanged();
        }
        else if (change.Property == BoundsProperty)
        {
            UpdateResizeHandlePositions();
        }
    }

    protected override void OnLoaded(Avalonia.Interactivity.RoutedEventArgs e)
    {
        base.OnLoaded(e);
        UpdateVisualState();
        UpdateResizeHandlePositions();
    }

    private void OnElementChanged()
    {
        if (Element != null)
        {
            ElementTypeName = GetElementTypeName(Element.ElementType);
            IsLocked = Element.IsLocked;
            UpdateFromElement();
        }
    }

    private static string GetElementTypeName(ReportElementType type)
    {
        return type switch
        {
            ReportElementType.Chart => "Chart",
            ReportElementType.Table => "Table",
            ReportElementType.Label => "Label",
            ReportElementType.Image => "Image",
            ReportElementType.DateRange => "Date Range",
            ReportElementType.Summary => "Summary",
            _ => "Element"
        };
    }

    private void UpdateFromElement()
    {
        if (Element == null) return;

        // Update position and size from element
        Canvas.SetLeft(this, Element.X);
        Canvas.SetTop(this, Element.Y);
        Width = Element.Width;
        Height = Element.Height;
    }

    private void UpdateVisualState()
    {
        if (_hoverBorder != null)
        {
            _hoverBorder.IsVisible = _isHovering && !IsSelected;
        }

        if (_selectionBorder != null)
        {
            _selectionBorder.IsVisible = IsSelected;
        }

        if (_resizeHandlesCanvas != null)
        {
            _resizeHandlesCanvas.IsVisible = IsSelected && !IsLocked;
        }

        if (_typeIndicator != null)
        {
            _typeIndicator.IsVisible = IsSelected && ShowTypeIndicator;
        }

        if (_lockIndicator != null)
        {
            _lockIndicator.IsVisible = IsSelected && IsLocked;
        }

        // Update cursor based on state
        Cursor = IsLocked ? new Cursor(StandardCursorType.Arrow) :
                 IsSelected ? new Cursor(StandardCursorType.SizeAll) :
                 new Cursor(StandardCursorType.Hand);
    }

    private void UpdateResizeHandlePositions()
    {
        if (_resizeHandlesCanvas == null) return;

        double width = Bounds.Width;
        double height = Bounds.Height;

        // Position each handle
        SetHandlePosition(_handleTopLeft, -HandleOffset, -HandleOffset);
        SetHandlePosition(_handleTop, width / 2 - HandleOffset, -HandleOffset);
        SetHandlePosition(_handleTopRight, width - HandleOffset, -HandleOffset);
        SetHandlePosition(_handleLeft, -HandleOffset, height / 2 - HandleOffset);
        SetHandlePosition(_handleRight, width - HandleOffset, height / 2 - HandleOffset);
        SetHandlePosition(_handleBottomLeft, -HandleOffset, height - HandleOffset);
        SetHandlePosition(_handleBottom, width / 2 - HandleOffset, height - HandleOffset);
        SetHandlePosition(_handleBottomRight, width - HandleOffset, height - HandleOffset);
    }

    private static void SetHandlePosition(Border? handle, double x, double y)
    {
        if (handle == null) return;
        Canvas.SetLeft(handle, x);
        Canvas.SetTop(handle, y);
    }

    #region Pointer Event Handlers

    protected override void OnPointerEntered(PointerEventArgs e)
    {
        base.OnPointerEntered(e);
        _isHovering = true;
        UpdateVisualState();
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        _isHovering = false;
        UpdateVisualState();
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var point = e.GetCurrentPoint(this);

        if (point.Properties.IsLeftButtonPressed)
        {
            // Select this element
            var ctrlPressed = (e.KeyModifiers & KeyModifiers.Control) != 0;
            ElementSelected?.Invoke(this, new ElementSelectedEventArgs(Element, ctrlPressed));

            if (!IsLocked)
            {
                // Start dragging
                StartDrag(e);
            }

            e.Handled = true;
        }
        else if (point.Properties.IsRightButtonPressed)
        {
            // Context menu handling could go here
            ElementSelected?.Invoke(this, new ElementSelectedEventArgs(Element, false));
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (_isDragging && Parent is Canvas canvas)
        {
            var currentPoint = e.GetPosition(canvas);
            var delta = currentPoint - _dragStartPoint;

            var newX = _elementStartPosition.X + delta.X;
            var newY = _elementStartPosition.Y + delta.Y;

            // Apply snapping if enabled
            if (SnapGridSize > 0)
            {
                newX = Math.Round(newX / SnapGridSize) * SnapGridSize;
                newY = Math.Round(newY / SnapGridSize) * SnapGridSize;
            }

            // Constrain to canvas bounds
            newX = Math.Max(0, Math.Min(newX, canvas.Bounds.Width - Width));
            newY = Math.Max(0, Math.Min(newY, canvas.Bounds.Height - Height));

            Canvas.SetLeft(this, newX);
            Canvas.SetTop(this, newY);

            if (Element != null)
            {
                Element.X = newX;
                Element.Y = newY;
            }

            PositionChanged?.Invoke(this, new ElementPositionChangedEventArgs(
                Element, _elementStartPosition, new Point(newX, newY), false));

            e.Handled = true;
        }
        else if (_isResizing && Parent is Canvas resizeCanvas)
        {
            HandleResize(e, resizeCanvas);
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);

        if (_isDragging)
        {
            EndDrag(e);
        }
        else if (_isResizing)
        {
            EndResize(e);
        }
    }

    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);

        if (_isDragging)
        {
            _isDragging = false;
            DragEnded?.Invoke(this, new ElementDragEventArgs(Element, true));
        }
        else if (_isResizing)
        {
            _isResizing = false;
            _resizeHandle = null;
            ResizeEnded?.Invoke(this, new ElementResizeEventArgs(Element, true));
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (IsSelected && !IsLocked)
        {
            switch (e.Key)
            {
                case Key.Delete:
                case Key.Back:
                    DeleteRequested?.Invoke(this, new ElementDeleteRequestedEventArgs(Element));
                    e.Handled = true;
                    break;

                case Key.Up:
                    MoveByArrowKey(0, e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -10 : -1);
                    e.Handled = true;
                    break;

                case Key.Down:
                    MoveByArrowKey(0, e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1);
                    e.Handled = true;
                    break;

                case Key.Left:
                    MoveByArrowKey(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? -10 : -1, 0);
                    e.Handled = true;
                    break;

                case Key.Right:
                    MoveByArrowKey(e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? 10 : 1, 0);
                    e.Handled = true;
                    break;
            }
        }
    }

    private void MoveByArrowKey(double deltaX, double deltaY)
    {
        if (Parent is not Canvas canvas) return;

        var currentX = Canvas.GetLeft(this);
        var currentY = Canvas.GetTop(this);
        var newX = Math.Max(0, Math.Min(currentX + deltaX, canvas.Bounds.Width - Width));
        var newY = Math.Max(0, Math.Min(currentY + deltaY, canvas.Bounds.Height - Height));

        Canvas.SetLeft(this, newX);
        Canvas.SetTop(this, newY);

        if (Element != null)
        {
            Element.X = newX;
            Element.Y = newY;
        }

        PositionChanged?.Invoke(this, new ElementPositionChangedEventArgs(
            Element, new Point(currentX, currentY), new Point(newX, newY), true));
    }

    #endregion

    #region Drag Handling

    private void StartDrag(PointerPressedEventArgs e)
    {
        if (Parent is not Canvas canvas) return;

        _isDragging = true;
        _dragStartPoint = e.GetPosition(canvas);
        _elementStartPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));

        e.Pointer.Capture(this);
        DragStarted?.Invoke(this, new ElementDragEventArgs(Element, false));
    }

    private void EndDrag(PointerReleasedEventArgs e)
    {
        _isDragging = false;
        e.Pointer.Capture(null);

        var finalPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        PositionChanged?.Invoke(this, new ElementPositionChangedEventArgs(
            Element, _elementStartPosition, finalPosition, true));

        DragEnded?.Invoke(this, new ElementDragEventArgs(Element, false));
    }

    #endregion

    #region Resize Handling

    private void OnResizeHandlePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Border handle || IsLocked) return;
        if (Parent is not Canvas canvas) return;

        var point = e.GetCurrentPoint(handle);
        if (!point.Properties.IsLeftButtonPressed) return;

        _isResizing = true;
        _resizeHandle = handle.Tag?.ToString();
        _dragStartPoint = e.GetPosition(canvas);
        _elementStartPosition = new Point(Canvas.GetLeft(this), Canvas.GetTop(this));
        _elementStartSize = new Size(Width, Height);

        e.Pointer.Capture(this);
        ResizeStarted?.Invoke(this, new ElementResizeEventArgs(Element, false));

        e.Handled = true;
    }

    private void HandleResize(PointerEventArgs e, Canvas canvas)
    {
        var currentPoint = e.GetPosition(canvas);
        var delta = currentPoint - _dragStartPoint;

        double newX = _elementStartPosition.X;
        double newY = _elementStartPosition.Y;
        double newWidth = _elementStartSize.Width;
        double newHeight = _elementStartSize.Height;

        switch (_resizeHandle)
        {
            case "TopLeft":
                newX = _elementStartPosition.X + delta.X;
                newY = _elementStartPosition.Y + delta.Y;
                newWidth = _elementStartSize.Width - delta.X;
                newHeight = _elementStartSize.Height - delta.Y;
                break;

            case "Top":
                newY = _elementStartPosition.Y + delta.Y;
                newHeight = _elementStartSize.Height - delta.Y;
                break;

            case "TopRight":
                newY = _elementStartPosition.Y + delta.Y;
                newWidth = _elementStartSize.Width + delta.X;
                newHeight = _elementStartSize.Height - delta.Y;
                break;

            case "Left":
                newX = _elementStartPosition.X + delta.X;
                newWidth = _elementStartSize.Width - delta.X;
                break;

            case "Right":
                newWidth = _elementStartSize.Width + delta.X;
                break;

            case "BottomLeft":
                newX = _elementStartPosition.X + delta.X;
                newWidth = _elementStartSize.Width - delta.X;
                newHeight = _elementStartSize.Height + delta.Y;
                break;

            case "Bottom":
                newHeight = _elementStartSize.Height + delta.Y;
                break;

            case "BottomRight":
                newWidth = _elementStartSize.Width + delta.X;
                newHeight = _elementStartSize.Height + delta.Y;
                break;
        }

        // Apply minimum size constraints
        if (newWidth < MinElementWidth)
        {
            if (_resizeHandle?.Contains("Left") == true)
            {
                newX = _elementStartPosition.X + _elementStartSize.Width - MinElementWidth;
            }
            newWidth = MinElementWidth;
        }

        if (newHeight < MinElementHeight)
        {
            if (_resizeHandle?.Contains("Top") == true)
            {
                newY = _elementStartPosition.Y + _elementStartSize.Height - MinElementHeight;
            }
            newHeight = MinElementHeight;
        }

        // Apply snapping if enabled
        if (SnapGridSize > 0)
        {
            newX = Math.Round(newX / SnapGridSize) * SnapGridSize;
            newY = Math.Round(newY / SnapGridSize) * SnapGridSize;
            newWidth = Math.Round(newWidth / SnapGridSize) * SnapGridSize;
            newHeight = Math.Round(newHeight / SnapGridSize) * SnapGridSize;
        }

        // Constrain to canvas bounds
        newX = Math.Max(0, newX);
        newY = Math.Max(0, newY);
        newWidth = Math.Min(newWidth, canvas.Bounds.Width - newX);
        newHeight = Math.Min(newHeight, canvas.Bounds.Height - newY);

        // Apply changes
        Canvas.SetLeft(this, newX);
        Canvas.SetTop(this, newY);
        Width = newWidth;
        Height = newHeight;

        if (Element != null)
        {
            Element.X = newX;
            Element.Y = newY;
            Element.Width = newWidth;
            Element.Height = newHeight;
        }

        SizeChanged?.Invoke(this, new ElementSizeChangedEventArgs(
            Element, _elementStartSize, new Size(newWidth, newHeight), false));
    }

    private void EndResize(PointerReleasedEventArgs e)
    {
        _isResizing = false;
        _resizeHandle = null;
        e.Pointer.Capture(null);

        var finalSize = new Size(Width, Height);
        SizeChanged?.Invoke(this, new ElementSizeChangedEventArgs(
            Element, _elementStartSize, finalSize, true));

        ResizeEnded?.Invoke(this, new ElementResizeEventArgs(Element, false));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Refreshes the control's visual state.
    /// </summary>
    public void RefreshVisualState()
    {
        UpdateVisualState();
        UpdateResizeHandlePositions();
    }

    /// <summary>
    /// Updates the control position and size from the element.
    /// </summary>
    public void SyncFromElement()
    {
        UpdateFromElement();
    }

    /// <summary>
    /// Brings focus to this element.
    /// </summary>
    public void BringToFocus()
    {
        Focus();
        IsSelected = true;
    }

    #endregion
}

#region Event Args Classes

/// <summary>
/// Event args for element selection events.
/// </summary>
public class ElementSelectedEventArgs : EventArgs
{
    public ReportElementBase? Element { get; }
    public bool IsMultiSelect { get; }

    public ElementSelectedEventArgs(ReportElementBase? element, bool isMultiSelect)
    {
        Element = element;
        IsMultiSelect = isMultiSelect;
    }
}

/// <summary>
/// Event args for element position change events.
/// </summary>
public class ElementPositionChangedEventArgs : EventArgs
{
    public ReportElementBase? Element { get; }
    public Point OldPosition { get; }
    public Point NewPosition { get; }
    public bool IsComplete { get; }

    public ElementPositionChangedEventArgs(ReportElementBase? element, Point oldPosition, Point newPosition, bool isComplete)
    {
        Element = element;
        OldPosition = oldPosition;
        NewPosition = newPosition;
        IsComplete = isComplete;
    }
}

/// <summary>
/// Event args for element size change events.
/// </summary>
public class ElementSizeChangedEventArgs : EventArgs
{
    public ReportElementBase? Element { get; }
    public Size OldSize { get; }
    public Size NewSize { get; }
    public bool IsComplete { get; }

    public ElementSizeChangedEventArgs(ReportElementBase? element, Size oldSize, Size newSize, bool isComplete)
    {
        Element = element;
        OldSize = oldSize;
        NewSize = newSize;
        IsComplete = isComplete;
    }
}

/// <summary>
/// Event args for drag events.
/// </summary>
public class ElementDragEventArgs : EventArgs
{
    public ReportElementBase? Element { get; }
    public bool WasCancelled { get; }

    public ElementDragEventArgs(ReportElementBase? element, bool wasCancelled)
    {
        Element = element;
        WasCancelled = wasCancelled;
    }
}

/// <summary>
/// Event args for resize events.
/// </summary>
public class ElementResizeEventArgs : EventArgs
{
    public ReportElementBase? Element { get; }
    public bool WasCancelled { get; }

    public ElementResizeEventArgs(ReportElementBase? element, bool wasCancelled)
    {
        Element = element;
        WasCancelled = wasCancelled;
    }
}

/// <summary>
/// Event args for element deletion requests.
/// </summary>
public class ElementDeleteRequestedEventArgs : EventArgs
{
    public ReportElementBase? Element { get; }

    public ElementDeleteRequestedEventArgs(ReportElementBase? element)
    {
        Element = element;
    }
}

#endregion
