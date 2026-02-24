using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Service for generating table data for reports.
/// </summary>
public class ReportTableDataService(CompanyData? companyData, ReportFilters filters)
{
    /// <summary>
    /// Gets the date range based on filters (delegates to shared ReportFilters.GetDateRange).
    /// </summary>
    private (DateTime Start, DateTime End) GetDateRange() => filters.GetDateRange();

    #region Sales Data

    /// <summary>
    /// Gets sales transaction data for table display.
    /// </summary>
    public List<TransactionTableRow> GetRevenueTableData(TableReportElement tableConfig)
    {
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate);

        // Apply data selection
        query = tableConfig.DataSelection switch
        {
            TableDataSelection.TopByAmount => query.OrderByDescending(s => s.Total),
            TableDataSelection.BottomByAmount => query.OrderBy(s => s.Total),
            _ => query.OrderByDescending(s => s.Date)
        };

        // Apply sort order
        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(s => s.Date),
            TableSortOrder.DateDescending => query.OrderByDescending(s => s.Date),
            TableSortOrder.AmountAscending => query.OrderBy(s => s.Total),
            TableSortOrder.AmountDescending => query.OrderByDescending(s => s.Total),
            _ => query
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            query = query.Take(tableConfig.MaxRows);
        }

        return query.Select(CreateRevenueRow).ToList();
    }

    private TransactionTableRow CreateRevenueRow(Revenue revenue)
    {
        var customer = companyData?.GetCustomer(revenue.CustomerId ?? "");
        var accountant = companyData?.GetAccountant(revenue.AccountantId ?? "");
        var primaryItem = revenue.LineItems.FirstOrDefault();
        var product = primaryItem != null ? companyData?.GetProduct(primaryItem.ProductId ?? "") : null;

        return new TransactionTableRow
        {
            Id = revenue.Id,
            TransactionId = revenue.ReferenceNumber,
            Date = revenue.Date,
            TransactionType = "Revenue",
            CompanyName = customer?.Name ?? "Unknown",
            ProductName = product?.Description ?? (revenue.LineItems.Count > 1 ? $"Multiple ({revenue.LineItems.Count} items)" : revenue.Description),
            Quantity = (int)(revenue.LineItems.Sum(i => i.Quantity)),
            UnitPrice = primaryItem?.UnitPrice ?? revenue.UnitPrice,
            Total = revenue.Total,
            Status = revenue.PaymentStatus,
            AccountantName = accountant?.Name ?? "Unknown",
            ShippingCost = revenue.ShippingCost,
            Country = customer?.Address.Country ?? "",
            Notes = revenue.Notes
        };
    }

    #endregion

    #region Purchase Data

    /// <summary>
    /// Gets purchase transaction data for table display.
    /// </summary>
    public List<TransactionTableRow> GetExpensesTableData(TableReportElement tableConfig)
    {
        if (companyData?.Expenses == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate);

        // Apply data selection
        query = tableConfig.DataSelection switch
        {
            TableDataSelection.TopByAmount => query.OrderByDescending(p => p.Total),
            TableDataSelection.BottomByAmount => query.OrderBy(p => p.Total),
            _ => query.OrderByDescending(p => p.Date)
        };

        // Apply sort order
        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(p => p.Date),
            TableSortOrder.DateDescending => query.OrderByDescending(p => p.Date),
            TableSortOrder.AmountAscending => query.OrderBy(p => p.Total),
            TableSortOrder.AmountDescending => query.OrderByDescending(p => p.Total),
            _ => query
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            query = query.Take(tableConfig.MaxRows);
        }

        return query.Select(CreateExpenseRow).ToList();
    }

    private TransactionTableRow CreateExpenseRow(Expense expense)
    {
        var supplier = companyData?.GetSupplier(expense.SupplierId ?? "");
        var accountant = companyData?.GetAccountant(expense.AccountantId ?? "");

        return new TransactionTableRow
        {
            Id = expense.Id,
            TransactionId = expense.ReferenceNumber,
            Date = expense.Date,
            TransactionType = "Expense",
            CompanyName = supplier?.Name ?? "Unknown",
            ProductName = expense.Description,
            Quantity = (int)expense.Quantity,
            UnitPrice = expense.UnitPrice,
            Total = expense.Total,
            Status = "Completed",
            AccountantName = accountant?.Name ?? "Unknown",
            ShippingCost = expense.ShippingCost,
            Country = supplier?.Address.Country ?? "",
            Notes = expense.Notes
        };
    }

    #endregion

    #region Combined Data

    /// <summary>
    /// Gets combined transaction data (sales and purchases) for table display.
    /// </summary>
    public List<TransactionTableRow> GetAllTransactionsTableData(TableReportElement tableConfig)
    {
        var noMaxConfig = new TableReportElement
        {
            DataSelection = tableConfig.DataSelection,
            SortOrder = tableConfig.SortOrder,
            MaxRows = 0
        };

        var sales = filters.TransactionType is TransactionType.Revenue
            ? GetRevenueTableData(noMaxConfig)
            : [];

        var purchases = filters.TransactionType is TransactionType.Expenses
            ? GetExpensesTableData(noMaxConfig)
            : [];

        var combined = sales.Concat(purchases).ToList();

        // Apply sort order
        combined = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => combined.OrderBy(t => t.Date).ToList(),
            TableSortOrder.DateDescending => combined.OrderByDescending(t => t.Date).ToList(),
            TableSortOrder.AmountAscending => combined.OrderBy(t => t.Total).ToList(),
            TableSortOrder.AmountDescending => combined.OrderByDescending(t => t.Total).ToList(),
            _ => combined.OrderByDescending(t => t.Date).ToList()
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            combined = combined.Take(tableConfig.MaxRows).ToList();
        }

        return combined;
    }

    #endregion

    #region Invoices Data

    public List<InvoiceTableRow> GetInvoicesTableData(TableReportElement tableConfig)
    {
        if (companyData?.Invoices == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.Invoices
            .Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate);

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(i => i.IssueDate),
            TableSortOrder.DateDescending => query.OrderByDescending(i => i.IssueDate),
            TableSortOrder.AmountAscending => query.OrderBy(i => i.Total),
            TableSortOrder.AmountDescending => query.OrderByDescending(i => i.Total),
            _ => query.OrderByDescending(i => i.IssueDate)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(i =>
        {
            var customer = companyData?.GetCustomer(i.CustomerId);
            return new InvoiceTableRow
            {
                Id = i.Id,
                InvoiceNumber = i.InvoiceNumber,
                IssueDate = i.IssueDate,
                DueDate = i.DueDate,
                CustomerName = customer?.Name ?? "Unknown",
                Total = i.Total,
                AmountPaid = i.AmountPaid,
                Balance = i.Balance,
                Status = i.Status.ToString()
            };
        }).ToList();
    }

    #endregion

    #region Payments Data

    public List<PaymentTableRow> GetPaymentsTableData(TableReportElement tableConfig)
    {
        if (companyData?.Payments == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.Payments
            .Where(p => p.Date >= startDate && p.Date <= endDate);

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(p => p.Date),
            TableSortOrder.DateDescending => query.OrderByDescending(p => p.Date),
            TableSortOrder.AmountAscending => query.OrderBy(p => p.Amount),
            TableSortOrder.AmountDescending => query.OrderByDescending(p => p.Amount),
            _ => query.OrderByDescending(p => p.Date)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(p =>
        {
            var customer = companyData?.GetCustomer(p.CustomerId);
            return new PaymentTableRow
            {
                Id = p.Id,
                Date = p.Date,
                CustomerName = customer?.Name ?? "Unknown",
                Amount = p.Amount,
                PaymentMethod = p.PaymentMethod.ToString(),
                ReferenceNumber = p.ReferenceNumber ?? "",
                InvoiceId = p.InvoiceId
            };
        }).ToList();
    }

    #endregion

    #region Rental Records Data

    public List<RentalRecordTableRow> GetRentalRecordsTableData(TableReportElement tableConfig)
    {
        if (companyData?.Rentals == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.Rentals
            .Where(r => r.StartDate >= startDate && r.StartDate <= endDate);

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(r => r.StartDate),
            TableSortOrder.DateDescending => query.OrderByDescending(r => r.StartDate),
            TableSortOrder.AmountAscending => query.OrderBy(r => r.TotalCost ?? 0),
            TableSortOrder.AmountDescending => query.OrderByDescending(r => r.TotalCost ?? 0),
            _ => query.OrderByDescending(r => r.StartDate)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(r =>
        {
            var customer = companyData?.GetCustomer(r.CustomerId);
            var rentalItem = companyData?.RentalInventory.FirstOrDefault(ri => ri.Id == r.RentalItemId);
            return new RentalRecordTableRow
            {
                Id = r.Id,
                ItemName = rentalItem?.Name ?? "Unknown",
                CustomerName = customer?.Name ?? "Unknown",
                StartDate = r.StartDate,
                DueDate = r.DueDate,
                ReturnDate = r.ReturnDate,
                RateAmount = r.RateAmount,
                TotalCost = r.TotalCost ?? 0,
                Status = r.Status.ToString()
            };
        }).ToList();
    }

    #endregion

    #region Rental Items Data

    public List<RentalItemTableRow> GetRentalItemsTableData(TableReportElement tableConfig)
    {
        if (companyData?.RentalInventory == null)
            return [];

        var query = companyData.RentalInventory.AsEnumerable();

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.AmountAscending => query.OrderBy(r => r.DailyRate),
            TableSortOrder.AmountDescending => query.OrderByDescending(r => r.DailyRate),
            _ => query.OrderBy(r => r.Name)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(r => new RentalItemTableRow
        {
            Id = r.Id,
            Name = r.Name,
            TotalQuantity = r.TotalQuantity,
            AvailableQuantity = r.AvailableQuantity,
            RentedQuantity = r.RentedQuantity,
            DailyRate = r.DailyRate,
            WeeklyRate = r.WeeklyRate,
            MonthlyRate = r.MonthlyRate,
            Status = r.Status.ToString()
        }).ToList();
    }

    #endregion

    #region Inventory Data

    public List<InventoryTableRow> GetInventoryTableData(TableReportElement tableConfig)
    {
        if (companyData?.Inventory == null)
            return [];

        var query = companyData.Inventory.AsEnumerable();

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.AmountAscending => query.OrderBy(i => i.TotalValue),
            TableSortOrder.AmountDescending => query.OrderByDescending(i => i.TotalValue),
            _ => query.OrderBy(i => i.Sku)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(i =>
        {
            var product = companyData?.GetProduct(i.ProductId);
            var location = companyData?.Locations.FirstOrDefault(l => l.Id == i.LocationId);
            return new InventoryTableRow
            {
                Id = i.Id,
                ProductName = product?.Name ?? "Unknown",
                Sku = i.Sku,
                LocationName = location?.Name ?? "Unknown",
                InStock = i.InStock,
                Reserved = i.Reserved,
                Available = i.Available,
                UnitCost = i.UnitCost,
                TotalValue = i.TotalValue,
                Status = i.Status.ToString()
            };
        }).ToList();
    }

    #endregion

    #region Purchase Orders Data

    public List<PurchaseOrderTableRow> GetPurchaseOrdersTableData(TableReportElement tableConfig)
    {
        if (companyData?.PurchaseOrders == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.PurchaseOrders
            .Where(po => po.OrderDate >= startDate && po.OrderDate <= endDate);

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(po => po.OrderDate),
            TableSortOrder.DateDescending => query.OrderByDescending(po => po.OrderDate),
            TableSortOrder.AmountAscending => query.OrderBy(po => po.Total),
            TableSortOrder.AmountDescending => query.OrderByDescending(po => po.Total),
            _ => query.OrderByDescending(po => po.OrderDate)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(po =>
        {
            var supplier = companyData?.GetSupplier(po.SupplierId);
            return new PurchaseOrderTableRow
            {
                Id = po.Id,
                PoNumber = po.PoNumber,
                SupplierName = supplier?.Name ?? "Unknown",
                OrderDate = po.OrderDate,
                ExpectedDeliveryDate = po.ExpectedDeliveryDate,
                Total = po.Total,
                Status = po.Status.ToString()
            };
        }).ToList();
    }

    #endregion

    #region Stock Adjustments Data

    public List<StockAdjustmentTableRow> GetStockAdjustmentsTableData(TableReportElement tableConfig)
    {
        if (companyData?.StockAdjustments == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.StockAdjustments
            .Where(sa => sa.Timestamp >= startDate && sa.Timestamp <= endDate);

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(sa => sa.Timestamp),
            TableSortOrder.DateDescending => query.OrderByDescending(sa => sa.Timestamp),
            _ => query.OrderByDescending(sa => sa.Timestamp)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(sa =>
        {
            var inventoryItem = companyData?.Inventory.FirstOrDefault(i => i.Id == sa.InventoryItemId);
            var product = inventoryItem != null ? companyData?.GetProduct(inventoryItem.ProductId) : null;
            return new StockAdjustmentTableRow
            {
                Id = sa.Id,
                ProductName = product?.Name ?? "Unknown",
                AdjustmentType = sa.AdjustmentType.ToString(),
                Quantity = sa.Quantity,
                PreviousStock = sa.PreviousStock,
                NewStock = sa.NewStock,
                Reason = sa.Reason,
                Timestamp = sa.Timestamp
            };
        }).ToList();
    }

    #endregion

    #region Stock Transfers Data

    public List<StockTransferTableRow> GetStockTransfersTableData(TableReportElement tableConfig)
    {
        if (companyData?.StockTransfers == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.StockTransfers
            .Where(st => st.TransferDate >= startDate && st.TransferDate <= endDate);

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(st => st.TransferDate),
            TableSortOrder.DateDescending => query.OrderByDescending(st => st.TransferDate),
            _ => query.OrderByDescending(st => st.TransferDate)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(st =>
        {
            var inventoryItem = companyData?.Inventory.FirstOrDefault(i => i.Id == st.InventoryItemId);
            var product = inventoryItem != null ? companyData?.GetProduct(inventoryItem.ProductId) : null;
            var sourceLoc = companyData?.Locations.FirstOrDefault(l => l.Id == st.SourceLocationId);
            var destLoc = companyData?.Locations.FirstOrDefault(l => l.Id == st.DestinationLocationId);
            return new StockTransferTableRow
            {
                Id = st.Id,
                ProductName = product?.Name ?? "Unknown",
                SourceLocation = sourceLoc?.Name ?? "Unknown",
                DestinationLocation = destLoc?.Name ?? "Unknown",
                Quantity = st.Quantity,
                TransferDate = st.TransferDate,
                Status = st.Status.ToString()
            };
        }).ToList();
    }

    #endregion

    #region Receipts Data

    public List<ReceiptTableRow> GetReceiptsTableData(TableReportElement tableConfig)
    {
        if (companyData?.Receipts == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.Receipts
            .Where(r => r.Date >= startDate && r.Date <= endDate);

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(r => r.Date),
            TableSortOrder.DateDescending => query.OrderByDescending(r => r.Date),
            TableSortOrder.AmountAscending => query.OrderBy(r => r.Amount),
            TableSortOrder.AmountDescending => query.OrderByDescending(r => r.Amount),
            _ => query.OrderByDescending(r => r.Date)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(r => new ReceiptTableRow
        {
            Id = r.Id,
            Date = r.Date,
            FileName = r.FileName,
            Supplier = r.Supplier,
            Amount = r.Amount,
            Source = r.Source
        }).ToList();
    }

    #endregion

    #region Entity Data (Customers, Suppliers, Products, Employees, Departments, Categories, Locations, Accountants)

    public List<CustomerTableRow> GetCustomersTableData(TableReportElement tableConfig)
    {
        if (companyData?.Customers == null)
            return [];

        var query = companyData.Customers.AsEnumerable();

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.AmountAscending => query.OrderBy(c => c.TotalPurchases),
            TableSortOrder.AmountDescending => query.OrderByDescending(c => c.TotalPurchases),
            _ => query.OrderBy(c => c.Name)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(c => new CustomerTableRow
        {
            Id = c.Id,
            Name = c.Name,
            CompanyName = c.CompanyName ?? "",
            Country = c.Address.Country ?? "",
            TotalPurchases = c.TotalPurchases,
            Status = c.Status.ToString()
        }).ToList();
    }

    public List<SupplierTableRow> GetSuppliersTableData(TableReportElement tableConfig)
    {
        if (companyData?.Suppliers == null)
            return [];

        var query = companyData.Suppliers.AsEnumerable();

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(s => new SupplierTableRow
        {
            Id = s.Id,
            Name = s.Name,
            ContactPerson = s.ContactPerson,
            Country = s.Address.Country ?? "",
            PaymentTerms = s.PaymentTerms
        }).ToList();
    }

    public List<ProductTableRow> GetProductsTableData(TableReportElement tableConfig)
    {
        if (companyData?.Products == null)
            return [];

        var query = companyData.Products.AsEnumerable();

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.AmountAscending => query.OrderBy(p => p.UnitPrice),
            TableSortOrder.AmountDescending => query.OrderByDescending(p => p.UnitPrice),
            _ => query.OrderBy(p => p.Name)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(p =>
        {
            var category = companyData?.GetCategory(p.CategoryId ?? "");
            return new ProductTableRow
            {
                Id = p.Id,
                Name = p.Name,
                Sku = p.Sku,
                CategoryName = category?.Name ?? "",
                UnitPrice = p.UnitPrice,
                CostPrice = p.CostPrice,
                Status = p.Status.ToString()
            };
        }).ToList();
    }

    public List<EmployeeTableRow> GetEmployeesTableData(TableReportElement tableConfig)
    {
        if (companyData?.Employees == null)
            return [];

        var query = companyData.Employees.AsEnumerable();

        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(e => e.HireDate),
            TableSortOrder.DateDescending => query.OrderByDescending(e => e.HireDate),
            TableSortOrder.AmountAscending => query.OrderBy(e => e.SalaryAmount),
            TableSortOrder.AmountDescending => query.OrderByDescending(e => e.SalaryAmount),
            _ => query.OrderBy(e => e.LastName)
        };

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(e =>
        {
            var dept = companyData?.Departments.FirstOrDefault(d => d.Id == e.DepartmentId);
            return new EmployeeTableRow
            {
                Id = e.Id,
                FullName = e.FullName,
                Position = e.Position,
                DepartmentName = dept?.Name ?? "",
                HireDate = e.HireDate,
                EmploymentType = e.EmploymentType,
                SalaryAmount = e.SalaryAmount,
                Status = e.Status.ToString()
            };
        }).ToList();
    }

    public List<DepartmentTableRow> GetDepartmentsTableData(TableReportElement tableConfig)
    {
        if (companyData?.Departments == null)
            return [];

        var query = companyData.Departments.AsEnumerable();

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(d =>
        {
            var headEmployee = !string.IsNullOrEmpty(d.HeadEmployeeId)
                ? companyData?.Employees.FirstOrDefault(e => e.Id == d.HeadEmployeeId)
                : null;
            var employeeCount = companyData?.Employees.Count(e => e.DepartmentId == d.Id) ?? 0;
            return new DepartmentTableRow
            {
                Id = d.Id,
                Name = d.Name,
                HeadName = headEmployee?.FullName ?? "",
                EmployeeCount = employeeCount,
                Budget = d.Budget
            };
        }).ToList();
    }

    public List<CategoryTableRow> GetCategoriesTableData(TableReportElement tableConfig)
    {
        if (companyData?.Categories == null)
            return [];

        var query = companyData.Categories.AsEnumerable();

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(c => new CategoryTableRow
        {
            Id = c.Id,
            Name = c.Name,
            Type = c.Type.ToString(),
            ItemType = c.ItemType
        }).ToList();
    }

    public List<LocationTableRow> GetLocationsTableData(TableReportElement tableConfig)
    {
        if (companyData?.Locations == null)
            return [];

        var query = companyData.Locations.AsEnumerable();

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(l => new LocationTableRow
        {
            Id = l.Id,
            Name = l.Name,
            ContactPerson = l.ContactPerson,
            Capacity = l.Capacity,
            CurrentUtilization = l.CurrentUtilization,
            UtilizationPercentage = l.UtilizationPercentage
        }).ToList();
    }

    public List<AccountantTableRow> GetAccountantsTableData(TableReportElement tableConfig)
    {
        if (companyData?.Accountants == null)
            return [];

        var query = companyData.Accountants.AsEnumerable();

        if (tableConfig.MaxRows > 0)
            query = query.Take(tableConfig.MaxRows);

        return query.Select(a => new AccountantTableRow
        {
            Id = a.Id,
            Name = a.Name,
            Email = a.Email,
            Phone = a.Phone,
            AssignedTransactions = a.AssignedTransactions
        }).ToList();
    }

    #endregion

    #region Returns Data

    /// <summary>
    /// Gets returns data for table display.
    /// </summary>
    public List<ReturnTableRow> GetReturnsTableData(TableReportElement tableConfig)
    {
        if (companyData?.Returns == null || !filters.IncludeReturns)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.Returns
            .Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate);

        // Apply sort order
        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(r => r.ReturnDate),
            TableSortOrder.DateDescending => query.OrderByDescending(r => r.ReturnDate),
            TableSortOrder.AmountAscending => query.OrderBy(r => r.RefundAmount),
            TableSortOrder.AmountDescending => query.OrderByDescending(r => r.RefundAmount),
            _ => query.OrderByDescending(r => r.ReturnDate)
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            query = query.Take(tableConfig.MaxRows);
        }

        return query.Select(CreateReturnRow).ToList();
    }

    private ReturnTableRow CreateReturnRow(Return returnRecord)
    {
        // Get first item's product info (returns can have multiple items)
        var firstItem = returnRecord.Items.FirstOrDefault();
        var product = firstItem != null ? companyData?.GetProduct(firstItem.ProductId) : null;
        var category = product != null ? companyData?.GetCategory(product.CategoryId ?? "") : null;

        return new ReturnTableRow
        {
            Id = returnRecord.Id,
            ReturnDate = returnRecord.ReturnDate,
            OriginalTransactionId = returnRecord.OriginalTransactionId,
            ReturnType = "Revenue", // Returns are from revenue transactions
            ProductName = product?.Name ?? (returnRecord.Items.Count > 1 ? $"Multiple ({returnRecord.Items.Count} items)" : "Unknown"),
            CategoryName = category?.Name ?? "Unknown",
            Quantity = returnRecord.Items.Sum(i => i.Quantity),
            RefundAmount = returnRecord.RefundAmount,
            Reason = firstItem?.Reason ?? "Not specified",
            Status = returnRecord.Status.ToString(),
            Notes = returnRecord.Notes
        };
    }

    #endregion

    #region Losses Data

    /// <summary>
    /// Gets losses data for table display.
    /// </summary>
    public List<LossTableRow> GetLossesTableData(TableReportElement tableConfig)
    {
        if (companyData?.LostDamaged == null || !filters.IncludeLosses)
            return [];

        var (startDate, endDate) = GetDateRange();

        var query = companyData.LostDamaged
            .Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate);

        // Apply sort order
        query = tableConfig.SortOrder switch
        {
            TableSortOrder.DateAscending => query.OrderBy(l => l.DateDiscovered),
            TableSortOrder.DateDescending => query.OrderByDescending(l => l.DateDiscovered),
            TableSortOrder.AmountAscending => query.OrderBy(l => l.ValueLost),
            TableSortOrder.AmountDescending => query.OrderByDescending(l => l.ValueLost),
            _ => query.OrderByDescending(l => l.DateDiscovered)
        };

        // Apply max rows
        if (tableConfig.MaxRows > 0)
        {
            query = query.Take(tableConfig.MaxRows);
        }

        return query.Select(CreateLossRow).ToList();
    }

    private LossTableRow CreateLossRow(LostDamaged lossRecord)
    {
        var product = companyData?.GetProduct(lossRecord.ProductId);
        var category = product != null ? companyData?.GetCategory(product.CategoryId ?? "") : null;

        return new LossTableRow
        {
            Id = lossRecord.Id,
            ReportedDate = lossRecord.DateDiscovered,
            LossType = lossRecord.Reason.ToString(),
            ProductName = product?.Name ?? "Unknown",
            CategoryName = category?.Name ?? "Unknown",
            Quantity = lossRecord.Quantity,
            EstimatedValue = lossRecord.ValueLost,
            Reason = lossRecord.Reason.ToString(),
            Location = "", // Not available in model
            Notes = lossRecord.Notes
        };
    }

    #endregion

    #region Summary Statistics

    /// <summary>
    /// Gets summary statistics for the filtered data.
    /// </summary>
    public ReportSummaryStatistics GetSummaryStatistics()
    {
        var (startDate, endDate) = GetDateRange();

        var stats = new ReportSummaryStatistics
        {
            StartDate = startDate,
            EndDate = endDate
        };

        // Calculate revenue statistics
        if (companyData?.Revenues != null &&
            filters.TransactionType is TransactionType.Revenue)
        {
            var sales = companyData.Revenues.Where(s => s.Date >= startDate && s.Date <= endDate).ToList();
            stats.TotalRevenue = sales.Sum(s => s.Total);
            stats.RevenueTransactionCount = sales.Count;
            stats.AverageRevenueTransaction = sales.Count > 0 ? stats.TotalRevenue / sales.Count : 0;
            stats.LargestRevenue = sales.Count > 0 ? sales.Max(s => s.Total) : 0;
            stats.SmallestRevenue = sales.Count > 0 ? sales.Min(s => s.Total) : 0;
        }

        // Calculate expense statistics
        if (companyData?.Expenses != null &&
            filters.TransactionType is TransactionType.Expenses)
        {
            var purchases = companyData.Expenses.Where(p => p.Date >= startDate && p.Date <= endDate).ToList();
            stats.TotalExpenses = purchases.Sum(p => p.Total);
            stats.ExpenseTransactionCount = purchases.Count;
            stats.AverageExpenseTransaction = purchases.Count > 0 ? stats.TotalExpenses / purchases.Count : 0;
            stats.LargestExpense = purchases.Count > 0 ? purchases.Max(p => p.Total) : 0;
            stats.SmallestExpense = purchases.Count > 0 ? purchases.Min(p => p.Total) : 0;
        }

        // Calculate profit
        stats.NetProfit = stats.TotalRevenue - stats.TotalExpenses;
        stats.ProfitMargin = stats.TotalRevenue > 0 ? (stats.NetProfit / stats.TotalRevenue) * 100 : 0;

        // Calculate returns statistics
        if (companyData?.Returns != null && filters.IncludeReturns)
        {
            var returns = companyData.Returns.Where(r => r.ReturnDate >= startDate && r.ReturnDate <= endDate).ToList();
            stats.TotalReturns = returns.Count;
            stats.TotalReturnAmount = returns.Sum(r => r.RefundAmount);
            stats.ReturnRate = stats.RevenueTransactionCount > 0
                ? (decimal)stats.TotalReturns / stats.RevenueTransactionCount * 100
                : 0;
        }

        // Calculate losses statistics
        if (companyData?.LostDamaged != null && filters.IncludeLosses)
        {
            var losses = companyData.LostDamaged.Where(l => l.DateDiscovered >= startDate && l.DateDiscovered <= endDate).ToList();
            stats.TotalLosses = losses.Count;
            stats.TotalLossAmount = losses.Sum(l => l.ValueLost);
        }

        // Calculate shipping statistics
        if (companyData != null)
        {
            var shippingCosts = new List<decimal>();

            shippingCosts.AddRange(companyData.Revenues
                .Where(s => s.Date >= startDate && s.Date <= endDate)
                .Select(s => s.ShippingCost));

            shippingCosts.AddRange(companyData.Expenses
                .Where(p => p.Date >= startDate && p.Date <= endDate)
                .Select(p => p.ShippingCost));

            stats.TotalShippingCosts = shippingCosts.Sum();
            stats.AverageShippingCost = shippingCosts.Count > 0 ? shippingCosts.Average() : 0;
        }

        // Calculate growth rate
        stats.GrowthRate = CalculateGrowthRate(startDate, endDate);

        return stats;
    }

    private decimal CalculateGrowthRate(DateTime startDate, DateTime endDate)
    {
        if (companyData?.Revenues == null)
            return 0;

        var periodLength = (endDate - startDate).Days;
        var previousStartDate = startDate.AddDays(-periodLength);
        var previousEndDate = startDate.AddDays(-1);

        var currentRevenue = companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .Sum(s => s.Total);

        var previousRevenue = companyData.Revenues
            .Where(s => s.Date >= previousStartDate && s.Date <= previousEndDate)
            .Sum(s => s.Total);

        if (previousRevenue == 0)
            return currentRevenue > 0 ? 100 : 0;

        return (currentRevenue - previousRevenue) / previousRevenue * 100;
    }

    #endregion

    #region Top/Bottom Analysis

    /// <summary>
    /// Gets top products by revenue.
    /// </summary>
    public List<ProductAnalysisRow> GetTopProductsByRevenue(int count = 10)
    {
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var productSales = companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .SelectMany(s => s.LineItems)
            .GroupBy(i => i.ProductId)
            .Select(g =>
            {
                var product = companyData.GetProduct(g.Key ?? "");
                var category = product != null ? companyData.GetCategory(product.CategoryId ?? "") : null;

                return new ProductAnalysisRow
                {
                    ProductId = g.Key ?? "",
                    ProductName = product?.Name ?? "Unknown",
                    CategoryName = category?.Name ?? "Unknown",
                    TotalQuantity = (int)g.Sum(i => i.Quantity),
                    TotalRevenue = g.Sum(i => i.Amount),
                    TransactionCount = g.Count(),
                    AveragePrice = g.Average(i => i.UnitPrice)
                };
            })
            .OrderByDescending(p => p.TotalRevenue)
            .Take(count)
            .ToList();

        return productSales;
    }

    /// <summary>
    /// Gets top customers by revenue.
    /// </summary>
    public List<CustomerAnalysisRow> GetTopCustomersByRevenue(int count = 10)
    {
        if (companyData?.Revenues == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var customerSales = companyData.Revenues
            .Where(s => s.Date >= startDate && s.Date <= endDate)
            .GroupBy(s => s.CustomerId)
            .Select(g =>
            {
                var customer = companyData.GetCustomer(g.Key ?? "");

                return new CustomerAnalysisRow
                {
                    CustomerId = g.Key ?? "",
                    CustomerName = customer?.Name ?? "Unknown",
                    Country = customer?.Address.Country ?? "",
                    TotalRevenue = g.Sum(s => s.Total),
                    TransactionCount = g.Count(),
                    AverageTransaction = g.Average(s => s.Total),
                    FirstPurchase = g.Min(s => s.Date),
                    LastPurchase = g.Max(s => s.Date)
                };
            })
            .OrderByDescending(c => c.TotalRevenue)
            .Take(count)
            .ToList();

        return customerSales;
    }

    /// <summary>
    /// Gets top suppliers by purchase volume.
    /// </summary>
    public List<SupplierAnalysisRow> GetTopSuppliersByVolume(int count = 10)
    {
        if (companyData?.Expenses == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var supplierPurchases = companyData.Expenses
            .Where(p => p.Date >= startDate && p.Date <= endDate)
            .GroupBy(p => p.SupplierId)
            .Select(g =>
            {
                var supplier = companyData.GetSupplier(g.Key ?? "");

                return new SupplierAnalysisRow
                {
                    SupplierId = g.Key ?? "",
                    SupplierName = supplier?.Name ?? "Unknown",
                    Country = supplier?.Address.Country ?? "",
                    TotalPurchases = g.Sum(p => p.Total),
                    TransactionCount = g.Count(),
                    AverageTransaction = g.Average(p => p.Total),
                    FirstPurchase = g.Min(p => p.Date),
                    LastPurchase = g.Max(p => p.Date)
                };
            })
            .OrderByDescending(s => s.TotalPurchases)
            .Take(count)
            .ToList();

        return supplierPurchases;
    }

    /// <summary>
    /// Gets top accountants by transaction volume.
    /// </summary>
    public List<AccountantAnalysisRow> GetTopAccountantsByVolume(int count = 10)
    {
        if (companyData == null)
            return [];

        var (startDate, endDate) = GetDateRange();

        var accountantData = new Dictionary<string, (decimal Revenues, decimal Expenses, int Count)>();

        foreach (var revenue in companyData.Revenues.Where(s => s.Date >= startDate && s.Date <= endDate))
        {
            var accountantId = revenue.AccountantId ?? "";
            if (!accountantData.ContainsKey(accountantId))
                accountantData[accountantId] = (0, 0, 0);

            var current = accountantData[accountantId];
            accountantData[accountantId] = (current.Revenues + revenue.Total, current.Expenses, current.Count + 1);
        }

        foreach (var expense in companyData.Expenses.Where(p => p.Date >= startDate && p.Date <= endDate))
        {
            var accountantId = expense.AccountantId ?? "";
            if (!accountantData.ContainsKey(accountantId))
                accountantData[accountantId] = (0, 0, 0);

            var current = accountantData[accountantId];
            accountantData[accountantId] = (current.Revenues, current.Expenses + expense.Total, current.Count + 1);
        }

        return accountantData
            .Select(kvp =>
            {
                var accountant = companyData.GetAccountant(kvp.Key);
                return new AccountantAnalysisRow
                {
                    AccountantId = kvp.Key,
                    AccountantName = accountant?.Name ?? "Unknown",
                    TotalRevenue = kvp.Value.Revenues,
                    TotalPurchases = kvp.Value.Expenses,
                    TotalVolume = kvp.Value.Revenues + kvp.Value.Expenses,
                    TransactionCount = kvp.Value.Count
                };
            })
            .OrderByDescending(a => a.TotalVolume)
            .Take(count)
            .ToList();
    }

    #endregion
}

#region Table Row Models

/// <summary>
/// Represents a transaction row in a table.
/// </summary>
public class TransactionTableRow
{
    public string Id { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string TransactionType { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public string AccountantName { get; set; } = string.Empty;
    public decimal ShippingCost { get; set; }
    public string Country { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents a return row in a table.
/// </summary>
public class ReturnTableRow
{
    public string Id { get; set; } = string.Empty;
    public DateTime ReturnDate { get; set; }
    public string OriginalTransactionId { get; set; } = string.Empty;
    public string ReturnType { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal RefundAmount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents a loss row in a table.
/// </summary>
public class LossTableRow
{
    public string Id { get; set; } = string.Empty;
    public DateTime ReportedDate { get; set; }
    public string LossType { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal EstimatedValue { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// Represents an invoice row in a table.
/// </summary>
public class InvoiceTableRow
{
    public string Id { get; set; } = string.Empty;
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime DueDate { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal Balance { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a payment row in a table.
/// </summary>
public class PaymentTableRow
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string ReferenceNumber { get; set; } = string.Empty;
    public string InvoiceId { get; set; } = string.Empty;
}

/// <summary>
/// Represents a rental record row in a table.
/// </summary>
public class RentalRecordTableRow
{
    public string Id { get; set; } = string.Empty;
    public string ItemName { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime DueDate { get; set; }
    public DateTime? ReturnDate { get; set; }
    public decimal RateAmount { get; set; }
    public decimal TotalCost { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a rental item row in a table.
/// </summary>
public class RentalItemTableRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int RentedQuantity { get; set; }
    public decimal DailyRate { get; set; }
    public decimal WeeklyRate { get; set; }
    public decimal MonthlyRate { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents an inventory item row in a table.
/// </summary>
public class InventoryTableRow
{
    public string Id { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int InStock { get; set; }
    public int Reserved { get; set; }
    public int Available { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalValue { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a purchase order row in a table.
/// </summary>
public class PurchaseOrderTableRow
{
    public string Id { get; set; } = string.Empty;
    public string PoNumber { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public DateTime ExpectedDeliveryDate { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a stock adjustment row in a table.
/// </summary>
public class StockAdjustmentTableRow
{
    public string Id { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string AdjustmentType { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public string Reason { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Represents a stock transfer row in a table.
/// </summary>
public class StockTransferTableRow
{
    public string Id { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string SourceLocation { get; set; } = string.Empty;
    public string DestinationLocation { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public DateTime TransferDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a receipt row in a table.
/// </summary>
public class ReceiptTableRow
{
    public string Id { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Supplier { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Source { get; set; } = string.Empty;
}

/// <summary>
/// Represents a customer row in a table.
/// </summary>
public class CustomerTableRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal TotalPurchases { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a supplier row in a table.
/// </summary>
public class SupplierTableRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string PaymentTerms { get; set; } = string.Empty;
}

/// <summary>
/// Represents a product row in a table.
/// </summary>
public class ProductTableRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public decimal CostPrice { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents an employee row in a table.
/// </summary>
public class EmployeeTableRow
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }
    public string EmploymentType { get; set; } = string.Empty;
    public decimal SalaryAmount { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Represents a department row in a table.
/// </summary>
public class DepartmentTableRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string HeadName { get; set; } = string.Empty;
    public int EmployeeCount { get; set; }
    public decimal Budget { get; set; }
}

/// <summary>
/// Represents a category row in a table.
/// </summary>
public class CategoryTableRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ItemType { get; set; } = string.Empty;
}

/// <summary>
/// Represents a location row in a table.
/// </summary>
public class LocationTableRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ContactPerson { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public int CurrentUtilization { get; set; }
    public double UtilizationPercentage { get; set; }
}

/// <summary>
/// Represents an accountant row in a table.
/// </summary>
public class AccountantTableRow
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int AssignedTransactions { get; set; }
}

/// <summary>
/// Represents summary statistics for a report.
/// </summary>
public class ReportSummaryStatistics
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }

    // Revenue
    public decimal TotalRevenue { get; set; }
    public int RevenueTransactionCount { get; set; }
    public decimal AverageRevenueTransaction { get; set; }
    public decimal LargestRevenue { get; set; }
    public decimal SmallestRevenue { get; set; }

    // Expenses
    public decimal TotalExpenses { get; set; }
    public int ExpenseTransactionCount { get; set; }
    public decimal AverageExpenseTransaction { get; set; }
    public decimal LargestExpense { get; set; }
    public decimal SmallestExpense { get; set; }

    // Profit
    public decimal NetProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public decimal GrowthRate { get; set; }

    // Returns
    public int TotalReturns { get; set; }
    public decimal TotalReturnAmount { get; set; }
    public decimal ReturnRate { get; set; }

    // Losses
    public int TotalLosses { get; set; }
    public decimal TotalLossAmount { get; set; }

    // Shipping
    public decimal TotalShippingCosts { get; set; }
    public decimal AverageShippingCost { get; set; }
}

/// <summary>
/// Represents a product analysis row.
/// </summary>
public class ProductAnalysisRow
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantity { get; set; }
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
    public decimal AveragePrice { get; set; }
}

/// <summary>
/// Represents a customer analysis row.
/// </summary>
public class CustomerAnalysisRow
{
    public string CustomerId { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public DateTime FirstPurchase { get; set; }
    public DateTime LastPurchase { get; set; }
}

/// <summary>
/// Represents a supplier analysis row.
/// </summary>
public class SupplierAnalysisRow
{
    public string SupplierId { get; set; } = string.Empty;
    public string SupplierName { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public decimal TotalPurchases { get; set; }
    public int TransactionCount { get; set; }
    public decimal AverageTransaction { get; set; }
    public DateTime FirstPurchase { get; set; }
    public DateTime LastPurchase { get; set; }
}

/// <summary>
/// Represents an accountant analysis row.
/// </summary>
public class AccountantAnalysisRow
{
    public string AccountantId { get; set; } = string.Empty;
    public string AccountantName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalPurchases { get; set; }
    public decimal TotalVolume { get; set; }
    public int TransactionCount { get; set; }
}

#endregion
