using System.Reflection;

namespace ArgoBooks.Services;

/// <summary>
/// Provides application information such as version.
/// </summary>
public static class AppInfo
{
    /// <summary>
    /// Gets the application version in "V.X.X.X" format.
    /// </summary>
    public static string Version
    {
        get
        {
            if (field == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                field = version != null
                    ? $"V.{version.Major}.{version.Minor}.{version.Build}"
                    : "V.1.0.0";
            }
            return field;
        }
    }

    /// <summary>
    /// Gets the raw version object.
    /// </summary>
    public static Version? AssemblyVersion =>
        Assembly.GetExecutingAssembly().GetName().Version;
}
