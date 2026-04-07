using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls.Dashboard;

public class DragDropIndicator : Control
{
    public static readonly StyledProperty<IBrush?> IndicatorBrushProperty =
        AvaloniaProperty.Register<DragDropIndicator, IBrush?>(nameof(IndicatorBrush));

    public IBrush? IndicatorBrush
    {
        get => GetValue(IndicatorBrushProperty);
        set => SetValue(IndicatorBrushProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var brush = IndicatorBrush ?? Brushes.DodgerBlue;
        var pen = new Pen(brush, 3, lineCap: PenLineCap.Round);
        var y = Bounds.Height / 2;
        context.DrawLine(pen, new Point(4, y), new Point(Bounds.Width - 4, y));
        context.DrawEllipse(brush, null, new Point(4, y), 4, 4);
        context.DrawEllipse(brush, null, new Point(Bounds.Width - 4, y), 4, 4);
    }

    protected override Size MeasureOverride(Size availableSize) => new(availableSize.Width, 12);
}
