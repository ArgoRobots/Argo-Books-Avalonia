using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ArgoBooks.Core.Models;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for managing license storage with machine-specific encryption.
/// </summary>
public class LicenseService
{
    private readonly IEncryptionService _encryptionService;
    private readonly IGlobalSettingsService _settingsService;

    /// <summary>
    /// Internal license data structure.
    /// </summary>
    private class LicenseData
    {
        public bool HasStandard { get; set; }
        public bool HasPremium { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime ActivationDate { get; set; }
    }

    public LicenseService(IEncryptionService encryptionService, IGlobalSettingsService settingsService)
    {
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
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
            if (settings?.License?.LicenseData == null ||
                settings.License.Salt == null ||
                settings.License.Iv == null)
            {
                return (false, false);
            }

            var encryptedData = Convert.FromBase64String(settings.License.LicenseData);
            var machineKey = GetMachineKey();

            // Decrypt the license data
            var decryptedData = _encryptionService.Decrypt(
                encryptedData,
                machineKey,
                settings.License.Salt,
                settings.License.Iv);

            var json = Encoding.UTF8.GetString(decryptedData);
            var licenseData = JsonSerializer.Deserialize<LicenseData>(json);

            if (licenseData == null)
                return (false, false);

            return (licenseData.HasStandard, licenseData.HasPremium);
        }
        catch (CryptographicException)
        {
            // Decryption failed - likely different machine or tampered data
            return (false, false);
        }
        catch (Exception)
        {
            // Any other error - return no license
            return (false, false);
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
    /// Gets a machine-specific key for encryption.
    /// Uses a combination of machine name and MAC address.
    /// </summary>
    private static string GetMachineKey()
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

        // Add static application key to make it harder to reverse
        machineInfo.Append("ArgoBooks_License_v1");

        // Hash the combined data to create a fixed-length key
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineInfo.ToString()));
        return Convert.ToBase64String(hashBytes);
    }
}
