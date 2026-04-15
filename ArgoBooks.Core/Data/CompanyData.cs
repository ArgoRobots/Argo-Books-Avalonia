using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Insights;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;

// FUTURE MULTI-ACCOUNTANT SUPPORT:
// When multi-accountant support is added, the EventLog will record which accountant
// performed each action via AuditEvent.AccountantId. The sync layer should merge
// event logs from multiple clients, using timestamps and accountant IDs to detect
// and resolve conflicts. Admin permissions can be enforced by checking the accountant's
// role before allowing undo of another accountant's actions.

namespace ArgoBooks.Core.Data;

/// <summary>
/// Main container for all company data.
/// This class holds all data collections that are stored in the .argo file.
/// </summary>
public class CompanyData
{
    /// <summary>
    /// Lock object for thread-safe access to collections.
    /// Callers performing multi-step operations should lock on this object.
    /// </summary>
    [JsonIgnore]
    public object SyncRoot { get; } = new();

    /// <summary>
    /// Company settings (stored as appSettings.json).
    /// </summary>
    [JsonPropertyName("settings")]
    public CompanySettings Settings { get; init; } = new();

    /// <summary>
    /// ID counters for generating sequential IDs.
    /// </summary>
    [JsonPropertyName("idCounters")]
    public IdCounters IdCounters { get; init; } = new();

    #region Entities

    /// <summary>
    /// All customers.
    /// </summary>
    [JsonPropertyName("customers")]
    public List<Customer> Customers { get; init; } = [];

    /// <summary>
    /// All products and services.
    /// </summary>
    [JsonPropertyName("products")]
    public List<Product> Products { get; init; } = [];

    /// <summary>
    /// All suppliers.
    /// </summary>
    [JsonPropertyName("suppliers")]
    public List<Supplier> Suppliers { get; init; } = [];

    /// <summary>
    /// All employees.
    /// </summary>
    [JsonPropertyName("employees")]
    public List<Employee> Employees { get; init; } = [];

    /// <summary>
    /// All departments.
    /// </summary>
    [JsonPropertyName("departments")]
    public List<Department> Departments { get; init; } = [];

    /// <summary>
    /// All categories (revenue, expense, rental).
    /// </summary>
    [JsonPropertyName("categories")]
    public List<Category> Categories { get; init; } = [];

    /// <summary>
    /// All accountants.
    /// </summary>
    [JsonPropertyName("accountants")]
    public List<Accountant> Accountants { get; init; } = [];

    /// <summary>
    /// All warehouse/storage locations.
    /// </summary>
    [JsonPropertyName("locations")]
    public List<Location> Locations { get; init; } = [];

    #endregion

    #region Transactions

    /// <summary>
    /// All revenue transactions.
    /// </summary>
    [JsonPropertyName("revenues")]
    public List<Revenue> Revenues { get; init; } = [];

    /// <summary>
    /// All expense transactions.
    /// </summary>
    [JsonPropertyName("expenses")]
    public List<Expense> Expenses { get; init; } = [];

    /// <summary>
    /// All invoices.
    /// </summary>
    [JsonPropertyName("invoices")]
    public List<Invoice> Invoices { get; init; } = [];

    /// <summary>
    /// All payments received.
    /// </summary>
    [JsonPropertyName("payments")]
    public List<Payment> Payments { get; init; } = [];

    /// <summary>
    /// All recurring invoice schedules.
    /// </summary>
    [JsonPropertyName("recurringInvoices")]
    public List<RecurringInvoice> RecurringInvoices { get; init; } = [];

    #endregion

    #region Inventory

    /// <summary>
    /// All inventory items (stock levels).
    /// </summary>
    [JsonPropertyName("inventory")]
    public List<InventoryItem> Inventory { get; init; } = [];

    /// <summary>
    /// All stock adjustments.
    /// </summary>
    [JsonPropertyName("stockAdjustments")]
    public List<StockAdjustment> StockAdjustments { get; init; } = [];

    /// <summary>
    /// All stock transfers.
    /// </summary>
    [JsonPropertyName("stockTransfers")]
    public List<StockTransfer> StockTransfers { get; init; } = [];

    /// <summary>
    /// All purchase orders.
    /// </summary>
    [JsonPropertyName("purchaseOrders")]
    public List<PurchaseOrder> PurchaseOrders { get; init; } = [];

    #endregion

    #region Rentals

