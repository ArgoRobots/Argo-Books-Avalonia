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
        return sheetName switch
        {
            "Customers" => SpreadsheetSheetType.Customers,
            "Suppliers" => SpreadsheetSheetType.Suppliers,
            "Products" => SpreadsheetSheetType.Products,
            "Categories" => SpreadsheetSheetType.Categories,
            "Locations" => SpreadsheetSheetType.Locations,
            "Departments" => SpreadsheetSheetType.Departments,
            "Invoices" => SpreadsheetSheetType.Invoices,
            "Expenses" or "Purchases" => SpreadsheetSheetType.Expenses,
            "Inventory" => SpreadsheetSheetType.Inventory,
            "Payments" => SpreadsheetSheetType.Payments,
            "Revenue" or "Sales" => SpreadsheetSheetType.Revenue,
            "Rental Inventory" => SpreadsheetSheetType.RentalInventory,
            "Rental Records" => SpreadsheetSheetType.RentalRecords,
            "Employees" => SpreadsheetSheetType.Employees,
            "Recurring Invoices" => SpreadsheetSheetType.RecurringInvoices,
            "Stock Adjustments" => SpreadsheetSheetType.StockAdjustments,
            "Purchase Orders" => SpreadsheetSheetType.PurchaseOrders,
            "Purchase Order Line Items" => SpreadsheetSheetType.PurchaseOrderLineItems,
            "Returns" => SpreadsheetSheetType.Returns,
            "Lost Damaged" or "Lost / Damaged" or "Lost/Damaged" => SpreadsheetSheetType.LostDamaged,
            _ => SpreadsheetSheetType.Unknown
        };
    }
}
