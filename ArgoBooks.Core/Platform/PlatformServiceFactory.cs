using System.Runtime.InteropServices;

namespace ArgoBooks.Core.Platform;

/// <summary>
/// Factory for creating platform-specific service instances.
/// </summary>
public static class PlatformServiceFactory
{
    private static IPlatformService? _instance;
    private static readonly Lock Lock = new();

    /// <summary>
    /// Gets the platform service for the current platform.
    /// </summary>
    /// <returns>Platform-specific service instance.</returns>
    public static IPlatformService GetPlatformService()
    {
        if (_instance != null)
            return _instance;

        lock (Lock)
        {
            _instance ??= CreatePlatformService();
            return _instance;
        }
    }

    /// <summary>
    /// Creates a platform service for a specific platform type.
    /// Useful for testing or cross-platform scenarios.
    /// </summary>
    /// <param name="platformType">The platform type to create a service for.</param>
    /// <returns>Platform service instance.</returns>
    public static IPlatformService CreateForPlatform(PlatformType platformType)
    {
        return platformType switch
        {
            PlatformType.Windows when OperatingSystem.IsWindows() => new WindowsPlatformService(),
            PlatformType.MacOS when OperatingSystem.IsMacOS() => new MacPlatformService(),
            PlatformType.Linux when OperatingSystem.IsLinux() => new LinuxPlatformService(),
            PlatformType.Browser when OperatingSystem.IsBrowser() => new BrowserPlatformService(),
            // Fallback for cross-platform scenarios (e.g., testing)
            PlatformType.Windows => new LinuxPlatformService(),
            PlatformType.MacOS => new LinuxPlatformService(),
            PlatformType.Linux => new LinuxPlatformService(),
            PlatformType.Browser => new LinuxPlatformService(),
            _ => throw new ArgumentOutOfRangeException(nameof(platformType))
        };
    }

    /// <summary>
    /// Detects the current platform and returns the appropriate type.
    /// </summary>
    /// <returns>The detected platform type.</returns>
    public static PlatformType DetectPlatform()
    {
        // Check for browser/WebAssembly first
        if (OperatingSystem.IsBrowser())
            return PlatformType.Browser;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return PlatformType.Windows;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return PlatformType.MacOS;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return PlatformType.Linux;

        // Default to Linux for unknown Unix-like systems
        return PlatformType.Linux;
    }

    private static IPlatformService CreatePlatformService()
    {
        var platform = DetectPlatform();
        return CreateForPlatform(platform);
    }

    /// <summary>
    /// Resets the cached instance. Useful for testing.
    /// </summary>
    internal static void Reset()
    {
        lock (Lock)
        {
            _instance = null;
        }
    }
}
