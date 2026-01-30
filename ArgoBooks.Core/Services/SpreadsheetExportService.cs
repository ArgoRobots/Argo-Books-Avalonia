using System.Globalization;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for exporting company data to spreadsheet formats (xlsx, csv, pdf).
/// </summary>
public class SpreadsheetExportService
{
    /// <summary>
    /// Exports selected data to an Excel file.
    /// </summary>
    public async Task ExportToExcelAsync(
        string filePath,
        CompanyData companyData,
        List<string> selectedDataItems,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(companyData);
        ArgumentNullException.ThrowIfNull(selectedDataItems);

        using var workbook = new XLWorkbook();

        foreach (var dataItem in selectedDataItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AddWorksheet(workbook, dataItem, companyData, startDate, endDate);
        }

        // Ensure at least one worksheet exists
        if (workbook.Worksheets.Count == 0)
        {
            var ws = workbook.Worksheets.Add("Empty");
            ws.Cell(1, 1).Value = "No data selected for export";
        }

        await Task.Run(() => workbook.SaveAs(filePath), cancellationToken);
    }

    /// <summary>
    /// Exports selected data to a CSV file.
    /// </summary>
    public async Task ExportToCsvAsync(
        string filePath,
        CompanyData companyData,
        List<string> selectedDataItems,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(companyData);
        ArgumentNullException.ThrowIfNull(selectedDataItems);

        var sb = new StringBuilder();

        foreach (var dataItem in selectedDataItems)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendCsvSection(sb, dataItem, companyData, startDate, endDate);
        }

