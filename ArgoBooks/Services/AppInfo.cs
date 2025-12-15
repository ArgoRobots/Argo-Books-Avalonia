using System.Reflection;

namespace ArgoBooks.Services;

/// <summary>
/// Provides application information such as version.
/// </summary>
public static class AppInfo
{
    private static string? _version;

    /// <summary>
    /// Gets the application version in "V.X.X.X" format.
    /// </summary>
    public static string Version
    {
        get
        {
            if (_version == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                _version = version != null
                    ? $"V.{version.Major}.{version.Minor}.{version.Build}"
                    : "V.1.0.0";
            }
            return _version;
        }
    }

    /// <summary>
    /// Gets the raw version object.
    /// </summary>
    public static Version? AssemblyVersion =>
        Assembly.GetExecutingAssembly().GetName().Version;
}
