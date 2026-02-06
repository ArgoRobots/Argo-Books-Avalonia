using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable empty state placeholder for charts. Shows a context-aware message:
/// either the default message (no data at all) or a date range message (data exists
/// but not in the selected range).
/// </summary>
public partial class ChartEmptyState : UserControl
{
    public static readonly StyledProperty<string> DefaultMessageProperty =
        AvaloniaProperty.Register<ChartEmptyState, string>(nameof(DefaultMessage), "No data available");

    public static readonly StyledProperty<bool> ShowDateRangeMessageProperty =
        AvaloniaProperty.Register<ChartEmptyState, bool>(nameof(ShowDateRangeMessage));

    /// <summary>
    /// Gets or sets the default message shown when no data exists at all.
    /// </summary>
    public string DefaultMessage
    {
        get => GetValue(DefaultMessageProperty);
        set => SetValue(DefaultMessageProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show the date range message instead of the default.
    /// When true, shows "No data available in the selected date range".
    /// </summary>
    public bool ShowDateRangeMessage
    {
        get => GetValue(ShowDateRangeMessageProperty);
        set => SetValue(ShowDateRangeMessageProperty, value);
    }

    public ChartEmptyState()
    {
        InitializeComponent();
    }
}
