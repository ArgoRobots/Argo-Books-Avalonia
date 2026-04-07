using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls.Dashboard;

public class DashboardFlowPanel : Panel
{
    public static readonly AttachedProperty<double> WidgetFractionProperty =
        AvaloniaProperty.RegisterAttached<DashboardFlowPanel, Control, double>("WidgetFraction", 0.5);

    public static double GetWidgetFraction(Control element) => element.GetValue(WidgetFractionProperty);
    public static void SetWidgetFraction(Control element, double value) => element.SetValue(WidgetFractionProperty, value);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<DashboardFlowPanel, double>(nameof(Spacing), 12.0);

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    public static readonly StyledProperty<double> RowSpacingProperty =
        AvaloniaProperty.Register<DashboardFlowPanel, double>(nameof(RowSpacing), 12.0);

    public double RowSpacing
    {
        get => GetValue(RowSpacingProperty);
        set => SetValue(RowSpacingProperty, value);
    }

    private List<(int start, int end, double height)> _rows = [];

    public IReadOnlyList<Rect> GetChildBounds()
    {
        var bounds = new List<Rect>();
        foreach (var child in Children)
        {
            if (child.IsVisible)
                bounds.Add(child.Bounds);
        }
        return bounds;
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        _rows.Clear();
        if (Children.Count == 0) return default;

        var panelWidth = availableSize.Width;
        double rowFractionSum = 0;
        int rowStart = 0;
        double rowHeight = 0;
        double totalHeight = 0;
        int visibleStartIndex = -1;

        for (int i = 0; i < Children.Count; i++)
        {
            var child = Children[i];
            if (!child.IsVisible) continue;

            if (visibleStartIndex < 0) visibleStartIndex = i;

            var fraction = GetWidgetFraction(child);
            if (fraction <= 0) fraction = 0.5;

            // Check if adding this widget would exceed the row
            if (rowFractionSum > 0 && rowFractionSum + fraction > 1.001)
            {
                // Close current row
                _rows.Add((rowStart, i - 1, rowHeight));
                if (_rows.Count > 1) totalHeight += RowSpacing;
                totalHeight += rowHeight;
                rowFractionSum = 0;
                rowHeight = 0;
                rowStart = i;
            }

            rowFractionSum += fraction;

            // Measure child with estimated width
            var visibleInRow = CountVisibleInRange(rowStart, i);
            var spacingInRow = Math.Max(0, visibleInRow - 1) * Spacing;
            var childWidth = Math.Max(0, (panelWidth - spacingInRow) * fraction);
            child.Measure(new Size(childWidth, availableSize.Height));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
        }

        // Close final row
        if (rowFractionSum > 0)
        {
            var lastVisibleIndex = -1;
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                if (Children[i].IsVisible) { lastVisibleIndex = i; break; }
            }
            if (lastVisibleIndex >= 0)
            {
                _rows.Add((rowStart, lastVisibleIndex, rowHeight));
                if (_rows.Count > 1) totalHeight += RowSpacing;
                totalHeight += rowHeight;
            }
        }

        return new Size(panelWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        if (_rows.Count == 0) return finalSize;

        var panelWidth = finalSize.Width;
        double y = 0;

        foreach (var (start, end, rowHeight) in _rows)
        {
            // Calculate total fraction for this row and visible count
            double totalFraction = 0;
            int visibleCount = 0;
            for (int i = start; i <= end; i++)
            {
                if (!Children[i].IsVisible) continue;
                var f = GetWidgetFraction(Children[i]);
                if (f <= 0) f = 0.5;
                totalFraction += f;
                visibleCount++;
            }

            if (visibleCount == 0) continue;

            // Scale factor to stretch partial rows to fill width
            var scaleFactor = totalFraction > 0 ? 1.0 / totalFraction : 1.0;
            var totalSpacing = Math.Max(0, visibleCount - 1) * Spacing;
            var availableForWidgets = panelWidth - totalSpacing;

            double x = 0;
            for (int i = start; i <= end; i++)
            {
                if (!Children[i].IsVisible) continue;

                var fraction = GetWidgetFraction(Children[i]);
                if (fraction <= 0) fraction = 0.5;
                var childWidth = availableForWidgets * fraction * scaleFactor;

                Children[i].Arrange(new Rect(x, y, childWidth, rowHeight));
                x += childWidth + Spacing;
            }

            y += rowHeight + RowSpacing;
        }

        return new Size(panelWidth, Math.Max(0, y - RowSpacing));
    }

    private int CountVisibleInRange(int start, int end)
    {
        int count = 0;
        for (int i = start; i <= end; i++)
        {
            if (Children[i].IsVisible) count++;
        }
        return count;
    }
}
