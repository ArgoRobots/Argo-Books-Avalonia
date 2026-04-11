namespace ArgoBooks.Core.Models.Dashboard;

public class DashboardRow
{
    public List<DashboardWidgetEntry> Widgets { get; set; } = [];

    public DashboardRow() { }

    public DashboardRow(params DashboardWidgetEntry[] widgets)
    {
        Widgets = [..widgets];
    }

    public double TotalFraction => Widgets.Sum(w => w.Size.ToFraction());

    public bool CanFit(WidgetSize size) => TotalFraction + size.ToFraction() <= 1.001;

    public DashboardRow Clone() => new()
    {
        Widgets = Widgets.Select(w => w.Clone()).ToList()
    };
}
