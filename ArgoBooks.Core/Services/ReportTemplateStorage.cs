using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Telemetry;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Handles loading and saving custom report templates.
/// </summary>
public class ReportTemplateStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _templatesDirectory;
    private readonly IErrorLogger? _errorLogger;

    public ReportTemplateStorage(IErrorLogger? errorLogger = null)
    {
        // Default to AppData/Roaming/ArgoBooks/ReportTemplates
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _templatesDirectory = Path.Combine(appData, "ArgoBooks", "ReportTemplates");
        _errorLogger = errorLogger;
    }

    public ReportTemplateStorage(string templatesDirectory, IErrorLogger? errorLogger = null)
    {
        _templatesDirectory = templatesDirectory;
        _errorLogger = errorLogger;
    }

    /// <summary>
    /// Gets the templates directory path.
    /// </summary>
    public string TemplatesDirectory => _templatesDirectory;

    /// <summary>
    /// Ensures the templates directory exists.
    /// </summary>
    private void EnsureDirectoryExists()
    {
        if (!Directory.Exists(_templatesDirectory))
        {
            Directory.CreateDirectory(_templatesDirectory);
        }
    }

    /// <summary>
    /// Saves a template to storage.
    /// </summary>
    public async Task<bool> SaveTemplateAsync(ReportConfiguration config, string templateName)
    {
        try
        {
            EnsureDirectoryExists();

            var sanitizedName = SanitizeFileName(templateName);
            var filePath = Path.Combine(_templatesDirectory, $"{sanitizedName}.argotemplate");

            var templateData = new SavedTemplate
            {
                Name = templateName,
                Configuration = config,
                CreatedAt = DateTime.UtcNow,
                ModifiedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(templateData, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);

            return true;
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to save report template");
            return false;
        }
    }

    /// <summary>
    /// Loads a template from storage.
    /// </summary>
    public async Task<ReportConfiguration?> LoadTemplateAsync(string templateName)
    {
        try
        {
            var sanitizedName = SanitizeFileName(templateName);
            var filePath = Path.Combine(_templatesDirectory, $"{sanitizedName}.argotemplate");

            if (!File.Exists(filePath))
                return null;

            var json = await File.ReadAllTextAsync(filePath);
            var templateData = JsonSerializer.Deserialize<SavedTemplate>(json, JsonOptions);

            return templateData?.Configuration;
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to load report template");
            return null;
        }
    }

    /// <summary>
    /// Gets all saved template names.
    /// </summary>
    public List<string> GetSavedTemplateNames()
    {
        var names = new List<string>();

        try
        {
            EnsureDirectoryExists();

            var files = Directory.GetFiles(_templatesDirectory, "*.argotemplate");
            foreach (var file in files)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var templateData = JsonSerializer.Deserialize<SavedTemplate>(json, JsonOptions);
                    if (templateData?.Name != null)
                    {
                        names.Add(templateData.Name);
                    }
                }
                catch (Exception ex)
                {
                    _errorLogger?.LogWarning($"Failed to read template file {Path.GetFileName(file)}: {ex.Message}", "ReportTemplateStorage");
                }
            }
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to enumerate report templates");
        }

        return names;
    }

    /// <summary>
    /// Gets all saved templates with metadata.
    /// </summary>
    public async Task<List<SavedTemplate>> GetAllTemplatesAsync()
    {
        var templates = new List<SavedTemplate>();

        try
        {
            EnsureDirectoryExists();

            var files = Directory.GetFiles(_templatesDirectory, "*.argotemplate");
            foreach (var file in files)
            {
                try
                {
                    var json = await File.ReadAllTextAsync(file);
                    var templateData = JsonSerializer.Deserialize<SavedTemplate>(json, JsonOptions);
                    if (templateData != null)
                    {
                        templateData.FilePath = file;
                        templates.Add(templateData);
                    }
                }
                catch (Exception ex)
                {
                    _errorLogger?.LogWarning($"Failed to read template file {Path.GetFileName(file)}: {ex.Message}", "ReportTemplateStorage");
                }
            }
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to enumerate report templates");
        }

        return templates.OrderByDescending(t => t.ModifiedAt).ToList();
    }

    /// <summary>
    /// Deletes a template from storage.
    /// </summary>
    public bool DeleteTemplate(string templateName)
    {
        try
        {
            var sanitizedName = SanitizeFileName(templateName);
            var filePath = Path.Combine(_templatesDirectory, $"{sanitizedName}.argotemplate");

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, $"Failed to delete report template '{templateName}'");
        }

        return false;
    }

    /// <summary>
    /// Renames a template asynchronously.
    /// </summary>
    public async Task<bool> RenameTemplateAsync(string oldName, string newName)
    {
        try
        {
            var oldSanitized = SanitizeFileName(oldName);
            var newSanitized = SanitizeFileName(newName);

            var oldPath = Path.Combine(_templatesDirectory, $"{oldSanitized}.argotemplate");
            var newPath = Path.Combine(_templatesDirectory, $"{newSanitized}.argotemplate");

            if (!File.Exists(oldPath) || File.Exists(newPath))
                return false;

            // Load, update, and save
            var json = await File.ReadAllTextAsync(oldPath);
            var templateData = JsonSerializer.Deserialize<SavedTemplate>(json, JsonOptions);

            if (templateData != null)
            {
                templateData.Name = newName;
                templateData.ModifiedAt = DateTime.UtcNow;

                var newJson = JsonSerializer.Serialize(templateData, JsonOptions);
                await File.WriteAllTextAsync(newPath, newJson);
                File.Delete(oldPath);

                return true;
            }
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, $"Failed to rename report template '{oldName}' to '{newName}'");
        }

        return false;
    }

    /// <summary>
    /// Checks if a template exists.
    /// </summary>
    public bool TemplateExists(string templateName)
    {
        var sanitizedName = SanitizeFileName(templateName);
        var filePath = Path.Combine(_templatesDirectory, $"{sanitizedName}.argotemplate");
        return File.Exists(filePath);
    }

    /// <summary>
    /// Gets the images directory path for storing embedded images.
    /// </summary>
    public string GetImagesDirectory()
    {
        var imagesDir = Path.Combine(_templatesDirectory, "Images");
        if (!Directory.Exists(imagesDir))
        {
            Directory.CreateDirectory(imagesDir);
        }
        return imagesDir;
    }

    /// <summary>
    /// Copies an image to the template images directory.
    /// </summary>
    public string CopyImageToStorage(string sourcePath)
    {
        if (string.IsNullOrEmpty(sourcePath) || !File.Exists(sourcePath))
            return sourcePath;

        try
        {
            var imagesDir = GetImagesDirectory();
            var fileName = Path.GetFileName(sourcePath);
            var destPath = Path.Combine(imagesDir, fileName);

            // Generate unique filename if needed
            int counter = 1;
            var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            while (File.Exists(destPath))
            {
                fileName = $"{nameWithoutExt}_{counter}{extension}";
                destPath = Path.Combine(imagesDir, fileName);
                counter++;
            }

            File.Copy(sourcePath, destPath, false);
            return fileName;
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.FileSystem, "Failed to copy image to template storage");
            return sourcePath;
        }
    }

    /// <summary>
    /// Resolves an image path (handles both relative and absolute paths).
    /// </summary>
    public string ResolveImagePath(string imagePath)
    {
        if (string.IsNullOrEmpty(imagePath))
            return imagePath;

        if (Path.IsPathRooted(imagePath) && File.Exists(imagePath))
            return imagePath;

        var imagesDir = GetImagesDirectory();
        var possiblePath = Path.Combine(imagesDir, imagePath);

        if (File.Exists(possiblePath))
            return possiblePath;

        return imagePath;
    }

    /// <summary>
    /// Sanitizes a filename by removing invalid characters.
    /// </summary>
    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        return sanitized.Trim();
    }
}

/// <summary>
/// Container for saved template data.
/// </summary>
public class SavedTemplate
{
    public string Name { get; set; } = string.Empty;
    public ReportConfiguration? Configuration { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? FilePath { get; set; }
}
