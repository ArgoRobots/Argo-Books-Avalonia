using System.Reflection;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Provides application version information.
/// </summary>
public static class AppInfo
{
    private static string? _versionNumber;
    private static Version? _assemblyVersion;

    /// <summary>
    /// Gets the application version in "X.X.X" format.
    /// </summary>
    public static string VersionNumber
    {
        get
        {
            if (_versionNumber == null)
            {
                var version = AssemblyVersion;
                _versionNumber = version != null
                    ? $"{version.Major}.{version.Minor}.{version.Build}"
                    : "1.0.0";
            }
            return _versionNumber;
        }
    }

    /// <summary>
    /// Gets the raw version object from the entry assembly.
    /// </summary>
    public static Version? AssemblyVersion
    {
        get
        {
            if (_assemblyVersion == null)
            {
                try
                {
                    _assemblyVersion = Assembly.GetEntryAssembly()?.GetName().Version;
                }
                catch
                {
                    // Ignore errors accessing assembly
                }
            }
            return _assemblyVersion;
        }
    }
}