    /// <summary>
    /// All rental inventory items.
    /// </summary>
    [JsonPropertyName("rentalInventory")]
    public List<RentalItem> RentalInventory { get; init; } = [];

    /// <summary>
    /// All rental records.
    /// </summary>
    [JsonPropertyName("rentals")]
    public List<RentalRecord> Rentals { get; init; } = [];

    #endregion

    #region Tracking

    /// <summary>
    /// All return records.
    /// </summary>
    [JsonPropertyName("returns")]
    public List<Return> Returns { get; init; } = [];

    /// <summary>
    /// All lost/damaged records.
    /// </summary>
    [JsonPropertyName("lostDamaged")]
    public List<LostDamaged> LostDamaged { get; init; } = [];

    /// <summary>
    /// All receipts.
    /// </summary>
    [JsonPropertyName("receipts")]
    public List<Receipt> Receipts { get; init; } = [];

    #endregion

    #region Reports

    /// <summary>
    /// All custom report templates.
    /// </summary>
    [JsonPropertyName("reportTemplates")]
    public List<ReportTemplate> ReportTemplates { get; init; } = [];

    #endregion

    #region Invoice Templates

    /// <summary>
    /// All custom invoice templates for email sending.
    /// </summary>
    [JsonPropertyName("invoiceTemplates")]
    public List<InvoiceTemplate> InvoiceTemplates { get; init; } = [];

    #endregion

    #region Pending Conversions

    /// <summary>
    /// Transactions saved offline that are awaiting USD conversion.
    /// Persisted in the .argo file as a secondary backup (primary backup is in app data directory).
    /// </summary>
    [JsonPropertyName("pendingConversions")]
    public List<PendingConversion> PendingConversions { get; init; } = [];

    #endregion

    #region Insights

    /// <summary>
    /// Historical forecast records for accuracy tracking.
    /// </summary>
    [JsonPropertyName("forecastRecords")]
    public List<ForecastAccuracyRecord> ForecastRecords { get; init; } = [];

    #endregion

    #region Version History

    /// <summary>
    /// Audit event log for version history tracking.
    /// Records all entity changes for audit trail and undo support.
    /// </summary>
    [JsonPropertyName("eventLog")]
    public List<AuditEvent> EventLog { get; init; } = [];

    #endregion

    #region State Tracking

