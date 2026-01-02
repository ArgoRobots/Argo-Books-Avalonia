using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable date range selector control.
/// Binds to DataContext properties: DateRangeOptions, SelectedDateRange, IsCustomDateRange, StartDate, EndDate.
/// </summary>
public partial class DateRangeSelector : UserControl
{
    public DateRangeSelector()
    {
        InitializeComponent();
    }
}
