using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// Displays a <see cref="ViewModels.MonthlyMatchCalendar"/> as a 12-month, color-coded status grid
/// with year navigation and a legend. Reused for both the bank-lines and books sides of bank matching.
/// </summary>
public partial class MonthCalendarView : UserControl
{
    public MonthCalendarView()
    {
        InitializeComponent();
    }
}
