using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ArgoBooks.Core.Platform;

/// <summary>
/// Handles file associations and icon registration for Argo Books file types.
/// </summary>
[SupportedOSPlatform("windows")]
public static class ArgoFiles
{
    /// <summary>
    /// Import for the Windows Shell32 API function to notify the system of association changes.
    /// </summary>
    [DllImport("shell32.dll", SetLastError = true)]
    private static extern void SHChangeNotify(uint wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

    // Shell change notification constants
    private const uint SHCNE_ASSOCCHANGED = 0x8000000;  // Notifies system of association change
    private const uint SHCNF_IDLIST = 0x0;              // No additional flags needed

    /// <summary>
    /// Registers a file extension with Windows and associates it with an icon and the current application.
    /// </summary>
    /// <param name="extension">The file extension including the dot (e.g., ".argo")</param>
    /// <param name="iconSourcePath">Path to the source icon file</param>
    /// <param name="iconIndex">Icon index (usually 0)</param>
    /// <param name="fileTypeDescription">Human-readable description of the file type</param>
    public static void RegisterFileIcon(string extension, string iconSourcePath, int iconIndex, string fileTypeDescription = "Argo Books File")
    {
        if (string.IsNullOrEmpty(iconSourcePath) || !File.Exists(iconSourcePath))
            return;

        try
        {
            // Create a persistent copy of the icon in local app data
            string tempIconPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ArgoBooks",
                $"{extension.Replace(".", "")}.ico"
            );

            // Ensure directory exists
            var directory = Path.GetDirectoryName(tempIconPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Copy the icon to the persistent location
            File.Copy(iconSourcePath, tempIconPath, overwrite: true);

            // Generate a unique class name for this file type
            string className = $"ArgoBooks{extension.Replace(".", "")}";

            // Registry path for user-specific file associations
            string userClassesRoot = @"Software\Classes";

            // Create file extension association
            using (RegistryKey? extensionKey = Registry.CurrentUser.CreateSubKey($@"{userClassesRoot}\{extension}"))
            {
                extensionKey?.SetValue("", className);
            }

            // Create file type information
            using (RegistryKey? classKey = Registry.CurrentUser.CreateSubKey($@"{userClassesRoot}\{className}"))
            {
                classKey?.SetValue("", fileTypeDescription);
            }

            // Associate icon with file type
            using (RegistryKey? defaultIconKey = Registry.CurrentUser.CreateSubKey($@"{userClassesRoot}\{className}\DefaultIcon"))
            {
                defaultIconKey?.SetValue("", $"{tempIconPath},{iconIndex}");
            }

            // Get the current executable path
            var executablePath = Environment.ProcessPath;
            if (!string.IsNullOrEmpty(executablePath))
            {
                // Set up command to open files with this application
                using RegistryKey? commandKey = Registry.CurrentUser.CreateSubKey($@"{userClassesRoot}\{className}\shell\open\command");
                commandKey?.SetValue("", $"\"{executablePath}\" \"%1\"");
            }

            // Notify Windows to refresh icon cache and file associations
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
        }
        catch (Exception ex)
        {
            // Log but don't crash - file association is not critical
            System.Diagnostics.Debug.WriteLine($"Failed to register file icon for {extension}: {ex.Message}");
        }
    }

    /// <summary>
    /// Registers all Argo Books file types with their icons.
    /// </summary>
    /// <param name="iconPath">Path to the Argo Books icon file</param>
    public static void RegisterAllFileTypes(string iconPath)
    {
        // Register the main .argo company file extension
        RegisterFileIcon(".argo", iconPath, 0, "Argo Books Company File");

        // Register backup file extension
        RegisterFileIcon(".argobk", iconPath, 0, "Argo Books Backup File");

        // Register template file extension
        RegisterFileIcon(".argotemplate", iconPath, 0, "Argo Books Template File");
    }
}
