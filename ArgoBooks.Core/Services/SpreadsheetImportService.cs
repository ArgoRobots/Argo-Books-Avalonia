using System.Globalization;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Transactions;
using ClosedXML.Excel;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for importing company data from spreadsheet formats (xlsx).
/// </summary>
public class SpreadsheetImportService
{
    /// <summary>
    /// Imports data from an Excel file into the company data.
    /// </summary>
    public async Task ImportFromExcelAsync(
        string filePath,
        CompanyData companyData,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(companyData);

        await Task.Run(() =>
        {
            using var workbook = new XLWorkbook(filePath);

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
    }

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
        }
    }

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
        return headers.IndexOf(columnName);
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

    #region Import Methods

    private void ImportCustomers(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Customers.Clear();
        foreach (var row in rows)
        {
            var customer = new Customer
            {
                Id = GetString(row, headers, "ID"),
                Name = GetString(row, headers, "Name"),
                CompanyName = GetNullableString(row, headers, "Company"),
                Email = GetString(row, headers, "Email"),
                Phone = GetString(row, headers, "Phone"),
                Address = new Address
                {
                    Street = GetString(row, headers, "Street"),
                    City = GetString(row, headers, "City"),
                    State = GetString(row, headers, "State"),
                    ZipCode = GetString(row, headers, "Zip Code"),
                    Country = GetString(row, headers, "Country")
                },
                Notes = GetString(row, headers, "Notes"),
                Status = ParseEnum(GetString(row, headers, "Status"), EntityStatus.Active),
                TotalPurchases = GetDecimal(row, headers, "Total Purchases")
            };
            data.Customers.Add(customer);
        }
    }

    private void ImportInvoices(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Invoices.Clear();
        foreach (var row in rows)
        {
            var invoiceNumber = GetString(row, headers, "Invoice #");
            var invoice = new Invoice
            {
                Id = invoiceNumber,
                InvoiceNumber = invoiceNumber,
                CustomerId = GetString(row, headers, "Customer ID"),
                IssueDate = GetDateTime(row, headers, "Issue Date"),
                DueDate = GetDateTime(row, headers, "Due Date"),
                Subtotal = GetDecimal(row, headers, "Subtotal"),
                TaxAmount = GetDecimal(row, headers, "Tax"),
                Total = GetDecimal(row, headers, "Total"),
                AmountPaid = GetDecimal(row, headers, "Paid"),
                Balance = GetDecimal(row, headers, "Balance"),
                Status = ParseEnum(GetString(row, headers, "Status"), InvoiceStatus.Draft)
            };
            data.Invoices.Add(invoice);
        }
    }

    private void ImportPurchases(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Purchases.Clear();
        foreach (var row in rows)
        {
            var purchase = new Purchase
            {
                Id = GetString(row, headers, "ID"),
                Date = GetDateTime(row, headers, "Date"),
                SupplierId = GetNullableString(row, headers, "Supplier ID"),
                Description = GetString(row, headers, "Description"),
                Amount = GetDecimal(row, headers, "Amount"),
                TaxAmount = GetDecimal(row, headers, "Tax"),
                Total = GetDecimal(row, headers, "Total"),
                ReferenceNumber = GetString(row, headers, "Reference"),
                PaymentMethod = ParseEnum(GetString(row, headers, "Payment Method"), PaymentMethod.Cash)
            };
            data.Purchases.Add(purchase);
        }
    }

    private void ImportProducts(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Products.Clear();
        foreach (var row in rows)
        {
            var typeStr = GetString(row, headers, "Type");
            var productType = typeStr.ToLowerInvariant() switch
            {
                "revenue" or "sales" => CategoryType.Sales,
                "expenses" or "purchase" => CategoryType.Purchase,
                "rental" => CategoryType.Rental,
                _ => CategoryType.Sales
            };

            var itemType = GetString(row, headers, "Item Type");
            if (string.IsNullOrEmpty(itemType))
                itemType = "Product";

            var product = new Product
            {
                Id = GetString(row, headers, "ID"),
                Name = GetString(row, headers, "Name"),
                Type = productType,
                ItemType = itemType,
                Sku = GetString(row, headers, "SKU"),
                Description = GetString(row, headers, "Description"),
                CategoryId = GetNullableString(row, headers, "Category ID"),
                SupplierId = GetNullableString(row, headers, "Supplier ID")
            };
            data.Products.Add(product);
        }
    }

