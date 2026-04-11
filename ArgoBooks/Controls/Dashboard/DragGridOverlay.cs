using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace ArgoBooks.Controls.Dashboard;

/// <summary>
/// Draws dotted grid lines showing the column structure of the dashboard during drag.
/// Shows a 4-column grid (25%, 50%, 75%) across every row, plus horizontal lines between rows.
/// </summary>
public class DragGridOverlay : Control
{
    private readonly StackPanel _rowsContainer;

    private static readonly IPen GridPen = new Pen(
        new SolidColorBrush(Color.FromArgb(120, 128, 128, 128)),
        1,
        new DashStyle([4.0, 4.0], 0));

    public DragGridOverlay(StackPanel rowsContainer)
    {
        _rowsContainer = rowsContainer;
        IsHitTestVisible = false;
    }

    public override void Render(DrawingContext context)
    {
        var panelWidth = _rowsContainer.Bounds.Width;
        if (panelWidth <= 0 || _rowsContainer.Children.Count == 0) return;

        // Collect row bounds relative to the rowsContainer
        var rows = new List<Rect>();
        foreach (var child in _rowsContainer.Children)
        {
            if (child is not DashboardRowHost rowHost || !rowHost.IsVisible) continue;
            var topLeft = rowHost.TranslatePoint(new Point(0, 0), _rowsContainer) ?? new Point();
            rows.Add(new Rect(topLeft, rowHost.Bounds.Size));
        }

        if (rows.Count == 0) return;

        var gridTop = rows[0].Top;
        var gridBottom = rows[^1].Bottom;

        // Draw vertical column lines at 0%, 25%, 50%, 75%, 100%
        double[] columnFractions = [0, 0.25, 0.5, 0.75, 1.0];
        foreach (var frac in columnFractions)
        {
            var x = panelWidth * frac;
            context.DrawLine(GridPen, new Point(x, gridTop), new Point(x, gridBottom));
        }

        // Draw horizontal lines: top of first row, between rows, bottom of last row
        context.DrawLine(GridPen, new Point(0, gridTop), new Point(panelWidth, gridTop));
        for (int r = 1; r < rows.Count; r++)
        {
            var y = (rows[r - 1].Bottom + rows[r].Top) / 2;
            context.DrawLine(GridPen, new Point(0, y), new Point(panelWidth, y));
        }
        context.DrawLine(GridPen, new Point(0, gridBottom), new Point(panelWidth, gridBottom));
    }
}