    /// <summary>
    /// Gets or sets whether there are unsaved changes.
    /// </summary>
    [JsonIgnore]
    public bool ChangesMade
    {
        get => Settings.ChangesMade;
        set => Settings.ChangesMade = value;
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Gets the earliest transaction date across revenues, expenses, and payments only.
    /// Returns DateTime.Today if no transactions exist.
    /// Prefer <see cref="GetEarliestDate"/> when you need the earliest date across all data.
    /// </summary>
    public DateTime GetEarliestTransactionDate()
    {
        // Use year 1900 as minimum to avoid DateTime arithmetic overflow from default/unset dates
        var minValid = new DateTime(1900, 1, 1);
        var dates = new List<DateTime>();
        if (Revenues.Count > 0) dates.Add(Revenues.Where(r => r.Date >= minValid).Select(r => r.Date).DefaultIfEmpty(DateTime.Today).Min());
        if (Expenses.Count > 0) dates.Add(Expenses.Where(e => e.Date >= minValid).Select(e => e.Date).DefaultIfEmpty(DateTime.Today).Min());
        if (Payments.Count > 0) dates.Add(Payments.Where(p => p.Date >= minValid).Select(p => p.Date).DefaultIfEmpty(DateTime.Today).Min());

        return dates.Count > 0 ? dates.Min() : DateTime.Today;
    }

    /// <summary>
    /// Gets the earliest date across all dated collections (revenues, expenses, payments,
    /// invoices, stock adjustments, purchase orders, and rental records).
    /// Returns DateTime.Today if no dated records exist.
    /// </summary>
    public DateTime GetEarliestDate()
    {
        // Use year 1900 as minimum to avoid DateTime arithmetic overflow from default/unset dates
        var minValid = new DateTime(1900, 1, 1);
        DateTime MinDate(IEnumerable<DateTime> source) => source.Where(d => d >= minValid).DefaultIfEmpty(DateTime.Today).Min();

        var dates = new List<DateTime>();
        if (Revenues.Count > 0) dates.Add(MinDate(Revenues.Select(r => r.Date)));
        if (Expenses.Count > 0) dates.Add(MinDate(Expenses.Select(e => e.Date)));
        if (Payments.Count > 0) dates.Add(MinDate(Payments.Select(p => p.Date)));
        if (Invoices.Count > 0) dates.Add(MinDate(Invoices.Select(i => i.IssueDate)));
        if (StockAdjustments.Count > 0) dates.Add(MinDate(StockAdjustments.Select(s => s.Timestamp)));
        if (PurchaseOrders.Count > 0) dates.Add(MinDate(PurchaseOrders.Select(p => p.OrderDate)));
        if (Rentals.Count > 0) dates.Add(MinDate(Rentals.Select(r => r.StartDate)));

        return dates.Count > 0 ? dates.Min() : DateTime.Today;
    }

    #region Cached Lookups

    [JsonIgnore] private Dictionary<string, Customer>? _customerLookup;
    [JsonIgnore] private int _customerLookupCount = -1;
    [JsonIgnore] private Dictionary<string, Product>? _productLookup;
    [JsonIgnore] private int _productLookupCount = -1;
    [JsonIgnore] private Dictionary<string, Supplier>? _supplierLookup;
    [JsonIgnore] private int _supplierLookupCount = -1;
    [JsonIgnore] private Dictionary<string, Employee>? _employeeLookup;
    [JsonIgnore] private int _employeeLookupCount = -1;
    [JsonIgnore] private Dictionary<string, Department>? _departmentLookup;
    [JsonIgnore] private int _departmentLookupCount = -1;
    [JsonIgnore] private Dictionary<string, Category>? _categoryLookup;
    [JsonIgnore] private int _categoryLookupCount = -1;
    [JsonIgnore] private Dictionary<string, Accountant>? _accountantLookup;
    [JsonIgnore] private int _accountantLookupCount = -1;
    [JsonIgnore] private Dictionary<string, Location>? _locationLookup;
    [JsonIgnore] private int _locationLookupCount = -1;
    [JsonIgnore] private Dictionary<string, Invoice>? _invoiceLookup;
    [JsonIgnore] private int _invoiceLookupCount = -1;

    private static Dictionary<string, T> BuildLookup<T>(List<T> list, Func<T, string> keySelector)
    {
        var dict = new Dictionary<string, T>(list.Count, StringComparer.Ordinal);
        foreach (var item in list)
        {
            var key = keySelector(item);
            if (!string.IsNullOrEmpty(key))
                dict[key] = item;
        }
        return dict;
    }

    /// <summary>
    /// Invalidates all cached lookup dictionaries. Call after bulk modifications
    /// that add and remove items in the same operation (count stays the same).
    /// </summary>
    public void InvalidateLookupCaches()
    {
        _customerLookup = null;
        _productLookup = null;
        _supplierLookup = null;
        _employeeLookup = null;
        _departmentLookup = null;
        _categoryLookup = null;
        _accountantLookup = null;
        _locationLookup = null;
        _invoiceLookup = null;
    }

    #endregion

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    public Customer? GetCustomer(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_customerLookup == null || _customerLookupCount != Customers.Count)
        {
            _customerLookup = BuildLookup(Customers, c => c.Id);
            _customerLookupCount = Customers.Count;
        }
        return _customerLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    public Product? GetProduct(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_productLookup == null || _productLookupCount != Products.Count)
        {
            _productLookup = BuildLookup(Products, p => p.Id);
            _productLookupCount = Products.Count;
        }
        return _productLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets a supplier by ID.
    /// </summary>
    public Supplier? GetSupplier(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_supplierLookup == null || _supplierLookupCount != Suppliers.Count)
        {
            _supplierLookup = BuildLookup(Suppliers, s => s.Id);
            _supplierLookupCount = Suppliers.Count;
        }
        return _supplierLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets an employee by ID.
    /// </summary>
    public Employee? GetEmployee(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_employeeLookup == null || _employeeLookupCount != Employees.Count)
        {
            _employeeLookup = BuildLookup(Employees, e => e.Id);
            _employeeLookupCount = Employees.Count;
        }
        return _employeeLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets a department by ID.
    /// </summary>
    public Department? GetDepartment(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_departmentLookup == null || _departmentLookupCount != Departments.Count)
        {
            _departmentLookup = BuildLookup(Departments, d => d.Id);
            _departmentLookupCount = Departments.Count;
        }
        return _departmentLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    public Category? GetCategory(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_categoryLookup == null || _categoryLookupCount != Categories.Count)
        {
            _categoryLookup = BuildLookup(Categories, c => c.Id);
            _categoryLookupCount = Categories.Count;
        }
        return _categoryLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets an accountant by ID.
    /// </summary>
    public Accountant? GetAccountant(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_accountantLookup == null || _accountantLookupCount != Accountants.Count)
        {
            _accountantLookup = BuildLookup(Accountants, a => a.Id);
            _accountantLookupCount = Accountants.Count;
        }
        return _accountantLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets a location by ID.
    /// </summary>
    public Location? GetLocation(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_locationLookup == null || _locationLookupCount != Locations.Count)
        {
            _locationLookup = BuildLookup(Locations, l => l.Id);
            _locationLookupCount = Locations.Count;
        }
        return _locationLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets an invoice by ID.
    /// </summary>
    public Invoice? GetInvoice(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (_invoiceLookup == null || _invoiceLookupCount != Invoices.Count)
        {
            _invoiceLookup = BuildLookup(Invoices, i => i.Id);
            _invoiceLookupCount = Invoices.Count;
        }
        return _invoiceLookup.GetValueOrDefault(id);
    }

    /// <summary>
    /// Gets inventory item by product and location.
    /// </summary>
    public InventoryItem? GetInventoryItem(string productId, string locationId) =>
        Inventory.FirstOrDefault(i => i.ProductId == productId && i.LocationId == locationId);

    /// <summary>
    /// Gets an invoice template by ID.
    /// </summary>
    public InvoiceTemplate? GetInvoiceTemplate(string id) =>
        InvoiceTemplates.FirstOrDefault(t => t.Id == id);

    /// <summary>
    /// Gets the default invoice template, or null if none is set.
    /// </summary>
    public InvoiceTemplate? GetDefaultInvoiceTemplate() =>
        InvoiceTemplates.FirstOrDefault(t => t.IsDefault) ?? InvoiceTemplates.FirstOrDefault();

    /// <summary>
    /// Marks the data as modified.
    /// </summary>
    public void MarkAsModified()
    {
        Settings.ChangesMade = true;
    }

    /// <summary>
    /// Marks the data as saved (not modified).
    /// </summary>
    public void MarkAsSaved()
    {
        Settings.ChangesMade = false;
    }

    #endregion
}

/// <summary>
/// Counters for generating sequential IDs.
/// </summary>
public class IdCounters
{
    [JsonPropertyName("customer")]
    public int Customer { get; set; }