    private void ImportInventory(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Inventory.Clear();
        foreach (var row in rows)
        {
            var item = new InventoryItem
            {
                Id = GetString(row, headers, "ID"),
                ProductId = GetString(row, headers, "Product ID"),
                LocationId = GetString(row, headers, "Location ID"),
                InStock = GetInt(row, headers, "In Stock"),
                Reserved = GetInt(row, headers, "Reserved"),
                ReorderPoint = GetInt(row, headers, "Reorder Point"),
                UnitCost = GetDecimal(row, headers, "Unit Cost"),
                LastUpdated = GetDateTime(row, headers, "Last Updated")
            };
            if (item.LastUpdated == DateTime.MinValue)
                item.LastUpdated = DateTime.UtcNow;
            data.Inventory.Add(item);
        }
    }

    private void ImportPayments(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Payments.Clear();
        foreach (var row in rows)
        {
            var payment = new Payment
            {
                Id = GetString(row, headers, "ID"),
                InvoiceId = GetString(row, headers, "Invoice ID"),
                CustomerId = GetString(row, headers, "Customer ID"),
                Date = GetDateTime(row, headers, "Date"),
                Amount = GetDecimal(row, headers, "Amount"),
                PaymentMethod = ParseEnum(GetString(row, headers, "Payment Method"), PaymentMethod.Cash),
                ReferenceNumber = GetNullableString(row, headers, "Reference"),
                Notes = GetString(row, headers, "Notes")
            };
            data.Payments.Add(payment);
        }
    }

    private void ImportSuppliers(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Suppliers.Clear();
        foreach (var row in rows)
        {
            var supplier = new Supplier
            {
                Id = GetString(row, headers, "ID"),
                Name = GetString(row, headers, "Name"),
                Email = GetString(row, headers, "Email"),
                Phone = GetString(row, headers, "Phone"),
                Website = GetNullableString(row, headers, "Website"),
                Address = new Address
                {
                    Street = GetString(row, headers, "Street"),
                    City = GetString(row, headers, "City"),
                    State = GetString(row, headers, "State"),
                    ZipCode = GetString(row, headers, "Zip Code"),
                    Country = GetString(row, headers, "Country")
                },
                Notes = GetString(row, headers, "Notes")
            };
            data.Suppliers.Add(supplier);
        }
    }

    private void ImportSales(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Sales.Clear();
        foreach (var row in rows)
        {
            var sale = new Sale
            {
                Id = GetString(row, headers, "ID"),
                Date = GetDateTime(row, headers, "Date"),
                CustomerId = GetNullableString(row, headers, "Customer ID"),
                Description = GetString(row, headers, "Description"),
                Amount = GetDecimal(row, headers, "Amount"),
                TaxAmount = GetDecimal(row, headers, "Tax"),
                Total = GetDecimal(row, headers, "Total"),
                ReferenceNumber = GetString(row, headers, "Reference"),
                PaymentStatus = GetString(row, headers, "Payment Status")
            };
            if (string.IsNullOrEmpty(sale.PaymentStatus))
                sale.PaymentStatus = "Paid";
            data.Sales.Add(sale);
        }
    }

    private void ImportRentalInventory(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.RentalInventory.Clear();
        foreach (var row in rows)
        {
            var item = new RentalItem
            {
                Id = GetString(row, headers, "ID"),
                Name = GetString(row, headers, "Name"),
                TotalQuantity = GetInt(row, headers, "Total Qty"),
                AvailableQuantity = GetInt(row, headers, "Available"),
                RentedQuantity = GetInt(row, headers, "Rented"),
                DailyRate = GetDecimal(row, headers, "Daily Rate"),
                WeeklyRate = GetDecimal(row, headers, "Weekly Rate"),
                MonthlyRate = GetDecimal(row, headers, "Monthly Rate"),
                SecurityDeposit = GetDecimal(row, headers, "Deposit"),
                Status = ParseEnum(GetString(row, headers, "Status"), EntityStatus.Active)
            };
            data.RentalInventory.Add(item);
        }
    }

