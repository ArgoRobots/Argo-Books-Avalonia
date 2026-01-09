using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
#if WINDOWS
using Windows.Security.Credentials.UI;
#endif

namespace ArgoBooks.Core.Platform;

/// <summary>
/// Windows-specific platform service implementation.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsPlatformService : BasePlatformService
{
    /// <inheritdoc />
    public override PlatformType Platform => PlatformType.Windows;

    /// <inheritdoc />
    public override string GetAppDataPath()
    {
        // %APPDATA%/ArgoBooks (Roaming AppData)
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return CombinePaths(appData, ApplicationName);
    }

    /// <inheritdoc />
    public override string GetCachePath()
    {
        // %LOCALAPPDATA%/ArgoBooks/Cache (Local AppData for machine-specific cache)
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return CombinePaths(localAppData, ApplicationName, "Cache");
    }

    /// <inheritdoc />
    public override bool SupportsBiometrics => true; // Windows Hello

    /// <inheritdoc />
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override async Task<bool> IsBiometricAvailableAsync()
    {
#if WINDOWS
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            return availability == UserConsentVerifierAvailability.Available;
        }
        catch
        {
            return false;
        }
#else
        return await Task.FromResult(false);
#endif
    }

    /// <summary>
    /// Gets detailed information about Windows Hello availability.
    /// </summary>
    [SupportedOSPlatform("windows10.0.10240.0")]
    public async Task<string> GetBiometricAvailabilityDetailsAsync()
    {
#if WINDOWS
        try
        {
            var availability = await UserConsentVerifier.CheckAvailabilityAsync();
            return availability switch
            {
                UserConsentVerifierAvailability.Available => "Available",
                UserConsentVerifierAvailability.DeviceBusy => "Device is busy",
                UserConsentVerifierAvailability.DeviceNotPresent => "No biometric device found",
                UserConsentVerifierAvailability.DisabledByPolicy => "Disabled by policy",
                UserConsentVerifierAvailability.NotConfiguredForUser => "Not configured for current user. Please set up Windows Hello in Windows Settings > Accounts > Sign-in options.",
                _ => $"Unknown status: {availability}"
            };
        }
        catch (Exception ex)
        {
            return $"Error checking availability: {ex.Message}";
        }
#else
        // This means the app was built with the cross-platform target (net10.0)
        // instead of the Windows-specific target (net10.0-windows10.0.17763.0)
        return await Task.FromResult("Windows Hello requires the Windows-specific build. Please rebuild the application targeting 'net10.0-windows10.0.17763.0'.");
#endif
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows10.0.10240.0")]
    public override async Task<bool> AuthenticateWithBiometricAsync(string reason)
    {
#if WINDOWS
        try
        {
            var result = await UserConsentVerifier.RequestVerificationAsync(reason);
            return result == UserConsentVerificationResult.Verified;
        }
        catch
        {
            return false;
        }
#else
        return await Task.FromResult(false);
#endif
    }

    /// <inheritdoc />
    public override bool SupportsAutoUpdate => true;

    /// <inheritdoc />
    public override string NormalizePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return path;

        // Handle Windows-specific path normalization
        var normalized = Path.GetFullPath(path);

        // Ensure consistent casing for drive letter
        if (normalized.Length >= 2 && normalized[1] == ':')
        {
            normalized = char.ToUpperInvariant(normalized[0]) + normalized[1..];
        }

        return normalized;
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public override void StorePasswordForBiometric(string fileId, string password)
    {
#if WINDOWS
        try
        {
            var data = Encoding.UTF8.GetBytes(password);
            var protectedData = ProtectedData.Protect(data, null, DataProtectionScope.CurrentUser);
            var base64 = Convert.ToBase64String(protectedData);

            // Store in app data directory
            var storagePath = GetBiometricStoragePath();
            EnsureDirectoryExists(storagePath);

            var filePath = Path.Combine(storagePath, $"{fileId}.bio");
            File.WriteAllText(filePath, base64);
        }
        catch
        {
            // Silently fail - user will need to enter password manually
        }
#endif
    }

    /// <inheritdoc />
    [SupportedOSPlatform("windows")]
    public override string? GetPasswordForBiometric(string fileId)
    {
#if WINDOWS
        try
        {
            var filePath = Path.Combine(GetBiometricStoragePath(), $"{fileId}.bio");
            if (!File.Exists(filePath))
                return null;

            var base64 = File.ReadAllText(filePath);
            var protectedData = Convert.FromBase64String(base64);
            var data = ProtectedData.Unprotect(protectedData, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(data);
        }
        catch
        {
            return null;
        }
#else
        return null;
#endif
    }

    /// <inheritdoc />
    public override void ClearPasswordForBiometric(string fileId)
    {
        try
        {
            var filePath = Path.Combine(GetBiometricStoragePath(), $"{fileId}.bio");
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
        catch
        {
            // Silently fail
        }
    }

    private string GetBiometricStoragePath()
    {
        return CombinePaths(GetAppDataPath(), "Biometric");
    }

    /// <inheritdoc />
    public override string GetMachineId()
    {
        try
        {
            // Use Windows Cryptography MachineGuid - stable across reboots and network changes
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Cryptography");
            var machineGuid = key?.GetValue("MachineGuid") as string;
            if (!string.IsNullOrEmpty(machineGuid))
            {
                return machineGuid;
            }
        }
        catch
        {
            // Registry access failed, fall back to base implementation
        }

        return base.GetMachineId();
    }

    /// <inheritdoc />
    public override void RegisterFileTypeAssociations(string iconPath)
    {
        // Register all Argo Books file types with the Windows shell
        ArgoFiles.RegisterAllFileTypes(iconPath);
    }

    /// <inheritdoc />
    public override StringComparer PathComparer => StringComparer.OrdinalIgnoreCase;
}
