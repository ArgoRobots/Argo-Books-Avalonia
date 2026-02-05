using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace ArgoBooks.Helpers;

/// <summary>
/// Shared helper for tutorial overlay highlighting. Handles element lookup,
/// highlight bounds calculation with edge clamping, and backdrop geometry
/// with a transparent cutout over the highlighted area.
/// </summary>
public static class TutorialHighlightHelper
{
    private const double BorderThickness = 3;
    private const double EdgeOffset = 8;

    /// <summary>
    /// Finds a named control in the visual tree.
    /// </summary>
    public static T? FindElementByName<T>(Visual root, string name) where T : Control
    {
        if (root is Control control && control.Name == name)
            return control as T;

        foreach (var child in root.GetVisualDescendants())
        {
            if (child is T typedChild && typedChild.Name == name)
                return typedChild;
        }

        return null;
    }

    /// <summary>
    /// Computes a bounding rectangle around the actual TabItem headers inside
    /// a named TabControl, relative to the overlay. The ItemsPresenter stretches
    /// full width, so we union the bounds of each TabItem to get a tight fit.
    /// </summary>
    public static Rect? GetTabItemsBounds(Control overlay, Visual root, string tabControlName)
    {
        var tabControl = FindElementByName<TabControl>(root, tabControlName);
        if (tabControl == null)
            return null;

        Rect? union = null;

        foreach (var descendant in tabControl.GetVisualDescendants())
        {
            if (descendant is not TabItem tabItem || !tabItem.IsVisible)
                continue;

            try
            {
                var transform = tabItem.TransformToVisual(overlay);
                if (transform == null)
                    continue;

                var itemBounds = new Rect(0, 0, tabItem.Bounds.Width, tabItem.Bounds.Height);
                var topLeft = transform.Value.Transform(itemBounds.TopLeft);
                var bottomRight = transform.Value.Transform(itemBounds.BottomRight);
                var mapped = new Rect(topLeft, bottomRight);

                union = union == null ? mapped : union.Value.Union(mapped);
            }
            catch
            {
                // skip items we can't transform
            }
        }

        if (union == null)
            return null;

        // Add padding around the tab items, then border thickness outside that
        const double padding = 6;
        var r = union.Value;
        return new Rect(
            r.X - padding - BorderThickness,
            r.Y - padding - BorderThickness,
            r.Width + (padding + BorderThickness) * 2,
            r.Height + (padding + BorderThickness) * 2);
    }

    /// <summary>
    /// Calculates highlight bounds relative to the overlay, with proper edge
    /// clamping to prevent overflow.
    /// </summary>
    public static Rect? GetHighlightBounds(Control overlay, Control element)
    {
        try
        {
            var transform = element.TransformToVisual(overlay);
            if (transform == null)
                return null;

            var elementBounds = new Rect(0, 0, element.Bounds.Width, element.Bounds.Height);
            var topLeft = transform.Value.Transform(elementBounds.TopLeft);
            var bottomRight = transform.Value.Transform(elementBounds.BottomRight);

            var overlayWidth = overlay.Bounds.Width;
            var overlayHeight = overlay.Bounds.Height;

            // Inset with edge clamping (same as AppTourOverlay)
            var leftOffset = topLeft.X <= EdgeOffset ? EdgeOffset - 3 : 0;
            var topOffset = topLeft.Y <= EdgeOffset ? EdgeOffset - 1 : BorderThickness - 1;
            var rightOffset = bottomRight.X >= overlayWidth - EdgeOffset ? EdgeOffset : BorderThickness;
            var bottomOffset = bottomRight.Y >= overlayHeight - EdgeOffset ? EdgeOffset : BorderThickness;

            var left = topLeft.X + leftOffset;
            var top = topLeft.Y + topOffset;
            var width = (bottomRight.X - topLeft.X) - leftOffset - rightOffset;
            var height = (bottomRight.Y - topLeft.Y) - topOffset - bottomOffset;

            if (width <= 0 || height <= 0)
                return null;

            return new Rect(left, top, width, height);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Creates a backdrop geometry with a transparent cutout over the highlight area.
    /// Uses EvenOdd fill rule: the outer rectangle fills the backdrop, and the inner
    /// rectangle (cutout) becomes transparent where it overlaps.
    /// </summary>
    public static Geometry CreateBackdropGeometry(Size overlaySize, Rect? highlightBounds, CornerRadius cornerRadius)
    {
        if (highlightBounds == null || overlaySize.Width <= 0 || overlaySize.Height <= 0)
        {
            // No cutout - full solid backdrop
            return new RectangleGeometry(new Rect(0, 0, overlaySize.Width, overlaySize.Height));
        }

        var cutout = highlightBounds.Value;
        var r = cornerRadius.TopLeft; // Use uniform corner radius

        var geometry = new PathGeometry { FillRule = FillRule.EvenOdd };

        // Outer rectangle (full overlay)
        var outerFigure = new PathFigure
        {
            StartPoint = new Point(0, 0),
            IsClosed = true,
            IsFilled = true,
            Segments = new PathSegments
            {
                new LineSegment { Point = new Point(overlaySize.Width, 0) },
                new LineSegment { Point = new Point(overlaySize.Width, overlaySize.Height) },
                new LineSegment { Point = new Point(0, overlaySize.Height) },
            }
        };
        geometry.Figures!.Add(outerFigure);

        // Inner rectangle (cutout) - with optional corner radius
        PathFigure cutoutFigure;

        if (r > 0)
        {
            var arcSize = new Size(r, r);
            cutoutFigure = new PathFigure
            {
                StartPoint = new Point(cutout.X + r, cutout.Y),
                IsClosed = true,
                IsFilled = true,
                Segments = new PathSegments
                {
                    new LineSegment { Point = new Point(cutout.Right - r, cutout.Y) },
                    new ArcSegment { Point = new Point(cutout.Right, cutout.Y + r), Size = arcSize, SweepDirection = SweepDirection.Clockwise },
                    new LineSegment { Point = new Point(cutout.Right, cutout.Bottom - r) },
                    new ArcSegment { Point = new Point(cutout.Right - r, cutout.Bottom), Size = arcSize, SweepDirection = SweepDirection.Clockwise },
                    new LineSegment { Point = new Point(cutout.X + r, cutout.Bottom) },
                    new ArcSegment { Point = new Point(cutout.X, cutout.Bottom - r), Size = arcSize, SweepDirection = SweepDirection.Clockwise },
                    new LineSegment { Point = new Point(cutout.X, cutout.Y + r) },
                    new ArcSegment { Point = new Point(cutout.X + r, cutout.Y), Size = arcSize, SweepDirection = SweepDirection.Clockwise },
                }
            };
        }
        else
        {
            cutoutFigure = new PathFigure
            {
                StartPoint = new Point(cutout.X, cutout.Y),
                IsClosed = true,
                IsFilled = true,
                Segments = new PathSegments
                {
                    new LineSegment { Point = new Point(cutout.Right, cutout.Y) },
                    new LineSegment { Point = new Point(cutout.Right, cutout.Bottom) },
                    new LineSegment { Point = new Point(cutout.X, cutout.Bottom) },
                }
            };
        }

        geometry.Figures!.Add(cutoutFigure);

        return geometry;
    }
}
