using System.Diagnostics;

namespace ArgoBooks.Core.Platform;

/// <summary>
/// macOS-specific platform service implementation.
/// </summary>
public class MacPlatformService : BasePlatformService
{
    /// <inheritdoc />
    public override PlatformType Platform => PlatformType.MacOS;

    /// <inheritdoc />
    public override string GetAppDataPath()
    {
        // ~/Library/Application Support/ArgoBooks
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, "Library", "Application Support", ApplicationName);
    }

    /// <inheritdoc />
    public override string GetCachePath()
    {
        // ~/Library/Caches/ArgoBooks
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, "Library", "Caches", ApplicationName);
    }

    /// <inheritdoc />
    public override string GetLogsPath()
    {
        // ~/Library/Logs/ArgoBooks
        var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return CombinePaths(homeDir, "Library", "Logs", ApplicationName);
    }

    /// <inheritdoc />
    public override string GetDefaultDocumentsPath()
    {
        // ~/Documents/ArgoBooks
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(documentsPath))
        {
            var homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            documentsPath = CombinePaths(homeDir, "Documents");
        }
        return CombinePaths(documentsPath, ApplicationName);
    }

    /// <inheritdoc />
    public override bool SupportsBiometrics => true; // Touch ID

    /// <inheritdoc />
    public override bool SupportsAutoUpdate => true;

    /// <inheritdoc />
    public override string GetMachineId()
    {
        try
        {
            // Use ioreg to get the hardware UUID (IOPlatformUUID)
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "/usr/sbin/ioreg",
                Arguments = "-rd1 -c IOPlatformExpertDevice",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000); // 5 second timeout

            // Parse the IOPlatformUUID from the output
            // Format: "IOPlatformUUID" = "XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
            const string uuidKey = "\"IOPlatformUUID\"";
            var keyIndex = output.IndexOf(uuidKey, StringComparison.Ordinal);
            if (keyIndex >= 0)
            {
                var startQuote = output.IndexOf('"', keyIndex + uuidKey.Length + 1);
                if (startQuote >= 0)
                {
                    var endQuote = output.IndexOf('"', startQuote + 1);
                    if (endQuote > startQuote)
                    {
                        var uuid = output.Substring(startQuote + 1, endQuote - startQuote - 1);
                        if (!string.IsNullOrEmpty(uuid))
                        {
                            return uuid;
                        }
                    }
                }
            }
        }
        catch
        {
            // Process execution failed
        }

        return base.GetMachineId();
    }
}
