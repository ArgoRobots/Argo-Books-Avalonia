namespace ArgoBooks.Core.Models.Dashboard;

public enum WidgetSize
{
    Tiny,    // 25% — 4 per row
    Small,   // 33% — 3 per row
    Medium,  // 50% — 2 per row
    Large    // 100% — 1 per row
}

public static class WidgetSizeExtensions
{
    public static double ToFraction(this WidgetSize size) => size switch
    {
        WidgetSize.Tiny => 0.25,
        WidgetSize.Small => 1.0 / 3.0,
        WidgetSize.Medium => 0.5,
        WidgetSize.Large => 1.0,
        _ => 0.5
    };
}
