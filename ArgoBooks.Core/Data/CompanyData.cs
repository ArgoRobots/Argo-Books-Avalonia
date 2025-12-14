using System.Text.Json.Serialization;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Reports;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Data;

/// <summary>
/// Main container for all company data.
/// This class holds all data collections that are stored in the .argo file.
/// </summary>
public class CompanyData
{
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
    /// All categories (sales, purchase, rental).
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
    /// All sales/revenue transactions.
    /// </summary>
    [JsonPropertyName("sales")]
    public List<Sale> Sales { get; init; } = [];

    /// <summary>
    /// All purchases/expense transactions.
    /// </summary>
    [JsonPropertyName("purchases")]
    public List<Purchase> Purchases { get; init; } = [];

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
    /// Gets a customer by ID.
    /// </summary>
    public Customer? GetCustomer(string id) => Customers.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Gets a product by ID.
    /// </summary>
    public Product? GetProduct(string id) => Products.FirstOrDefault(p => p.Id == id);

    /// <summary>
    /// Gets a supplier by ID.
    /// </summary>
    public Supplier? GetSupplier(string id) => Suppliers.FirstOrDefault(s => s.Id == id);

    /// <summary>
    /// Gets an employee by ID.
    /// </summary>
    public Employee? GetEmployee(string id) => Employees.FirstOrDefault(e => e.Id == id);

    /// <summary>
    /// Gets a department by ID.
    /// </summary>
    public Department? GetDepartment(string id) => Departments.FirstOrDefault(d => d.Id == id);

    /// <summary>
    /// Gets a category by ID.
    /// </summary>
    public Category? GetCategory(string id) => Categories.FirstOrDefault(c => c.Id == id);

    /// <summary>
    /// Gets an accountant by ID.
    /// </summary>
    public Accountant? GetAccountant(string id) => Accountants.FirstOrDefault(a => a.Id == id);

    /// <summary>
    /// Gets a location by ID.
    /// </summary>
    public Location? GetLocation(string id) => Locations.FirstOrDefault(l => l.Id == id);

    /// <summary>
    /// Gets an invoice by ID.
    /// </summary>
    public Invoice? GetInvoice(string id) => Invoices.FirstOrDefault(i => i.Id == id);

    /// <summary>
    /// Gets inventory item by product and location.
    /// </summary>
    public InventoryItem? GetInventoryItem(string productId, string locationId) =>
        Inventory.FirstOrDefault(i => i.ProductId == productId && i.LocationId == locationId);

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

    [JsonPropertyName("sale")]
    public int Sale { get; set; }

    [JsonPropertyName("purchase")]
    public int Purchase { get; set; }

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
}
