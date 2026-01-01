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
        Console.WriteLine($"[LicenseService] SaveLicenseAsync called: hasStandard={hasStandard}, hasPremium={hasPremium}");

        var settings = _settingsService.GetSettings();
        if (settings == null)
        {
            Console.WriteLine("[LicenseService] ERROR: GetSettings() returned null!");
            return;
        }
        Console.WriteLine("[LicenseService] Got settings successfully");

        var licenseData = new LicenseData
        {
            HasStandard = hasStandard,
            HasPremium = hasPremium,
            LicenseKey = licenseKey,
            ActivationDate = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(licenseData);
        Console.WriteLine($"[LicenseService] License JSON: {json}");

        var dataBytes = Encoding.UTF8.GetBytes(json);

        // Generate salt and IV
        var salt = _encryptionService.GenerateSalt();
        var iv = _encryptionService.GenerateIv();
        Console.WriteLine($"[LicenseService] Generated salt: {salt?.Substring(0, Math.Min(10, salt?.Length ?? 0))}..., iv: {iv?.Substring(0, Math.Min(10, iv?.Length ?? 0))}...");

        // Use machine-specific password for encryption
        var machineKey = GetMachineKey();
        Console.WriteLine($"[LicenseService] Machine key: {machineKey.Substring(0, Math.Min(20, machineKey.Length))}...");

        // Encrypt the license data
        var encryptedData = _encryptionService.Encrypt(dataBytes, machineKey, salt, iv);
        Console.WriteLine($"[LicenseService] Encrypted data length: {encryptedData?.Length ?? 0}");

        // Store in settings
        settings.License.LicenseData = Convert.ToBase64String(encryptedData);
        settings.License.Salt = salt;
        settings.License.Iv = iv;
        settings.License.LastValidationDate = DateTime.UtcNow;
        Console.WriteLine($"[LicenseService] Updated settings.License - LicenseData length: {settings.License.LicenseData?.Length ?? 0}");

        Console.WriteLine("[LicenseService] Calling SaveAsync...");
        await _settingsService.SaveAsync(settings);
        Console.WriteLine("[LicenseService] SaveAsync completed!");
    }

    /// <summary>
    /// Loads the saved license status.
    /// </summary>
    /// <returns>Tuple of (hasStandard, hasPremium) or (false, false) if no valid license.</returns>
    public (bool HasStandard, bool HasPremium) LoadLicense()
    {
        Console.WriteLine("[LicenseService] LoadLicense called");

        try
        {
            var settings = _settingsService.GetSettings();
            Console.WriteLine($"[LicenseService] GetSettings returned: {(settings == null ? "null" : "not null")}");

            if (settings?.License == null)
            {
                Console.WriteLine("[LicenseService] settings.License is null");
                return (false, false);
            }

            Console.WriteLine($"[LicenseService] License data present: LicenseData={settings.License.LicenseData?.Length ?? 0} chars, Salt={settings.License.Salt?.Length ?? 0} chars, Iv={settings.License.Iv?.Length ?? 0} chars");

            if (settings.License.LicenseData == null ||
                settings.License.Salt == null ||
                settings.License.Iv == null)
            {
                Console.WriteLine("[LicenseService] Missing license data fields - returning (false, false)");
                return (false, false);
            }

            var encryptedData = Convert.FromBase64String(settings.License.LicenseData);
            var machineKey = GetMachineKey();
            Console.WriteLine($"[LicenseService] Machine key for decrypt: {machineKey.Substring(0, Math.Min(20, machineKey.Length))}...");

            // Decrypt the license data
            var decryptedData = _encryptionService.Decrypt(
                encryptedData,
                machineKey,
                settings.License.Salt,
                settings.License.Iv);

            var json = Encoding.UTF8.GetString(decryptedData);
            Console.WriteLine($"[LicenseService] Decrypted JSON: {json}");

            var licenseData = JsonSerializer.Deserialize<LicenseData>(json);

            if (licenseData == null)
            {
                Console.WriteLine("[LicenseService] Deserialized license data is null");
                return (false, false);
            }

            Console.WriteLine($"[LicenseService] Loaded license: HasStandard={licenseData.HasStandard}, HasPremium={licenseData.HasPremium}");
            return (licenseData.HasStandard, licenseData.HasPremium);
        }
        catch (CryptographicException ex)
        {
            // Decryption failed - likely different machine or tampered data
            Console.WriteLine($"[LicenseService] CryptographicException during decrypt: {ex.Message}");
            return (false, false);
        }
        catch (Exception ex)
        {
            // Any other error - return no license
            Console.WriteLine($"[LicenseService] Exception during load: {ex.GetType().Name}: {ex.Message}");
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
        Console.WriteLine($"[LicenseService] GetMachineKey - MachineName: {Environment.MachineName}");

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

            Console.WriteLine($"[LicenseService] GetMachineKey - Found {macAddresses.Count} MAC addresses");
            foreach (var mac in macAddresses)
            {
                Console.WriteLine($"[LicenseService] GetMachineKey - MAC: {mac}");
                machineInfo.Append(mac);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[LicenseService] GetMachineKey - Error getting MAC addresses: {ex.Message}");
        }

        // Add static application key to make it harder to reverse
        machineInfo.Append("ArgoBooks_License_v1");

        // Hash the combined data to create a fixed-length key
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(machineInfo.ToString()));
        var result = Convert.ToBase64String(hashBytes);
        Console.WriteLine($"[LicenseService] GetMachineKey - Final hash: {result}");
        return result;
    }
}
