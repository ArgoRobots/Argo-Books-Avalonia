using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.Services;

/// <summary>
/// Service for managing shared chart settings between Dashboard and Analytics pages.
/// Settings are persisted per-company to global settings and synchronized across pages.
/// </summary>
public partial class ChartSettingsService : ObservableObject
{
    private static readonly Lock Lock = new();

    private readonly IGlobalSettingsService? _globalSettingsService;
    private bool _isInitialized;
    private string? _currentCompanyPath;
    private bool _isLoading;

    /// <summary>
    /// Gets the singleton instance of the ChartSettingsService.
    /// </summary>
    public static ChartSettingsService Instance
    {
        get
        {
            if (field == null)
            {
                lock (Lock)
                {
                    field ??= new ChartSettingsService();
                }
            }
            return field;
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

    /// <summary>
    /// Gets the label for comparison period based on selected date range.
    /// </summary>
    public string ComparisonPeriodLabel => SelectedDateRange switch
    {
        "This Month" => "from last month",
        "Last Month" => "from prior month",
        "Last 30 Days" => "from prior 30 days",
        "Last 100 Days" => "from prior 100 days",
        "Last 365 Days" => "from prior 365 days",
        "This Quarter" => "from last quarter",
        "Last Quarter" => "from prior quarter",
        "This Year" => "from last year",
        "Last Year" => "from prior year",
        "All Time" => "",
        "Custom Range" => "from prior period",
        _ => "from last period"
    };

    private ChartSettingsService()
    {
        // Try to get the global settings service from the app
        _globalSettingsService = App.SettingsService;
    }

    /// <summary>
    /// Initializes the service by loading settings from global settings.
    /// Called from ViewModel constructors; only runs once as a fallback
    /// if LoadForCompany hasn't been called yet.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized) return;

        LoadFromGlobalSettings();
        _isInitialized = true;
    }

    /// <summary>
    /// Loads chart settings for a specific company. Resets to defaults first,
    /// then loads saved per-company preferences if they exist.
    /// Should be called when a company is opened.
    /// </summary>
    /// <param name="companyPath">File path of the company being opened.</param>
    public void LoadForCompany(string? companyPath)
    {
        _currentCompanyPath = companyPath;

        _isLoading = true;
        try
        {
            // Reset to defaults before loading company-specific settings
            SelectedChartType = "Line";
            SelectedDateRange = "This Month";
            HasAppliedCustomRange = false;
            UpdateDateRangeFromSelection();

            // Load company-specific settings (or keep defaults if none found)
            LoadFromGlobalSettings();
        }
        finally
        {
            _isLoading = false;
        }

        _isInitialized = true;

        // Notify all computed properties after load
        OnPropertyChanged(nameof(AppliedDateRangeText));
        OnPropertyChanged(nameof(ComparisonPeriodLabel));
    }

    /// <summary>
    /// Loads chart settings from global settings.
    /// Uses per-company settings if a company path is set and has saved preferences,
    /// otherwise uses defaults.
    /// </summary>
    private void LoadFromGlobalSettings()
    {
        var settings = _globalSettingsService?.GetSettings();
        if (settings == null) return;

        // Try to load company-specific settings first
        ChartSettings? chartSettings = null;
        if (!string.IsNullOrEmpty(_currentCompanyPath) &&
            settings.Ui.CompanyChartSettings.TryGetValue(_currentCompanyPath, out var companyChart))
        {
            chartSettings = companyChart;
        }

        // If no company-specific settings found, keep current defaults
        if (chartSettings == null) return;

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

        // Notify computed property changed
        OnPropertyChanged(nameof(AppliedDateRangeText));
    }

    /// <summary>
    /// Saves chart settings to global settings, keyed by current company path.
    /// </summary>
    private void SaveToGlobalSettings()
    {
        if (_isLoading) return;

        var settings = _globalSettingsService?.GetSettings();
        if (settings == null) return;

        var chartData = new ChartSettings
        {
            ChartType = SelectedChartType,
            DateRange = SelectedDateRange,
            MaxPieSlices = settings.Ui.Chart.MaxPieSlices
        };

        if (HasAppliedCustomRange && SelectedDateRange == "Custom Range")
        {
            chartData.CustomStartDate = StartDate;
            chartData.CustomEndDate = EndDate;
        }

        // Save to company-specific settings if we have a company path
        if (!string.IsNullOrEmpty(_currentCompanyPath))
        {
            settings.Ui.CompanyChartSettings[_currentCompanyPath] = chartData;
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
        OnPropertyChanged(nameof(ComparisonPeriodLabel));

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

            case "Last 30 Days":
                StartDate = now.AddDays(-29).Date;
                EndDate = now;
                break;

            case "Last 100 Days":
                StartDate = now.AddDays(-99).Date;
                EndDate = now;
                break;

            case "Last 365 Days":
                StartDate = now.AddDays(-364).Date;
                EndDate = now;
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
