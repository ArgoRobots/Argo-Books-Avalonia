using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Services;

/// <summary>
/// Service for managing shared chart settings between Dashboard and Analytics pages.
/// Settings are persisted to global settings and synchronized across pages.
/// </summary>
public partial class ChartSettingsService : ObservableObject
{
    private static ChartSettingsService? _instance;
    private static readonly object _lock = new();

    private readonly IGlobalSettingsService? _globalSettingsService;
    private bool _isInitialized;

    /// <summary>
    /// Gets the singleton instance of the ChartSettingsService.
    /// </summary>
    public static ChartSettingsService Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    _instance ??= new ChartSettingsService();
                }
            }
            return _instance;
        }
    }

    /// <summary>
    /// Available chart type options.
    /// </summary>
    public string[] ChartTypeOptions { get; } = ["Line", "Column", "Step Line", "Area", "Scatter"];

    /// <summary>
    /// Available date range options.
    /// </summary>
    public string[] DateRangeOptions { get; } = DatePresetNames.StandardDateRangeOptions;

    [ObservableProperty]
    private string _selectedChartType = "Line";

    [ObservableProperty]
    private string _selectedDateRange = "This Month";

    [ObservableProperty]
    private DateTime _startDate = new(DateTime.Now.Year, DateTime.Now.Month, 1);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Now;

    [ObservableProperty]
    private bool _hasAppliedCustomRange;

    /// <summary>
    /// Gets the formatted text showing the applied custom date range.
    /// </summary>
    public string AppliedDateRangeText => HasAppliedCustomRange
        ? $"{StartDate:MMM d, yyyy} - {EndDate:MMM d, yyyy}"
        : string.Empty;

    private ChartSettingsService()
    {
        // Try to get the global settings service from the app
        _globalSettingsService = App.SettingsService;
    }

    /// <summary>
    /// Initializes the service by loading settings from global settings.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;

        LoadFromGlobalSettings();
        _isInitialized = true;
    }

    /// <summary>
    /// Loads chart settings from global settings.
    /// </summary>
    private void LoadFromGlobalSettings()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings?.Ui.Chart == null) return;

        var chartSettings = settings.Ui.Chart;

        // Load chart type
        if (!string.IsNullOrEmpty(chartSettings.ChartType) &&
            ChartTypeOptions.Contains(chartSettings.ChartType))
        {
            SelectedChartType = chartSettings.ChartType;
        }

        // Load date range
        if (!string.IsNullOrEmpty(chartSettings.DateRange))
        {
            if (chartSettings.DateRange == "Custom Range" &&
                chartSettings.CustomStartDate.HasValue &&
                chartSettings.CustomEndDate.HasValue)
            {
                SelectedDateRange = chartSettings.DateRange;
                StartDate = chartSettings.CustomStartDate.Value;
                EndDate = chartSettings.CustomEndDate.Value;
                HasAppliedCustomRange = true;
            }
            else if (DateRangeOptions.Contains(chartSettings.DateRange))
            {
                SelectedDateRange = chartSettings.DateRange;
                UpdateDateRangeFromSelection();
            }
        }
        else
        {
            UpdateDateRangeFromSelection();
        }

        // Notify computed property changed
        OnPropertyChanged(nameof(AppliedDateRangeText));
    }

    /// <summary>
    /// Saves chart settings to global settings.
    /// </summary>
    private void SaveToGlobalSettings()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings == null) return;

        settings.Ui.Chart.ChartType = SelectedChartType;
        settings.Ui.Chart.DateRange = SelectedDateRange;

        if (HasAppliedCustomRange && SelectedDateRange == "Custom Range")
        {
            settings.Ui.Chart.CustomStartDate = StartDate;
            settings.Ui.Chart.CustomEndDate = EndDate;
        }
        else
        {
            settings.Ui.Chart.CustomStartDate = null;
            settings.Ui.Chart.CustomEndDate = null;
        }

        _globalSettingsService?.SaveSettings(settings);
    }

    partial void OnSelectedChartTypeChanged(string value)
    {
        SaveToGlobalSettings();
        ChartTypeChanged?.Invoke(this, value);
    }

    partial void OnSelectedDateRangeChanged(string value)
    {
        OnPropertyChanged(nameof(AppliedDateRangeText));

        if (value != "Custom Range")
        {
            HasAppliedCustomRange = false;
            UpdateDateRangeFromSelection();
        }

        SaveToGlobalSettings();
        DateRangeChanged?.Invoke(this, value);
    }

    partial void OnStartDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(AppliedDateRangeText));
    }

    partial void OnEndDateChanged(DateTime value)
    {
        OnPropertyChanged(nameof(AppliedDateRangeText));
    }

    partial void OnHasAppliedCustomRangeChanged(bool value)
    {
        OnPropertyChanged(nameof(AppliedDateRangeText));
    }

    /// <summary>
    /// Applies a custom date range.
    /// </summary>
    public void ApplyCustomDateRange(DateTime startDate, DateTime endDate)
    {
        StartDate = startDate;
        EndDate = endDate;
        HasAppliedCustomRange = true;
        SelectedDateRange = "Custom Range";
        SaveToGlobalSettings();
        DateRangeChanged?.Invoke(this, "Custom Range");
    }

    /// <summary>
    /// Updates the start and end dates based on the selected date range option.
    /// </summary>
    public void UpdateDateRangeFromSelection()
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
                // Keep current values
                break;
        }
    }

    /// <summary>
    /// Event raised when the chart type changes.
    /// </summary>
    public event EventHandler<string>? ChartTypeChanged;

    /// <summary>
    /// Event raised when the date range changes.
    /// </summary>
    public event EventHandler<string>? DateRangeChanged;

    /// <summary>
    /// Static event raised when max pie slices setting changes.
    /// </summary>
    public static event EventHandler? MaxPieSlicesChanged;

    /// <summary>
    /// Notifies that the max pie slices setting has changed.
    /// </summary>
    public static void NotifyMaxPieSlicesChanged()
    {
        MaxPieSlicesChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the current max pie slices setting from global settings.
    /// </summary>
    public static int GetMaxPieSlices()
    {
        var settings = App.SettingsService?.GlobalSettings;
        return settings?.Ui.Chart.MaxPieSlices ?? 6;
    }
}
