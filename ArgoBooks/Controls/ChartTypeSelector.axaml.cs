using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable chart type selector control.
/// Binds to DataContext properties: ChartTypeOptions, SelectedChartType.
/// </summary>
public partial class ChartTypeSelector : UserControl
{
    public ChartTypeSelector()
    {
        InitializeComponent();
    }
}