    private void ImportRentalRecords(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Rentals.Clear();
        foreach (var row in rows)
        {
            var record = new RentalRecord
            {
                Id = GetString(row, headers, "ID"),
                CustomerId = GetString(row, headers, "Customer ID"),
                RentalItemId = GetString(row, headers, "Rental Item ID"),
                Quantity = GetInt(row, headers, "Quantity"),
                StartDate = GetDateTime(row, headers, "Start Date"),
                DueDate = GetDateTime(row, headers, "Due Date"),
                ReturnDate = GetNullableDateTime(row, headers, "Return Date"),
                TotalCost = GetDecimal(row, headers, "Total Cost"),
                Status = ParseEnum(GetString(row, headers, "Status"), RentalStatus.Active)
            };
            if (record.TotalCost == 0)
                record.TotalCost = null;
            data.Rentals.Add(record);
        }
    }

    private void ImportCategories(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Categories.Clear();
        foreach (var row in rows)
        {
            var typeStr = GetString(row, headers, "Type");
            var categoryType = typeStr.ToLowerInvariant() switch
            {
                "revenue" or "sales" => CategoryType.Sales,
                "expenses" or "purchase" => CategoryType.Purchase,
                "rental" => CategoryType.Rental,
                _ => CategoryType.Sales
            };

            var category = new Category
            {
                Id = GetString(row, headers, "ID"),
                Name = GetString(row, headers, "Name"),
                Type = categoryType,
                ParentId = GetNullableString(row, headers, "Parent ID"),
                Description = GetNullableString(row, headers, "Description"),
                ItemType = GetString(row, headers, "Item Type"),
                Icon = GetString(row, headers, "Icon")
            };
            if (string.IsNullOrEmpty(category.ItemType))
                category.ItemType = "Product";
            if (string.IsNullOrEmpty(category.Icon))
                category.Icon = "📦";
            data.Categories.Add(category);
        }
    }

    private void ImportDepartments(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Departments.Clear();
        foreach (var row in rows)
        {
            var department = new Department
            {
                Id = GetString(row, headers, "ID"),
                Name = GetString(row, headers, "Name"),
                Description = GetNullableString(row, headers, "Description")
            };
            data.Departments.Add(department);
        }
    }

    private void ImportEmployees(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Employees.Clear();
        foreach (var row in rows)
        {
            var employee = new Employee
            {
                Id = GetString(row, headers, "ID"),
                FirstName = GetString(row, headers, "First Name"),
                LastName = GetString(row, headers, "Last Name"),
                Email = GetString(row, headers, "Email"),
                Phone = GetString(row, headers, "Phone"),
                DateOfBirth = GetNullableDateTime(row, headers, "Date of Birth"),
                DepartmentId = GetNullableString(row, headers, "Department ID"),
                Position = GetString(row, headers, "Position"),
                HireDate = GetDateTime(row, headers, "Hire Date"),
                EmploymentType = GetString(row, headers, "Employment Type"),
                SalaryType = GetString(row, headers, "Salary Type"),
                SalaryAmount = GetDecimal(row, headers, "Salary Amount"),
                PayFrequency = GetString(row, headers, "Pay Frequency"),
                Status = ParseEnum(GetString(row, headers, "Status"), EmployeeStatus.Active)
            };
            if (string.IsNullOrEmpty(employee.EmploymentType))
                employee.EmploymentType = "Full-time";
            if (string.IsNullOrEmpty(employee.SalaryType))
                employee.SalaryType = "Annual";
            if (string.IsNullOrEmpty(employee.PayFrequency))
                employee.PayFrequency = "Bi-weekly";
            data.Employees.Add(employee);
        }
    }

