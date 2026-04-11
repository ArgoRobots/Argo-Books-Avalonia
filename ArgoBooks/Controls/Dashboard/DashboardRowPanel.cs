using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls.Dashboard;

public class DashboardRowPanel : Panel
{
    public static readonly AttachedProperty<double> WidgetFractionProperty =
        AvaloniaProperty.RegisterAttached<DashboardRowPanel, Control, double>("WidgetFraction", 0.5);

    public static double GetWidgetFraction(Control element) => element.GetValue(WidgetFractionProperty);
    public static void SetWidgetFraction(Control element, double value) => element.SetValue(WidgetFractionProperty, value);

    public static readonly StyledProperty<double> SpacingProperty =
        AvaloniaProperty.Register<DashboardRowPanel, double>(nameof(Spacing), 12.0);

    public double Spacing
    {
        get => GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var panelWidth = availableSize.Width;
        if (panelWidth <= 0 || Children.Count == 0) return default;

        double totalFraction = 0;
        int visibleCount = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var f = GetWidgetFraction(child);
            if (f <= 0) f = 0.5;
            totalFraction += f;
            visibleCount++;
        }
        if (visibleCount == 0) return default;

        var scaleFactor = totalFraction >= 0.999 ? 1.0 / totalFraction : 1.0;
        var totalSpacing = Math.Max(0, visibleCount - 1) * Spacing;
        var availableForWidgets = panelWidth - totalSpacing;

        double rowHeight = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var fraction = GetWidgetFraction(child);
            if (fraction <= 0) fraction = 0.5;
            var childWidth = Math.Max(0, availableForWidgets * fraction * scaleFactor);
            child.Measure(new Size(childWidth, availableSize.Height));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
        }

        return new Size(panelWidth, rowHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var panelWidth = finalSize.Width;
        if (panelWidth <= 0 || Children.Count == 0) return finalSize;

        double totalFraction = 0;
        int visibleCount = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var f = GetWidgetFraction(child);
            if (f <= 0) f = 0.5;
            totalFraction += f;
            visibleCount++;
        }
        if (visibleCount == 0) return finalSize;

        var scaleFactor = totalFraction >= 0.999 ? 1.0 / totalFraction : 1.0;
        var totalSpacing = Math.Max(0, visibleCount - 1) * Spacing;
        var availableForWidgets = panelWidth - totalSpacing;

        double rowHeight = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Height);
        }

        double x = 0;
        foreach (var child in Children)
        {
            if (!child.IsVisible) continue;
            var fraction = GetWidgetFraction(child);
            if (fraction <= 0) fraction = 0.5;
            var childWidth = availableForWidgets * fraction * scaleFactor;
            child.Arrange(new Rect(x, 0, childWidth, rowHeight));
            x += childWidth + Spacing;
        }

        return new Size(panelWidth, rowHeight);
    }
}
