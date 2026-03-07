namespace ArgoBooks.Core.Enums;

/// <summary>
/// Types of worksheets that can be imported from a spreadsheet.
/// </summary>
public enum SpreadsheetSheetType
{
    Customers,
    Suppliers,
    Products,
    Categories,
    Locations,
    Departments,
    Invoices,
    Expenses,
    Inventory,
    Payments,
    Revenue,
    RentalInventory,
    RentalRecords,
    Employees,
    RecurringInvoices,
    StockAdjustments,
    PurchaseOrders,
    PurchaseOrderLineItems,
    Returns,
    LostDamaged,
    Unknown
}

/// <summary>
/// Extension methods for SpreadsheetSheetType.
/// </summary>
public static class SpreadsheetSheetTypeExtensions
{
    /// <summary>
    /// Parses a worksheet name string to a SpreadsheetSheetType enum value.
    /// Handles common aliases (e.g., "Sales" → Revenue, "Purchases" → Expenses).
    /// </summary>
    public static SpreadsheetSheetType ParseSheetName(string sheetName)
    {
        return sheetName.Trim().ToLowerInvariant() switch
        {
            "customers" => SpreadsheetSheetType.Customers,
            "suppliers" => SpreadsheetSheetType.Suppliers,
            "products" => SpreadsheetSheetType.Products,
            "categories" => SpreadsheetSheetType.Categories,
            "locations" => SpreadsheetSheetType.Locations,
            "departments" => SpreadsheetSheetType.Departments,
            "invoices" => SpreadsheetSheetType.Invoices,
            "expenses" or "purchases" => SpreadsheetSheetType.Expenses,
            "inventory" => SpreadsheetSheetType.Inventory,
            "payments" => SpreadsheetSheetType.Payments,
            "revenue" or "sales" => SpreadsheetSheetType.Revenue,
            "rental inventory" => SpreadsheetSheetType.RentalInventory,
            "rental records" => SpreadsheetSheetType.RentalRecords,
            "employees" => SpreadsheetSheetType.Employees,
            "recurring invoices" => SpreadsheetSheetType.RecurringInvoices,
            "stock adjustments" => SpreadsheetSheetType.StockAdjustments,
            "purchase orders" => SpreadsheetSheetType.PurchaseOrders,
            "purchase order line items" => SpreadsheetSheetType.PurchaseOrderLineItems,
            "returns" => SpreadsheetSheetType.Returns,
            "lost damaged" or "lost / damaged" or "lost/damaged" => SpreadsheetSheetType.LostDamaged,
            _ => SpreadsheetSheetType.Unknown
        };
    }
}
