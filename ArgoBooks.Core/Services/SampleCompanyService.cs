using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Transactions;

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

            // Patch rental records with financial data from rental items
            // (the XLSX may not include rate/deposit columns)
            PatchRentalRecordFinancials(context.CompanyData);

            // Link invoices to revenue records so both paths are consistent.
            // For each finalized invoice, either match an existing revenue or create one.
            LinkInvoicesToRevenues(context.CompanyData);

            // Add additional active rental records for richer sample data
            // Use the data's max date as the reference point (not DateTime.Today)
            // so that TimeShiftSampleData can shift these dates along with everything else.
            var maxDate = FindMaxDate(context.CompanyData);
            if (maxDate == DateTime.MinValue)
                maxDate = DateTime.Today;
            AddSampleActiveRentals(context.CompanyData, maxDate);

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
            Phone = "+1 5551000000",
            Country = "Canada"
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
    /// Patches rental records that are missing financial data (RateAmount, SecurityDeposit)
    /// by looking up the associated rental item's rates.
    /// </summary>
    private static void PatchRentalRecordFinancials(CompanyData data)
    {
        foreach (var record in data.Rentals)
        {
            // Patch line items with missing rates/deposits
            foreach (var li in record.LineItems)
            {
                if (li.RateAmount == 0 || li.SecurityDeposit == 0)
                {
                    var liItem = data.RentalInventory.FirstOrDefault(i => i.Id == li.RentalItemId);
                    if (liItem == null) continue;

                    if (li.RateAmount == 0)
                    {
                        li.RateAmount = li.RateType switch
                        {
                            RateType.Weekly => liItem.WeeklyRate,
                            RateType.Monthly => liItem.MonthlyRate,
                            _ => liItem.DailyRate
                        };
                    }
                    if (li.SecurityDeposit == 0)
                        li.SecurityDeposit = liItem.SecurityDeposit;
                }
            }

            // Patch top-level fields
            if (record.RateAmount == 0 || record.SecurityDeposit == 0)
            {
                var item = data.RentalInventory.FirstOrDefault(i => i.Id == record.RentalItemId);
                if (item != null)
                {
                    if (record.RateAmount == 0)
                    {
                        record.RateAmount = record.RateType switch
                        {
                            RateType.Weekly => item.WeeklyRate,
                            RateType.Monthly => item.MonthlyRate,
                            _ => item.DailyRate
                        };
                    }

                    if (record.SecurityDeposit == 0)
                    {
                        record.SecurityDeposit = item.SecurityDeposit;
                    }
                }
            }

            // Mark returned rentals as paid and set deposit refunded
            if (record.Status == RentalStatus.Returned)
            {
                record.Paid = true;
                if (record.DepositRefunded == null)
                {
                    record.DepositRefunded = record.SecurityDeposit;
                }
            }
        }
    }

    /// <summary>
    /// Links sample invoices to revenue records. For each finalized invoice (Sent, Paid, Partial),
    /// tries to match an existing unlinked revenue (same customer and total). If no match is found,
    /// creates a new revenue from the invoice. This ensures the revenue table is the single source
    /// of truth for all financial data, matching what happens when users create invoices in the UI.
    /// </summary>
    private static void LinkInvoicesToRevenues(CompanyData data)
    {
        // Only process finalized invoices (not drafts or cancelled)
        var finalizedStatuses = new[]
        {
            InvoiceStatus.Sent, InvoiceStatus.Paid, InvoiceStatus.Partial, InvoiceStatus.Pending
        };

        // Track which revenues have already been matched so we don't double-link
        var matchedRevenueIds = new HashSet<string>();

        foreach (var invoice in data.Invoices.Where(i => finalizedStatuses.Contains(i.Status)))
        {
            // Skip if this invoice already has a linked revenue
            if (data.Revenues.Any(r => r.InvoiceId == invoice.Id))
                continue;

            // Try to find a matching unlinked revenue (same customer + same total)
            var matchingRevenue = data.Revenues.FirstOrDefault(r =>
                r.InvoiceId == null &&
                !matchedRevenueIds.Contains(r.Id) &&
                r.CustomerId == invoice.CustomerId &&
                Math.Abs(r.Total - invoice.Total) < 0.01m);

            if (matchingRevenue != null)
            {
                // Link existing revenue to this invoice
                matchingRevenue.InvoiceId = invoice.Id;
                matchedRevenueIds.Add(matchingRevenue.Id);
            }
            else
            {
                // Create a new revenue from the invoice
                data.IdCounters.Revenue++;
                var revenueId = $"REV-{invoice.IssueDate:yyyy}-{data.IdCounters.Revenue:D5}";

                // If the invoice has no line items (common for sample data imported from Excel),
                // build a line item and try to resolve a product from the customer's other revenues
                // so the revenue gets properly categorized (avoids "Uncategorized" in reports).
                List<LineItem> lineItems;
                string description;

                if (invoice.LineItems.Count > 0)
                {
                    description = invoice.LineItems.Count == 1
                        ? invoice.LineItems[0].Description
                        : $"{invoice.LineItems[0].Description} (+{invoice.LineItems.Count - 1} more)";
                    lineItems = invoice.LineItems.Select(li => new LineItem
                    {
                        ProductId = li.ProductId,
                        Description = li.Description,
                        Quantity = li.Quantity,
                        UnitPrice = li.UnitPrice,
                        TaxRate = li.TaxRate,
                        Discount = li.Discount
                    }).ToList();
                }
                else
                {
                    // Find a product from the customer's existing revenues for proper categorization
                    var customerRevenue = data.Revenues.FirstOrDefault(r =>
                        r.CustomerId == invoice.CustomerId &&
                        r.LineItems.Count > 0 &&
                        !string.IsNullOrEmpty(r.LineItems[0].ProductId));

                    var productId = customerRevenue?.LineItems[0].ProductId;
                    var productDescription = customerRevenue?.Description ?? $"Invoice {invoice.InvoiceNumber}";
                    description = productDescription;

                    lineItems =
                    [
                        new LineItem
                        {
                            ProductId = productId,
                            Description = productDescription,
                            Quantity = 1,
                            UnitPrice = invoice.Subtotal > 0 ? invoice.Subtotal : invoice.Total,
                            TaxRate = invoice.Subtotal > 0 ? invoice.TaxAmount / invoice.Subtotal : 0
                        }
                    ];
                }

                var totalQuantity = lineItems.Sum(li => li.Quantity);
                if (totalQuantity == 0) totalQuantity = 1;

                var paymentStatus = invoice.Status == InvoiceStatus.Paid ? "Paid" : "Unpaid";

                var revenue = new Revenue
                {
                    Id = revenueId,
                    Date = invoice.IssueDate,
                    CustomerId = invoice.CustomerId,
                    Description = description,
                    LineItems = lineItems,
                    Quantity = totalQuantity,
                    UnitPrice = totalQuantity > 0 ? Math.Round(invoice.Subtotal / totalQuantity, 2) : 0,
                    Subtotal = invoice.Subtotal,
                    Amount = invoice.Subtotal,
                    TaxRate = invoice.Subtotal > 0 ? invoice.TaxAmount / invoice.Subtotal * 100 : 0,
                    TaxAmount = invoice.TaxAmount,
                    Total = invoice.Total,
                    PaymentMethod = PaymentMethod.Other,
                    PaymentStatus = paymentStatus,
                    InvoiceId = invoice.Id,
                    ReferenceNumber = invoice.InvoiceNumber,
                    CreatedAt = invoice.CreatedAt,
                    UpdatedAt = invoice.UpdatedAt,
                    OriginalCurrency = invoice.OriginalCurrency,
                    TotalUSD = invoice.TotalUSD > 0 ? invoice.TotalUSD : invoice.Total,
                    TaxAmountUSD = invoice.TaxAmount
                };

                data.Revenues.Add(revenue);
                matchedRevenueIds.Add(revenueId);
            }
        }
    }

    /// <summary>
    /// Adds additional active rental records to the sample company for richer demo data.
    /// </summary>
    private static void AddSampleActiveRentals(CompanyData data, DateTime referenceDate)
    {
        var items = data.RentalInventory;
        var customers = data.Customers;
        if (items.Count == 0 || customers.Count == 0) return;

        var today = referenceDate;
        var maxId = data.Rentals
            .Select(r => int.TryParse(r.Id.Replace("RNT-", ""), out var n) ? n : 0)
            .DefaultIfEmpty(0)
            .Max();

        var newRentals = new List<RentalRecord>();

        // Pick items and customers for new rentals
        var item0 = items.Count > 0 ? items[0] : items[0];
        var item1 = items.Count > 1 ? items[1] : items[0];
        var item2 = items.Count > 2 ? items[2] : items[0];

        var cust0 = customers.Count > 0 ? customers[0] : customers[0];
        var cust1 = customers.Count > 1 ? customers[1] : customers[0];
        var cust2 = customers.Count > 2 ? customers[2] : customers[0];
        var cust3 = customers.Count > 3 ? customers[3] : customers[0];

        // Active rental 1 - started 5 days ago, due in 9 days, daily rate (single item)
        newRentals.Add(new RentalRecord
        {
            Id = $"RNT-{++maxId:D3}",
            RentalItemId = item0.Id,
            CustomerId = cust0.Id!,
            Quantity = 1,
            RateType = RateType.Daily,
            RateAmount = item0.DailyRate,
            SecurityDeposit = item0.SecurityDeposit,
            LineItems = [new RentalLineItem { RentalItemId = item0.Id, Quantity = 1, RateType = RateType.Daily, RateAmount = item0.DailyRate, SecurityDeposit = item0.SecurityDeposit }],
            StartDate = today.AddDays(-5),
            DueDate = today.AddDays(9),
            Status = RentalStatus.Active,
            Paid = false,
            CreatedAt = referenceDate,
            UpdatedAt = referenceDate
        });

        // Active rental 2 - started 2 days ago, due in 5 days, multi-item (2 different items)
        var multiQty = 1 + 1;
        var multiDeposit = item0.SecurityDeposit + item1.SecurityDeposit;
        newRentals.Add(new RentalRecord
        {
            Id = $"RNT-{++maxId:D3}",
            RentalItemId = item0.Id,
            CustomerId = cust1.Id!,
            Quantity = multiQty,
            RateType = RateType.Daily,
            RateAmount = item0.DailyRate,
            SecurityDeposit = multiDeposit,
            LineItems =
            [
                new RentalLineItem { RentalItemId = item0.Id, Quantity = 1, RateType = RateType.Daily, RateAmount = item0.DailyRate, SecurityDeposit = item0.SecurityDeposit },
                new RentalLineItem { RentalItemId = item1.Id, Quantity = 1, RateType = RateType.Daily, RateAmount = item1.DailyRate, SecurityDeposit = item1.SecurityDeposit }
            ],
            StartDate = today.AddDays(-2),
            DueDate = today.AddDays(5),
            Status = RentalStatus.Active,
            Paid = false,
            CreatedAt = referenceDate,
            UpdatedAt = referenceDate
        });

        // Active rental 3 - started 10 days ago, due in 4 days, weekly rate (single item)
        newRentals.Add(new RentalRecord
        {
            Id = $"RNT-{++maxId:D3}",
            RentalItemId = item2.Id,
            CustomerId = cust2.Id!,
            Quantity = 1,
            RateType = RateType.Weekly,
            RateAmount = item2.WeeklyRate,
            SecurityDeposit = item2.SecurityDeposit,
            LineItems = [new RentalLineItem { RentalItemId = item2.Id, Quantity = 1, RateType = RateType.Weekly, RateAmount = item2.WeeklyRate, SecurityDeposit = item2.SecurityDeposit }],
            StartDate = today.AddDays(-10),
            DueDate = today.AddDays(4),
            Status = RentalStatus.Active,
            Paid = false,
            CreatedAt = referenceDate,
            UpdatedAt = referenceDate
        });

        // Active rental 4 - overdue, started 20 days ago, due 3 days ago (single item)
        newRentals.Add(new RentalRecord
        {
            Id = $"RNT-{++maxId:D3}",
            RentalItemId = item0.Id,
            CustomerId = cust3.Id!,
            Quantity = 1,
            RateType = RateType.Weekly,
            RateAmount = item0.WeeklyRate,
            SecurityDeposit = item0.SecurityDeposit,
            LineItems = [new RentalLineItem { RentalItemId = item0.Id, Quantity = 1, RateType = RateType.Weekly, RateAmount = item0.WeeklyRate, SecurityDeposit = item0.SecurityDeposit }],
            StartDate = today.AddDays(-20),
            DueDate = today.AddDays(-3),
            Status = RentalStatus.Active,
            Paid = false,
            CreatedAt = referenceDate,
            UpdatedAt = referenceDate
        });

        foreach (var rental in newRentals)
        {
            data.Rentals.Add(rental);

            // Update inventory counts for all line items
            foreach (var li in rental.LineItems)
            {
                var rentalItem = data.RentalInventory.FirstOrDefault(i => i.Id == li.RentalItemId);
                if (rentalItem != null && rentalItem.AvailableQuantity >= li.Quantity)
                {
                    rentalItem.AvailableQuantity -= li.Quantity;
                    rentalItem.RentedQuantity += li.Quantity;
                }
            }
        }

        // Update the ID counter
        data.IdCounters.Rental = maxId;
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

        var targetDate = DateTime.Today.AddDays(-1);
        var offset = targetDate - maxDate.Date;

        if (Math.Abs(offset.TotalDays) <= 1)
            return false;

        ApplyDateOffset(data, offset);
        return true;
    }

    private static DateTime FindMaxDate(CompanyData data)
    {
        var dates = new List<DateTime>();

        // Consider all date-bearing records so the time-shift offset ensures
        // no records end up with future dates (which would be excluded by filters)
        dates.AddRange(data.Revenues.Select(s => s.Date));
        dates.AddRange(data.Expenses.Select(p => p.Date));
        dates.AddRange(data.Returns.Select(r => r.ReturnDate));
        dates.AddRange(data.LostDamaged.Select(ld => ld.DateDiscovered));

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