    private void ImportLocations(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.Locations.Clear();
        foreach (var row in rows)
        {
            var location = new Location
            {
                Id = GetString(row, headers, "ID"),
                Name = GetString(row, headers, "Name"),
                ContactPerson = GetString(row, headers, "Contact Person"),
                Phone = GetString(row, headers, "Phone"),
                Address = new Address
                {
                    Street = GetString(row, headers, "Street"),
                    City = GetString(row, headers, "City"),
                    State = GetString(row, headers, "State"),
                    ZipCode = GetString(row, headers, "Zip Code"),
                    Country = GetString(row, headers, "Country")
                },
                Capacity = GetInt(row, headers, "Capacity"),
                CurrentUtilization = GetInt(row, headers, "Utilization")
            };
            data.Locations.Add(location);
        }
    }

    private void ImportRecurringInvoices(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.RecurringInvoices.Clear();
        foreach (var row in rows)
        {
            var recurring = new RecurringInvoice
            {
                Id = GetString(row, headers, "ID"),
                CustomerId = GetString(row, headers, "Customer ID"),
                Amount = GetDecimal(row, headers, "Amount"),
                Description = GetString(row, headers, "Description"),
                Frequency = ParseEnum(GetString(row, headers, "Frequency"), Frequency.Monthly),
                NextInvoiceDate = GetDateTime(row, headers, "Next Date"),
                Status = GetString(row, headers, "Status")
            };
            if (string.IsNullOrEmpty(recurring.Status))
                recurring.Status = "Active";
            data.RecurringInvoices.Add(recurring);
        }
    }

    private void ImportStockAdjustments(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.StockAdjustments.Clear();
        foreach (var row in rows)
        {
            var adjustment = new StockAdjustment
            {
                Id = GetString(row, headers, "ID"),
                InventoryItemId = GetString(row, headers, "Inventory Item ID"),
                AdjustmentType = ParseEnum(GetString(row, headers, "Type"), AdjustmentType.Set),
                Quantity = GetInt(row, headers, "Quantity"),
                PreviousStock = GetInt(row, headers, "Previous Stock"),
                NewStock = GetInt(row, headers, "New Stock"),
                Reason = GetString(row, headers, "Reason"),
                Timestamp = GetDateTime(row, headers, "Timestamp")
            };
            if (adjustment.Timestamp == DateTime.MinValue)
                adjustment.Timestamp = DateTime.UtcNow;
            data.StockAdjustments.Add(adjustment);
        }
    }

    private void ImportPurchaseOrders(CompanyData data, List<string> headers, List<List<object?>> rows)
    {
        data.PurchaseOrders.Clear();
        foreach (var row in rows)
        {
            var po = new PurchaseOrder
            {
                Id = GetString(row, headers, "ID"),
                SupplierId = GetString(row, headers, "Supplier ID"),
                OrderDate = GetDateTime(row, headers, "Order Date"),
                ExpectedDeliveryDate = GetDateTime(row, headers, "Expected Date"),
                Total = GetDecimal(row, headers, "Total"),
                Status = ParseEnum(GetString(row, headers, "Status"), PurchaseOrderStatus.Draft)
            };
            data.PurchaseOrders.Add(po);
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
        data.IdCounters.Sale = GetMaxIdNumber(data.Sales.Select(s => s.Id), "SAL-");
        data.IdCounters.Purchase = GetMaxIdNumber(data.Purchases.Select(p => p.Id), "PUR-");
        data.IdCounters.Invoice = GetMaxIdNumber(data.Invoices.Select(i => i.Id), "INV-");
        data.IdCounters.Payment = GetMaxIdNumber(data.Payments.Select(p => p.Id), "PAY-");
        data.IdCounters.RecurringInvoice = GetMaxIdNumber(data.RecurringInvoices.Select(r => r.Id), "REC-INV-");
        data.IdCounters.InventoryItem = GetMaxIdNumber(data.Inventory.Select(i => i.Id), "INV-ITM-");
        data.IdCounters.StockAdjustment = GetMaxIdNumber(data.StockAdjustments.Select(s => s.Id), "ADJ-");
        data.IdCounters.PurchaseOrder = GetMaxIdNumber(data.PurchaseOrders.Select(p => p.Id), "PO-");
        data.IdCounters.RentalItem = GetMaxIdNumber(data.RentalInventory.Select(r => r.Id), "RNT-ITM-");
        data.IdCounters.Rental = GetMaxIdNumber(data.Rentals.Select(r => r.Id), "RNT-");
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
