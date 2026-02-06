using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A WrapPanel variant that centers each row of items horizontally.
/// Standard WrapPanel left-aligns items per row; this panel distributes
/// the leftover space equally on both sides of each row.
/// </summary>
public class CenteringWrapPanel : Panel
{
    protected override Size MeasureOverride(Size availableSize)
    {
        double rowWidth = 0;
        double rowHeight = 0;
        double totalHeight = 0;
        double maxRowWidth = 0;

        foreach (var child in Children)
        {
            child.Measure(availableSize);

            if (!child.IsVisible)
                continue;

            var desired = child.DesiredSize;

            if (rowWidth + desired.Width > availableSize.Width && rowWidth > 0)
            {
                maxRowWidth = Math.Max(maxRowWidth, rowWidth);
                totalHeight += rowHeight;
                rowWidth = 0;
                rowHeight = 0;
            }

            rowWidth += desired.Width;
            rowHeight = Math.Max(rowHeight, desired.Height);
        }

        maxRowWidth = Math.Max(maxRowWidth, rowWidth);
        totalHeight += rowHeight;

        return new Size(maxRowWidth, totalHeight);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        // Collect visible children into rows, then center each row.
        var children = Children;
        double y = 0;
        int rowStart = 0;
        double rowWidth = 0;
        double rowHeight = 0;

        for (int i = 0; i <= children.Count; i++)
        {
            bool isEnd = i == children.Count;
            bool wrap = false;
            Size desired = default;

            if (!isEnd)
            {
                if (!children[i].IsVisible)
                    continue;

                desired = children[i].DesiredSize;
                wrap = rowWidth + desired.Width > finalSize.Width && rowWidth > 0;
            }

            if (isEnd || wrap)
            {
                // Arrange the completed row centered
                double x = (finalSize.Width - rowWidth) / 2.0;
                for (int j = rowStart; j < i; j++)
                {
                    if (!children[j].IsVisible)
                        continue;
                    var d = children[j].DesiredSize;
                    children[j].Arrange(new Rect(x, y, d.Width, rowHeight));
                    x += d.Width;
                }

                y += rowHeight;
                rowStart = i;
                rowWidth = 0;
                rowHeight = 0;
            }

            if (!isEnd)
            {
                rowWidth += desired.Width;
                rowHeight = Math.Max(rowHeight, desired.Height);
            }
        }

        return new Size(finalSize.Width, y);
    }
}
