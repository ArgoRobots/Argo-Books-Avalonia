namespace ArgoBooks.Helpers;

/// <summary>
/// Helper for persisting column visibility settings across app restarts.
/// </summary>
public static class ColumnVisibilityHelper
{
    /// <summary>
    /// Loads a saved column visibility value, or returns the default if not found.
    /// </summary>
    public static bool Load(string pageName, string columnName, bool defaultValue)
    {
        var settings = App.SettingsService?.GlobalSettings?.Ui;
        if (settings == null)
            return defaultValue;

        if (settings.ColumnVisibility.TryGetValue(pageName, out var pageColumns) &&
            pageColumns.TryGetValue(columnName, out var isVisible))
        {
            return isVisible;
        }

        return defaultValue;
    }

    /// <summary>
    /// Saves a column visibility value and persists to disk.
    /// </summary>
    public static void Save(string pageName, string columnName, bool isVisible)
    {
        var settings = App.SettingsService?.GlobalSettings?.Ui;
        if (settings == null)
            return;

        if (!settings.ColumnVisibility.TryGetValue(pageName, out var pageColumns))
        {
            pageColumns = new Dictionary<string, bool>();
            settings.ColumnVisibility[pageName] = pageColumns;
        }

        // Only save to disk if value actually changed
        if (pageColumns.TryGetValue(columnName, out var existing) && existing == isVisible)
            return;

        pageColumns[columnName] = isVisible;
        _ = App.SettingsService!.SaveGlobalSettingsAsync();
    }
}
