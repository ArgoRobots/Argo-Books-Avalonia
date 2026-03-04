namespace ArgoBooks.Core.Enums;

/// <summary>
/// Standard date range presets used in chart settings and date filters.
/// </summary>
public enum DateRangePreset
{
    ThisMonth,
    LastMonth,
    Last30Days,
    Last100Days,
    Last365Days,
    ThisQuarter,
    LastQuarter,
    ThisYear,
    LastYear,
    AllTime,
    CustomRange
}

/// <summary>
/// Extension methods for DateRangePreset.
/// </summary>
public static class DateRangePresetExtensions
{
    /// <summary>
    /// Gets the display name for a date range preset (used in UI and serialization).
    /// </summary>
    public static string GetDisplayName(this DateRangePreset preset)
    {
        return preset switch
        {
            DateRangePreset.ThisMonth => "This Month",
            DateRangePreset.LastMonth => "Last Month",
            DateRangePreset.Last30Days => "Last 30 Days",
            DateRangePreset.Last100Days => "Last 100 Days",
            DateRangePreset.Last365Days => "Last 365 Days",
            DateRangePreset.ThisQuarter => "This Quarter",
            DateRangePreset.LastQuarter => "Last Quarter",
            DateRangePreset.ThisYear => "This Year",
            DateRangePreset.LastYear => "Last Year",
            DateRangePreset.AllTime => "All Time",
            DateRangePreset.CustomRange => "Custom Range",
            _ => preset.ToString()
        };
    }

    /// <summary>
    /// Parses a display name string to a DateRangePreset enum value.
    /// </summary>
    public static DateRangePreset? ParseDateRange(string? displayName)
    {
        return displayName switch
        {
            "This Month" => DateRangePreset.ThisMonth,
            "Last Month" => DateRangePreset.LastMonth,
            "Last 30 Days" => DateRangePreset.Last30Days,
            "Last 100 Days" => DateRangePreset.Last100Days,
            "Last 365 Days" => DateRangePreset.Last365Days,
            "This Quarter" => DateRangePreset.ThisQuarter,
            "Last Quarter" => DateRangePreset.LastQuarter,
            "This Year" => DateRangePreset.ThisYear,
            "Last Year" => DateRangePreset.LastYear,
            "All Time" => DateRangePreset.AllTime,
            "Custom Range" => DateRangePreset.CustomRange,
            _ => null
        };
    }

    /// <summary>
    /// Gets the comparison period label for a date range preset.
    /// </summary>
    public static string GetComparisonPeriodLabel(this DateRangePreset preset)
    {
        return preset switch
        {
            DateRangePreset.ThisMonth => "from last month",
            DateRangePreset.LastMonth => "from prior month",
            DateRangePreset.Last30Days => "from prior 30 days",
            DateRangePreset.Last100Days => "from prior 100 days",
            DateRangePreset.Last365Days => "from prior 365 days",
            DateRangePreset.ThisQuarter => "from last quarter",
            DateRangePreset.LastQuarter => "from prior quarter",
            DateRangePreset.ThisYear => "from last year",
            DateRangePreset.LastYear => "from prior year",
            DateRangePreset.AllTime => "",
            DateRangePreset.CustomRange => "from prior period",
            _ => "from last period"
        };
    }

    /// <summary>
    /// Gets all standard date range preset display names for UI dropdowns.
    /// </summary>
    public static string[] GetStandardOptions()
    {
        return
        [
            DateRangePreset.ThisMonth.GetDisplayName(),
            DateRangePreset.LastMonth.GetDisplayName(),
            DateRangePreset.Last30Days.GetDisplayName(),
            DateRangePreset.Last100Days.GetDisplayName(),
            DateRangePreset.Last365Days.GetDisplayName(),
            DateRangePreset.ThisQuarter.GetDisplayName(),
            DateRangePreset.LastQuarter.GetDisplayName(),
            DateRangePreset.ThisYear.GetDisplayName(),
            DateRangePreset.LastYear.GetDisplayName(),
            DateRangePreset.AllTime.GetDisplayName(),
            DateRangePreset.CustomRange.GetDisplayName()
        ];
    }
}