        await File.WriteAllTextAsync(filePath, sb.ToString(), Encoding.UTF8, cancellationToken);
    }

    /// <summary>
    /// Exports selected data to a PDF file.
    /// </summary>
    public async Task ExportToPdfAsync(
        string filePath,
        CompanyData companyData,
        List<string> selectedDataItems,
        DateTime? startDate,
        DateTime? endDate,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(companyData);
        ArgumentNullException.ThrowIfNull(selectedDataItems);

        QuestPDF.Settings.License = LicenseType.Community;

        await Task.Run(() =>
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Text($"{companyData.Settings.Company.Name} - Data Export")
                        .SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);

                    page.Content().Column(column =>
                    {
                        column.Spacing(20);

                        foreach (var dataItem in selectedDataItems)
                        {
                            cancellationToken.ThrowIfCancellationRequested();
                            AddPdfSection(column, dataItem, companyData, startDate, endDate);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" of ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }, cancellationToken);
    }

    private void AddWorksheet(XLWorkbook workbook, string dataItem, CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var (headers, rows) = GetExportData(dataItem, data, startDate, endDate);
        if (headers.Length == 0) return;

        var ws = workbook.Worksheets.Add(TruncateSheetName(dataItem));

        // Add headers
        for (int col = 0; col < headers.Length; col++)
        {
            var cell = ws.Cell(1, col + 1);
            cell.Value = headers[col];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightBlue;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        }

        // Add data rows
        for (int row = 0; row < rows.Count; row++)
        {
            for (int col = 0; col < rows[row].Length; col++)
            {
                var value = rows[row][col];
                var cell = ws.Cell(row + 2, col + 1);

                if (value is decimal d)
                {
                    cell.Value = d;
                    cell.Style.NumberFormat.Format = "#,##0.00";
                }
                else if (value is DateTime dt)
                {
                    cell.Value = dt;
                    cell.Style.NumberFormat.Format = "yyyy-MM-dd";
                }
                else if (value is int i)
                {
                    cell.Value = i;
                }
                else
                {
                    cell.Value = value.ToString() ?? "";
                }
            }
        }

        // Auto-fit columns
        ws.Columns().AdjustToContents();
    }

    private void AppendCsvSection(StringBuilder sb, string dataItem, CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var (headers, rows) = GetExportData(dataItem, data, startDate, endDate);
        if (headers.Length == 0) return;

        // Section header
        sb.AppendLine($"# {dataItem}");

        // Column headers
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        // Data rows
        foreach (var row in rows)
        {
            sb.AppendLine(string.Join(",", row.Select(v => EscapeCsv(FormatValue(v)))));
        }

        sb.AppendLine();
    }

    private void AddPdfSection(ColumnDescriptor column, string dataItem, CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var (headers, rows) = GetExportData(dataItem, data, startDate, endDate);
        if (headers.Length == 0) return;

        column.Item().Text(dataItem).SemiBold().FontSize(12);

        if (rows.Count == 0)
        {
            column.Item().Text("No records found").Italic().FontSize(9);
            return;
        }

        column.Item().Table(table =>
        {
            // Define columns
            table.ColumnsDefinition(columns =>
            {
                foreach (var _ in headers)
                {
                    columns.RelativeColumn();
                }
            });

            // Header row
            table.Header(header =>
            {
                foreach (var h in headers)
                {
                    header.Cell().Background(Colors.Blue.Lighten4)
                        .Padding(4).Text(h).SemiBold().FontSize(8);
                }
            });

            // Data rows (limit to avoid huge PDFs)
            var maxRows = Math.Min(rows.Count, 500);
            for (int i = 0; i < maxRows; i++)
            {
                var row = rows[i];
                var bgColor = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                foreach (var value in row)
                {
                    table.Cell().Background(bgColor)
                        .Padding(3).Text(FormatValue(value)).FontSize(8);
                }
            }

            if (rows.Count > maxRows)
            {
                for (int col = 0; col < headers.Length; col++)
                {
                    table.Cell().Background(Colors.Yellow.Lighten4)
                        .Padding(3).Text(col == 0 ? $"... and {rows.Count - maxRows} more records" : "").FontSize(8);
                }
            }
        });
    }

    private (string[] Headers, List<object[]> Rows) GetExportData(string dataItem, CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        return dataItem switch
        {
            "Customers" => GetCustomersData(data),
            "Invoices" => GetInvoicesData(data, startDate, endDate),
            "Expenses" or "Purchases" => GetExpensesData(data, startDate, endDate),
            "Products" => GetProductsData(data),
            "Inventory" => GetInventoryData(data),
            "Payments" => GetPaymentsData(data, startDate, endDate),
            "Suppliers" => GetSuppliersData(data),
            "Revenue" or "Sales" => GetRevenueData(data, startDate, endDate),
            "Rental Inventory" => GetRentalInventoryData(data),
            "Rental Records" => GetRentalRecordsData(data, startDate, endDate),
            "Categories" => GetCategoriesData(data),
            "Departments" => GetDepartmentsData(data),
            "Employees" => GetEmployeesData(data),
            "Locations" => GetLocationsData(data),
            "Recurring Invoices" => GetRecurringInvoicesData(data),
            "Stock Adjustments" => GetStockAdjustmentsData(data, startDate, endDate),
            "Purchase Orders" => GetPurchaseOrdersData(data, startDate, endDate),
            "Purchase Order Line Items" => GetPurchaseOrderLineItemsData(data, startDate, endDate),
            _ => ([], [])
        };
    }

    private (string[] Headers, List<object[]> Rows) GetCustomersData(CompanyData data)
    {
        var headers = new[] { "ID", "Name", "Company", "Email", "Phone", "Street", "City", "State", "Zip Code", "Country", "Notes", "Status", "Total Purchases" };
        var rows = data.Customers.Select(c => new object[]
        {
            c.Id,
            c.Name,
            c.CompanyName ?? "",
            c.Email,
            c.Phone,
            c.Address.Street,
            c.Address.City,
            c.Address.State,
            c.Address.ZipCode,
            c.Address.Country,
            c.Notes,
            c.Status.ToString(),
            c.TotalPurchases
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetInvoicesData(CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var headers = new[] { "Invoice #", "Customer ID", "Issue Date", "Due Date", "Subtotal", "Tax", "Total", "Paid", "Balance", "Status" };
        var filtered = data.Invoices.Where(i => IsInDateRange(i.IssueDate, startDate, endDate));
        var rows = filtered.Select(i => new object[]
        {
            i.InvoiceNumber,
            i.CustomerId,
            i.IssueDate,
            i.DueDate,
            i.Subtotal,
            i.TaxAmount,
            i.Total,
            i.AmountPaid,
            i.Balance,
            i.Status.ToString()
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetExpensesData(CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var headers = new[] { "ID", "Date", "Supplier ID", "Product", "Amount", "Tax", "Total", "Reference", "Payment Method" };
        var filtered = data.Expenses.Where(p => IsInDateRange(p.Date, startDate, endDate));
        var rows = filtered.Select(p => new object[]
        {
            p.Id,
            p.Date,
            p.SupplierId ?? "",
            p.Description,
            p.Amount,
            p.TaxAmount,
            p.Total,
            p.ReferenceNumber,
            p.PaymentMethod.ToString()
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetProductsData(CompanyData data)
    {
        // Build lookup dictionaries for category and supplier names
        var categoryLookup = data.Categories.ToDictionary(c => c.Id, c => c.Name);
        var supplierLookup = data.Suppliers.ToDictionary(s => s.Id, s => s.Name);

        var headers = new[] { "ID", "Name", "Type", "Item Type", "SKU", "Description", "Category ID", "Category Name", "Supplier ID", "Supplier Name", "Reorder Point", "Overstock Threshold" };
        var rows = data.Products.Select(p => new object[]
        {
            p.Id,
            p.Name,
            p.Type switch
            {
                CategoryType.Revenue => "Revenue",
                CategoryType.Expense => "Expenses",
                _ => p.Type.ToString()
            },
            p.ItemType,
            p.Sku,
            p.Description,
            p.CategoryId ?? "",
            p.CategoryId != null && categoryLookup.TryGetValue(p.CategoryId, out var catName) ? catName : "",
            p.SupplierId ?? "",
            p.SupplierId != null && supplierLookup.TryGetValue(p.SupplierId, out var supName) ? supName : "",
            p.ReorderPoint,
            p.OverstockThreshold
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetInventoryData(CompanyData data)
    {
        var headers = new[] { "ID", "Product ID", "Location ID", "In Stock", "Reserved", "Available", "Reorder Point", "Unit Cost", "Last Updated" };
        var rows = data.Inventory.Select(i => new object[]
        {
            i.Id,
            i.ProductId,
            i.LocationId,
            i.InStock,
            i.Reserved,
            i.Available,
            i.ReorderPoint,
            i.UnitCost,
            i.LastUpdated
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetPaymentsData(CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var headers = new[] { "ID", "Invoice ID", "Customer ID", "Date", "Amount", "Payment Method", "Reference", "Notes" };
        var filtered = data.Payments.Where(p => IsInDateRange(p.Date, startDate, endDate));
        var rows = filtered.Select(p => new object[]
        {
            p.Id,
            p.InvoiceId,
            p.CustomerId,
            p.Date,
            p.Amount,
            p.PaymentMethod.ToString(),
            p.ReferenceNumber ?? "",
            p.Notes
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetSuppliersData(CompanyData data)
    {
        var headers = new[] { "ID", "Name", "Email", "Phone", "Website", "Street", "City", "State", "Zip Code", "Country", "Notes" };
        var rows = data.Suppliers.Select(s => new object[]
        {
            s.Id,
            s.Name,
            s.Email,
            s.Phone,
            s.Website,
            s.Address.Street,
            s.Address.City,
            s.Address.State,
            s.Address.ZipCode,
            s.Address.Country,
            s.Notes
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetRevenueData(CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var headers = new[] { "ID", "Date", "Customer ID", "Product", "Amount", "Tax", "Total", "Reference", "Payment Status" };
        var filtered = data.Revenues.Where(s => IsInDateRange(s.Date, startDate, endDate));
        var rows = filtered.Select(s => new object[]
        {
            s.Id,
            s.Date,
            s.CustomerId ?? "",
            s.Description,
            s.Amount,
            s.TaxAmount,
            s.Total,
            s.ReferenceNumber,
            s.PaymentStatus
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetRentalInventoryData(CompanyData data)
    {
        var headers = new[] { "ID", "Name", "Total Qty", "Available", "Rented", "Daily Rate", "Weekly Rate", "Monthly Rate", "Deposit", "Status" };
        var rows = data.RentalInventory.Select(r => new object[]
        {
            r.Id,
            r.Name,
            r.TotalQuantity,
            r.AvailableQuantity,
            r.RentedQuantity,
            r.DailyRate,
            r.WeeklyRate,
            r.MonthlyRate,
            r.SecurityDeposit,
            r.Status.ToString()
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetRentalRecordsData(CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var headers = new[] { "ID", "Customer ID", "Rental Item ID", "Quantity", "Start Date", "Due Date", "Return Date", "Total Cost", "Status" };
        var filtered = data.Rentals.Where(r => IsInDateRange(r.StartDate, startDate, endDate));
        var rows = filtered.Select(r => new object[]
        {
            r.Id,
            r.CustomerId,
            r.RentalItemId,
            r.Quantity,
            r.StartDate,
            r.DueDate,
            r.ReturnDate ?? DateTime.MinValue,
            r.TotalCost ?? 0m,
            r.Status.ToString()
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetCategoriesData(CompanyData data)
    {
        var headers = new[] { "ID", "Name", "Type", "Parent ID", "Description", "Item Type", "Icon" };
        var rows = data.Categories.Select(c => new object[]
        {
            c.Id,
            c.Name,
            c.Type switch
            {
                CategoryType.Revenue => "Revenue",
                CategoryType.Expense => "Expenses",
                _ => c.Type.ToString()
            },
            c.ParentId ?? "",
            c.Description ?? "",
            c.ItemType,
            c.Icon
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetDepartmentsData(CompanyData data)
    {
        var headers = new[] { "ID", "Name", "Description" };
        var rows = data.Departments.Select(d => new object[]
        {
            d.Id,
            d.Name,
            d.Description ?? ""
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetEmployeesData(CompanyData data)
    {
        var headers = new[] { "ID", "First Name", "Last Name", "Email", "Phone", "Date of Birth", "Department ID", "Position", "Hire Date", "Employment Type", "Salary Type", "Salary Amount", "Pay Frequency", "Status" };
        var rows = data.Employees.Select(e => new object[]
        {
            e.Id,
            e.FirstName,
            e.LastName,
            e.Email,
            e.Phone,
            e.DateOfBirth ?? DateTime.MinValue,
            e.DepartmentId ?? "",
            e.Position,
            e.HireDate,
            e.EmploymentType,
            e.SalaryType,
            e.SalaryAmount,
            e.PayFrequency,
            e.Status.ToString()
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetLocationsData(CompanyData data)
    {
        var headers = new[] { "ID", "Name", "Contact Person", "Phone", "Street", "City", "State", "Zip Code", "Country", "Capacity", "Utilization" };
        var rows = data.Locations.Select(l => new object[]
        {
            l.Id,
            l.Name,
            l.ContactPerson,
            l.Phone,
            l.Address.Street,
            l.Address.City,
            l.Address.State,
            l.Address.ZipCode,
            l.Address.Country,
            l.Capacity,
            l.CurrentUtilization
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetRecurringInvoicesData(CompanyData data)
    {
        var headers = new[] { "ID", "Customer ID", "Amount", "Description", "Frequency", "Next Date", "Status" };
        var rows = data.RecurringInvoices.Select(r => new object[]
        {
            r.Id,
            r.CustomerId,
            r.Amount,
            r.Description,
            r.Frequency.ToString(),
            r.NextInvoiceDate,
            r.Status
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetStockAdjustmentsData(CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var headers = new[] { "ID", "Inventory Item ID", "Type", "Quantity", "Previous Stock", "New Stock", "Reason", "Timestamp" };
        var filtered = data.StockAdjustments.Where(s => IsInDateRange(s.Timestamp, startDate, endDate));
        var rows = filtered.Select(s => new object[]
        {
            s.Id,
            s.InventoryItemId,
            s.AdjustmentType.ToString(),
            s.Quantity,
            s.PreviousStock,
            s.NewStock,
            s.Reason,
            s.Timestamp
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetPurchaseOrdersData(CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var headers = new[] { "ID", "Supplier ID", "Order Date", "Expected Date", "Total", "Status" };
        var filtered = data.PurchaseOrders.Where(p => IsInDateRange(p.OrderDate, startDate, endDate));
        var rows = filtered.Select(p => new object[]
        {
            p.Id,
            p.SupplierId,
            p.OrderDate,
            p.ExpectedDeliveryDate,
            p.Total,
            p.Status.ToString()
        }).ToList();
        return (headers, rows);
    }

    private (string[] Headers, List<object[]> Rows) GetPurchaseOrderLineItemsData(CompanyData data, DateTime? startDate, DateTime? endDate)
    {
        var headers = new[] { "PO ID", "Product ID", "Quantity", "Unit Cost", "Quantity Received" };
        var filtered = data.PurchaseOrders.Where(p => IsInDateRange(p.OrderDate, startDate, endDate));
        var rows = filtered
            .SelectMany(p => p.LineItems.Select(li => new object[]
            {
                p.Id,
                li.ProductId,
                li.Quantity,
                li.UnitCost,
                li.QuantityReceived
            }))
            .ToList();
        return (headers, rows);
    }

    private static bool IsInDateRange(DateTime date, DateTime? startDate, DateTime? endDate)
    {
        if (startDate.HasValue && date.Date < startDate.Value.Date)
            return false;
        if (endDate.HasValue && date.Date > endDate.Value.Date)
            return false;
        return true;
    }

    private static string FormatValue(object? value)
    {
        return value switch
        {
            null => "",
            DateTime dt => dt == DateTime.MinValue ? "" : dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            decimal d => d.ToString("F2", CultureInfo.InvariantCulture),
            _ => value.ToString() ?? ""
        };
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static string TruncateSheetName(string name)
    {
        // Excel worksheet names are limited to 31 characters
        if (name.Length > 31)
            return name[..31];
        return name;
    }
}
