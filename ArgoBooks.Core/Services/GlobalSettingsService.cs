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

        if (!File.Exists(settingsPath))
        {
            _globalSettings = new GlobalSettings();
            return;
        }

        try
        {
            await using var fileStream = File.OpenRead(settingsPath);
            var settings = await JsonSerializer.DeserializeAsync<GlobalSettings>(
                fileStream,
                _jsonOptions,
                cancellationToken);

            _globalSettings = settings ?? new GlobalSettings();
        }
        catch (JsonException)
        {
            // Corrupted settings file, use defaults
            _globalSettings = new GlobalSettings();
        }
    }

    /// <inheritdoc />
    public async Task SaveGlobalSettingsAsync(CancellationToken cancellationToken = default)
    {
        var settingsPath = GetGlobalSettingsPath();
        var directory = Path.GetDirectoryName(settingsPath);

        if (!string.IsNullOrEmpty(directory))
        {
            _platformService.EnsureDirectoryExists(directory);
        }

        await using var fileStream = File.Create(settingsPath);
        await JsonSerializer.SerializeAsync(
            fileStream,
            _globalSettings,
            _jsonOptions,
            cancellationToken);
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

        // Don't add sample company to recent list
        var samplePath = SampleCompanyService.GetSampleCompanyPath();
        if (string.Equals(filePath, samplePath, StringComparison.OrdinalIgnoreCase))
            return;

        var normalizedPath = _platformService.NormalizePath(filePath);
        var recentCompanies = _globalSettings.RecentCompanies;

        // Remove existing entry if present (using platform-appropriate comparison)
        var existingIndex = recentCompanies.FindIndex(p =>
            _platformService.PathComparer.Equals(p, normalizedPath));
        if (existingIndex >= 0)
        {
            recentCompanies.RemoveAt(existingIndex);
        }

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
        var recentCompanies = _globalSettings.RecentCompanies;

        // Remove using platform-appropriate comparison
        var existingIndex = recentCompanies.FindIndex(p =>
            _platformService.PathComparer.Equals(p, normalizedPath));
        if (existingIndex >= 0)
        {
            recentCompanies.RemoveAt(existingIndex);
        }
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
        // Get sample company path to filter it out
        var samplePath = SampleCompanyService.GetSampleCompanyPath();

        if (!_platformService.SupportsFileSystem)
        {
            // Browser platform - return all without file existence check, but exclude sample company
            return _globalSettings.RecentCompanies
                .Where(p => !string.Equals(p, samplePath, StringComparison.OrdinalIgnoreCase))
                .ToList()
                .AsReadOnly();
        }

        // Filter to existing files, exclude sample company, and deduplicate using platform-appropriate comparison
        // (handles case-insensitive duplicates on Windows)
        return _globalSettings.RecentCompanies
            .Where(p => File.Exists(p) && !string.Equals(p, samplePath, StringComparison.OrdinalIgnoreCase))
            .Distinct(_platformService.PathComparer)
            .ToList()
            .AsReadOnly();
    }

    /// <summary>
    /// Removes recent companies that no longer exist on disk and deduplicates entries.
    /// </summary>
    /// <returns>Number of entries removed.</returns>
    public int CleanupRecentCompanies()
    {
        if (!_platformService.SupportsFileSystem)
            return 0;

        var recentCompanies = _globalSettings.RecentCompanies;
        var originalCount = recentCompanies.Count;

        // Remove entries where file doesn't exist
        var toRemove = recentCompanies
            .Where(path => !File.Exists(path))
            .ToList();

        foreach (var path in toRemove)
        {
            recentCompanies.Remove(path);
        }

        // Remove case-insensitive duplicates (keep first occurrence)
        var seen = new HashSet<string>(_platformService.PathComparer);
        var duplicateIndices = new List<int>();
        for (var i = 0; i < recentCompanies.Count; i++)
        {
            if (!seen.Add(recentCompanies[i]))
            {
                duplicateIndices.Add(i);
            }
        }
        // Remove in reverse order to preserve indices
        for (var i = duplicateIndices.Count - 1; i >= 0; i--)
        {
            recentCompanies.RemoveAt(duplicateIndices[i]);
        }

        return originalCount - recentCompanies.Count;
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
    GlobalSettings IGlobalSettingsService.GetSettings() => _globalSettings;

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
