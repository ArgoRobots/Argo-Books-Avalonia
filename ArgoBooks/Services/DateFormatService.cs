namespace ArgoBooks.Services;

/// <summary>
/// Service for formatting dates based on user settings.
/// </summary>
public static class DateFormatService
{
    /// <summary>
    /// Gets the current date format setting from company settings.
    /// </summary>
    private static string CurrentFormat =>
        App.CompanyManager?.CompanyData?.Settings?.Localization?.DateFormat ?? "MM/DD/YYYY";

    /// <summary>
    /// Converts the user-friendly format string to a .NET format string.
    /// </summary>
    private static string GetDotNetFormat(string userFormat) => userFormat switch
    {
        "MM/DD/YYYY" => "MM/dd/yyyy",
        "DD/MM/YYYY" => "dd/MM/yyyy",
        "YYYY-MM-DD" => "yyyy-MM-dd",
        "MMM D, YYYY" => "MMM d, yyyy",
        _ => "MM/dd/yyyy"
    };

    /// <summary>
    /// Formats a date using the current date format setting.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>The formatted date string.</returns>
    public static string Format(DateTime date)
    {
        return date.ToString(GetDotNetFormat(CurrentFormat));
    }

    /// <summary>
    /// Formats a date using the current date format setting.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>The formatted date string, or empty if null.</returns>
    public static string Format(DateTime? date)
    {
        return date?.ToString(GetDotNetFormat(CurrentFormat)) ?? string.Empty;
    }

    /// <summary>
    /// Gets the .NET format string for the current date format setting.
    /// Useful for chart axis labelers.
    /// </summary>
    public static string GetCurrentDotNetFormat() => GetDotNetFormat(CurrentFormat);

    /// <summary>
    /// Formats a month/year for chart labels (always uses "MMM yyyy" format).
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>The formatted month/year string.</returns>
    public static string FormatMonthYear(DateTime date)
    {
        return date.ToString("MMM yyyy");
    }
}
