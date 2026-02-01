using System.Diagnostics;
using System.Globalization;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Telemetry;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;
using ClosedXML.Excel;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Options for controlling import behavior.
/// </summary>
public class ImportOptions
{
    /// <summary>
    /// If true, automatically create placeholder entities for missing references.
    /// </summary>
    public bool AutoCreateMissingReferences { get; set; }

    /// <summary>
    /// Specific reference types to auto-create (if AutoCreateMissingReferences is false).
    /// Keys: "Products", "Categories", "Customers", "Suppliers", "Locations", "Departments", etc.
    /// </summary>
    public HashSet<string> AutoCreateTypes { get; set; } = [];
}

/// <summary>
/// Service for importing company data from spreadsheet formats (xlsx).
/// </summary>
public class SpreadsheetImportService
{
    private readonly IErrorLogger? _errorLogger;
    private readonly ITelemetryManager? _telemetryManager;

    /// <summary>
    /// Creates a new SpreadsheetImportService.
    /// </summary>
    public SpreadsheetImportService(IErrorLogger? errorLogger = null, ITelemetryManager? telemetryManager = null)
    {
        _errorLogger = errorLogger;
        _telemetryManager = telemetryManager;
    }
    /// <summary>
    /// Validates an Excel file before importing, checking for missing references.
    /// </summary>
    public async Task<ImportValidationResult> ValidateImportAsync(
        string filePath,
        CompanyData companyData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(companyData);

        return await Task.Run(() =>
        {
            var result = new ImportValidationResult();

            try
            {
                // Open file with read sharing to allow importing even if file is open in Excel
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var workbook = new XLWorkbook(fileStream);

                // First pass: collect all IDs that will be imported
                var importedIds = CollectImportedIds(workbook);

                // Second pass: validate references
                foreach (var worksheet in workbook.Worksheets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ValidateWorksheet(worksheet, companyData, importedIds, result);
                }
            }
            catch (Exception ex)
            {
                _errorLogger?.LogError(ex, ErrorCategory.Import, $"Failed to validate import file: {Path.GetFileName(filePath)}");
                result.Errors.Add($"Failed to read file: {ex.Message}");
            }

            return result;
        }, cancellationToken);
    }

    /// <summary>
    /// Imports data from an Excel file into the company data using merge logic.
    /// Existing records with matching IDs are updated, new records are added.
    /// </summary>
    public async Task ImportFromExcelAsync(
        string filePath,
        CompanyData companyData,
        ImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(companyData);

        options ??= new ImportOptions();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await Task.Run(() =>
            {
                // Open file with read sharing to allow importing even if file is open in Excel
                using var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var workbook = new XLWorkbook(fileStream);

                // If auto-creating references, do that first
                if (options.AutoCreateMissingReferences || options.AutoCreateTypes.Count > 0)
                {
                    CreateMissingReferences(workbook, companyData, options);
                }

                foreach (var worksheet in workbook.Worksheets)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    ImportWorksheet(worksheet, companyData);
                }

                // Update ID counters based on imported data
                UpdateIdCounters(companyData);

                // Mark data as modified
                companyData.MarkAsModified();
            }, cancellationToken);

