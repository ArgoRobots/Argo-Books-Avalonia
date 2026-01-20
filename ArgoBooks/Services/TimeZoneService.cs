using ArgoBooks.Data;

namespace ArgoBooks.Services;

/// <summary>
/// Service for handling timezone conversions.
/// </summary>
public static class TimeZoneService
{
    /// <summary>
    /// Gets the user's selected timezone from global settings.
    /// </summary>
    public static string GetUserTimeZone()
    {
        return App.SettingsService?.GlobalSettings?.Ui.TimeZone ?? "UTC";
    }

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
