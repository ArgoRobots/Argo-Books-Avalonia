using ArgoBooks.Core.Models;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for managing global application settings.
/// </summary>
public interface IGlobalSettingsService
{
    /// <summary>
    /// Gets the current global settings.
    /// </summary>
    /// <returns>The global settings or null if not yet loaded.</returns>
    GlobalSettings? GetSettings();

    /// <summary>
    /// Saves the global settings.
    /// </summary>
    /// <param name="settings">Settings to save.</param>
    void SaveSettings(GlobalSettings settings);

    /// <summary>
    /// Loads settings from disk.
    /// </summary>
    /// <returns>The loaded settings.</returns>
    Task<GlobalSettings> LoadAsync();

    /// <summary>
    /// Saves settings to disk asynchronously.
    /// </summary>
    /// <param name="settings">Settings to save.</param>
    Task SaveAsync(GlobalSettings settings);

    /// <summary>
    /// Gets the list of recent companies.
    /// </summary>
    /// <returns>List of recent company file paths.</returns>
    IReadOnlyList<string> GetRecentCompanies();

    /// <summary>
    /// Adds a company to the recent list.
    /// </summary>
    /// <param name="filePath">Path to company file.</param>
    void AddRecentCompany(string filePath);

    /// <summary>
    /// Removes a company from the recent list.
    /// </summary>
    /// <param name="filePath">Path to company file to remove.</param>
    void RemoveRecentCompany(string filePath);
}

/// <summary>
/// Settings class for window state persistence.
/// </summary>
public class WindowStateSettings
{
    /// <summary>
    /// Window width in pixels.
    /// </summary>
    public double Width { get; set; } = 1280;

    /// <summary>
    /// Window height in pixels.
    /// </summary>
    public double Height { get; set; } = 800;

    /// <summary>
    /// Window left position in pixels.
    /// </summary>
    public double Left { get; set; } = -1;

    /// <summary>
    /// Window top position in pixels.
    /// </summary>
    public double Top { get; set; } = -1;

    /// <summary>
    /// Whether window is maximized.
    /// </summary>
    public bool IsMaximized { get; set; }
}
