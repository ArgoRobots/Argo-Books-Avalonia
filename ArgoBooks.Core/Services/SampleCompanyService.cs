using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for creating and managing sample company data.
/// Creates a temporary company populated with sample data from an Excel file.
/// </summary>
public class SampleCompanyService
{
    private readonly FileService _fileService;
    private readonly SpreadsheetImportService _importService;

    private const string SampleCompanyName = "TechFlow Solutions";

    /// <summary>
    /// Creates a new SampleCompanyService instance.
    /// </summary>
    public SampleCompanyService(FileService fileService, SpreadsheetImportService importService)
    {
        _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        _importService = importService ?? throw new ArgumentNullException(nameof(importService));
    }

    /// <summary>
    /// Creates a sample company from the provided Excel data stream.
    /// </summary>
    /// <param name="excelDataStream">Stream containing the sample company Excel data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file path to the created sample company .argo file.</returns>
    public async Task<string> CreateSampleCompanyAsync(
        Stream excelDataStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(excelDataStream);

        // Create temporary directories
        var tempRoot = Path.Combine(Path.GetTempPath(), "ArgoBooks", "Sample", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        var companyDir = Path.Combine(tempRoot, SampleCompanyName);
        Directory.CreateDirectory(companyDir);

        // Create temporary Excel file for import
        var tempExcelPath = Path.Combine(tempRoot, "SampleData.xlsx");

        try
        {
            // Write the stream to a temp file (SpreadsheetImportService requires a file path)
            await using (var fileStream = new FileStream(tempExcelPath, FileMode.Create, FileAccess.Write))
            {
                await excelDataStream.CopyToAsync(fileStream, cancellationToken);
            }

            // Create company data with sample company settings
            var companyData = CreateSampleCompanyData();

            // Import data from Excel with auto-create missing references
            var importOptions = new ImportOptions
            {
                AutoCreateMissingReferences = true
            };

            await _importService.ImportFromExcelAsync(
                tempExcelPath,
                companyData,
                importOptions,
                cancellationToken);

            // Save company data to temp directory
            await _fileService.SaveCompanyDataAsync(companyDir, companyData, cancellationToken);

            // Create receipts subdirectory
            Directory.CreateDirectory(Path.Combine(companyDir, "receipts"));

            // Get the sample company file path
            var sampleFilePath = GetSampleCompanyPath();

            // Ensure directory exists
            var sampleDir = Path.GetDirectoryName(sampleFilePath);
            if (!string.IsNullOrEmpty(sampleDir))
            {
                Directory.CreateDirectory(sampleDir);
            }

            // Save as unencrypted .argo file
            await _fileService.SaveCompanyAsync(sampleFilePath, tempRoot, password: null, cancellationToken);

            return sampleFilePath;
        }
        finally
        {
            // Clean up temp Excel file
            if (File.Exists(tempExcelPath))
            {
                try { File.Delete(tempExcelPath); }
                catch { /* Best effort */ }
            }

            // Clean up temp directory structure (the extracted data)
            // Note: The companyDir is packaged into the .argo file, so we can delete it
            if (Directory.Exists(tempRoot))
            {
                try { Directory.Delete(tempRoot, recursive: true); }
                catch { /* Best effort */ }
            }
        }
    }

    /// <summary>
    /// Creates the sample company data with default settings.
    /// </summary>
    private static CompanyData CreateSampleCompanyData()
    {
        var companyData = new CompanyData();

        // Set company information
        companyData.Settings.Company = new CompanyInfo
        {
            Name = SampleCompanyName,
            BusinessType = "Retail & Services",
            Industry = "Technology",
            Email = "info@techflowsolutions.com",
            Phone = "555-100-0000"
        };

        companyData.Settings.AppVersion = "2.0.0";

        // Enable all modules so users can explore everything
        companyData.Settings.EnabledModules = new EnabledModulesSettings
        {
            Invoices = true,
            Payments = true,
            Inventory = true,
            Employees = true,
            Rentals = true
        };

        // Set localization defaults
        companyData.Settings.Localization = new LocalizationSettings
        {
            Language = "English",
            Currency = "USD",
            DateFormat = "MM/dd/yyyy"
        };

        return companyData;
    }

    /// <summary>
    /// Gets the path where the sample company file is stored.
    /// </summary>
    public static string GetSampleCompanyPath()
    {
        return Path.Combine(
            Path.GetTempPath(),
            "ArgoBooks",
            "SampleCompany.argo");
    }

    /// <summary>
    /// Cleans up any leftover sample company files.
    /// </summary>
    public static void CleanupSampleCompanyFiles()
    {
        var sampleFilePath = GetSampleCompanyPath();

        if (File.Exists(sampleFilePath))
        {
            try { File.Delete(sampleFilePath); }
            catch { /* Best effort */ }
        }
    }
}
