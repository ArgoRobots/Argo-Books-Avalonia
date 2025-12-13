using ArgoBooks.Core.Models;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for managing application settings.
/// Handles both global settings (stored in AppData) and company settings (stored in .argo file).
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the global application settings.
    /// </summary>
    GlobalSettings GlobalSettings { get; }

    /// <summary>
    /// Gets the current company settings (null if no company is open).
    /// </summary>
    CompanySettings? CompanySettings { get; }

    /// <summary>
    /// Loads global settings from the AppData directory.
    /// </summary>
    Task LoadGlobalSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves global settings to the AppData directory.
    /// </summary>
    Task SaveGlobalSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads company settings from the temporary directory.
    /// </summary>
    /// <param name="tempDirectory">Path to the extracted company files</param>
    Task LoadCompanySettingsAsync(string tempDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves company settings to the temporary directory.
    /// </summary>
    /// <param name="tempDirectory">Path to the extracted company files</param>
    Task SaveCompanySettingsAsync(string tempDirectory, CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the current company settings (when closing a company).
    /// </summary>
    void ClearCompanySettings();

    /// <summary>
    /// Adds a company path to the recent companies list.
    /// </summary>
    /// <param name="filePath">Path to the .argo file</param>
    void AddRecentCompany(string filePath);

    /// <summary>
    /// Removes a company path from the recent companies list.
    /// </summary>
    /// <param name="filePath">Path to the .argo file</param>
    void RemoveRecentCompany(string filePath);

    /// <summary>
    /// Gets the application data directory path for the current platform.
    /// </summary>
    string GetAppDataPath();
}
