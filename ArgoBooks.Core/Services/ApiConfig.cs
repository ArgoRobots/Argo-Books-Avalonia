namespace ArgoBooks.Core.Services;

/// <summary>
/// Centralized API configuration. Controls whether the app targets
/// production (argorobots.com) or the sandbox/dev environment (dev.argorobots.com).
///
/// To enable sandbox mode, set ARGO_ENV=sandbox in the .env file.
/// This is developer-only — the .env file is not accessible to end users.
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
    public static string BaseUrl { get; } = ResolveBaseUrl();

    /// <summary>
    /// True when running against the sandbox/dev environment.
    /// </summary>
    public static bool IsSandbox { get; } = ResolveSandbox();

    private static string ResolveBaseUrl()
    {
        return ResolveSandbox() ? SandboxHost : ProductionHost;
    }

    private static bool ResolveSandbox()
    {
        var env = DotEnv.Get(EnvVar);
        return string.Equals(env, SandboxValue, StringComparison.OrdinalIgnoreCase);
    }
}