    [JsonPropertyName("product")]
    public int Product { get; set; }

    [JsonPropertyName("supplier")]
    public int Supplier { get; set; }

    [JsonPropertyName("employee")]
    public int Employee { get; set; }

    [JsonPropertyName("department")]
    public int Department { get; set; }

    [JsonPropertyName("category")]
    public int Category { get; set; }

    [JsonPropertyName("accountant")]
    public int Accountant { get; set; }

    [JsonPropertyName("location")]
    public int Location { get; set; }

    [JsonPropertyName("revenue")]
    public int Revenue { get; set; }

    [JsonPropertyName("expense")]
    public int Expense { get; set; }

    [JsonPropertyName("invoice")]
    public int Invoice { get; set; }

    [JsonPropertyName("payment")]
    public int Payment { get; set; }

    [JsonPropertyName("recurringInvoice")]
    public int RecurringInvoice { get; set; }

    [JsonPropertyName("inventoryItem")]
    public int InventoryItem { get; set; }

    [JsonPropertyName("stockAdjustment")]
    public int StockAdjustment { get; set; }

    [JsonPropertyName("stockTransfer")]
    public int StockTransfer { get; set; }

    [JsonPropertyName("purchaseOrder")]
    public int PurchaseOrder { get; set; }

    [JsonPropertyName("rentalItem")]
    public int RentalItem { get; set; }

    [JsonPropertyName("rental")]
    public int Rental { get; set; }

    [JsonPropertyName("return")]
    public int Return { get; set; }

    [JsonPropertyName("lostDamaged")]
    public int LostDamaged { get; set; }

    [JsonPropertyName("receipt")]
    public int Receipt { get; set; }

    [JsonPropertyName("reportTemplate")]
    public int ReportTemplate { get; set; }

    [JsonPropertyName("invoiceTemplate")]
    public int InvoiceTemplate { get; set; }
}
