using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A panel that arranges up to 4 children in equal-width columns with consistent 12px spacing.
/// First child: Margin="0,0,6,0", Middle children: Margin="6,0", Last child: Margin="6,0,0,0"
/// </summary>
public class StatCardsRow : Panel
{
    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        var childCount = Children.Count;
        if (childCount == 0) return Size.Empty;

        // Calculate available width per column (accounting for margins: 6px between each)
        var totalMargin = (childCount - 1) * 12.0; // 6px right + 6px left between adjacent cards
        var columnWidth = (availableSize.Width - totalMargin) / childCount;
        var childAvailable = new Size(columnWidth, availableSize.Height);

        var maxHeight = 0.0;
        for (var i = 0; i < childCount; i++)
        {
            var child = Children[i];
            child.Measure(childAvailable);
            if (child.DesiredSize.Height > maxHeight)
                maxHeight = child.DesiredSize.Height;
        }

        return new Size(availableSize.Width, maxHeight);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        var childCount = Children.Count;
        if (childCount == 0) return finalSize;

        // Calculate column width (equal widths, with 12px gaps between)
        var totalMargin = (childCount - 1) * 12.0;
        var columnWidth = (finalSize.Width - totalMargin) / childCount;

        var x = 0.0;
        for (var i = 0; i < childCount; i++)
        {
            var child = Children[i];
            var rect = new Rect(x, 0, columnWidth, finalSize.Height);
            child.Arrange(rect);

            // Move x position: column width + 12px gap
            x += columnWidth + 12;
        }

        return finalSize;
    }
}
