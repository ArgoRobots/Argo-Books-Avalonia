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
    /// <returns>List of recent company info.</returns>
    IReadOnlyList<RecentCompanyInfo> GetRecentCompanies();

    /// <summary>
    /// Adds a company to the recent list.
    /// </summary>
    /// <param name="name">Company name.</param>
    /// <param name="filePath">Path to company file.</param>
    void AddRecentCompany(string name, string filePath);

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

/// <summary>
/// Global application settings including window state.
/// </summary>
public class GlobalSettings
{
    /// <summary>
    /// Saved window state.
    /// </summary>
    public WindowStateSettings? WindowState { get; set; }

    /// <summary>
    /// List of recently opened companies.
    /// </summary>
    public List<RecentCompanyInfo>? RecentCompanies { get; set; }

    /// <summary>
    /// Application theme (System, Light, Dark).
    /// </summary>
    public string? Theme { get; set; } = "System";

    /// <summary>
    /// Application language/locale.
    /// </summary>
    public string? Language { get; set; } = "en-US";

    /// <summary>
    /// Whether auto-save is enabled.
    /// </summary>
    public bool AutoSaveEnabled { get; set; } = true;

    /// <summary>
    /// Auto-save interval in seconds.
    /// </summary>
    public int AutoSaveIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Whether to check for updates on startup.
    /// </summary>
    public bool CheckForUpdates { get; set; } = true;

    /// <summary>
    /// Maximum number of recent companies to remember.
    /// </summary>
    public int MaxRecentCompanies { get; set; } = 10;
}

/// <summary>
/// Information about a recently opened company.
/// </summary>
public class RecentCompanyInfo
{
    /// <summary>
    /// Company name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Full path to the company file.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// When the company was last opened.
    /// </summary>
    public DateTime LastOpened { get; set; }
}
