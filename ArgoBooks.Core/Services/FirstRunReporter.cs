using System.Net.Http.Json;
using System.Runtime.InteropServices;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Reports first-run telemetry to the Argo Books website so the referral
/// funnel can attribute installs back to the originating ad click.
///
/// Token sources, by platform:
///   Windows: Advanced Installer writes the token to
///            %LOCALAPPDATA%\ArgoBooks\install_token.txt during install.
///   Mac:     reads the ?t= parameter out of the download URL macOS records on
///            the bundle as com.apple.metadata:kMDItemWhereFroms. The archive's
///            own filename can't be used: the user expands the .zip and the
///            extracted "Argo Books.app" carries none of its name.
///   Linux:   parses the AppImage filename for the _xxxxxxxx token.
///
/// Idempotency: once a first-run POST succeeds, a marker file
/// (first_run_reported.marker) is written. Subsequent launches are no-ops.
/// </summary>
public sealed class FirstRunReporter
{
    private const string EndpointPath = "/api/track-app-event.php";
    private const string TokenFileName = "install_token.txt";
    private const string MarkerFileName = "first_run_reported.marker";
    private const int MaxRetryAttempts = 3;

    private readonly HttpClient _httpClient;
    private readonly IErrorLogger? _errorLogger;
    private readonly string _appVersion;
    private readonly string _appDataDir;

    public FirstRunReporter(
        HttpClient httpClient,
        string appVersion,
        IErrorLogger? errorLogger = null)
    {
        _httpClient = httpClient;
        _errorLogger = errorLogger;
        _appVersion = appVersion;
        _appDataDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ArgoBooks");
    }

    /// <summary>
    /// Fire-and-forget first-run report. Safe to call on every launch; the
    /// marker file ensures the network call happens at most once per machine.
    /// </summary>
    public async Task ReportIfFirstRunAsync(CancellationToken cancellationToken = default)
    {
        string? attemptsPath = null;
        int attempts = 0;
        bool postCompleted = false;

        try
        {
            Directory.CreateDirectory(_appDataDir);
            var markerPath = Path.Combine(_appDataDir, MarkerFileName);

            // Idempotency guard: if we already reported, do nothing.
            if (File.Exists(markerPath))
            {
                return;
            }

            attemptsPath = markerPath + ".attempts";
            attempts = ReadAttemptCount(attemptsPath);
            if (attempts >= MaxRetryAttempts)
            {
                // Give up after MaxRetryAttempts so we don't pester the user's
                // network on every launch.
                await WriteMarker(markerPath, "gave_up_after_retries", cancellationToken);
                TryDelete(attemptsPath);
                return;
            }

            var token = ResolveInstallToken();
            var machineUuid = GetOrCreateMachineUuid();
            var platform = GetPlatformKey();

            var payload = new FirstRunPayload
            {
                Token = token ?? string.Empty,
                Event = "app_first_run",
                Platform = platform,
                AppVersion = _appVersion,
                MachineUuid = machineUuid,
            };

            var url = $"{ApiConfig.BaseUrl}{EndpointPath}";
            using var response = await _httpClient.PostAsJsonAsync(url, payload, cancellationToken);
            postCompleted = true;

            if (response.IsSuccessStatusCode)
            {
                await WriteMarker(markerPath, token != null ? "token" : "no_token", cancellationToken);
                // Clean up the install token file so it doesn't sit around
                TryDeleteInstallToken();
                // Clean up the attempts counter
                TryDelete(attemptsPath);
            }
            else
            {
                IncrementAttemptCount(attemptsPath, attempts + 1);
                _errorLogger?.LogWarning(
                    $"FirstRunReporter received HTTP {(int)response.StatusCode}",
                    context: "FirstRunReporter.ReportIfFirstRunAsync");
            }
        }
        catch (OperationCanceledException)
        {
            // Cancellation isn't an error to log.
        }
        catch (Exception ex)
        {
            // Network exceptions (offline, DNS, timeout, TLS) reach here. Count
            // them toward MaxRetryAttempts so we eventually give up instead of
            // retrying on every launch forever.
            if (attemptsPath != null && !postCompleted)
            {
                IncrementAttemptCount(attemptsPath, attempts + 1);
            }
            NetworkFailure.Report(_errorLogger, ex, "FirstRunReporter.ReportIfFirstRunAsync");
        }
    }

    /// <summary>
    /// Looks up the 8-char hex install token. Returns null if there's nothing to
    /// report (token file missing on Windows, no recorded download URL on Mac, or
    /// filename has no token suffix on Linux). A null token still produces a
    /// "first_run without attribution" event server-side, so the funnel sees the
    /// install either way.
    /// </summary>
    private string? ResolveInstallToken()
    {
        try
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var tokenPath = Path.Combine(_appDataDir, TokenFileName);
                if (File.Exists(tokenPath))
                {
                    var raw = File.ReadAllText(tokenPath).Trim();
                    if (IsValidToken(raw))
                    {
                        return raw.ToLowerInvariant();
                    }
                }
                return null;
            }

            var processPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (string.IsNullOrEmpty(processPath))
            {
                return null;
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                // The download is a .zip the user expands, so by the time we run,
                // the executable sits at "Argo Books.app/Contents/MacOS/Argo Books"
                // and nothing in that path came from the archive's name. The one
                // surviving trace of where it came from is the source URL the
                // browser stamped onto the file, which is where the token lives.
                return TryReadTokenFromWhereFroms(GetAppBundlePath(processPath));
            }

