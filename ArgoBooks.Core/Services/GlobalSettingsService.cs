using System.Text.Json;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Platform;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for managing global and company settings.
/// </summary>
public class GlobalSettingsService : ISettingsService, IGlobalSettingsService
{
    private const string GlobalSettingsFileName = "settings.json";
    private const string CompanySettingsFileName = "settings.json";

    private readonly IPlatformService _platformService;
    private readonly JsonSerializerOptions _jsonOptions;

    private GlobalSettings _globalSettings = new();
    private CompanySettings? _companySettings;

    /// <summary>
    /// Initializes a new instance of the GlobalSettingsService.
    /// </summary>
    public GlobalSettingsService() : this(PlatformServiceFactory.GetPlatformService())
    {
    }

    /// <summary>
    /// Initializes a new instance of the GlobalSettingsService with a specific platform service.
    /// </summary>
    /// <param name="platformService">Platform service for file paths.</param>
    public GlobalSettingsService(IPlatformService platformService)
    {
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };
    }

    /// <inheritdoc />
    public GlobalSettings GlobalSettings => _globalSettings;

    /// <inheritdoc />
    public CompanySettings? CompanySettings => _companySettings;

    /// <inheritdoc />
    public async Task LoadGlobalSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settingsPath = GetGlobalSettingsPath();
        Console.WriteLine($"[GlobalSettingsService] LoadGlobalSettingsAsync - Path: {settingsPath}");

        if (!File.Exists(settingsPath))
        {
            Console.WriteLine("[GlobalSettingsService] Settings file does not exist, using defaults");
            _globalSettings = new GlobalSettings();
            return;
        }

        try
        {
            Console.WriteLine("[GlobalSettingsService] Reading settings file...");
            await using var fileStream = File.OpenRead(settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<GlobalSettings>(
                fileStream,
                _jsonOptions,
                cancellationToken);

            _globalSettings = settings ?? new GlobalSettings();
            Console.WriteLine($"[GlobalSettingsService] Loaded settings. License data present: {_globalSettings.License?.LicenseData?.Length ?? 0} chars");
        }
        catch (JsonException ex)
        {
            // Corrupted settings file, use defaults
            Console.WriteLine($"[GlobalSettingsService] JsonException loading settings: {ex.Message}");
            _globalSettings = new GlobalSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveGlobalSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settingsPath = GetGlobalSettingsPath();
        Console.WriteLine($"[GlobalSettingsService] SaveGlobalSettingsAsync - Path: {settingsPath}");
        Console.WriteLine($"[GlobalSettingsService] License data to save: {_globalSettings.License?.LicenseData?.Length ?? 0} chars");

        var directory = Path.GetDirectoryName(settingsPath);

        if (!string.IsNullOrEmpty(directory))
        {
            _platformService.EnsureDirectoryExists(directory);
            Console.WriteLine($"[GlobalSettingsService] Ensured directory exists: {directory}");
        }

        try
        {
            await using var fileStream = File.Create(settingsPath);
            await JsonSerializer.SerializeAsync(
                fileStream,
                _globalSettings,
                _jsonOptions,
                cancellationToken);
            Console.WriteLine("[GlobalSettingsService] Settings saved successfully!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[GlobalSettingsService] ERROR saving settings: {ex.GetType().Name}: {ex.Message}");
            throw;
        }
    }

    /// <inheritdoc />
    public async Task LoadCompanySettingsAsync(string tempDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tempDirectory);

        var settingsPath = Path.Combine(tempDirectory, CompanySettingsFileName);

        if (!File.Exists(settingsPath))
        {
            _companySettings = new CompanySettings();
            return;
        }

        try
        {
            await using var fileStream = File.OpenRead(settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<CompanySettings>(
                fileStream,
                _jsonOptions,
                cancellationToken);

            _companySettings = settings ?? new CompanySettings();
        }
        catch (JsonException)
        {
            // Corrupted settings file, use defaults
            _companySettings = new CompanySettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveCompanySettingsAsync(string tempDirectory, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(tempDirectory);

        if (_companySettings == null)
            return;

        var settingsPath = Path.Combine(tempDirectory, CompanySettingsFileName);
        _platformService.EnsureDirectoryExists(tempDirectory);

        await using var fileStream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(
            fileStream,
            _companySettings,
            _jsonOptions,
            cancellationToken);
    }

    /// <inheritdoc />
    public void ClearCompanySettings()
    {
        _companySettings = null;
    }

    /// <inheritdoc />
    public void AddRecentCompany(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var normalizedPath = _platformService.NormalizePath(filePath);
        var recentCompanies = _globalSettings.RecentCompanies;

        // Remove existing entry if present
        recentCompanies.Remove(normalizedPath);

        // Add to the beginning
        recentCompanies.Insert(0, normalizedPath);

        // Trim to max size
        var maxRecent = _platformService.MaxRecentCompanies;
        while (recentCompanies.Count > maxRecent)
        {
            recentCompanies.RemoveAt(recentCompanies.Count - 1);
        }
    }

    /// <inheritdoc />
    public void RemoveRecentCompany(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(filePath);

        var normalizedPath = _platformService.NormalizePath(filePath);
        _globalSettings.RecentCompanies.Remove(normalizedPath);
    }

    /// <inheritdoc />
    public string GetAppDataPath()
    {
        return _platformService.GetAppDataPath();
    }

    /// <summary>
    /// Gets all recent company paths that still exist.
    /// </summary>
    /// <returns>List of valid recent company paths.</returns>
    public IReadOnlyList<string> GetValidRecentCompanies()
    {
        if (!_platformService.SupportsFileSystem)
        {
            // Browser platform - return all without file existence check
            return _globalSettings.RecentCompanies.AsReadOnly();
        }

        return _globalSettings.RecentCompanies
            .Where(File.Exists)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Removes recent companies that no longer exist on disk.
    /// </summary>
    /// <returns>Number of entries removed.</returns>
    public int CleanupRecentCompanies()
    {
        if (!_platformService.SupportsFileSystem)
            return 0;

        var toRemove = _globalSettings.RecentCompanies
            .Where(path => !File.Exists(path))
            .ToList();

        foreach (var path in toRemove)
        {
            _globalSettings.RecentCompanies.Remove(path);
        }

        return toRemove.Count;
    }

    /// <summary>
    /// Creates a new company settings instance for a new company.
    /// </summary>
    /// <param name="companyName">Name of the company.</param>
    /// <returns>The created company settings.</returns>
    public CompanySettings CreateCompanySettings(string companyName)
    {
        _companySettings = new CompanySettings
        {
            Company = new CompanyInfo
            {
                Name = companyName
            }
        };
        return _companySettings;
    }

    /// <summary>
    /// Sets the company settings directly (used when loading existing company).
    /// </summary>
    /// <param name="settings">Company settings to use.</param>
    public void SetCompanySettings(CompanySettings settings)
    {
        _companySettings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    private string GetGlobalSettingsPath()
    {
        return _platformService.CombinePaths(_platformService.GetAppDataPath(), GlobalSettingsFileName);
    }

    #region IGlobalSettingsService Implementation

    /// <inheritdoc />
    GlobalSettings? IGlobalSettingsService.GetSettings() => _globalSettings;

    /// <inheritdoc />
    void IGlobalSettingsService.SaveSettings(GlobalSettings settings)
    {
        _globalSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        _ = SaveGlobalSettingsAsync();
    }

    /// <inheritdoc />
    async Task<GlobalSettings> IGlobalSettingsService.LoadAsync()
    {
        await LoadGlobalSettingsAsync();
        return _globalSettings;
    }

    /// <inheritdoc />
    Task IGlobalSettingsService.SaveAsync(GlobalSettings settings)
    {
        _globalSettings = settings ?? throw new ArgumentNullException(nameof(settings));
        return SaveGlobalSettingsAsync();
    }

    /// <inheritdoc />
    IReadOnlyList<string> IGlobalSettingsService.GetRecentCompanies() => GetValidRecentCompanies();

    #endregion
}
