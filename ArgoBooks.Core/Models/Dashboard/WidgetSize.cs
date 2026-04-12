namespace ArgoBooks.Core.Models.Dashboard;

public enum WidgetSize
{
    Tiny,      // 25%
    Small,     // 33%
    Medium,    // 50%
    MedLarge,  // 75%
    Large      // 100%
}

public static class WidgetSizeExtensions
{
    public static double ToFraction(this WidgetSize size) => size switch
    {
        WidgetSize.Tiny => 0.25,
        WidgetSize.Small => 1.0 / 3.0,
        WidgetSize.Medium => 0.5,
        WidgetSize.MedLarge => 0.75,
        WidgetSize.Large => 1.0,
        _ => 0.5
    };
}
