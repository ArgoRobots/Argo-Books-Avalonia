using ArgoBooks.Data;

namespace ArgoBooks.Services;

/// <summary>
/// Service for handling timezone conversions and time formatting.
/// </summary>
public static class TimeZoneService
{
    /// <summary>
    /// Event raised when the timezone or time format setting changes.
    /// </summary>
    public static event EventHandler? TimeSettingsChanged;

    /// <summary>
    /// Raises the TimeSettingsChanged event to notify subscribers.
    /// </summary>
    public static void NotifyTimeSettingsChanged()
    {
        TimeSettingsChanged?.Invoke(null, EventArgs.Empty);
    }

    /// <summary>
    /// Gets the user's selected timezone from global settings.
    /// </summary>
    public static string GetUserTimeZone()
    {
        return App.SettingsService?.GlobalSettings?.Ui.TimeZone ?? "UTC";
    }

    /// <summary>
    /// Gets the user's selected time format from global settings.
    /// </summary>
    /// <returns>"12h" for 12-hour format, "24h" for 24-hour format.</returns>
    public static string GetUserTimeFormat()
    {
        return App.SettingsService?.GlobalSettings?.Ui.TimeFormat ?? "12h";
    }

    /// <summary>
    /// Returns true if the user prefers 24-hour time format.
    /// </summary>
    public static bool Is24HourFormat => GetUserTimeFormat() == "24h";

    /// <summary>
    /// Converts a UTC DateTime to the user's selected timezone.
    /// </summary>
    public static DateTime ConvertToUserTimeZone(DateTime utcDateTime)
    {
        var timeZoneId = GetUserTimeZone();
        return ConvertToTimeZone(utcDateTime, timeZoneId);
    }

    /// <summary>
    /// Converts a UTC DateTime to the specified timezone.
    /// </summary>
    public static DateTime ConvertToTimeZone(DateTime utcDateTime, string timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId) || timeZoneId == "UTC")
        {
            return utcDateTime;
        }

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
        }
        catch (TimeZoneNotFoundException)
        {
            // If timezone not found, try common IANA to Windows mappings
            // This handles legacy settings that might have IANA identifiers on Windows
            var windowsTimeZone = MapIanaToWindows(timeZoneId);
            if (windowsTimeZone != null)
            {
                try
                {
                    var timeZone = TimeZoneInfo.FindSystemTimeZoneById(windowsTimeZone);
                    return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZone);
                }
                catch
                {
                    // Fall back to UTC
                    return utcDateTime;
                }
            }
            return utcDateTime;
        }
        catch
        {
            // Fall back to UTC on any error
            return utcDateTime;
        }
    }

    /// <summary>
    /// Formats a time using the user's preferred time format (12h or 24h).
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>Formatted time string (e.g., "2:30 PM" or "14:30").</returns>
    public static string FormatTime(DateTime dateTime)
    {
        return Is24HourFormat
            ? dateTime.ToString("HH:mm")
            : dateTime.ToString("h:mm tt");
    }

    /// <summary>
    /// Formats a time with seconds using the user's preferred time format (12h or 24h).
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>Formatted time string with seconds (e.g., "2:30:45 PM" or "14:30:45").</returns>
    public static string FormatTimeWithSeconds(DateTime dateTime)
    {
        return Is24HourFormat
            ? dateTime.ToString("HH:mm:ss")
            : dateTime.ToString("h:mm:ss tt");
    }

    /// <summary>
    /// Gets the time format pattern for the user's preference.
    /// </summary>
    /// <returns>Format pattern string (e.g., "HH:mm" or "h:mm tt").</returns>
    public static string GetTimeFormatPattern()
    {
        return Is24HourFormat ? "HH:mm" : "h:mm tt";
    }

    /// <summary>
    /// Formats a date and time using the user's preferred time format (12h or 24h).
    /// </summary>
    /// <param name="dateTime">The DateTime to format.</param>
    /// <returns>Formatted date and time string (e.g., "Jan 5, 2024 at 2:30 PM" or "Jan 5, 2024 at 14:30").</returns>
    public static string FormatDateTime(DateTime dateTime)
    {
        var timeFormat = Is24HourFormat ? "HH:mm" : "h:mm tt";
        return dateTime.ToString($"MMM d, yyyy 'at' {timeFormat}");
    }

    /// <summary>
    /// Maps common IANA timezone IDs to Windows timezone IDs.
    /// Used for backwards compatibility with legacy settings.
    /// </summary>
    private static string? MapIanaToWindows(string ianaTimeZone)
    {
        // Common mappings for Windows systems that don't support IANA identifiers
        // This handles legacy settings that might have been saved with IANA IDs
        return ianaTimeZone switch
        {
            "America/New_York" => "Eastern Standard Time",
            "America/Chicago" => "Central Standard Time",
            "America/Denver" => "Mountain Standard Time",
            "America/Los_Angeles" => "Pacific Standard Time",
            "America/Toronto" => "Eastern Standard Time",
            "America/Vancouver" => "Pacific Standard Time",
            "Europe/London" => "GMT Standard Time",
            "Europe/Paris" => "Romance Standard Time",
            "Europe/Berlin" => "W. Europe Standard Time",
            "Europe/Amsterdam" => "W. Europe Standard Time",
            "Asia/Tokyo" => "Tokyo Standard Time",
            "Asia/Shanghai" => "China Standard Time",
            "Asia/Singapore" => "Singapore Standard Time",
            "Asia/Dubai" => "Arabian Standard Time",
            "Australia/Sydney" => "AUS Eastern Standard Time",
            "Australia/Melbourne" => "AUS Eastern Standard Time",
            "Pacific/Auckland" => "New Zealand Standard Time",
            _ => null
        };
    }

    /// <summary>
    /// Gets the display name for a timezone using the TimeZones data class.
    /// </summary>
    public static string GetTimeZoneDisplayName(string timeZoneId)
    {
        if (string.IsNullOrEmpty(timeZoneId) || timeZoneId == "UTC")
        {
            return "(UTC+00:00) UTC";
        }

        var tzItem = TimeZones.FindById(timeZoneId);
        return tzItem.DisplayName;
    }
}
