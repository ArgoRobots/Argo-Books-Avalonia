using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels;

/// <summary>
/// ViewModel for the Analytics page.
/// Handles tab selection, date range filtering, and chart type toggling.
/// </summary>
public partial class AnalyticsPageViewModel : ViewModelBase
{
    #region Tab Selection

    [ObservableProperty]
    private int _selectedTabIndex;

    /// <summary>
    /// Gets whether the Dashboard tab is selected.
    /// </summary>
    public bool IsDashboardTabSelected => SelectedTabIndex == 0;

    /// <summary>
    /// Gets whether the Geographic tab is selected.
    /// </summary>
    public bool IsGeographicTabSelected => SelectedTabIndex == 1;

    /// <summary>
    /// Gets whether the Operational tab is selected.
    /// </summary>
    public bool IsOperationalTabSelected => SelectedTabIndex == 2;

    /// <summary>
    /// Gets whether the Performance tab is selected.
    /// </summary>
    public bool IsPerformanceTabSelected => SelectedTabIndex == 3;

    /// <summary>
    /// Gets whether the Customers tab is selected.
    /// </summary>
    public bool IsCustomersTabSelected => SelectedTabIndex == 4;

    /// <summary>
    /// Gets whether the Returns tab is selected.
    /// </summary>
    public bool IsReturnsTabSelected => SelectedTabIndex == 5;

    /// <summary>
    /// Gets whether the Losses tab is selected.
    /// </summary>
    public bool IsLossesTabSelected => SelectedTabIndex == 6;

    partial void OnSelectedTabIndexChanged(int value)
    {
        OnPropertyChanged(nameof(IsDashboardTabSelected));
        OnPropertyChanged(nameof(IsGeographicTabSelected));
        OnPropertyChanged(nameof(IsOperationalTabSelected));
        OnPropertyChanged(nameof(IsPerformanceTabSelected));
        OnPropertyChanged(nameof(IsCustomersTabSelected));
        OnPropertyChanged(nameof(IsReturnsTabSelected));
        OnPropertyChanged(nameof(IsLossesTabSelected));
    }

    #endregion

    #region Date Range

    /// <summary>
    /// Available date range options.
    /// </summary>
    public ObservableCollection<string> DateRangeOptions { get; } =
    [
        "This Month",
        "Last Month",
        "This Quarter",
        "Last Quarter",
        "This Year",
        "Last Year",
        "All Time",
        "Custom Range"
    ];

    [ObservableProperty]
    private string _selectedDateRange = "This Month";

    [ObservableProperty]
    private DateTime _startDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now;

    /// <summary>
    /// Gets whether the custom date range option is selected.
    /// </summary>
    public bool IsCustomDateRange => SelectedDateRange == "Custom Range";

    partial void OnSelectedDateRangeChanged(string value)
    {
        OnPropertyChanged(nameof(IsCustomDateRange));
        UpdateDateRangeFromSelection();
    }

    /// <summary>
    /// Updates the start and end dates based on the selected date range option.
    /// </summary>
    private void UpdateDateRangeFromSelection()
    {
        var now = DateTime.Now;

        switch (SelectedDateRange)
        {
            case "This Month":
                StartDate = new DateTime(now.Year, now.Month, 1);
                EndDate = now;
                break;

            case "Last Month":
                var lastMonth = now.AddMonths(-1);
                StartDate = new DateTime(lastMonth.Year, lastMonth.Month, 1);
                EndDate = new DateTime(lastMonth.Year, lastMonth.Month, DateTime.DaysInMonth(lastMonth.Year, lastMonth.Month));
                break;

            case "This Quarter":
                var quarterStart = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1);
                StartDate = quarterStart;
                EndDate = now;
                break;

            case "Last Quarter":
                var lastQuarterEnd = new DateTime(now.Year, ((now.Month - 1) / 3) * 3 + 1, 1).AddDays(-1);
                var lastQuarterStart = lastQuarterEnd.AddMonths(-2);
                lastQuarterStart = new DateTime(lastQuarterStart.Year, lastQuarterStart.Month, 1);
                StartDate = lastQuarterStart;
                EndDate = lastQuarterEnd;
                break;

            case "This Year":
                StartDate = new DateTime(now.Year, 1, 1);
                EndDate = now;
                break;

            case "Last Year":
                StartDate = new DateTime(now.Year - 1, 1, 1);
                EndDate = new DateTime(now.Year - 1, 12, 31);
                break;

            case "All Time":
                StartDate = new DateTime(2000, 1, 1);
                EndDate = now;
                break;

            case "Custom Range":
                // Keep current values, let user modify
                break;
        }
    }

    #endregion

    #region Chart Type Toggle

    [ObservableProperty]
    private bool _useLineChart = true;

    #endregion

    #region Map Mode Toggle

    [ObservableProperty]
    private bool _isMapModeOrigin = true;

    /// <summary>
    /// Gets or sets whether the map mode is Destination.
    /// </summary>
    public bool IsMapModeDestination
    {
        get => !IsMapModeOrigin;
        set
        {
            if (value != !IsMapModeOrigin)
            {
                IsMapModeOrigin = !value;
            }
        }
    }

    partial void OnIsMapModeOriginChanged(bool value)
    {
        OnPropertyChanged(nameof(IsMapModeDestination));
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Default constructor.
    /// </summary>
    public AnalyticsPageViewModel()
    {
        // Initialize with default values
        UpdateDateRangeFromSelection();
    }

    #endregion
}
