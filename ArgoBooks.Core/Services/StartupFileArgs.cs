namespace ArgoBooks.Core.Services;

/// <summary>
/// Extracts the company file path Windows passes on the command line when a user
/// double-clicks a .argo file (the shell association runs "ArgoBooks.exe" "%1").
/// </summary>
public static class StartupFileArgs
{
    private const string CompanyExtension = ".argo";

    /// <summary>
    /// Returns the full path of the company file to open at startup, or null when the
    /// arguments contain none.
    /// </summary>
    /// <param name="args">Raw command-line arguments, excluding the executable itself.</param>
    /// <param name="fileExists">File-existence probe; overridable for tests.</param>
    public static string? GetCompanyFilePath(IReadOnlyList<string>? args, Func<string, bool>? fileExists = null)
    {
        if (args == null)
            return null;

        fileExists ??= File.Exists;

        foreach (var raw in args)
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            // Avalonia and the .NET host consume switches from the same array. A leading '/' is
            // not tested for: it introduces a switch on Windows but begins every absolute path on
            // Linux and macOS, which is exactly what a file manager passes for a double-clicked
            // .argo, so rejecting it left the whole feature dead there. The extension test below
            // already turns away a Windows switch, which would have to be named something.argo
            // and exist on disk to get any further.
            var candidate = raw.Trim().Trim('"');
            if (candidate.Length == 0 || candidate[0] == '-')
                continue;

            if (!candidate.EndsWith(CompanyExtension, StringComparison.OrdinalIgnoreCase))
                continue;

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(candidate);
            }
            catch
            {
                // Malformed path (invalid characters, too long): not something we can open.
                continue;
            }

            if (fileExists(fullPath))
                return fullPath;
        }

        return null;
    }
}
