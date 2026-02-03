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
    /// Result of validating sample company data before import.
    /// </summary>
    public class SampleCompanyValidationContext
    {
        public ImportValidationResult ValidationResult { get; set; } = new();
        public string TempExcelPath { get; set; } = string.Empty;
        public string TempRoot { get; set; } = string.Empty;
        public CompanyData CompanyData { get; set; } = new();
    }

    /// <summary>
    /// Validates sample company data from the provided Excel stream.
    /// Call FinishSampleCompanyCreationAsync to complete import, or CleanupValidationContext to cancel.
    /// </summary>
    public async Task<SampleCompanyValidationContext> ValidateSampleCompanyAsync(
        Stream excelDataStream,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(excelDataStream);

        var context = new SampleCompanyValidationContext();

        // Create temporary directories
        context.TempRoot = Path.Combine(Path.GetTempPath(), "ArgoBooks", "Sample", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(context.TempRoot);

        context.TempExcelPath = Path.Combine(context.TempRoot, "SampleData.xlsx");

        // Write the stream to a temp file
        await using (var fileStream = new FileStream(context.TempExcelPath, FileMode.Create, FileAccess.Write))
        {
            await excelDataStream.CopyToAsync(fileStream, cancellationToken);
        }

        // Create company data with sample company settings
        context.CompanyData = CreateSampleCompanyData();

        // Validate the import (same as regular imports)
        context.ValidationResult = await _importService.ValidateImportAsync(
            context.TempExcelPath,
            context.CompanyData,
            cancellationToken);

        return context;
    }

    /// <summary>
    /// Completes sample company creation after validation.
    /// </summary>
    public async Task<string> FinishSampleCompanyCreationAsync(
        SampleCompanyValidationContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var companyDir = Path.Combine(context.TempRoot, SampleCompanyName);
        Directory.CreateDirectory(companyDir);

        try
        {
            // Import data from Excel with auto-create missing references
            var importOptions = new ImportOptions
            {
                AutoCreateMissingReferences = true
            };

            await _importService.ImportFromExcelAsync(
                context.TempExcelPath,
                context.CompanyData,
                importOptions,
                cancellationToken);

            // Save company data to temp directory
            await _fileService.SaveCompanyDataAsync(companyDir, context.CompanyData, cancellationToken);

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
            await _fileService.SaveCompanyAsync(sampleFilePath, context.TempRoot, password: null, cancellationToken);

            return sampleFilePath;
        }
        finally
        {
            CleanupValidationContext(context);
        }
    }

    /// <summary>
    /// Cleans up temporary files from validation (call if user cancels).
    /// </summary>
    public static void CleanupValidationContext(SampleCompanyValidationContext context)
    {
        if (context == null) return;

        if (File.Exists(context.TempExcelPath))
        {
            try { File.Delete(context.TempExcelPath); }
            catch { /* Best effort */ }
        }

        if (Directory.Exists(context.TempRoot))
        {
            try { Directory.Delete(context.TempRoot, recursive: true); }
            catch { /* Best effort */ }
        }
    }

    /// <summary>
    /// Creates a sample company from the provided Excel data stream.
    /// </summary>
    /// <param name="excelDataStream">Stream containing the sample company Excel data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result containing file path and any validation warnings.</returns>
    [Obsolete("Use ValidateSampleCompanyAsync + FinishSampleCompanyCreationAsync for proper validation flow")]
    public async Task<SampleCompanyResult> CreateSampleCompanyAsync(
        Stream excelDataStream,
        CancellationToken cancellationToken = default)
    {
        var context = await ValidateSampleCompanyAsync(excelDataStream, cancellationToken);
        var filePath = await FinishSampleCompanyCreationAsync(context, cancellationToken);
        return new SampleCompanyResult
        {
            FilePath = filePath,
            ValidationResult = context.ValidationResult
        };
    }

    /// <summary>
    /// Result of creating a sample company (for legacy API).
    /// </summary>
    public class SampleCompanyResult
    {
        public string FilePath { get; set; } = string.Empty;
        public ImportValidationResult? ValidationResult { get; set; }
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

        companyData.Settings.AppVersion = AppInfo.VersionNumber;

        // Set localization defaults
        companyData.Settings.Localization = new LocalizationSettings
        {
            Language = "English",
            Currency = "USD",
            DateFormat = "MM/DD/YYYY"
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

    /// <summary>
    /// Time-shifts all dates in the sample data so that the most recent transaction
    /// appears as if it happened recently (within the last few days).
    /// This ensures the dashboard shows meaningful data regardless of when the sample was created.
    /// Should be called every time the sample company is opened.
    /// </summary>
    /// <param name="data">The company data to time-shift.</param>
    /// <returns>True if data was shifted, false if already current.</returns>
    public static bool TimeShiftSampleData(CompanyData data)
    {
        var maxDate = FindMaxDate(data);
        if (maxDate == DateTime.MinValue)
            return false;

        var targetDate = DateTime.Today.AddDays(-3);
        var offset = targetDate - maxDate.Date;

        if (Math.Abs(offset.TotalDays) <= 3)
            return false;

        ApplyDateOffset(data, offset);
        return true;
    }

    private static DateTime FindMaxDate(CompanyData data)
    {
        var dates = new List<DateTime>();

        // Only consider Revenue and Expense dates for time-shifting
        // This ensures the dashboard "This Month" view shows meaningful data
        dates.AddRange(data.Revenues.Select(s => s.Date));
        dates.AddRange(data.Expenses.Select(p => p.Date));

        return dates.Count > 0 ? dates.Max() : DateTime.MinValue;
    }

    private static void ApplyDateOffset(CompanyData data, TimeSpan offset)
    {
        DateTime Shift(DateTime dt)
        {
            if (dt == DateTime.MinValue || dt.Year < 1900)
                return dt;
            try { return dt.Add(offset); }
            catch (ArgumentOutOfRangeException) { return dt; }
        }

        DateTime? ShiftNullable(DateTime? dt)
        {
            if (!dt.HasValue || dt.Value == DateTime.MinValue || dt.Value.Year < 1900)
                return dt;
            try { return dt.Value.Add(offset); }
            catch (ArgumentOutOfRangeException) { return dt; }
        }

        foreach (var revenue in data.Revenues)
        {
            revenue.Date = Shift(revenue.Date);
            revenue.CreatedAt = Shift(revenue.CreatedAt);
            revenue.UpdatedAt = Shift(revenue.UpdatedAt);
        }

        foreach (var expense in data.Expenses)
        {
            expense.Date = Shift(expense.Date);
            expense.CreatedAt = Shift(expense.CreatedAt);
            expense.UpdatedAt = Shift(expense.UpdatedAt);
        }

        foreach (var invoice in data.Invoices)
        {
            invoice.IssueDate = Shift(invoice.IssueDate);
            invoice.DueDate = Shift(invoice.DueDate);
            invoice.CreatedAt = Shift(invoice.CreatedAt);
            invoice.UpdatedAt = Shift(invoice.UpdatedAt);
        }

        foreach (var payment in data.Payments)
        {
            payment.Date = Shift(payment.Date);
            payment.CreatedAt = Shift(payment.CreatedAt);
        }

        foreach (var rental in data.Rentals)
        {
            rental.StartDate = Shift(rental.StartDate);
            rental.DueDate = Shift(rental.DueDate);
            rental.ReturnDate = ShiftNullable(rental.ReturnDate);
            rental.CreatedAt = Shift(rental.CreatedAt);
            rental.UpdatedAt = Shift(rental.UpdatedAt);
        }

        foreach (var ret in data.Returns)
        {
            ret.ReturnDate = Shift(ret.ReturnDate);
            ret.CreatedAt = Shift(ret.CreatedAt);
        }

        foreach (var receipt in data.Receipts)
        {
            receipt.Date = Shift(receipt.Date);
            receipt.CreatedAt = Shift(receipt.CreatedAt);
        }

        foreach (var ld in data.LostDamaged)
        {
            ld.DateDiscovered = Shift(ld.DateDiscovered);
            ld.CreatedAt = Shift(ld.CreatedAt);
        }

        foreach (var sa in data.StockAdjustments)
            sa.Timestamp = Shift(sa.Timestamp);

        foreach (var st in data.StockTransfers)
        {
            st.TransferDate = Shift(st.TransferDate);
            st.CreatedAt = Shift(st.CreatedAt);
            st.CompletedAt = ShiftNullable(st.CompletedAt);
        }

        foreach (var po in data.PurchaseOrders)
        {
            po.OrderDate = Shift(po.OrderDate);
            po.ExpectedDeliveryDate = Shift(po.ExpectedDeliveryDate);
            po.CreatedAt = Shift(po.CreatedAt);
            po.UpdatedAt = Shift(po.UpdatedAt);
        }

        foreach (var ri in data.RecurringInvoices)
        {
            ri.StartDate = Shift(ri.StartDate);
            ri.EndDate = ShiftNullable(ri.EndDate);
            ri.NextInvoiceDate = Shift(ri.NextInvoiceDate);
            ri.CreatedAt = Shift(ri.CreatedAt);
            ri.LastGeneratedAt = ShiftNullable(ri.LastGeneratedAt);
        }

        foreach (var customer in data.Customers)
            customer.LastTransactionDate = ShiftNullable(customer.LastTransactionDate);

        foreach (var item in data.RentalInventory)
        {
            item.CreatedAt = Shift(item.CreatedAt);
            item.UpdatedAt = Shift(item.UpdatedAt);
        }

        foreach (var product in data.Products)
        {
            product.CreatedAt = Shift(product.CreatedAt);
            product.UpdatedAt = Shift(product.UpdatedAt);
        }

        foreach (var item in data.Inventory)
            item.LastUpdated = Shift(item.LastUpdated);
    }
}
