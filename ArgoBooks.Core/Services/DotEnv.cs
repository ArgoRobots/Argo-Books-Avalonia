namespace ArgoBooks.Core.Services;

/// <summary>
/// Loads environment variables from a .env file.
/// </summary>
public static class DotEnv
{
    private static readonly Dictionary<string, string> EnvVars = new(StringComparer.OrdinalIgnoreCase);
    private static bool _isLoaded;

    /// <summary>
    /// Loads environment variables from the .env file.
    /// Searches for .env in the application directory and parent directories.
    /// </summary>
    public static void Load()
    {
        if (_isLoaded) return;

        var envPath = FindEnvFile();
        if (!string.IsNullOrEmpty(envPath) && File.Exists(envPath))
        {
            LoadFromFile(envPath);
        }

        _isLoaded = true;
    }

    /// <summary>
    /// Gets an environment variable value.
    /// First checks the loaded .env values, then falls back to system environment variables.
    /// </summary>
    /// <param name="key">The variable name.</param>
    /// <returns>The value, or empty string if not found.</returns>
    public static string Get(string key)
    {
        // Ensure .env is loaded
        if (!_isLoaded)
        {
            Load();
        }

        // First check our loaded values
        if (EnvVars.TryGetValue(key, out var value))
        {
            return value;
        }

        // Fall back to system environment variables
        return Environment.GetEnvironmentVariable(key) ?? string.Empty;
    }

    /// <summary>
    /// Checks if a key exists and has a non-empty value.
    /// </summary>
    /// <param name="key">The variable name.</param>
    /// <returns>True if the key exists and has a value.</returns>
    public static bool HasValue(string key)
    {
        return !string.IsNullOrEmpty(Get(key));
    }

    /// <summary>
    /// Finds the .env file by searching the application directory and parent directories.
    /// </summary>
    private static string? FindEnvFile()
    {
        // Start from the application's base directory
        var directory = AppDomain.CurrentDomain.BaseDirectory;

        // Search up the directory tree for .env file
        while (!string.IsNullOrEmpty(directory))
        {
            var envPath = Path.Combine(directory, ".env");
            if (File.Exists(envPath))
            {
                return envPath;
            }

            // Move to parent directory
            var parent = Directory.GetParent(directory);
            if (parent == null) break;
            directory = parent.FullName;
        }

        // Also check current working directory
        var cwdEnvPath = Path.Combine(Directory.GetCurrentDirectory(), ".env");
        if (File.Exists(cwdEnvPath))
        {
            return cwdEnvPath;
        }

        return null;
    }

    /// <summary>
    /// Loads environment variables from the specified .env file.
    /// </summary>
    private static void LoadFromFile(string path)
    {
        try
        {
            var lines = File.ReadAllLines(path);

            foreach (var line in lines)
            {
                // Skip empty lines and comments
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith('#'))
                {
                    continue;
                }

                // Parse KEY=VALUE format
                var separatorIndex = trimmedLine.IndexOf('=');
                if (separatorIndex <= 0) continue;

                var key = trimmedLine[..separatorIndex].Trim();
                var value = trimmedLine[(separatorIndex + 1)..].Trim();

                // Remove surrounding quotes if present
                if (value.Length >= 2)
                {
                    if ((value.StartsWith('"') && value.EndsWith('"')) ||
                        (value.StartsWith('\'') && value.EndsWith('\'')))
                    {
                        value = value[1..^1];
                    }
                }

                EnvVars[key] = value;

                // Also set as system environment variable for the current process
                Environment.SetEnvironmentVariable(key, value);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load .env file: {ex.Message}");
        }
    }

    /// <summary>
    /// Reloads the .env file, clearing any cached values.
    /// </summary>
    public static void Reload()
    {
        EnvVars.Clear();
        _isLoaded = false;
        Load();
    }
}