            // Linux: the AppImage is itself the executable, so it still carries
            // the downloaded filename.
            return ExtractTokenFromPath(processPath);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogWarning(
                $"FirstRunReporter token resolution failed: {ex.Message}",
                context: "FirstRunReporter.ResolveInstallToken");
            return null;
        }
    }

    /// <summary>
    /// Walks up from the running executable to the enclosing .app bundle.
    /// Returns the executable's own directory when there isn't one, which is the
    /// case for an unbundled build run straight out of the publish folder.
    /// </summary>
    private static string GetAppBundlePath(string exePath)
    {
        var dir = Path.GetDirectoryName(exePath);
        while (!string.IsNullOrEmpty(dir))
        {
            if (dir.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }
        return Path.GetDirectoryName(exePath) ?? exePath;
    }

    /// <summary>
    /// Reads the download URL macOS records on a downloaded file and pulls the
    /// ?t= install token out of it.
    ///
    /// The attribute holds a binary plist, but the URL inside it is plain ASCII,
    /// so the value is read as raw bytes (Latin1 maps them 1:1, where UTF-8 would
    /// mangle the surrounding plist framing) and matched directly. That avoids
    /// taking a plist parser dependency for one string.
    ///
    /// Returns null whenever the attribute is missing, which is the normal case
    /// for a build that wasn't downloaded through a browser.
    /// </summary>
    private static string? TryReadTokenFromWhereFroms(string bundlePath)
    {
        try
        {
            using var process = new System.Diagnostics.Process();
            process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "/usr/bin/xattr",
                ArgumentList = { "-p", "com.apple.metadata:kMDItemWhereFroms", bundlePath },
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.Latin1
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            if (!process.WaitForExit(5000))
            {
                return null;
            }
            if (process.ExitCode != 0 || string.IsNullOrEmpty(output))
            {
                return null;
            }

            var match = System.Text.RegularExpressions.Regex.Match(
                output, @"[?&]t=([0-9a-f]{8})\b",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
        }
        catch
        {
            // xattr missing, or the attribute isn't set. Reported as unattributed.
            return null;
        }
    }

    private static string? ExtractTokenFromPath(string path)
    {
        // Walk up looking for a path segment matching foo_xxxxxxxx.ext
        var segments = new[]
        {
            Path.GetFileName(path),
            Path.GetFileName(Path.GetDirectoryName(path) ?? string.Empty),
        };
        foreach (var seg in segments)
        {
            if (string.IsNullOrEmpty(seg)) continue;
            var match = System.Text.RegularExpressions.Regex.Match(
                seg, @"_([0-9a-f]{8})(?:\.|$)",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);
            if (match.Success)
            {
                return match.Groups[1].Value.ToLowerInvariant();
            }
        }
        return null;
    }

    private static bool IsValidToken(string token) =>
        System.Text.RegularExpressions.Regex.IsMatch(token, "^[0-9a-fA-F]{8}$");

    private static string GetPlatformKey()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "win";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))     return "mac";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))   return "linux";
        return "unknown";
    }

    /// <summary>
    /// Returns a stable per-machine UUID for dedup. Generated lazily and
    /// persisted to the app data directory so the same machine returns the
    /// same UUID across runs.
    /// </summary>
    private string GetOrCreateMachineUuid()
    {
        try
        {
            var path = Path.Combine(_appDataDir, "machine_uuid.txt");
            if (File.Exists(path))
            {
                var raw = File.ReadAllText(path).Trim();
                if (Guid.TryParse(raw, out _))
                {
                    return raw;
                }
            }
            var uuid = Guid.NewGuid().ToString();
            File.WriteAllText(path, uuid);
            return uuid;
        }
        catch
        {
            return Guid.NewGuid().ToString();
        }
    }

    private static int ReadAttemptCount(string path)
    {
        try
        {
            if (!File.Exists(path)) return 0;
            return int.TryParse(File.ReadAllText(path).Trim(), out var n) ? n : 0;
        }
        catch
        {
            return 0;
        }
    }

    private static void IncrementAttemptCount(string path, int newCount)
    {
        try
        {
            File.WriteAllText(path, newCount.ToString());
        }
        catch
        {
            // Best-effort
        }
    }

    private static async Task WriteMarker(string path, string reason, CancellationToken ct)
    {
        try
        {
            await File.WriteAllTextAsync(path,
                $"reported_at={DateTime.UtcNow:O}\nreason={reason}\n", ct);
        }
        catch
        {
            // Best-effort
        }
    }

    private void TryDeleteInstallToken()
    {
        try
        {
            var path = Path.Combine(_appDataDir, TokenFileName);
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort
        }
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }
        catch
        {
            // Best-effort
        }
    }

    private sealed class FirstRunPayload
    {
        [JsonPropertyName("token")]
        public string Token { get; set; } = string.Empty;

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;

        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;

        [JsonPropertyName("app_version")]
        public string AppVersion { get; set; } = string.Empty;

        [JsonPropertyName("machine_uuid")]
        public string MachineUuid { get; set; } = string.Empty;
    }
}
