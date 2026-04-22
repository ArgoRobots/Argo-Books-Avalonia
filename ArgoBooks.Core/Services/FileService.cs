using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Common;
using SkiaSharp;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for handling .argo company file operations.
/// </summary>
public class FileService(
    CompressionService compressionService,
    FooterService footerService,
    IEncryptionService? encryptionService = null)
    : IFileService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    /// <inheritdoc />
    public async Task CreateCompanyAsync(
        string filePath,
        string companyName,
        CancellationToken cancellationToken = default)
    {
        // Create temp directory
        var tempDirectory = CreateTempDirectory();

        try
        {
            // Create company directory inside temp (sanitize name to prevent path traversal)
            var sanitizedName = SanitizeDirectoryName(companyName);
            var companyDir = Path.Combine(tempDirectory, sanitizedName);
            Directory.CreateDirectory(companyDir);

            // Create default company data
            var companyData = new CompanyData();
            companyData.Settings.Company.Name = companyName;
            companyData.Settings.AppVersion = "1.0.0";

            // Write initial settings file
            await WriteJsonAsync(companyDir, "appSettings.json", companyData.Settings, cancellationToken);

            // Write empty data files
            await WriteJsonAsync(companyDir, "customers.json", companyData.Customers, cancellationToken);
            await WriteJsonAsync(companyDir, "products.json", companyData.Products, cancellationToken);
            await WriteJsonAsync(companyDir, "suppliers.json", companyData.Suppliers, cancellationToken);
            await WriteJsonAsync(companyDir, "employees.json", companyData.Employees, cancellationToken);
            await WriteJsonAsync(companyDir, "departments.json", companyData.Departments, cancellationToken);
            await WriteJsonAsync(companyDir, "categories.json", companyData.Categories, cancellationToken);
            await WriteJsonAsync(companyDir, "accountants.json", companyData.Accountants, cancellationToken);
            await WriteJsonAsync(companyDir, "locations.json", companyData.Locations, cancellationToken);
            await WriteJsonAsync(companyDir, "revenues.json", companyData.Revenues, cancellationToken);
            await WriteJsonAsync(companyDir, "expenses.json", companyData.Expenses, cancellationToken);
            await WriteJsonAsync(companyDir, "invoices.json", companyData.Invoices, cancellationToken);
            await WriteJsonAsync(companyDir, "payments.json", companyData.Payments, cancellationToken);
            await WriteJsonAsync(companyDir, "recurringInvoices.json", companyData.RecurringInvoices, cancellationToken);
            await WriteJsonAsync(companyDir, "inventory.json", companyData.Inventory, cancellationToken);
            await WriteJsonAsync(companyDir, "stockAdjustments.json", companyData.StockAdjustments, cancellationToken);
            await WriteJsonAsync(companyDir, "stockTransfers.json", companyData.StockTransfers, cancellationToken);
            await WriteJsonAsync(companyDir, "purchaseOrders.json", companyData.PurchaseOrders, cancellationToken);
            await WriteJsonAsync(companyDir, "rentalInventory.json", companyData.RentalInventory, cancellationToken);
            await WriteJsonAsync(companyDir, "rentals.json", companyData.Rentals, cancellationToken);
            await WriteJsonAsync(companyDir, "returns.json", companyData.Returns, cancellationToken);
            await WriteJsonAsync(companyDir, "lostDamaged.json", companyData.LostDamaged, cancellationToken);
            await WriteJsonAsync(companyDir, "receipts.json", companyData.Receipts, cancellationToken);
            await WriteJsonAsync(companyDir, "reportTemplates.json", companyData.ReportTemplates, cancellationToken);
            await WriteJsonAsync(companyDir, "idCounters.json", companyData.IdCounters, cancellationToken);
            await WriteJsonAsync(companyDir, "eventLog.json", companyData.EventLog, cancellationToken);
            await WriteJsonAsync(companyDir, "pendingConversions.json", companyData.PendingConversions, cancellationToken);

            // Create receipts subdirectory
            Directory.CreateDirectory(Path.Combine(companyDir, "receipts"));

            // Save to file
            await SaveCompanyAsync(filePath, companyDir, null, cancellationToken);
        }
        finally
        {
            // Cleanup temp directory
            if (Directory.Exists(tempDirectory))
                Directory.Delete(tempDirectory, recursive: true);
        }
    }

    /// <inheritdoc />
    public async Task<string> OpenCompanyAsync(
        string filePath,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        // Read footer first
        var footer = await footerService.ReadFooterAsync(filePath, cancellationToken)
            ?? throw new InvalidDataException("Invalid file format or corrupted file.");

        // Verify password if encrypted
        if (footer.IsEncrypted)
        {
            if (string.IsNullOrEmpty(password))
                throw new UnauthorizedAccessException("Password is required for this file.");

            if (encryptionService == null)
                throw new InvalidOperationException("Encryption service not available.");

            if (!encryptionService.ValidatePassword(password, footer.PasswordHash!, footer.Salt!))
                throw new UnauthorizedAccessException("Invalid password.");
        }

        // Read content (excluding footer)
        await using var contentStream = await footerService.ReadContentAsync(filePath, cancellationToken);

        // Decrypt if needed
        Stream dataStream = contentStream;
        if (footer.IsEncrypted && encryptionService != null)
        {
            dataStream = await encryptionService.DecryptAsync(contentStream, password!, footer.Salt!, footer.Iv!);
        }

        // Decompress GZip
        await using var decompressedStream = await compressionService.DecompressGZipAsync(dataStream, cancellationToken);

        // Extract TAR to temp directory
        var tempDirectory = CreateTempDirectory();
        await compressionService.ExtractTarArchiveAsync(decompressedStream, tempDirectory, cancellationToken);

        return tempDirectory;
    }

    /// <inheritdoc />
    public async Task SaveCompanyAsync(
        string filePath,
        string tempDirectory,
        string? password = null,
        CancellationToken cancellationToken = default)
    {
        // Create TAR archive - use includeBaseDirectory: false to avoid nesting under temp dir GUID
        await using var tarStream = await compressionService.CreateTarArchiveAsync(
            tempDirectory, includeBaseDirectory: false, cancellationToken);

        // Compress with GZip
        await using var compressedStream = await compressionService.CompressGZipAsync(
            tarStream, cancellationToken: cancellationToken);

        // Encrypt if password provided
        Stream contentStream = compressedStream;
        string? salt = null;
        string? iv = null;
        string? passwordHash = null;

        if (!string.IsNullOrEmpty(password) && encryptionService != null)
        {
            salt = encryptionService.GenerateSalt();
            iv = encryptionService.GenerateIv();
            passwordHash = encryptionService.HashPassword(password, salt);
            contentStream = await encryptionService.EncryptAsync(compressedStream, password, salt, iv);
        }

        // Create footer — read settings once and share across footer fields
        var cachedSettings = ReadSettingsFromDirectory(tempDirectory);
        var footer = new FileFooter
        {
            Version = GetAppVersionFromDirectory(tempDirectory, cachedSettings),
            IsEncrypted = !string.IsNullOrEmpty(password),
            Salt = salt,
            Iv = iv,
            PasswordHash = passwordHash,
            CompanyName = GetCompanyNameFromDirectory(tempDirectory, cachedSettings),
            Accountants = await GetAccountantNamesAsync(tempDirectory, cancellationToken),
            ModifiedAt = DateTime.UtcNow,
            BiometricEnabled = GetBiometricEnabledFromDirectory(cachedSettings),
            LogoThumbnail = GenerateLogoThumbnail(tempDirectory)
        };

        // Check if file exists to preserve created date
        if (File.Exists(filePath))
        {
            var existingFooter = await footerService.ReadFooterAsync(filePath, cancellationToken);
            if (existingFooter != null)
                footer.CreatedAt = existingFooter.CreatedAt;
        }

        // Write to file (atomic: write to temp, then move)
        var tempPath = filePath + ".tmp";
        try
        {
            await using (var fileStream = new FileStream(
                tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true))
            {
                contentStream.Position = 0;
                await contentStream.CopyToAsync(fileStream, cancellationToken);
                await footerService.WriteFooterAsync(fileStream, footer, cancellationToken);
            }
            File.Move(tempPath, filePath, overwrite: true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                try { File.Delete(tempPath); } catch { /* best effort */ }
            }
            throw;
        }
    }

    /// <inheritdoc />
    public Task CloseCompanyAsync(string tempDirectory)
    {
        if (Directory.Exists(tempDirectory))
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch
            {
                // Best effort cleanup
            }
        }

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<bool> IsFileEncryptedAsync(string filePath)
    {
        var footer = await footerService.ReadFooterAsync(filePath);
        return footer?.IsEncrypted ?? false;
    }

    /// <inheritdoc />
    public async Task<T?> ReadJsonAsync<T>(
        string tempDirectory,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        // Find the file (may be in a subdirectory with company name)
        var filePath = FindFileInDirectory(tempDirectory, fileName);
        if (filePath == null || !File.Exists(filePath))
            return default;

        var json = await File.ReadAllTextAsync(filePath, cancellationToken);
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    /// <inheritdoc />
    public async Task WriteJsonAsync<T>(
        string tempDirectory,
        string fileName,
        T data,
        CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(tempDirectory, fileName);
        var json = JsonSerializer.Serialize(data, JsonOptions);
        await File.WriteAllTextAsync(filePath, json, cancellationToken);
    }

    /// <summary>
    /// Loads all company data from a temporary directory.
    /// </summary>
    /// <remarks>
    /// Reads are issued concurrently — the files are already extracted to disk by the caller,
    /// the collections have no cross-deserialization dependencies, and ReadJsonAsync uses a
    /// shared immutable <see cref="JsonOptions"/> instance, so concurrent deserialization is safe.
    /// </remarks>
    public async Task<CompanyData> LoadCompanyDataAsync(
        string tempDirectory,
        CancellationToken cancellationToken = default)
    {
        var settingsTask          = ReadJsonAsync<CompanySettings>(tempDirectory, "appSettings.json", cancellationToken);
        var idCountersTask        = ReadJsonAsync<IdCounters>(tempDirectory, "idCounters.json", cancellationToken);
        var customersTask         = ReadJsonAsync<List<Models.Entities.Customer>>(tempDirectory, "customers.json", cancellationToken);
        var productsTask          = ReadJsonAsync<List<Models.Entities.Product>>(tempDirectory, "products.json", cancellationToken);
        var suppliersTask         = ReadJsonAsync<List<Models.Entities.Supplier>>(tempDirectory, "suppliers.json", cancellationToken);
        var employeesTask         = ReadJsonAsync<List<Models.Entities.Employee>>(tempDirectory, "employees.json", cancellationToken);
        var departmentsTask       = ReadJsonAsync<List<Models.Entities.Department>>(tempDirectory, "departments.json", cancellationToken);
        var categoriesTask        = ReadJsonAsync<List<Models.Entities.Category>>(tempDirectory, "categories.json", cancellationToken);
        var accountantsTask       = ReadJsonAsync<List<Models.Entities.Accountant>>(tempDirectory, "accountants.json", cancellationToken);
        var locationsTask         = ReadJsonAsync<List<Models.Entities.Location>>(tempDirectory, "locations.json", cancellationToken);
        var revenuesTask          = ReadJsonAsync<List<Models.Transactions.Revenue>>(tempDirectory, "revenues.json", cancellationToken);
        var expensesTask          = ReadJsonAsync<List<Models.Transactions.Expense>>(tempDirectory, "expenses.json", cancellationToken);
        var invoicesTask          = ReadJsonAsync<List<Models.Transactions.Invoice>>(tempDirectory, "invoices.json", cancellationToken);
        var paymentsTask          = ReadJsonAsync<List<Models.Transactions.Payment>>(tempDirectory, "payments.json", cancellationToken);
        var recurringInvoicesTask = ReadJsonAsync<List<Models.Transactions.RecurringInvoice>>(tempDirectory, "recurringInvoices.json", cancellationToken);
        var inventoryTask         = ReadJsonAsync<List<Models.Inventory.InventoryItem>>(tempDirectory, "inventory.json", cancellationToken);
        var stockAdjustmentsTask  = ReadJsonAsync<List<Models.Inventory.StockAdjustment>>(tempDirectory, "stockAdjustments.json", cancellationToken);
        var stockTransfersTask    = ReadJsonAsync<List<Models.Inventory.StockTransfer>>(tempDirectory, "stockTransfers.json", cancellationToken);
        var purchaseOrdersTask    = ReadJsonAsync<List<Models.Inventory.PurchaseOrder>>(tempDirectory, "purchaseOrders.json", cancellationToken);
        var rentalInventoryTask   = ReadJsonAsync<List<Models.Rentals.RentalItem>>(tempDirectory, "rentalInventory.json", cancellationToken);
        var rentalsTask           = ReadJsonAsync<List<Models.Rentals.RentalRecord>>(tempDirectory, "rentals.json", cancellationToken);
        var returnsTask           = ReadJsonAsync<List<Models.Tracking.Return>>(tempDirectory, "returns.json", cancellationToken);
        var lostDamagedTask       = ReadJsonAsync<List<Models.Tracking.LostDamaged>>(tempDirectory, "lostDamaged.json", cancellationToken);
        var receiptsTask          = ReadJsonAsync<List<Models.Tracking.Receipt>>(tempDirectory, "receipts.json", cancellationToken);
        var reportTemplatesTask   = ReadJsonAsync<List<Models.Reports.ReportTemplate>>(tempDirectory, "reportTemplates.json", cancellationToken);
        var invoiceTemplatesTask  = ReadJsonAsync<List<Models.Invoices.InvoiceTemplate>>(tempDirectory, "invoiceTemplates.json", cancellationToken);
        var eventLogTask          = ReadJsonAsync<List<AuditEvent>>(tempDirectory, "eventLog.json", cancellationToken);
        var pendingConversionsTask = ReadJsonAsync<List<PendingConversion>>(tempDirectory, "pendingConversions.json", cancellationToken);
        var forecastRecordsTask   = ReadJsonAsync<List<Models.Insights.ForecastAccuracyRecord>>(tempDirectory, "forecastRecords.json", cancellationToken);

        await Task.WhenAll(
            settingsTask, idCountersTask, customersTask, productsTask, suppliersTask,
            employeesTask, departmentsTask, categoriesTask, accountantsTask, locationsTask,
            revenuesTask, expensesTask, invoicesTask, paymentsTask, recurringInvoicesTask,
            inventoryTask, stockAdjustmentsTask, stockTransfersTask, purchaseOrdersTask,
            rentalInventoryTask, rentalsTask, returnsTask, lostDamagedTask, receiptsTask,
            reportTemplatesTask, invoiceTemplatesTask, eventLogTask, pendingConversionsTask,
            forecastRecordsTask);

        return new CompanyData
        {
            Settings = settingsTask.Result ?? new CompanySettings(),
            IdCounters = idCountersTask.Result ?? new IdCounters(),
            Customers = customersTask.Result ?? [],
            Products = productsTask.Result ?? [],
            Suppliers = suppliersTask.Result ?? [],
            Employees = employeesTask.Result ?? [],
            Departments = departmentsTask.Result ?? [],
            Categories = categoriesTask.Result ?? [],
            Accountants = accountantsTask.Result ?? [],
            Locations = locationsTask.Result ?? [],
            Revenues = revenuesTask.Result ?? [],
            Expenses = expensesTask.Result ?? [],
            Invoices = invoicesTask.Result ?? [],
            Payments = paymentsTask.Result ?? [],
            RecurringInvoices = recurringInvoicesTask.Result ?? [],
            Inventory = inventoryTask.Result ?? [],
            StockAdjustments = stockAdjustmentsTask.Result ?? [],
            StockTransfers = stockTransfersTask.Result ?? [],
            PurchaseOrders = purchaseOrdersTask.Result ?? [],
            RentalInventory = rentalInventoryTask.Result ?? [],
            Rentals = rentalsTask.Result ?? [],
            Returns = returnsTask.Result ?? [],
            LostDamaged = lostDamagedTask.Result ?? [],
            Receipts = receiptsTask.Result ?? [],
            ReportTemplates = reportTemplatesTask.Result ?? [],
            InvoiceTemplates = invoiceTemplatesTask.Result ?? [],
            EventLog = eventLogTask.Result ?? [],
            PendingConversions = pendingConversionsTask.Result ?? [],
            ForecastRecords = forecastRecordsTask.Result ?? []
        };
    }

    /// <summary>
    /// Saves all company data to a temporary directory.
    /// </summary>
    /// <param name="companyDirectory">The company subdirectory (not the temp root) where data files should be saved.</param>
    /// <param name="data">The company data to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task SaveCompanyDataAsync(
        string companyDirectory,
        CompanyData data,
        CancellationToken cancellationToken = default)
    {
        // Write directly to the provided company directory - caller is responsible for providing the correct path
        await WriteJsonAsync(companyDirectory, "appSettings.json", data.Settings, cancellationToken);
        await WriteJsonAsync(companyDirectory, "idCounters.json", data.IdCounters, cancellationToken);
        await WriteJsonAsync(companyDirectory, "customers.json", data.Customers, cancellationToken);
        await WriteJsonAsync(companyDirectory, "products.json", data.Products, cancellationToken);
        await WriteJsonAsync(companyDirectory, "suppliers.json", data.Suppliers, cancellationToken);
        await WriteJsonAsync(companyDirectory, "employees.json", data.Employees, cancellationToken);
        await WriteJsonAsync(companyDirectory, "departments.json", data.Departments, cancellationToken);
        await WriteJsonAsync(companyDirectory, "categories.json", data.Categories, cancellationToken);
        await WriteJsonAsync(companyDirectory, "accountants.json", data.Accountants, cancellationToken);
        await WriteJsonAsync(companyDirectory, "locations.json", data.Locations, cancellationToken);
        await WriteJsonAsync(companyDirectory, "revenues.json", data.Revenues, cancellationToken);
        await WriteJsonAsync(companyDirectory, "expenses.json", data.Expenses, cancellationToken);
        await WriteJsonAsync(companyDirectory, "invoices.json", data.Invoices, cancellationToken);
        await WriteJsonAsync(companyDirectory, "payments.json", data.Payments, cancellationToken);
        await WriteJsonAsync(companyDirectory, "recurringInvoices.json", data.RecurringInvoices, cancellationToken);
        await WriteJsonAsync(companyDirectory, "inventory.json", data.Inventory, cancellationToken);
        await WriteJsonAsync(companyDirectory, "stockAdjustments.json", data.StockAdjustments, cancellationToken);
        await WriteJsonAsync(companyDirectory, "stockTransfers.json", data.StockTransfers, cancellationToken);
        await WriteJsonAsync(companyDirectory, "purchaseOrders.json", data.PurchaseOrders, cancellationToken);
        await WriteJsonAsync(companyDirectory, "rentalInventory.json", data.RentalInventory, cancellationToken);
        await WriteJsonAsync(companyDirectory, "rentals.json", data.Rentals, cancellationToken);
        await WriteJsonAsync(companyDirectory, "returns.json", data.Returns, cancellationToken);
        await WriteJsonAsync(companyDirectory, "lostDamaged.json", data.LostDamaged, cancellationToken);
        await WriteJsonAsync(companyDirectory, "receipts.json", data.Receipts, cancellationToken);
        await WriteJsonAsync(companyDirectory, "reportTemplates.json", data.ReportTemplates, cancellationToken);
        await WriteJsonAsync(companyDirectory, "invoiceTemplates.json", data.InvoiceTemplates, cancellationToken);
        await WriteJsonAsync(companyDirectory, "eventLog.json", data.EventLog, cancellationToken);
        await WriteJsonAsync(companyDirectory, "pendingConversions.json", data.PendingConversions, cancellationToken);
        await WriteJsonAsync(companyDirectory, "forecastRecords.json", data.ForecastRecords, cancellationToken);

        data.MarkAsSaved();
    }

    /// <inheritdoc />
    public async Task<byte[]?> ExtractLogoFromFileAsync(
        string filePath,
        CancellationToken cancellationToken = default)
    {
        var footer = await footerService.ReadFooterAsync(filePath, cancellationToken);
        if (footer?.LogoThumbnail == null)
            return null;

        try
        {
            return Convert.FromBase64String(footer.LogoThumbnail);
        }
        catch
        {
            return null;
        }
    }

    #region Helper Methods

    private static string CreateTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "ArgoBooks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    /// <summary>
    /// Reads and deserializes appSettings.json once from the given directory.
    /// Returns null if the file is missing or cannot be parsed.
    /// </summary>
    private CompanySettings? ReadSettingsFromDirectory(string tempDirectory)
    {
        try
        {
            var settingsPath = FindFileInDirectory(tempDirectory, "appSettings.json");
            if (settingsPath != null && File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                return JsonSerializer.Deserialize<CompanySettings>(json, JsonOptions);
            }
        }
        catch
        {
            // Unreadable or malformed settings
        }

        return null;
    }

    private string GetCompanyNameFromDirectory(string tempDirectory, CompanySettings? settings = null)
    {
        settings ??= ReadSettingsFromDirectory(tempDirectory);
        if (!string.IsNullOrEmpty(settings?.Company.Name))
            return settings.Company.Name;

        // Look for company subdirectory
        var subdirs = Directory.GetDirectories(tempDirectory);
        if (subdirs.Length > 0)
            return Path.GetFileName(subdirs[0]);

        return Path.GetFileName(tempDirectory);
    }

    private static bool GetBiometricEnabledFromDirectory(CompanySettings? settings)
    {
        return settings?.Security.BiometricEnabled ?? false;
    }

    private static string GetAppVersionFromDirectory(string tempDirectory, CompanySettings? settings = null)
    {
        if (!string.IsNullOrEmpty(settings?.AppVersion))
            return settings.AppVersion;

        return "1.0.0";
    }

    private static string? FindFileInDirectory(string directory, string fileName, int maxDepth = 3)
    {
        // First check directly in directory
        var directPath = Path.Combine(directory, fileName);
        if (File.Exists(directPath))
            return directPath;

        if (maxDepth <= 0)
            return null;

        // Check in subdirectories recursively (for backward compatibility with nested archives)
        foreach (var subDir in Directory.GetDirectories(directory))
        {
            var result = FindFileInDirectory(subDir, fileName, maxDepth - 1);
            if (result != null)
                return result;
        }

        return null;
    }

    /// <summary>
    /// Sanitizes a directory name by removing path separators and traversal sequences.
    /// </summary>
    private static string SanitizeDirectoryName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(name.Where(c => !invalid.Contains(c)).ToArray());
        sanitized = sanitized.Replace("..", "");
        var result = string.IsNullOrWhiteSpace(sanitized) ? "Company" : sanitized.Trim();

        // Verify the sanitized name doesn't escape the intended directory
        var testPath = Path.Combine(Path.GetTempPath(), result);
        var resolvedPath = Path.GetFullPath(testPath);
        if (!resolvedPath.StartsWith(Path.GetTempPath(), StringComparison.OrdinalIgnoreCase))
            return "Company";

        return result;
    }

    private const int ThumbnailMaxSize = 64;

    private static string? GenerateLogoThumbnail(string tempDirectory)
    {
        // Find logo file in the temp directory
        var logoPath = FindLogoFileInDirectory(tempDirectory);
        if (logoPath == null)
            return null;

        try
        {
            using var bitmap = SKBitmap.Decode(logoPath);
            if (bitmap == null)
                return null;

            // Calculate scaled dimensions preserving aspect ratio
            var scale = Math.Min(
                (float)ThumbnailMaxSize / bitmap.Width,
                (float)ThumbnailMaxSize / bitmap.Height);

            SKBitmap target;
            if (scale >= 1f)
            {
                // Image is already small enough, use as-is
                target = bitmap;
            }
            else
            {
                var newWidth = Math.Max(1, (int)(bitmap.Width * scale));
                var newHeight = Math.Max(1, (int)(bitmap.Height * scale));

                target = new SKBitmap(newWidth, newHeight);
                using var canvas = new SKCanvas(target);
                canvas.DrawBitmap(bitmap, new SKRect(0, 0, newWidth, newHeight));
            }

            string result;
            using (var image = SKImage.FromBitmap(target))
            using (var encoded = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                result = Convert.ToBase64String(encoded.ToArray());
            }

            if (!ReferenceEquals(target, bitmap))
                target.Dispose();

            return result;
        }
        catch
        {
            return null;
        }
    }

    private static string? FindLogoFileInDirectory(string directory, int maxDepth = 3)
    {
        try
        {
            foreach (var file in Directory.GetFiles(directory))
            {
                var name = Path.GetFileName(file);
                if (name.StartsWith("logo.", StringComparison.OrdinalIgnoreCase))
                    return file;
            }

            if (maxDepth <= 0)
                return null;

            foreach (var subDir in Directory.GetDirectories(directory))
            {
                var result = FindLogoFileInDirectory(subDir, maxDepth - 1);
                if (result != null)
                    return result;
            }
        }
        catch
        {
            // Directory may be inaccessible
        }

        return null;
    }

    private async Task<List<string>> GetAccountantNamesAsync(
        string tempDirectory,
        CancellationToken cancellationToken)
    {
        var accountants = await ReadJsonAsync<List<Models.Entities.Accountant>>(
            tempDirectory, "accountants.json", cancellationToken);

        return accountants?.Select(a => a.Name).ToList() ?? [];
    }

    #endregion
}
