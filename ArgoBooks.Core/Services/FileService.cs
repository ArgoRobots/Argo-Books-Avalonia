using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models;

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
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
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
            // Create company directory inside temp
            var companyDir = Path.Combine(tempDirectory, companyName);
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
            await WriteJsonAsync(companyDir, "sales.json", companyData.Sales, cancellationToken);
            await WriteJsonAsync(companyDir, "purchases.json", companyData.Purchases, cancellationToken);
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

        // Check version compatibility
        if (!footerService.IsVersionCompatible(footer))
            throw new NotSupportedException($"File version {footer.Version} is not supported.");

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

        // Create footer
        var footer = new FileFooter
        {
            Version = "1.0.0",
            IsEncrypted = !string.IsNullOrEmpty(password),
            Salt = salt,
            Iv = iv,
            PasswordHash = passwordHash,
            CompanyName = GetCompanyNameFromDirectory(tempDirectory),
            Accountants = await GetAccountantNamesAsync(tempDirectory, cancellationToken),
            ModifiedAt = DateTime.UtcNow,
            BiometricEnabled = GetBiometricEnabledFromDirectory(tempDirectory)
        };

        // Check if file exists to preserve created date
        if (File.Exists(filePath))
        {
            var existingFooter = await footerService.ReadFooterAsync(filePath, cancellationToken);
            if (existingFooter != null)
                footer.CreatedAt = existingFooter.CreatedAt;
        }

        // Write to file
        await using var fileStream = new FileStream(
            filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, useAsync: true);

        // Write content
        contentStream.Position = 0;
        await contentStream.CopyToAsync(fileStream, cancellationToken);

        // Write footer
        await footerService.WriteFooterAsync(fileStream, footer, cancellationToken);
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
    public async Task<CompanyData> LoadCompanyDataAsync(
        string tempDirectory,
        CancellationToken cancellationToken = default)
    {
        var data = new CompanyData
        {
            Settings = await ReadJsonAsync<CompanySettings>(tempDirectory, "appSettings.json", cancellationToken)
                ?? new CompanySettings(),
            IdCounters = await ReadJsonAsync<IdCounters>(tempDirectory, "idCounters.json", cancellationToken)
                ?? new IdCounters(),
            Customers = await ReadJsonAsync<List<Models.Entities.Customer>>(tempDirectory, "customers.json", cancellationToken) ?? [],
            Products = await ReadJsonAsync<List<Models.Entities.Product>>(tempDirectory, "products.json", cancellationToken) ?? [],
            Suppliers = await ReadJsonAsync<List<Models.Entities.Supplier>>(tempDirectory, "suppliers.json", cancellationToken) ?? [],
            Employees = await ReadJsonAsync<List<Models.Entities.Employee>>(tempDirectory, "employees.json", cancellationToken) ?? [],
            Departments = await ReadJsonAsync<List<Models.Entities.Department>>(tempDirectory, "departments.json", cancellationToken) ?? [],
            Categories = await ReadJsonAsync<List<Models.Entities.Category>>(tempDirectory, "categories.json", cancellationToken) ?? [],
            Accountants = await ReadJsonAsync<List<Models.Entities.Accountant>>(tempDirectory, "accountants.json", cancellationToken) ?? [],
            Locations = await ReadJsonAsync<List<Models.Entities.Location>>(tempDirectory, "locations.json", cancellationToken) ?? [],
            Sales = await ReadJsonAsync<List<Models.Transactions.Sale>>(tempDirectory, "sales.json", cancellationToken) ?? [],
            Purchases = await ReadJsonAsync<List<Models.Transactions.Purchase>>(tempDirectory, "purchases.json", cancellationToken) ?? [],
            Invoices = await ReadJsonAsync<List<Models.Transactions.Invoice>>(tempDirectory, "invoices.json", cancellationToken) ?? [],
            Payments = await ReadJsonAsync<List<Models.Transactions.Payment>>(tempDirectory, "payments.json", cancellationToken) ?? [],
            RecurringInvoices = await ReadJsonAsync<List<Models.Transactions.RecurringInvoice>>(tempDirectory, "recurringInvoices.json", cancellationToken) ?? [],
            Inventory = await ReadJsonAsync<List<Models.Inventory.InventoryItem>>(tempDirectory, "inventory.json", cancellationToken) ?? [],
            StockAdjustments = await ReadJsonAsync<List<Models.Inventory.StockAdjustment>>(tempDirectory, "stockAdjustments.json", cancellationToken) ?? [],
            StockTransfers = await ReadJsonAsync<List<Models.Inventory.StockTransfer>>(tempDirectory, "stockTransfers.json", cancellationToken) ?? [],
            PurchaseOrders = await ReadJsonAsync<List<Models.Inventory.PurchaseOrder>>(tempDirectory, "purchaseOrders.json", cancellationToken) ?? [],
            RentalInventory = await ReadJsonAsync<List<Models.Rentals.RentalItem>>(tempDirectory, "rentalInventory.json", cancellationToken) ?? [],
            Rentals = await ReadJsonAsync<List<Models.Rentals.RentalRecord>>(tempDirectory, "rentals.json", cancellationToken) ?? [],
            Returns = await ReadJsonAsync<List<Models.Tracking.Return>>(tempDirectory, "returns.json", cancellationToken) ?? [],
            LostDamaged = await ReadJsonAsync<List<Models.Tracking.LostDamaged>>(tempDirectory, "lostDamaged.json", cancellationToken) ?? [],
            Receipts = await ReadJsonAsync<List<Models.Tracking.Receipt>>(tempDirectory, "receipts.json", cancellationToken) ?? [],
            ReportTemplates = await ReadJsonAsync<List<Models.Reports.ReportTemplate>>(tempDirectory, "reportTemplates.json", cancellationToken) ?? []
        };

        return data;
    }

    /// <summary>
    /// Saves all company data to a temporary directory.
    /// </summary>
    public async Task SaveCompanyDataAsync(
        string tempDirectory,
        CompanyData data,
        CancellationToken cancellationToken = default)
    {
        // Find or create the company subdirectory
        var companyDir = GetCompanyDirectory(tempDirectory);

        await WriteJsonAsync(companyDir, "appSettings.json", data.Settings, cancellationToken);
        await WriteJsonAsync(companyDir, "idCounters.json", data.IdCounters, cancellationToken);
        await WriteJsonAsync(companyDir, "customers.json", data.Customers, cancellationToken);
        await WriteJsonAsync(companyDir, "products.json", data.Products, cancellationToken);
        await WriteJsonAsync(companyDir, "suppliers.json", data.Suppliers, cancellationToken);
        await WriteJsonAsync(companyDir, "employees.json", data.Employees, cancellationToken);
        await WriteJsonAsync(companyDir, "departments.json", data.Departments, cancellationToken);
        await WriteJsonAsync(companyDir, "categories.json", data.Categories, cancellationToken);
        await WriteJsonAsync(companyDir, "accountants.json", data.Accountants, cancellationToken);
        await WriteJsonAsync(companyDir, "locations.json", data.Locations, cancellationToken);
        await WriteJsonAsync(companyDir, "sales.json", data.Sales, cancellationToken);
        await WriteJsonAsync(companyDir, "purchases.json", data.Purchases, cancellationToken);
        await WriteJsonAsync(companyDir, "invoices.json", data.Invoices, cancellationToken);
        await WriteJsonAsync(companyDir, "payments.json", data.Payments, cancellationToken);
        await WriteJsonAsync(companyDir, "recurringInvoices.json", data.RecurringInvoices, cancellationToken);
        await WriteJsonAsync(companyDir, "inventory.json", data.Inventory, cancellationToken);
        await WriteJsonAsync(companyDir, "stockAdjustments.json", data.StockAdjustments, cancellationToken);
        await WriteJsonAsync(companyDir, "stockTransfers.json", data.StockTransfers, cancellationToken);
        await WriteJsonAsync(companyDir, "purchaseOrders.json", data.PurchaseOrders, cancellationToken);
        await WriteJsonAsync(companyDir, "rentalInventory.json", data.RentalInventory, cancellationToken);
        await WriteJsonAsync(companyDir, "rentals.json", data.Rentals, cancellationToken);
        await WriteJsonAsync(companyDir, "returns.json", data.Returns, cancellationToken);
        await WriteJsonAsync(companyDir, "lostDamaged.json", data.LostDamaged, cancellationToken);
        await WriteJsonAsync(companyDir, "receipts.json", data.Receipts, cancellationToken);
        await WriteJsonAsync(companyDir, "reportTemplates.json", data.ReportTemplates, cancellationToken);

        data.MarkAsSaved();
    }

    #region Helper Methods

    private static string CreateTempDirectory()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), "ArgoBooks", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempPath);
        return tempPath;
    }

    private string GetCompanyNameFromDirectory(string tempDirectory)
    {
        // First try to read the company name from settings (in case it was renamed)
        try
        {
            var settingsPath = FindFileInDirectory(tempDirectory, "appSettings.json");
            if (settingsPath != null && File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<CompanySettings>(json, JsonOptions);
                if (!string.IsNullOrEmpty(settings?.Company.Name))
                    return settings.Company.Name;
            }
        }
        catch
        {
            // Fall back to directory name
        }

        // Look for company subdirectory
        var subdirs = Directory.GetDirectories(tempDirectory);
        if (subdirs.Length > 0)
            return Path.GetFileName(subdirs[0]);

        return Path.GetFileName(tempDirectory);
    }

    private bool GetBiometricEnabledFromDirectory(string tempDirectory)
    {
        // Read the biometric enabled setting from the company settings
        try
        {
            var settingsPath = FindFileInDirectory(tempDirectory, "appSettings.json");
            if (settingsPath != null && File.Exists(settingsPath))
            {
                var json = File.ReadAllText(settingsPath);
                var settings = JsonSerializer.Deserialize<CompanySettings>(json, JsonOptions);
                return settings?.Security.BiometricEnabled ?? false;
            }
        }
        catch
        {
            // Default to false
        }

        return false;
    }

    private static string GetCompanyDirectory(string tempDirectory)
    {
        // Find the deepest directory that contains data files (for backward compatibility)
        var subdirs = Directory.GetDirectories(tempDirectory);
        if (subdirs.Length == 0)
            return tempDirectory;

        // Check if this level has data files (appSettings.json is our marker)
        var candidate = subdirs[0];
        if (File.Exists(Path.Combine(candidate, "appSettings.json")))
            return candidate;

        // Otherwise recurse into subdirectories (handles nested archive case)
        return GetCompanyDirectory(candidate);
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
