namespace ArgoBooks.Core.Services;

/// <summary>
/// Centralized API configuration. Controls whether the app targets
/// production (argorobots.com) or the sandbox/dev environment (dev.argorobots.com).
///
/// To enable sandbox mode, set ARGO_ENV=sandbox in the .env file
/// at the solution root (or anywhere between the binary and the root).
/// This is developer-only — end users don't have access to the .env file.
/// </summary>
public static class ApiConfig
{
    private const string EnvVar = "ARGO_ENV";
    private const string SandboxValue = "sandbox";

    private const string ProductionHost = "https://argorobots.com";
    private const string SandboxHost = "https://dev.argorobots.com";

    /// <summary>
    /// The base URL for all API calls (e.g. "https://argorobots.com" or "https://dev.argorobots.com").
    /// Determined once at startup from the ARGO_ENV environment variable.
    /// </summary>
    public static string BaseUrl { get; } = ResolveSandbox() ? SandboxHost : ProductionHost;

    /// <summary>
    /// True when running against the sandbox/dev environment.
    /// </summary>
    public static bool IsSandbox { get; } = ResolveSandbox();

    /// <summary>
    /// Maximum parent directories to search upward from the binary for a .env file.
    /// Binary is typically at ArgoBooks.Desktop/bin/Debug/net10.0/ — 4 levels below solution root.
    /// </summary>
    private const int MaxSearchDepth = 6;

    private static bool ResolveSandbox()
    {
        // 1. Check DotEnv (.env next to binary) and system environment variables
        var env = DotEnv.Get(EnvVar);
        if (string.Equals(env, SandboxValue, StringComparison.OrdinalIgnoreCase))
            return true;

        // 2. Walk upward from the binary directory to find a .env file
        //    (covers the solution root during IDE development)
        if (FindEnvValueUpward(AppDomain.CurrentDomain.BaseDirectory))
            return true;

        // 3. Also try from the current working directory (may differ from binary dir)
        var cwd = Directory.GetCurrentDirectory();
        if (cwd != AppDomain.CurrentDomain.BaseDirectory && FindEnvValueUpward(cwd))
            return true;

        return false;
    }

    /// <summary>
    /// Searches upward from the given directory for a .env file containing ARGO_ENV=sandbox.
    /// </summary>
    private static bool FindEnvValueUpward(string startDir)
    {
        var directory = startDir;

        for (var depth = 0; depth <= MaxSearchDepth && !string.IsNullOrEmpty(directory); depth++)
        {
            var envPath = Path.Combine(directory, ".env");
            if (File.Exists(envPath))
            {
                try
                {
                    foreach (var line in File.ReadLines(envPath))
                    {
                        var trimmed = line.Trim();
                        if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith('#') || !trimmed.Contains('='))
                            continue;

                        var sep = trimmed.IndexOf('=');
                        if (sep <= 0) continue;

                        var key = trimmed[..sep].Trim();
                        var value = trimmed[(sep + 1)..].Trim().Trim('"', '\'');

                        if (string.Equals(key, EnvVar, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(value, SandboxValue, StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
                catch
                {
                    // Best-effort — ignore read errors
                }
            }

            var parent = Directory.GetParent(directory);
            if (parent == null) break;
            directory = parent.FullName;
        }

        return false;
    }
}
