using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Platform;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for managing license storage with machine-specific encryption.
/// </summary>
public class LicenseService
{
    private readonly IEncryptionService _encryptionService;
    private readonly IGlobalSettingsService _settingsService;
    private readonly IPlatformService _platformService;
    private readonly IErrorLogger? _errorLogger;

    /// <summary>
    /// Internal license data structure.
    /// </summary>
    private class LicenseData
    {
        public bool HasStandard { get; init; }
        public bool HasPremium { get; init; }
        public string? LicenseKey { get; init; }
        public DateTime ActivationDate { get; init; }
    }

    /// <summary>
    /// Initializes a new instance of the LicenseService.
    /// Uses the default platform service from the factory.
    /// </summary>
    public LicenseService(IEncryptionService encryptionService, IGlobalSettingsService settingsService, IErrorLogger? errorLogger = null)
        : this(encryptionService, settingsService, PlatformServiceFactory.GetPlatformService(), errorLogger)
    {
    }

    /// <summary>
    /// Initializes a new instance of the LicenseService with a specific platform service.
    /// </summary>
    public LicenseService(IEncryptionService encryptionService, IGlobalSettingsService settingsService, IPlatformService platformService, IErrorLogger? errorLogger = null)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        _platformService = platformService ?? throw new ArgumentNullException(nameof(platformService));
        _errorLogger = errorLogger;
    }

    /// <summary>
    /// Saves the license status securely.
    /// </summary>
    /// <param name="hasStandard">Whether user has Standard plan.</param>
    /// <param name="hasPremium">Whether user has Premium plan.</param>
    /// <param name="licenseKey">The license key that was verified.</param>
    public async Task SaveLicenseAsync(bool hasStandard, bool hasPremium, string? licenseKey)
    {
        var settings = _settingsService.GetSettings();
        if (settings == null)
            return;

        var licenseData = new LicenseData
        {
            HasStandard = hasStandard,
            HasPremium = hasPremium,
            LicenseKey = licenseKey,
            ActivationDate = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(licenseData);
        var dataBytes = Encoding.UTF8.GetBytes(json);

        // Generate salt and IV
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();

        // Use machine-specific password for encryption
        var machineKey = GetMachineKey();

        // Encrypt the license data
        var encryptedData = _encryptionService.Encrypt(dataBytes, machineKey, salt, iv);

        // Store in settings
        settings.License.LicenseData = Convert.ToBase64String(encryptedData);
        settings.License.Salt = salt;
        settings.License.Iv = iv;
        settings.License.LastValidationDate = DateTime.UtcNow;

        await _settingsService.SaveAsync(settings);
    }

    /// <summary>
    /// Loads the saved license status.
    /// </summary>
    /// <returns>Tuple of (hasStandard, hasPremium) or (false, false) if no valid license.</returns>
    public (bool HasStandard, bool HasPremium) LoadLicense()
    {
        try
        {
            var settings = _settingsService.GetSettings();
            if (settings?.License.LicenseData == null ||
                settings.License.Salt == null ||
                settings.License.Iv == null)
            {
                return (false, false);
            }

            var encryptedData = Convert.FromBase64String(settings.License.LicenseData);

            // Try to decrypt with the new stable machine key first
            var machineKey = GetMachineKey();
            var licenseData = TryDecryptLicense(encryptedData, machineKey, settings.License.Salt, settings.License.Iv);

            // If that fails, try the legacy MAC-based key for backward compatibility
            if (licenseData == null)
            {
                var legacyKey = GetLegacyMachineKey();
                licenseData = TryDecryptLicense(encryptedData, legacyKey, settings.License.Salt, settings.License.Iv);

                // If legacy key worked, re-encrypt with the new stable key for future loads
                if (licenseData != null)
                {
                    _ = MigrateLicenseToNewKeyAsync(licenseData);
                }
            }

            if (licenseData == null)
                return (false, false);

            return (licenseData.HasStandard, licenseData.HasPremium);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.License, "Failed to load license status");
            return (false, false);
        }
    }

    /// <summary>
    /// Attempts to decrypt license data with the given key.
    /// </summary>
    private LicenseData? TryDecryptLicense(byte[] encryptedData, string machineKey, string salt, string iv)
    {
        try
        {
            var decryptedData = _encryptionService.Decrypt(encryptedData, machineKey, salt, iv);
            var json = Encoding.UTF8.GetString(decryptedData);
            return JsonSerializer.Deserialize<LicenseData>(json);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Migrates a license from the legacy key format to the new stable key format.
    /// </summary>
    private async Task MigrateLicenseToNewKeyAsync(LicenseData licenseData)
    {
        try
        {
            await SaveLicenseAsync(licenseData.HasStandard, licenseData.HasPremium, licenseData.LicenseKey);
        }
        catch
        {
            // Migration failed, but license still works with legacy key
        }
    }

    /// <summary>
    /// Gets the stored license key (if available).
    /// </summary>
    /// <returns>The license key, or null if not available.</returns>
    public string? GetLicenseKey()
    {
        try
        {
            var settings = _settingsService.GetSettings();
            if (settings?.License.LicenseData == null ||
                settings.License.Salt == null ||
                settings.License.Iv == null)
            {
                return null;
            }

            var encryptedData = Convert.FromBase64String(settings.License.LicenseData);

            // Try to decrypt with the new stable machine key first
            var machineKey = GetMachineKey();
            var licenseData = TryDecryptLicense(encryptedData, machineKey, settings.License.Salt, settings.License.Iv);

            // If that fails, try the legacy MAC-based key for backward compatibility
            if (licenseData == null)
            {
                var legacyKey = GetLegacyMachineKey();
                licenseData = TryDecryptLicense(encryptedData, legacyKey, settings.License.Salt, settings.License.Iv);
            }

            return licenseData?.LicenseKey;
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.License, "Failed to retrieve license key");
            return null;
        }
    }

    /// <summary>
    /// Clears the saved license (for logout or plan cancellation).
    /// </summary>
    public async Task ClearLicenseAsync()
    {
        var settings = _settingsService.GetSettings();
        if (settings == null)
            return;

        settings.License = new LicenseSettings();
        await _settingsService.SaveAsync(settings);
    }

    /// <summary>
    /// Gets a machine-specific key for encryption using stable platform identifiers.
    /// </summary>
    private string GetMachineKey()
    {
        var machineInfo = new StringBuilder();

        // Use the platform-specific stable machine ID
        machineInfo.Append(_platformService.GetMachineId());

        // Add static application key (v2 to differentiate from legacy key)
        machineInfo.Append("ArgoBooks_License_v2");

        // Hash the combined data to create a fixed-length key
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineInfo.ToString()));
        return Convert.ToBase64String(hashBytes);
    }

    /// <summary>
    /// Gets the legacy machine key based on MAC addresses.
    /// Used for backward compatibility with licenses encrypted before the fix.
    /// </summary>
    private static string GetLegacyMachineKey()
    {
        var machineInfo = new StringBuilder();

        // Add machine name
        machineInfo.Append(Environment.MachineName);

        // Add MAC addresses from all physical network adapters (sorted for consistency)
        try
        {
            var macAddresses = NetworkInterface.GetAllNetworkInterfaces()
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                           n.NetworkInterfaceType != NetworkInterfaceType.Tunnel)
                .Select(n => n.GetPhysicalAddress().ToString())
                .Where(mac => !string.IsNullOrEmpty(mac) && mac != "000000000000")
                .OrderBy(mac => mac)
                .ToList();

            foreach (var mac in macAddresses)
            {
                machineInfo.Append(mac);
            }
        }
        catch
        {
            // Ignore network errors
        }

        // Add static application key (v1 - legacy)
        machineInfo.Append("ArgoBooks_License_v1");

        // Hash the combined data to create a fixed-length key
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineInfo.ToString()));
        return Convert.ToBase64String(hashBytes);
    }
}