            stopwatch.Stop();
            var fileSize = new FileInfo(filePath).Length;
            _ = _telemetryManager?.TrackFeatureAsync(FeatureName.DataImported, Path.GetExtension(filePath), cancellationToken);
        }
        catch (Exception ex)
        {
            _errorLogger?.LogError(ex, ErrorCategory.Import, $"Failed to import from: {Path.GetFileName(filePath)}");
            throw;
        }
    }

    #region Validation

    private Dictionary<string, HashSet<string>> CollectImportedIds(XLWorkbook workbook)
    {
        var ids = new Dictionary<string, HashSet<string>>();

        foreach (var worksheet in workbook.Worksheets)
        {
            var headers = GetHeaders(worksheet);
            if (headers.Count == 0) continue;

            var rows = GetDataRows(worksheet, headers.Count);
            var sheetName = worksheet.Name;

            var idColumn = sheetName switch
            {
                "Invoices" => "Invoice #",
                _ => "ID"
            };

            if (!headers.Contains(idColumn)) continue;

            var entityType = GetEntityTypeFromSheetName(sheetName);
            if (string.IsNullOrEmpty(entityType)) continue;

            if (!ids.ContainsKey(entityType))
                ids[entityType] = [];

            foreach (var row in rows)
            {
                var id = GetString(row, headers, idColumn);
                if (!string.IsNullOrEmpty(id))
                    ids[entityType].Add(id);
            }
        }

        return ids;
    }

    private static string GetEntityTypeFromSheetName(string sheetName)
    {
        return sheetName switch
        {
            "Customers" => "Customers",
            "Suppliers" => "Suppliers",
            "Products" => "Products",
            "Categories" => "Categories",
            "Locations" => "Locations",
            "Departments" => "Departments",
            "Invoices" => "Invoices",
            "Inventory" => "Inventory",
            "Rental Inventory" => "RentalInventory",
            "Purchase Orders" => "PurchaseOrders",
            _ => string.Empty
        };
    }

    private void ValidateWorksheet(
        IXLWorksheet worksheet,
        CompanyData data,
        Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var sheetName = worksheet.Name;
        var headers = GetHeaders(worksheet);
        if (headers.Count == 0) return;

        var rows = GetDataRows(worksheet, headers.Count);
        if (rows.Count == 0) return;

        // Count new vs updated records
        var idColumn = sheetName switch
        {
            "Invoices" => "Invoice #",
            _ => "ID"
        };

        if (headers.Contains(idColumn))
        {
            var summary = new ImportSummary { TotalInFile = rows.Count };
            var existingIds = GetExistingIds(sheetName, data);

            foreach (var row in rows)
            {
                var id = GetString(row, headers, idColumn);
                if (existingIds.Contains(id))
                    summary.UpdatedRecords++;
                else
                    summary.NewRecords++;
            }

            result.ImportSummaries[sheetName] = summary;
        }

        // Validate references based on sheet type
        switch (sheetName)
        {
            case "Products":
                ValidateProductReferences(rows, headers, data, importedIds, result);
                break;
            case "Invoices":
                ValidateInvoiceReferences(rows, headers, data, importedIds, result);
                break;
            case "Expenses":
            case "Purchases":
                ValidateExpenseReferences(rows, headers, data, importedIds, result);
                break;
            case "Inventory":
                ValidateInventoryReferences(rows, headers, data, importedIds, result);
                break;
            case "Payments":
                ValidatePaymentReferences(rows, headers, data, importedIds, result);
                break;
            case "Revenue":
            case "Sales":
                ValidateRevenueReferences(rows, headers, data, importedIds, result);
                break;
            case "Rental Records":
                ValidateRentalRecordReferences(rows, headers, data, importedIds, result);
                break;
            case "Categories":
                ValidateCategoryReferences(rows, headers, data, importedIds, result);
                break;
            case "Employees":
                ValidateEmployeeReferences(rows, headers, data, importedIds, result);
                break;
            case "Recurring Invoices":
                ValidateRecurringInvoiceReferences(rows, headers, data, importedIds, result);
                break;
            case "Stock Adjustments":
                ValidateStockAdjustmentReferences(rows, headers, data, importedIds, result);
                break;
            case "Purchase Orders":
                ValidateExpenseOrderReferences(rows, headers, data, importedIds, result);
                break;
            case "Purchase Order Line Items":
                ValidatePurchaseOrderLineItemReferences(rows, headers, data, importedIds, result);
                break;
        }
    }

    private HashSet<string> GetExistingIds(string sheetName, CompanyData data)
    {
        return sheetName switch
        {
            "Customers" => data.Customers.Select(c => c.Id).ToHashSet(),
            "Suppliers" => data.Suppliers.Select(s => s.Id).ToHashSet(),
            "Products" => data.Products.Select(p => p.Id).ToHashSet(),
            "Categories" => data.Categories.Select(c => c.Id).ToHashSet(),
            "Locations" => data.Locations.Select(l => l.Id).ToHashSet(),
            "Departments" => data.Departments.Select(d => d.Id).ToHashSet(),
            "Invoices" => data.Invoices.Select(i => i.Id).ToHashSet(),
            "Expenses" or "Purchases" => data.Expenses.Select(p => p.Id).ToHashSet(),
            "Inventory" => data.Inventory.Select(i => i.Id).ToHashSet(),
            "Payments" => data.Payments.Select(p => p.Id).ToHashSet(),
            "Revenue" or "Sales" => data.Revenues.Select(s => s.Id).ToHashSet(),
            "Rental Inventory" => data.RentalInventory.Select(r => r.Id).ToHashSet(),
            "Rental Records" => data.Rentals.Select(r => r.Id).ToHashSet(),
            "Employees" => data.Employees.Select(e => e.Id).ToHashSet(),
            "Recurring Invoices" => data.RecurringInvoices.Select(r => r.Id).ToHashSet(),
            "Stock Adjustments" => data.StockAdjustments.Select(s => s.Id).ToHashSet(),
            "Purchase Orders" => data.PurchaseOrders.Select(p => p.Id).ToHashSet(),
            _ => []
        };
    }

    private void ValidateProductReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingCategories = data.Categories.Select(c => c.Id).ToHashSet();
        var existingSuppliers = data.Suppliers.Select(s => s.Id).ToHashSet();
        var importedCategories = importedIds.GetValueOrDefault("Categories") ?? [];
        var importedSuppliers = importedIds.GetValueOrDefault("Suppliers") ?? [];

        foreach (var row in rows)
        {
            var categoryId = GetNullableString(row, headers, "Category ID");
            var supplierId = GetNullableString(row, headers, "Supplier ID");

            if (!string.IsNullOrEmpty(categoryId) &&
                !existingCategories.Contains(categoryId) &&
                !importedCategories.Contains(categoryId))
            {
                result.AddMissingReference("Categories", categoryId);
            }

            if (!string.IsNullOrEmpty(supplierId) &&
                !existingSuppliers.Contains(supplierId) &&
                !importedSuppliers.Contains(supplierId))
            {
                result.AddMissingReference("Suppliers", supplierId);
            }
        }
    }

    private void ValidateInvoiceReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingCustomers = data.Customers.Select(c => c.Id).ToHashSet();
        var importedCustomers = importedIds.GetValueOrDefault("Customers") ?? [];

        foreach (var row in rows)
        {
            var customerId = GetNullableString(row, headers, "Customer ID");

            if (!string.IsNullOrEmpty(customerId) &&
                !existingCustomers.Contains(customerId) &&
                !importedCustomers.Contains(customerId))
            {
                result.AddMissingReference("Customers", customerId);
            }
        }
    }

    private void ValidateExpenseReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingSuppliers = data.Suppliers.Select(s => s.Id).ToHashSet();
        var existingProducts = data.Products.Select(p => p.Name).ToHashSet();
        var importedSuppliers = importedIds.GetValueOrDefault("Suppliers") ?? [];

        foreach (var row in rows)
        {
            var supplierId = GetNullableString(row, headers, "Supplier ID");
            var productName = GetString(row, headers, "Product");
            if (string.IsNullOrEmpty(productName))
                productName = GetString(row, headers, "Description");

            if (!string.IsNullOrEmpty(supplierId) &&
                !existingSuppliers.Contains(supplierId) &&
                !importedSuppliers.Contains(supplierId))
            {
                result.AddMissingReference("Suppliers", supplierId);
            }

            // Validate product exists (by name, since Sales/Purchases use product name)
            if (!string.IsNullOrEmpty(productName) &&
                !existingProducts.Contains(productName))
            {
                result.AddMissingReference("Products (by name)", productName);
            }
        }
    }

    private void ValidateInventoryReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingProducts = data.Products.Select(p => p.Id).ToHashSet();
        var existingLocations = data.Locations.Select(l => l.Id).ToHashSet();
        var importedProducts = importedIds.GetValueOrDefault("Products") ?? [];
        var importedLocations = importedIds.GetValueOrDefault("Locations") ?? [];

        foreach (var row in rows)
        {
            var productId = GetNullableString(row, headers, "Product ID");
            var locationId = GetNullableString(row, headers, "Location ID");

            if (!string.IsNullOrEmpty(productId) &&
                !existingProducts.Contains(productId) &&
                !importedProducts.Contains(productId))
            {
                result.AddMissingReference("Products", productId);
            }

            if (!string.IsNullOrEmpty(locationId) &&
                !existingLocations.Contains(locationId) &&
                !importedLocations.Contains(locationId))
            {
                result.AddMissingReference("Locations", locationId);
            }
        }
    }

    private void ValidatePaymentReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingInvoices = data.Invoices.Select(i => i.Id).ToHashSet();
        var existingCustomers = data.Customers.Select(c => c.Id).ToHashSet();
        var importedInvoices = importedIds.GetValueOrDefault("Invoices") ?? [];
        var importedCustomers = importedIds.GetValueOrDefault("Customers") ?? [];

        foreach (var row in rows)
        {
            var invoiceId = GetNullableString(row, headers, "Invoice ID");
            var customerId = GetNullableString(row, headers, "Customer ID");

            if (!string.IsNullOrEmpty(invoiceId) &&
                !existingInvoices.Contains(invoiceId) &&
                !importedInvoices.Contains(invoiceId))
            {
                result.AddMissingReference("Invoices", invoiceId);
            }

            if (!string.IsNullOrEmpty(customerId) &&
                !existingCustomers.Contains(customerId) &&
                !importedCustomers.Contains(customerId))
            {
                result.AddMissingReference("Customers", customerId);
            }
        }
    }

    private void ValidateRevenueReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingCustomers = data.Customers.Select(c => c.Id).ToHashSet();
        var existingProducts = data.Products.Select(p => p.Name).ToHashSet();
        var importedCustomers = importedIds.GetValueOrDefault("Customers") ?? [];

        foreach (var row in rows)
        {
            var customerId = GetNullableString(row, headers, "Customer ID");
            var productName = GetString(row, headers, "Product");
            if (string.IsNullOrEmpty(productName))
                productName = GetString(row, headers, "Description");

            if (!string.IsNullOrEmpty(customerId) &&
                !existingCustomers.Contains(customerId) &&
                !importedCustomers.Contains(customerId))
            {
                result.AddMissingReference("Customers", customerId);
            }

            // Validate product exists (by name)
            if (!string.IsNullOrEmpty(productName) &&
                !existingProducts.Contains(productName))
            {
                result.AddMissingReference("Products (by name)", productName);
            }
        }
    }

    private void ValidateRentalRecordReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingCustomers = data.Customers.Select(c => c.Id).ToHashSet();
        var existingRentalItems = data.RentalInventory.Select(r => r.Id).ToHashSet();
        var importedCustomers = importedIds.GetValueOrDefault("Customers") ?? [];
        var importedRentalItems = importedIds.GetValueOrDefault("RentalInventory") ?? [];

        foreach (var row in rows)
        {
            var customerId = GetNullableString(row, headers, "Customer ID");
            var rentalItemId = GetNullableString(row, headers, "Rental Item ID");

            if (!string.IsNullOrEmpty(customerId) &&
                !existingCustomers.Contains(customerId) &&
                !importedCustomers.Contains(customerId))
            {
                result.AddMissingReference("Customers", customerId);
            }

            if (!string.IsNullOrEmpty(rentalItemId) &&
                !existingRentalItems.Contains(rentalItemId) &&
                !importedRentalItems.Contains(rentalItemId))
            {
                result.AddMissingReference("Rental Items", rentalItemId);
            }
        }
    }

    private void ValidateCategoryReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingCategories = data.Categories.Select(c => c.Id).ToHashSet();
        var importedCategories = importedIds.GetValueOrDefault("Categories") ?? [];

        // Also collect IDs from this sheet for self-reference validation
        var sheetIds = new HashSet<string>();
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            if (!string.IsNullOrEmpty(id))
                sheetIds.Add(id);
        }

        foreach (var row in rows)
        {
            var parentId = GetNullableString(row, headers, "Parent ID");

            if (!string.IsNullOrEmpty(parentId) &&
                !existingCategories.Contains(parentId) &&
                !importedCategories.Contains(parentId) &&
                !sheetIds.Contains(parentId))
            {
                result.AddMissingReference("Categories (parent)", parentId);
            }
        }
    }

    private void ValidateEmployeeReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingDepartments = data.Departments.Select(d => d.Id).ToHashSet();
        var importedDepartments = importedIds.GetValueOrDefault("Departments") ?? [];

        foreach (var row in rows)
        {
            var departmentId = GetNullableString(row, headers, "Department ID");

            if (!string.IsNullOrEmpty(departmentId) &&
                !existingDepartments.Contains(departmentId) &&
                !importedDepartments.Contains(departmentId))
            {
                result.AddMissingReference("Departments", departmentId);
            }
        }
    }

    private void ValidateRecurringInvoiceReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingCustomers = data.Customers.Select(c => c.Id).ToHashSet();
        var importedCustomers = importedIds.GetValueOrDefault("Customers") ?? [];

        foreach (var row in rows)
        {
            var customerId = GetNullableString(row, headers, "Customer ID");

            if (!string.IsNullOrEmpty(customerId) &&
                !existingCustomers.Contains(customerId) &&
                !importedCustomers.Contains(customerId))
            {
                result.AddMissingReference("Customers", customerId);
            }
        }
    }

    private void ValidateStockAdjustmentReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingInventory = data.Inventory.Select(i => i.Id).ToHashSet();
        var importedInventory = importedIds.GetValueOrDefault("Inventory") ?? [];

        foreach (var row in rows)
        {
            var inventoryItemId = GetNullableString(row, headers, "Inventory Item ID");

            if (!string.IsNullOrEmpty(inventoryItemId) &&
                !existingInventory.Contains(inventoryItemId) &&
                !importedInventory.Contains(inventoryItemId))
            {
                result.AddMissingReference("Inventory Items", inventoryItemId);
            }
        }
    }

    private void ValidateExpenseOrderReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingSuppliers = data.Suppliers.Select(s => s.Id).ToHashSet();
        var importedSuppliers = importedIds.GetValueOrDefault("Suppliers") ?? [];

        foreach (var row in rows)
        {
            var supplierId = GetNullableString(row, headers, "Supplier ID");

            if (!string.IsNullOrEmpty(supplierId) &&
                !existingSuppliers.Contains(supplierId) &&
                !importedSuppliers.Contains(supplierId))
            {
                result.AddMissingReference("Suppliers", supplierId);
            }
        }
    }

    private void ValidatePurchaseOrderLineItemReferences(
        List<List<object?>> rows, List<string> headers,
        CompanyData data, Dictionary<string, HashSet<string>> importedIds,
        ImportValidationResult result)
    {
        var existingProducts = data.Products.Select(p => p.Id).ToHashSet();
        var existingPurchaseOrders = data.PurchaseOrders.Select(p => p.Id).ToHashSet();
        var importedProducts = importedIds.GetValueOrDefault("Products") ?? [];
        var importedPurchaseOrders = importedIds.GetValueOrDefault("PurchaseOrders") ?? [];

        foreach (var row in rows)
        {
            var productId = GetNullableString(row, headers, "Product ID");
            var poId = GetNullableString(row, headers, "PO ID");

            if (!string.IsNullOrEmpty(productId) &&
                !existingProducts.Contains(productId) &&
                !importedProducts.Contains(productId))
            {
                result.AddMissingReference("Products", productId);
            }

            if (!string.IsNullOrEmpty(poId) &&
                !existingPurchaseOrders.Contains(poId) &&
                !importedPurchaseOrders.Contains(poId))
            {
                result.AddMissingReference("Purchase Orders", poId);
            }
        }
    }

    #endregion

    #region Auto-Create Missing References

    private void CreateMissingReferences(XLWorkbook workbook, CompanyData data, ImportOptions options)
    {
        var result = new ImportValidationResult();
        var importedIds = CollectImportedIds(workbook);

        foreach (var worksheet in workbook.Worksheets)
        {
            ValidateWorksheet(worksheet, data, importedIds, result);
        }

        foreach (var (refType, ids) in result.MissingReferences)
        {
            if (!options.AutoCreateMissingReferences && !options.AutoCreateTypes.Contains(refType))
                continue;

            foreach (var id in ids)
            {
                CreatePlaceholderEntity(refType, id, data);
            }
        }
    }

    private void CreatePlaceholderEntity(string refType, string id, CompanyData data)
    {
        switch (refType)
        {
            case "Categories":
            case "Categories (parent)":
                if (data.Categories.All(c => c.Id != id))
                {
                    data.Categories.Add(new Category
                    {
                        Id = id,
                        Name = $"[Imported] {id}",
                        Type = CategoryType.Revenue,
                        ItemType = "Product",
                        Icon = "📦"
                    });
                }
                break;

            case "Suppliers":
                if (data.Suppliers.All(s => s.Id != id))
                {
                    data.Suppliers.Add(new Supplier
                    {
                        Id = id,
                        Name = $"[Imported] {id}"
                    });
                }
                break;

            case "Customers":
                if (data.Customers.All(c => c.Id != id))
                {
                    data.Customers.Add(new Customer
                    {
                        Id = id,
                        Name = $"[Imported] {id}",
                        Status = EntityStatus.Active
                    });
                }
                break;

            case "Products":
                if (data.Products.All(p => p.Id != id))
                {
                    data.Products.Add(new Product
                    {
                        Id = id,
                        Name = $"[Imported] {id}",
                        Type = CategoryType.Revenue,
                        ItemType = "Product"
                    });
                }
                break;

            case "Products (by name)":
                if (data.Products.All(p => p.Name != id))
                {
                    var newId = $"PRD-IMP-{data.Products.Count + 1:D3}";
                    data.Products.Add(new Product
                    {
                        Id = newId,
                        Name = id,
                        Type = CategoryType.Revenue,
                        ItemType = "Product"
                    });
                }
                break;

            case "Locations":
                if (data.Locations.All(l => l.Id != id))
                {
                    data.Locations.Add(new Location
                    {
                        Id = id,
                        Name = $"[Imported] {id}"
                    });
                }
                break;

            case "Departments":
                if (data.Departments.All(d => d.Id != id))
                {
                    data.Departments.Add(new Department
                    {
                        Id = id,
                        Name = $"[Imported] {id}"
                    });
                }
                break;

            case "Rental Items":
                if (data.RentalInventory.All(r => r.Id != id))
                {
                    data.RentalInventory.Add(new RentalItem
                    {
                        Id = id,
                        Name = $"[Imported] {id}",
                        Status = EntityStatus.Active
                    });
                }
                break;
        }
    }

    #endregion

    #region Worksheet Import

    private void ImportWorksheet(IXLWorksheet worksheet, CompanyData data)
    {
        var sheetName = worksheet.Name;

        // Get headers from first row
        var headers = GetHeaders(worksheet);
        if (headers.Count == 0) return;

        // Get all data rows (starting from row 2)
        var rows = GetDataRows(worksheet, headers.Count);
        if (rows.Count == 0) return;

        // Import based on sheet name
        switch (sheetName)
        {
            case "Customers":
                ImportCustomers(data, headers, rows);
                break;
            case "Invoices":
                ImportInvoices(data, headers, rows);
                break;
            case "Expenses":
            case "Purchases":
                ImportPurchases(data, headers, rows);
                break;
            case "Products":
                ImportProducts(data, headers, rows);
                break;
            case "Inventory":
                ImportInventory(data, headers, rows);
                break;
            case "Payments":
                ImportPayments(data, headers, rows);
                break;
            case "Suppliers":
                ImportSuppliers(data, headers, rows);
                break;
            case "Revenue":
            case "Sales":
                ImportSales(data, headers, rows);
                break;
            case "Rental Inventory":
                ImportRentalInventory(data, headers, rows);
                break;
            case "Rental Records":
                ImportRentalRecords(data, headers, rows);
                break;
            case "Categories":
                ImportCategories(data, headers, rows);
                break;
            case "Departments":
                ImportDepartments(data, headers, rows);
                break;
            case "Employees":
                ImportEmployees(data, headers, rows);
                break;
            case "Locations":
                ImportLocations(data, headers, rows);
                break;
            case "Recurring Invoices":
                ImportRecurringInvoices(data, headers, rows);
                break;
            case "Stock Adjustments":
                ImportStockAdjustments(data, headers, rows);
                break;
            case "Purchase Orders":
                ImportPurchaseOrders(data, headers, rows);
                break;
            case "Purchase Order Line Items":
                ImportPurchaseOrderLineItems(data, headers, rows);
                break;
            case "Returns":
                ImportReturns(data, headers, rows);
                break;
            case "Lost Damaged":
            case "Lost/Damaged":
                ImportLostDamaged(data, headers, rows);
                break;
        }
    }

    #endregion

    #region Helper Methods

    private static List<string> GetHeaders(IXLWorksheet worksheet)
    {
        var headers = new List<string>();
        var row = worksheet.Row(1);

        for (int col = 1; col <= worksheet.ColumnsUsed().Count(); col++)
        {
            var cell = row.Cell(col);
            if (cell.IsEmpty()) break;
            headers.Add(cell.GetString().Trim());
        }

        return headers;
    }

    private static List<List<object?>> GetDataRows(IXLWorksheet worksheet, int columnCount)
    {
        var rows = new List<List<object?>>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;

        for (int rowNum = 2; rowNum <= lastRow; rowNum++)
        {
            var row = worksheet.Row(rowNum);
            var rowData = new List<object?>();
            var isEmpty = true;

            for (int col = 1; col <= columnCount; col++)
            {
                var cell = row.Cell(col);
                if (!cell.IsEmpty()) isEmpty = false;
                rowData.Add(GetCellValue(cell));
            }

            if (!isEmpty)
            {
                rows.Add(rowData);
            }
        }

        return rows;
    }

    private static object? GetCellValue(IXLCell cell)
    {
        if (cell.IsEmpty()) return null;

        return cell.DataType switch
        {
            XLDataType.Number => cell.GetDouble(),
            XLDataType.DateTime => cell.GetDateTime(),
            XLDataType.Boolean => cell.GetBoolean(),
            _ => cell.GetString()
        };
    }

    private static int GetColumnIndex(List<string> headers, string columnName)
    {
        // Case-insensitive column lookup
        for (int i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i], columnName, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        return -1;
    }

    private static string GetString(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return string.Empty;
        return row[index]?.ToString() ?? string.Empty;
    }

    private static string? GetNullableString(List<object?> row, List<string> headers, string columnName)
    {
        var value = GetString(row, headers, columnName);
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static decimal GetDecimal(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return 0m;

        var value = row[index];
        return value switch
        {
            double d => (decimal)d,
            decimal dec => dec,
            int i => i,
            long l => l,
            string s when decimal.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) => result,
            _ => 0m
        };
    }

    private static int GetInt(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return 0;

        var value = row[index];
        return value switch
        {
            double d => (int)d,
            decimal dec => (int)dec,
            int i => i,
            long l => (int)l,
            string s when int.TryParse(s, out var result) => result,
            _ => 0
        };
    }

    private static DateTime GetDateTime(List<object?> row, List<string> headers, string columnName)
    {
        var index = GetColumnIndex(headers, columnName);
        if (index < 0 || index >= row.Count) return DateTime.MinValue;

        var value = row[index];
        return value switch
        {
            DateTime dt => dt,
            double d => DateTime.FromOADate(d),
            string s when DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result) => result,
            _ => DateTime.MinValue
        };
    }

    private static DateTime? GetNullableDateTime(List<object?> row, List<string> headers, string columnName)
    {
        var dt = GetDateTime(row, headers, columnName);
        return dt == DateTime.MinValue ? null : dt;
    }

    private static TEnum ParseEnum<TEnum>(string value, TEnum defaultValue) where TEnum : struct, Enum
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) ? result : defaultValue;
    }

    #endregion

    #region Import Methods (Merge Logic)

    private void ImportCustomers(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Customers.FirstOrDefault(c => c.Id == id);

            var customer = existing ?? new Customer();
            customer.Id = id;
            customer.Name = GetString(row, headers, "Name");
            customer.CompanyName = GetNullableString(row, headers, "Company");
            customer.Email = GetString(row, headers, "Email");
            customer.Phone = GetString(row, headers, "Phone");
            customer.Address = new Address
            {
                Street = GetString(row, headers, "Street"),
                City = GetString(row, headers, "City"),
                State = GetString(row, headers, "State"),
                ZipCode = GetString(row, headers, "Zip Code"),
                Country = GetString(row, headers, "Country")
            };
            customer.Notes = GetString(row, headers, "Notes");
            customer.Status = ParseEnum(GetString(row, headers, "Status"), EntityStatus.Active);
            customer.TotalPurchases = GetDecimal(row, headers, "Total Purchases");

            if (existing == null)
                data.Customers.Add(customer);
        }
    }

    private void ImportInvoices(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var invoiceNumber = GetString(row, headers, "Invoice #");
            var existing = data.Invoices.FirstOrDefault(i => i.Id == invoiceNumber);

            var invoice = existing ?? new Invoice();
            invoice.Id = invoiceNumber;
            invoice.InvoiceNumber = invoiceNumber;
            invoice.CustomerId = GetString(row, headers, "Customer ID");
            invoice.IssueDate = GetDateTime(row, headers, "Issue Date");
            invoice.DueDate = GetDateTime(row, headers, "Due Date");
            invoice.Subtotal = GetDecimal(row, headers, "Subtotal");
            invoice.TaxAmount = GetDecimal(row, headers, "Tax");
            invoice.Total = GetDecimal(row, headers, "Total");
            invoice.AmountPaid = GetDecimal(row, headers, "Paid");
            invoice.Balance = GetDecimal(row, headers, "Balance");
            invoice.Status = ParseEnum(GetString(row, headers, "Status"), InvoiceStatus.Draft);

            // Set USD values (assume imported data is in USD)
            invoice.OriginalCurrency = "USD";
            invoice.TotalUSD = invoice.Total;
            invoice.BalanceUSD = invoice.Balance;

            if (existing == null)
                data.Invoices.Add(invoice);
        }
    }

    private void ImportPurchases(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Expenses.FirstOrDefault(p => p.Id == id);

            // Support both "Product" (new) and "Description" (legacy) column names
            var description = GetString(row, headers, "Product");
            if (string.IsNullOrEmpty(description))
                description = GetString(row, headers, "Description");

            var purchase = existing ?? new Expense();
            purchase.Id = id;
            purchase.Date = GetDateTime(row, headers, "Date");
            purchase.SupplierId = GetNullableString(row, headers, "Supplier ID");
            purchase.Description = description;
            purchase.Amount = GetDecimal(row, headers, "Amount");
            purchase.TaxAmount = GetDecimal(row, headers, "Tax");
            purchase.Total = GetDecimal(row, headers, "Total");
            purchase.ReferenceNumber = GetString(row, headers, "Reference");
            purchase.PaymentMethod = ParseEnum(GetString(row, headers, "Payment Method"), PaymentMethod.Cash);
            purchase.ShippingCost = GetDecimal(row, headers, "Shipping");

            // Set USD values (assume imported data is in USD)
            purchase.OriginalCurrency = "USD";
            purchase.TotalUSD = purchase.Total;
            purchase.TaxAmountUSD = purchase.TaxAmount;
            purchase.ShippingCostUSD = purchase.ShippingCost;

            // Link product by looking up by name and creating a LineItem
            if (!string.IsNullOrEmpty(description))
            {
                var product = data.Products.FirstOrDefault(p =>
                    string.Equals(p.Name, description, StringComparison.OrdinalIgnoreCase));

                var lineItem = new LineItem
                {
                    ProductId = product?.Id,
                    Description = description,
                    Quantity = 1,
                    UnitPrice = purchase.Amount,
                    TaxRate = purchase.Amount > 0 ? purchase.TaxAmount / purchase.Amount : 0
                };
                purchase.LineItems = [lineItem];
            }

            if (existing == null)
                data.Expenses.Add(purchase);
        }
    }

    private void ImportProducts(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var name = GetString(row, headers, "Name");

            // Check for existing product by ID first, then by name to prevent duplicates
            // (auto-created placeholder products may have different IDs but same names)
            var existing = data.Products.FirstOrDefault(p => p.Id == id)
                ?? data.Products.FirstOrDefault(p =>
                    string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));

            var typeStr = GetString(row, headers, "Type");
            var productType = typeStr.ToLowerInvariant() switch
            {
                "revenue" or "sales" => CategoryType.Revenue,
                "expenses" or "purchase" => CategoryType.Expense,
                "rental" => CategoryType.Rental,
                _ => CategoryType.Revenue
            };

            var itemTypeRaw = GetString(row, headers, "Item Type");
            // Normalize item type to proper casing (case-insensitive match, trim whitespace)
            var itemType = itemTypeRaw.Trim().ToLowerInvariant() switch
            {
                "service" => "Service",
                _ => "Product"
            };

            var product = existing ?? new Product();
            product.Id = id;
            product.Name = GetString(row, headers, "Name");
            product.Type = productType;
            product.ItemType = itemType;
            product.Sku = GetString(row, headers, "SKU");
            product.Description = GetString(row, headers, "Description");

            // Handle Category - prefer ID, fall back to name lookup
            var categoryId = GetNullableString(row, headers, "Category ID");
            if (string.IsNullOrEmpty(categoryId))
            {
                var categoryName = GetNullableString(row, headers, "Category Name");
                if (!string.IsNullOrEmpty(categoryName))
                {
                    var category = data.Categories.FirstOrDefault(c =>
                        string.Equals(c.Name, categoryName, StringComparison.OrdinalIgnoreCase));
                    categoryId = category?.Id;
                }
            }
            product.CategoryId = categoryId;

            // Handle Supplier - prefer ID, fall back to name lookup
            var supplierId = GetNullableString(row, headers, "Supplier ID");
            if (string.IsNullOrEmpty(supplierId))
            {
                var supplierName = GetNullableString(row, headers, "Supplier Name");
                if (!string.IsNullOrEmpty(supplierName))
                {
                    var supplier = data.Suppliers.FirstOrDefault(s =>
                        string.Equals(s.Name, supplierName, StringComparison.OrdinalIgnoreCase));
                    supplierId = supplier?.Id;
                }
            }
            product.SupplierId = supplierId;

            // Handle Reorder Point and Overstock Threshold
            product.ReorderPoint = GetInt(row, headers, "Reorder Point");
            product.OverstockThreshold = GetInt(row, headers, "Overstock Threshold");

            // Set TrackInventory based on whether reorder/overstock values are set
            if (product.ReorderPoint > 0 || product.OverstockThreshold > 0)
            {
                product.TrackInventory = true;
            }

            if (existing == null)
                data.Products.Add(product);
        }
    }

    private void ImportInventory(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Inventory.FirstOrDefault(i => i.Id == id);

            var item = existing ?? new InventoryItem();
            item.Id = id;
            item.ProductId = GetString(row, headers, "Product ID");
            item.LocationId = GetString(row, headers, "Location ID");
            item.InStock = GetInt(row, headers, "In Stock");
            item.Reserved = GetInt(row, headers, "Reserved");
            item.ReorderPoint = GetInt(row, headers, "Reorder Point");
            item.UnitCost = GetDecimal(row, headers, "Unit Cost");
            item.LastUpdated = GetDateTime(row, headers, "Last Updated");

            if (item.LastUpdated == DateTime.MinValue)
                item.LastUpdated = DateTime.UtcNow;

            if (existing == null)
                data.Inventory.Add(item);
        }
    }

    private void ImportPayments(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Payments.FirstOrDefault(p => p.Id == id);

            var payment = existing ?? new Payment();
            payment.Id = id;
            payment.InvoiceId = GetString(row, headers, "Invoice ID");
            payment.CustomerId = GetString(row, headers, "Customer ID");
            payment.Date = GetDateTime(row, headers, "Date");
            payment.Amount = GetDecimal(row, headers, "Amount");
            payment.PaymentMethod = ParseEnum(GetString(row, headers, "Payment Method"), PaymentMethod.Cash);
            payment.ReferenceNumber = GetNullableString(row, headers, "Reference");
            payment.Notes = GetString(row, headers, "Notes");

            // Set USD values (assume imported data is in USD)
            payment.OriginalCurrency = "USD";
            payment.AmountUSD = payment.Amount;

            if (existing == null)
                data.Payments.Add(payment);
        }
    }

    private void ImportSuppliers(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Suppliers.FirstOrDefault(s => s.Id == id);

            var supplier = existing ?? new Supplier();
            supplier.Id = id;
            supplier.Name = GetString(row, headers, "Name");
            supplier.Email = GetString(row, headers, "Email");
            supplier.Phone = GetString(row, headers, "Phone");
            supplier.Website = GetNullableString(row, headers, "Website") ?? "";
            supplier.Address = new Address
            {
                Street = GetString(row, headers, "Street"),
                City = GetString(row, headers, "City"),
                State = GetString(row, headers, "State"),
                ZipCode = GetString(row, headers, "Zip Code"),
                Country = GetString(row, headers, "Country")
            };
            supplier.Notes = GetString(row, headers, "Notes");

            if (existing == null)
                data.Suppliers.Add(supplier);
        }
    }

    private void ImportSales(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Revenues.FirstOrDefault(s => s.Id == id);

            // Support both "Product" (new) and "Description" (legacy) column names
            var description = GetString(row, headers, "Product");
            if (string.IsNullOrEmpty(description))
                description = GetString(row, headers, "Description");

            var revenue = existing ?? new Revenue();
            revenue.Id = id;
            revenue.Date = GetDateTime(row, headers, "Date");
            revenue.CustomerId = GetNullableString(row, headers, "Customer ID");
            revenue.Description = description;
            revenue.Amount = GetDecimal(row, headers, "Amount");
            revenue.TaxAmount = GetDecimal(row, headers, "Tax");
            revenue.Total = GetDecimal(row, headers, "Total");
            revenue.ReferenceNumber = GetString(row, headers, "Reference");
            revenue.PaymentStatus = GetString(row, headers, "Payment Status");
            revenue.ShippingCost = GetDecimal(row, headers, "Shipping");

            // Set USD values (assume imported data is in USD)
            revenue.OriginalCurrency = "USD";
            revenue.TotalUSD = revenue.Total;
            revenue.TaxAmountUSD = revenue.TaxAmount;
            revenue.ShippingCostUSD = revenue.ShippingCost;

            if (string.IsNullOrEmpty(revenue.PaymentStatus))
                revenue.PaymentStatus = "Paid";

            // Link product by looking up by name and creating a LineItem
            if (!string.IsNullOrEmpty(description))
            {
                var product = data.Products.FirstOrDefault(p =>
                    string.Equals(p.Name, description, StringComparison.OrdinalIgnoreCase));

                var lineItem = new LineItem
                {
                    ProductId = product?.Id,
                    Description = description,
                    Quantity = 1,
                    UnitPrice = revenue.Amount,
                    TaxRate = revenue.Amount > 0 ? revenue.TaxAmount / revenue.Amount : 0
                };
                revenue.LineItems = [lineItem];
            }

            if (existing == null)
                data.Revenues.Add(revenue);
        }
    }

    private void ImportRentalInventory(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.RentalInventory.FirstOrDefault(r => r.Id == id);

            var item = existing ?? new RentalItem();
            item.Id = id;
            item.Name = GetString(row, headers, "Name");
            item.TotalQuantity = GetInt(row, headers, "Total Qty");
            item.AvailableQuantity = GetInt(row, headers, "Available");
            item.RentedQuantity = GetInt(row, headers, "Rented");
            item.DailyRate = GetDecimal(row, headers, "Daily Rate");
            item.WeeklyRate = GetDecimal(row, headers, "Weekly Rate");
            item.MonthlyRate = GetDecimal(row, headers, "Monthly Rate");
            item.SecurityDeposit = GetDecimal(row, headers, "Deposit");
            item.Status = ParseEnum(GetString(row, headers, "Status"), EntityStatus.Active);

            if (existing == null)
                data.RentalInventory.Add(item);
        }
    }

    private void ImportRentalRecords(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Rentals.FirstOrDefault(r => r.Id == id);

            var record = existing ?? new RentalRecord();
            record.Id = id;
            record.CustomerId = GetString(row, headers, "Customer ID");
            record.RentalItemId = GetString(row, headers, "Rental Item ID");
            record.Quantity = GetInt(row, headers, "Quantity");
            record.StartDate = GetDateTime(row, headers, "Start Date");
            record.DueDate = GetDateTime(row, headers, "Due Date");
            record.ReturnDate = GetNullableDateTime(row, headers, "Return Date");
            record.TotalCost = GetDecimal(row, headers, "Total Cost");
            record.Status = ParseEnum(GetString(row, headers, "Status"), RentalStatus.Active);

            if (record.TotalCost == 0)
                record.TotalCost = null;

            if (existing == null)
                data.Rentals.Add(record);
        }
    }

    private void ImportCategories(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Categories.FirstOrDefault(c => c.Id == id);

            var typeStr = GetString(row, headers, "Type");
            var categoryType = typeStr.ToLowerInvariant() switch
            {
                "revenue" or "sales" => CategoryType.Revenue,
                "expenses" or "purchase" => CategoryType.Expense,
                "rental" => CategoryType.Rental,
                _ => CategoryType.Revenue
            };

            var category = existing ?? new Category();
            category.Id = id;
            category.Name = GetString(row, headers, "Name");
            category.Type = categoryType;
            category.ParentId = GetNullableString(row, headers, "Parent ID");
            category.Description = GetNullableString(row, headers, "Description");
            // Normalize item type to proper casing (case-insensitive match, trim whitespace)
            var itemTypeStr = GetString(row, headers, "Item Type");
            category.ItemType = itemTypeStr.Trim().ToLowerInvariant() switch
            {
                "service" => "Service",
                _ => "Product"
            };
            category.Icon = GetString(row, headers, "Icon");
            if (string.IsNullOrEmpty(category.Icon))
                category.Icon = "📦";

            if (existing == null)
                data.Categories.Add(category);
        }
    }

    private void ImportDepartments(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Departments.FirstOrDefault(d => d.Id == id);

            var department = existing ?? new Department();
            department.Id = id;
            department.Name = GetString(row, headers, "Name");
            department.Description = GetNullableString(row, headers, "Description");

            if (existing == null)
                data.Departments.Add(department);
        }
    }

    private void ImportEmployees(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Employees.FirstOrDefault(e => e.Id == id);

            var employee = existing ?? new Employee();
            employee.Id = id;
            employee.FirstName = GetString(row, headers, "First Name");
            employee.LastName = GetString(row, headers, "Last Name");
            employee.Email = GetString(row, headers, "Email");
            employee.Phone = GetString(row, headers, "Phone");
            employee.DateOfBirth = GetNullableDateTime(row, headers, "Date of Birth");
            employee.DepartmentId = GetNullableString(row, headers, "Department ID");
            employee.Position = GetString(row, headers, "Position");
            employee.HireDate = GetDateTime(row, headers, "Hire Date");
            employee.EmploymentType = GetString(row, headers, "Employment Type");
            employee.SalaryType = GetString(row, headers, "Salary Type");
            employee.SalaryAmount = GetDecimal(row, headers, "Salary Amount");
            employee.PayFrequency = GetString(row, headers, "Pay Frequency");
            employee.Status = ParseEnum(GetString(row, headers, "Status"), EmployeeStatus.Active);

            if (string.IsNullOrEmpty(employee.EmploymentType))
                employee.EmploymentType = "Full-time";
            if (string.IsNullOrEmpty(employee.SalaryType))
                employee.SalaryType = "Annual";
            if (string.IsNullOrEmpty(employee.PayFrequency))
                employee.PayFrequency = "Bi-weekly";

            if (existing == null)
                data.Employees.Add(employee);
        }
    }

    private void ImportLocations(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Locations.FirstOrDefault(l => l.Id == id);

            var location = existing ?? new Location();
            location.Id = id;
            location.Name = GetString(row, headers, "Name");
            location.ContactPerson = GetString(row, headers, "Contact Person");
            location.Phone = GetString(row, headers, "Phone");
            location.Address = new Address
            {
                Street = GetString(row, headers, "Street"),
                City = GetString(row, headers, "City"),
                State = GetString(row, headers, "State"),
                ZipCode = GetString(row, headers, "Zip Code"),
                Country = GetString(row, headers, "Country")
            };
            location.Capacity = GetInt(row, headers, "Capacity");
            location.CurrentUtilization = GetInt(row, headers, "Utilization");

            if (existing == null)
                data.Locations.Add(location);
        }
    }

    private void ImportRecurringInvoices(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.RecurringInvoices.FirstOrDefault(r => r.Id == id);

            var recurring = existing ?? new RecurringInvoice();
            recurring.Id = id;
            recurring.CustomerId = GetString(row, headers, "Customer ID");
            recurring.Amount = GetDecimal(row, headers, "Amount");
            recurring.Description = GetString(row, headers, "Description");
            recurring.Frequency = ParseEnum(GetString(row, headers, "Frequency"), Frequency.Monthly);
            recurring.NextInvoiceDate = GetDateTime(row, headers, "Next Date");
            recurring.Status = GetString(row, headers, "Status");

            if (string.IsNullOrEmpty(recurring.Status))
                recurring.Status = "Active";

            if (existing == null)
                data.RecurringInvoices.Add(recurring);
        }
    }

    private void ImportStockAdjustments(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.StockAdjustments.FirstOrDefault(s => s.Id == id);

            var adjustment = existing ?? new StockAdjustment();
            adjustment.Id = id;
            adjustment.InventoryItemId = GetString(row, headers, "Inventory Item ID");
            adjustment.AdjustmentType = ParseEnum(GetString(row, headers, "Type"), AdjustmentType.Set);
            adjustment.Quantity = GetInt(row, headers, "Quantity");
            adjustment.PreviousStock = GetInt(row, headers, "Previous Stock");
            adjustment.NewStock = GetInt(row, headers, "New Stock");
            adjustment.Reason = GetString(row, headers, "Reason");
            adjustment.Timestamp = GetDateTime(row, headers, "Timestamp");

            if (adjustment.Timestamp == DateTime.MinValue)
                adjustment.Timestamp = DateTime.UtcNow;

            if (existing == null)
                data.StockAdjustments.Add(adjustment);
        }
    }

    private void ImportPurchaseOrders(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.PurchaseOrders.FirstOrDefault(p => p.Id == id);

            var po = existing ?? new PurchaseOrder();
            po.Id = id;
            po.SupplierId = GetString(row, headers, "Supplier ID");
            po.OrderDate = GetDateTime(row, headers, "Order Date");
            po.ExpectedDeliveryDate = GetDateTime(row, headers, "Expected Date");
            po.Total = GetDecimal(row, headers, "Total");
            po.Status = ParseEnum(GetString(row, headers, "Status"), PurchaseOrderStatus.Draft);

            if (existing == null)
                data.PurchaseOrders.Add(po);
        }
    }

    private void ImportPurchaseOrderLineItems(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        // Group line items by purchase order ID
        var lineItemsByPo = new Dictionary<string, List<PurchaseOrderLineItem>>();

        foreach (var row in rows)
        {
            var poId = GetString(row, headers, "PO ID");
            if (string.IsNullOrEmpty(poId)) continue;

            var lineItem = new PurchaseOrderLineItem
            {
                ProductId = GetString(row, headers, "Product ID"),
                Quantity = GetInt(row, headers, "Quantity"),
                UnitCost = GetDecimal(row, headers, "Unit Cost"),
                QuantityReceived = GetInt(row, headers, "Quantity Received")
            };

            if (!lineItemsByPo.ContainsKey(poId))
                lineItemsByPo[poId] = [];

            lineItemsByPo[poId].Add(lineItem);
        }

        // Assign line items to purchase orders
        foreach (var (poId, lineItems) in lineItemsByPo)
        {
            var po = data.PurchaseOrders.FirstOrDefault(p => p.Id == poId);
            if (po != null)
            {
                po.LineItems = lineItems;
                // Calculate subtotal from line items
                po.Subtotal = lineItems.Sum(li => li.Total);
            }
        }
    }

    private void ImportReturns(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.Returns.FirstOrDefault(r => r.Id == id);

            var returnRecord = existing ?? new Return();
            returnRecord.Id = id;
            returnRecord.OriginalTransactionId = GetString(row, headers, "Original Transaction ID");
            returnRecord.ReturnType = GetString(row, headers, "Return Type");
            if (string.IsNullOrEmpty(returnRecord.ReturnType))
                returnRecord.ReturnType = "Customer";
            returnRecord.CustomerId = GetString(row, headers, "Customer ID");
            returnRecord.SupplierId = GetString(row, headers, "Supplier ID");
            returnRecord.ReturnDate = GetDateTime(row, headers, "Return Date");
            returnRecord.RefundAmount = GetDecimal(row, headers, "Refund Amount");
            returnRecord.RestockingFee = GetDecimal(row, headers, "Restocking Fee");
            returnRecord.Status = ParseEnum(GetString(row, headers, "Status"), ReturnStatus.Pending);
            returnRecord.Notes = GetString(row, headers, "Notes");
            returnRecord.ProcessedBy = GetNullableString(row, headers, "Processed By");

            // Handle items - simple single product per return row
            var productId = GetNullableString(row, headers, "Product ID");
            var productName = GetNullableString(row, headers, "Product");
            var quantity = GetInt(row, headers, "Quantity");
            var reason = GetString(row, headers, "Reason");

            if (!string.IsNullOrEmpty(productId) || !string.IsNullOrEmpty(productName))
            {
                // Look up product by name if ID not provided
                if (string.IsNullOrEmpty(productId) && !string.IsNullOrEmpty(productName))
                {
                    var product = data.Products.FirstOrDefault(p =>
                        string.Equals(p.Name, productName, StringComparison.OrdinalIgnoreCase));
                    productId = product?.Id ?? "";
                }

                returnRecord.Items =
                [
                    new ReturnItem
                    {
                        ProductId = productId ?? "",
                        Quantity = quantity > 0 ? quantity : 1,
                        Reason = reason
                    }
                ];
            }

            if (existing == null)
                data.Returns.Add(returnRecord);
        }
    }

    private void ImportLostDamaged(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        foreach (var row in rows)
        {
            var id = GetString(row, headers, "ID");
            var existing = data.LostDamaged.FirstOrDefault(ld => ld.Id == id);

            var lostDamaged = existing ?? new LostDamaged();
            lostDamaged.Id = id;

            // Handle product - prefer ID, fall back to name lookup
            var productId = GetNullableString(row, headers, "Product ID");
            if (string.IsNullOrEmpty(productId))
            {
                var productName = GetNullableString(row, headers, "Product");
                if (!string.IsNullOrEmpty(productName))
                {
                    var product = data.Products.FirstOrDefault(p =>
                        string.Equals(p.Name, productName, StringComparison.OrdinalIgnoreCase));
                    productId = product?.Id;
                }
            }
            lostDamaged.ProductId = productId ?? "";

            lostDamaged.InventoryItemId = GetNullableString(row, headers, "Inventory Item ID");
            lostDamaged.Quantity = GetInt(row, headers, "Quantity");
            if (lostDamaged.Quantity == 0)
                lostDamaged.Quantity = 1;
            lostDamaged.Reason = ParseEnum(GetString(row, headers, "Reason"), LostDamagedReason.Damaged);
            lostDamaged.DateDiscovered = GetDateTime(row, headers, "Date Discovered");
            if (lostDamaged.DateDiscovered == DateTime.MinValue)
                lostDamaged.DateDiscovered = GetDateTime(row, headers, "Date");
            lostDamaged.ValueLost = GetDecimal(row, headers, "Value Lost");
            lostDamaged.Notes = GetString(row, headers, "Notes");

            var insuranceClaim = GetString(row, headers, "Insurance Claim");
            lostDamaged.InsuranceClaim = insuranceClaim.Equals("Yes", StringComparison.OrdinalIgnoreCase) ||
                                          insuranceClaim.Equals("True", StringComparison.OrdinalIgnoreCase);

            if (existing == null)
                data.LostDamaged.Add(lostDamaged);
        }
    }

    #endregion

    #region ID Counter Update

    private static void UpdateIdCounters(CompanyData data)
    {
        data.IdCounters.Customer = GetMaxIdNumber(data.Customers.Select(c => c.Id), "CUS-");
        data.IdCounters.Product = GetMaxIdNumber(data.Products.Select(p => p.Id), "PRD-");
        data.IdCounters.Supplier = GetMaxIdNumber(data.Suppliers.Select(s => s.Id), "SUP-");
        data.IdCounters.Employee = GetMaxIdNumber(data.Employees.Select(e => e.Id), "EMP-");
        data.IdCounters.Department = GetMaxIdNumber(data.Departments.Select(d => d.Id), "DEP-");
        data.IdCounters.Category = GetMaxIdNumber(data.Categories.Select(c => c.Id), "CAT-");
        data.IdCounters.Location = GetMaxIdNumber(data.Locations.Select(l => l.Id), "LOC-");
        data.IdCounters.Revenue = GetMaxIdNumber(data.Revenues.Select(s => s.Id), "SAL-");
        data.IdCounters.Expense = GetMaxIdNumber(data.Expenses.Select(p => p.Id), "PUR-");
        data.IdCounters.Invoice = GetMaxIdNumber(data.Invoices.Select(i => i.Id), "INV-");
        data.IdCounters.Payment = GetMaxIdNumber(data.Payments.Select(p => p.Id), "PAY-");
        data.IdCounters.RecurringInvoice = GetMaxIdNumber(data.RecurringInvoices.Select(r => r.Id), "REC-INV-");
        data.IdCounters.InventoryItem = GetMaxIdNumber(data.Inventory.Select(i => i.Id), "INV-ITM-");
        data.IdCounters.StockAdjustment = GetMaxIdNumber(data.StockAdjustments.Select(s => s.Id), "ADJ-");
        data.IdCounters.PurchaseOrder = GetMaxIdNumber(data.PurchaseOrders.Select(p => p.Id), "PO-");
        data.IdCounters.RentalItem = GetMaxIdNumber(data.RentalInventory.Select(r => r.Id), "RNT-ITM-");
        data.IdCounters.Rental = GetMaxIdNumber(data.Rentals.Select(r => r.Id), "RNT-");
        data.IdCounters.Return = GetMaxIdNumber(data.Returns.Select(r => r.Id), "RET-");
        data.IdCounters.LostDamaged = GetMaxIdNumber(data.LostDamaged.Select(ld => ld.Id), "LOST-");
    }

    private static int GetMaxIdNumber(IEnumerable<string> ids, string prefix)
    {
        var max = 0;
        foreach (var id in ids)
        {
            if (string.IsNullOrEmpty(id)) continue;

            // Try to extract number from ID (e.g., "CUS-001" -> 1)
            var idStr = id;
            if (idStr.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                idStr = idStr[prefix.Length..];
            }

            // Handle IDs that might have additional prefixes (e.g., "INV-2024-001")
            var parts = idStr.Split('-');
            var lastPart = parts[^1];

            if (int.TryParse(lastPart, out var num) && num > max)
            {
                max = num;
            }
        }
        return max;
    }

    #endregion
}
