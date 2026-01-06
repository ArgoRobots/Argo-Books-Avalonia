using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System.Collections.ObjectModel;

namespace ArgoBooks.Controls;

/// <summary>
/// Represents an item in the pie chart legend.
/// </summary>
public class PieLegendItem
{
    /// <summary>
    /// The display label for the legend item.
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// The value for this item.
    /// </summary>
    public double Value { get; set; }

    /// <summary>
    /// The percentage this item represents of the total.
    /// </summary>
    public double Percentage { get; set; }

    /// <summary>
    /// The color brush for the legend indicator.
    /// </summary>
    public IBrush? Color { get; set; }

    /// <summary>
    /// The hex color string.
    /// </summary>
    public string ColorHex { get; set; } = "#6495ED";

    /// <summary>
    /// Formatted value display string.
    /// </summary>
    public string FormattedValue => $"{Value:N0}";

    /// <summary>
    /// Formatted percentage display string.
    /// </summary>
    public string FormattedPercentage => $"{Percentage:F1}%";
}

/// <summary>
/// A custom scrollable legend component for pie charts.
/// </summary>
public partial class PieChartLegend : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<ObservableCollection<PieLegendItem>?> ItemsProperty =
        AvaloniaProperty.Register<PieChartLegend, ObservableCollection<PieLegendItem>?>(nameof(Items));

    public static readonly StyledProperty<double> MaxHeightOverrideProperty =
        AvaloniaProperty.Register<PieChartLegend, double>(nameof(MaxHeightOverride), 200);

    public static readonly StyledProperty<bool> ShowPercentageProperty =
        AvaloniaProperty.Register<PieChartLegend, bool>(nameof(ShowPercentage), true);

    public static readonly StyledProperty<bool> ShowValueProperty =
        AvaloniaProperty.Register<PieChartLegend, bool>(nameof(ShowValue), false);

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the legend items collection.
    /// </summary>
    public ObservableCollection<PieLegendItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    /// <summary>
    /// Gets or sets the maximum height for the legend scroll area.
    /// </summary>
    public double MaxHeightOverride
    {
        get => GetValue(MaxHeightOverrideProperty);
        set => SetValue(MaxHeightOverrideProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the percentage for each item.
    /// </summary>
    public bool ShowPercentage
    {
        get => GetValue(ShowPercentageProperty);
        set => SetValue(ShowPercentageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the value for each item.
    /// </summary>
    public bool ShowValue
    {
        get => GetValue(ShowValueProperty);
        set => SetValue(ShowValueProperty, value);
    }

    #endregion

    public PieChartLegend()
    {
        InitializeComponent();
    }
}
