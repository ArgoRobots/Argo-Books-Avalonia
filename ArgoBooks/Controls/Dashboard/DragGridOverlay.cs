using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls.Dashboard;

/// <summary>
/// Draws dotted grid lines showing the row/column structure of the dashboard during drag.
/// Shows a standard 4-column grid (25%, 50%, 75%) across every row, plus horizontal
/// lines between rows.
/// </summary>
public class DragGridOverlay : Control
{
    private IReadOnlyList<Rect>? _widgetBounds;
    private double _panelWidth;
    private double _panelHeight;

    private static readonly IPen GridPen = new Pen(
        new SolidColorBrush(Color.FromArgb(120, 128, 128, 128)),
        1,
        new DashStyle(new[] { 4.0, 4.0 }, 0));

    public void Update(IReadOnlyList<Rect> widgetBounds, double panelWidth, double panelHeight)
    {
        _widgetBounds = widgetBounds;
        _panelWidth = panelWidth;
        _panelHeight = panelHeight;
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        if (_widgetBounds == null || _widgetBounds.Count == 0 || _panelWidth <= 0) return;

        // Group bounds into rows by similar Y position
        var rows = new List<(double top, double bottom)>();
        double rowTop = _widgetBounds[0].Top;
        double rowBottom = _widgetBounds[0].Bottom;

        for (int i = 1; i < _widgetBounds.Count; i++)
        {
            if (Math.Abs(_widgetBounds[i].Top - rowTop) > 1)
            {
                rows.Add((rowTop, rowBottom));
                rowTop = _widgetBounds[i].Top;
                rowBottom = _widgetBounds[i].Bottom;
            }
            else
            {
                rowBottom = Math.Max(rowBottom, _widgetBounds[i].Bottom);
            }
        }
        rows.Add((rowTop, rowBottom));

        var gridTop = rows[0].top;
        var gridBottom = rows[^1].bottom;

        // Draw vertical column lines at 0%, 25%, 50%, 75%, 100% across full grid height
        double[] columnFractions = [0, 0.25, 0.5, 0.75, 1.0];
        foreach (var frac in columnFractions)
        {
            var x = _panelWidth * frac;
            context.DrawLine(GridPen, new Point(x, gridTop), new Point(x, gridBottom));
        }

        // Draw horizontal lines: top edge, between rows, and bottom edge
        context.DrawLine(GridPen, new Point(0, gridTop), new Point(_panelWidth, gridTop));
        for (int r = 1; r < rows.Count; r++)
        {
            var y = (rows[r - 1].bottom + rows[r].top) / 2;
            context.DrawLine(GridPen, new Point(0, y), new Point(_panelWidth, y));
        }
        context.DrawLine(GridPen, new Point(0, gridBottom), new Point(_panelWidth, gridBottom));
    }
}
