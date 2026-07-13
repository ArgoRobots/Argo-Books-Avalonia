namespace ArgoBooks.Mobile.ViewModels;

/// <summary>A single KPI stat card shown at the top of an Analytics tab.</summary>
public sealed class AnalyticsKpiViewModel
{
    public string Label { get; }

    public string Value { get; }

    public AnalyticsKpiViewModel(string label, string value)
    {
        Label = label;
        Value = value;
    }
}
