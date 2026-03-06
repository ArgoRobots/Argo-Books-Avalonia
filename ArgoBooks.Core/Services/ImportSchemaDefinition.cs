using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Defines a single column in the import schema.
/// </summary>
/// <param name="Name">Column name as expected by the importer.</param>
/// <param name="Type">Data type description (string, decimal, int, datetime, enum:Value1,Value2).</param>
/// <param name="Description">Human-readable description for the LLM.</param>
/// <param name="Required">Whether this column is required for import.</param>
public record SchemaColumn(string Name, string Type, string Description, bool Required = false);

/// <summary>
/// Defines the expected column schema for each entity type that can be imported.
/// Used to build LLM prompts so the AI knows what columns to map to.
/// Derived from the GetString/GetDecimal/etc. calls in SpreadsheetImportService.Import* methods.
/// </summary>
public static class ImportSchemaDefinition
{
    private static Dictionary<SpreadsheetSheetType, List<SchemaColumn>>? _schema;

    /// <summary>
    /// Gets the complete import schema for all entity types.
    /// </summary>
    public static Dictionary<SpreadsheetSheetType, List<SchemaColumn>> GetSchema()
    {
        return _schema ??= BuildSchema();
    }

    /// <summary>
    /// Gets the schema for a specific entity type.
    /// </summary>
    public static List<SchemaColumn>? GetSchemaForType(SpreadsheetSheetType type)
    {
        return GetSchema().TryGetValue(type, out var columns) ? columns : null;
    }

    /// <summary>
    /// Formats the schema as a readable string for LLM prompts.
    /// </summary>
    public static string FormatSchemaForPrompt()
    {
        var sb = new System.Text.StringBuilder();
        foreach (var (type, columns) in GetSchema())
        {
            sb.AppendLine($"### {type}");
            sb.AppendLine("| Column | Type | Required | Description |");
            sb.AppendLine("|--------|------|----------|-------------|");
            foreach (var col in columns)
            {
                var req = col.Required ? "Yes" : "No";
                sb.AppendLine($"| {col.Name} | {col.Type} | {req} | {col.Description} |");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static Dictionary<SpreadsheetSheetType, List<SchemaColumn>> BuildSchema()
    {
        return new Dictionary<SpreadsheetSheetType, List<SchemaColumn>>
        {
            [SpreadsheetSheetType.Customers] =
            [
                new("ID", "string", "Unique identifier (e.g., CUS-001)", Required: true),
                new("Name", "string", "Customer name", Required: true),
                new("Company", "string", "Company name"),
                new("Email", "string", "Email address"),
                new("Phone", "string", "Phone number"),
                new("Street", "string", "Street address"),
                new("City", "string", "City"),
                new("State", "string", "State or province"),
                new("Zip Code", "string", "ZIP or postal code"),
                new("Country", "string", "Country"),
                new("Notes", "string", "Additional notes"),
                new("Status", "enum:Active,Inactive", "Customer status"),
                new("Total Purchases", "decimal", "Total purchase amount"),
            ],

            [SpreadsheetSheetType.Suppliers] =
            [
                new("ID", "string", "Unique identifier (e.g., SUP-001)", Required: true),
                new("Name", "string", "Supplier name", Required: true),
                new("Email", "string", "Email address"),
                new("Phone", "string", "Phone number"),
                new("Website", "string", "Website URL"),
                new("Street", "string", "Street address"),
                new("City", "string", "City"),
                new("State", "string", "State or province"),
                new("Zip Code", "string", "ZIP or postal code"),
                new("Country", "string", "Country"),
                new("Notes", "string", "Additional notes"),
            ],

            [SpreadsheetSheetType.Products] =
            [
                new("ID", "string", "Unique identifier (e.g., PRD-001)", Required: true),
                new("Name", "string", "Product name", Required: true),
                new("Type", "enum:Revenue,Expenses,Rental", "Product category type"),
                new("Item Type", "enum:Product,Service", "Whether this is a product or service"),
                new("SKU", "string", "Stock keeping unit code"),
                new("Description", "string", "Product description"),
                new("Category ID", "string", "Category identifier"),
                new("Category Name", "string", "Category name (alternative to ID)"),
                new("Supplier ID", "string", "Supplier identifier"),
                new("Supplier Name", "string", "Supplier name (alternative to ID)"),
                new("Reorder Point", "int", "Stock level that triggers reorder"),
                new("Overstock Threshold", "int", "Stock level considered overstock"),
            ],

            [SpreadsheetSheetType.Categories] =
            [
                new("ID", "string", "Unique identifier (e.g., CAT-001)", Required: true),
                new("Name", "string", "Category name", Required: true),
                new("Type", "enum:Revenue,Expenses,Rental", "Category type"),
                new("Parent ID", "string", "Parent category ID for subcategories"),
                new("Description", "string", "Category description"),
                new("Item Type", "enum:Product,Service", "Default item type"),
                new("Icon", "string", "Emoji icon for the category"),
            ],

            [SpreadsheetSheetType.Invoices] =
            [
                new("Invoice #", "string", "Invoice number (e.g., INV-2024-001)", Required: true),
                new("Customer ID", "string", "Customer identifier", Required: true),
                new("Issue Date", "datetime", "Date invoice was issued"),
                new("Due Date", "datetime", "Payment due date"),
                new("Subtotal", "decimal", "Amount before tax"),
                new("Tax", "decimal", "Tax amount"),
                new("Total", "decimal", "Total amount due"),
                new("Paid", "decimal", "Amount already paid"),
                new("Balance", "decimal", "Remaining balance"),
                new("Status", "enum:Draft,Sent,Paid,Overdue,Cancelled", "Invoice status"),
            ],

            [SpreadsheetSheetType.Expenses] =
            [
                new("ID", "string", "Unique identifier (e.g., PUR-001)", Required: true),
                new("Date", "datetime", "Transaction date", Required: true),
                new("Supplier ID", "string", "Supplier identifier"),
                new("Product", "string", "Product or description of expense"),
                new("Description", "string", "Description (alternative to Product)"),
                new("Unit Price", "decimal", "Amount before tax"),
                new("Tax", "decimal", "Tax amount"),
                new("Total", "decimal", "Total amount including tax"),
                new("Reference", "string", "External reference number"),
                new("Payment Method", "enum:Cash,CreditCard,DebitCard,BankTransfer,Check,PayPal,Other", "How payment was made"),
                new("Shipping", "decimal", "Shipping cost"),
            ],

            [SpreadsheetSheetType.Revenue] =
            [
                new("ID", "string", "Unique identifier (e.g., SAL-001)", Required: true),
                new("Date", "datetime", "Transaction date", Required: true),
                new("Customer ID", "string", "Customer identifier"),
                new("Product", "string", "Product or description of sale"),
                new("Description", "string", "Description (alternative to Product)"),
                new("Unit Price", "decimal", "Amount before tax"),
                new("Tax", "decimal", "Tax amount"),
                new("Total", "decimal", "Total amount including tax"),
                new("Reference", "string", "External reference number"),
                new("Payment Status", "string", "Payment status (e.g., Paid, Pending)"),
                new("Shipping", "decimal", "Shipping cost"),
            ],

            [SpreadsheetSheetType.Inventory] =
            [
                new("ID", "string", "Unique identifier (e.g., INV-ITM-001)", Required: true),
                new("Product ID", "string", "Associated product identifier", Required: true),
                new("Location ID", "string", "Storage location identifier"),
                new("In Stock", "int", "Current stock quantity"),
                new("Reserved", "int", "Reserved/allocated quantity"),
                new("Reorder Point", "int", "Stock level that triggers reorder"),
                new("Unit Cost", "decimal", "Cost per unit"),
                new("Last Updated", "datetime", "When stock was last counted"),
            ],

            [SpreadsheetSheetType.Payments] =
            [
                new("ID", "string", "Unique identifier (e.g., PAY-001)", Required: true),
                new("Invoice ID", "string", "Associated invoice identifier"),
                new("Customer ID", "string", "Customer identifier"),
                new("Date", "datetime", "Payment date"),
                new("Amount", "decimal", "Payment amount"),
                new("Payment Method", "enum:Cash,CreditCard,DebitCard,BankTransfer,Check,PayPal,Other", "How payment was made"),
                new("Reference", "string", "Payment reference number"),
                new("Notes", "string", "Additional notes"),
            ],

            [SpreadsheetSheetType.Locations] =
            [
                new("ID", "string", "Unique identifier (e.g., LOC-001)", Required: true),
                new("Name", "string", "Location name", Required: true),
                new("Contact Person", "string", "Contact person at location"),
                new("Phone", "string", "Phone number"),
                new("Street", "string", "Street address"),
                new("City", "string", "City"),
                new("State", "string", "State or province"),
                new("Zip Code", "string", "ZIP or postal code"),
                new("Country", "string", "Country"),
                new("Capacity", "int", "Storage capacity"),
                new("Utilization", "int", "Current utilization"),
            ],

            [SpreadsheetSheetType.Departments] =
            [
                new("ID", "string", "Unique identifier (e.g., DEP-001)", Required: true),
                new("Name", "string", "Department name", Required: true),
                new("Description", "string", "Department description"),
            ],

            [SpreadsheetSheetType.Employees] =
            [
                new("ID", "string", "Unique identifier (e.g., EMP-001)", Required: true),
                new("First Name", "string", "Employee first name", Required: true),
                new("Last Name", "string", "Employee last name", Required: true),
                new("Email", "string", "Email address"),
                new("Phone", "string", "Phone number"),
                new("Date of Birth", "datetime", "Date of birth"),
                new("Department ID", "string", "Department identifier"),
                new("Position", "string", "Job title or position"),
                new("Hire Date", "datetime", "Date of hire"),
                new("Employment Type", "enum:Full-time,Part-time,Contract,Intern", "Type of employment"),
                new("Salary Type", "enum:Annual,Hourly", "Salary calculation basis"),
                new("Salary Amount", "decimal", "Salary amount"),
                new("Pay Frequency", "enum:Weekly,Bi-weekly,Monthly", "How often employee is paid"),
                new("Status", "enum:Active,Inactive,OnLeave,Terminated", "Employment status"),
            ],

            [SpreadsheetSheetType.RentalInventory] =
            [
                new("ID", "string", "Unique identifier (e.g., RNT-ITM-001)", Required: true),
                new("Name", "string", "Rental item name", Required: true),
                new("Total Qty", "int", "Total quantity owned"),
                new("Available", "int", "Currently available quantity"),
                new("Rented", "int", "Currently rented quantity"),
                new("Daily Rate", "decimal", "Daily rental rate"),
                new("Weekly Rate", "decimal", "Weekly rental rate"),
                new("Monthly Rate", "decimal", "Monthly rental rate"),
                new("Deposit", "decimal", "Security deposit required"),
                new("Product ID", "string", "Associated product identifier"),
                new("Status", "enum:Active,Inactive", "Item status"),
            ],

            [SpreadsheetSheetType.RentalRecords] =
            [
                new("ID", "string", "Unique identifier (e.g., RNT-001)", Required: true),
                new("Customer ID", "string", "Customer identifier", Required: true),
                new("Rental Item ID", "string", "Rental inventory item ID"),
                new("Start Date", "datetime", "Rental start date"),
                new("Due Date", "datetime", "Expected return date"),
                new("Return Date", "datetime", "Actual return date (if returned)"),
                new("Quantity", "int", "Quantity rented"),
                new("Rate Type", "enum:Daily,Weekly,Monthly", "Rental rate type"),
                new("Rate Amount", "decimal", "Rate amount per period"),
                new("Security Deposit", "decimal", "Security deposit amount"),
                new("Total Cost", "decimal", "Total rental cost"),
                new("Status", "enum:Active,Returned,Overdue,Cancelled", "Rental status"),
                new("Paid", "enum:Yes,No", "Whether the rental has been paid"),
            ],

            [SpreadsheetSheetType.RecurringInvoices] =
            [
                new("ID", "string", "Unique identifier (e.g., REC-INV-001)", Required: true),
                new("Customer ID", "string", "Customer identifier", Required: true),
                new("Amount", "decimal", "Invoice amount"),
                new("Description", "string", "Invoice description"),
                new("Frequency", "enum:Weekly,BiWeekly,Monthly,Quarterly,Annually", "Billing frequency"),
                new("Next Date", "datetime", "Next invoice date"),
                new("Status", "string", "Status (Active, Paused, etc.)"),
            ],

            [SpreadsheetSheetType.StockAdjustments] =
            [
                new("ID", "string", "Unique identifier (e.g., ADJ-001)", Required: true),
                new("Inventory Item ID", "string", "Inventory item identifier", Required: true),
                new("Type", "enum:Set,Add,Remove", "Adjustment type"),
                new("Quantity", "int", "Adjustment quantity"),
                new("Previous Stock", "int", "Stock before adjustment"),
                new("New Stock", "int", "Stock after adjustment"),
                new("Reason", "string", "Reason for adjustment"),
                new("Timestamp", "datetime", "When adjustment was made"),
            ],

            [SpreadsheetSheetType.PurchaseOrders] =
            [
                new("ID", "string", "Unique identifier (e.g., PO-001)", Required: true),
                new("Supplier ID", "string", "Supplier identifier", Required: true),
                new("Order Date", "datetime", "Date order was placed"),
                new("Expected Date", "datetime", "Expected delivery date"),
                new("Total", "decimal", "Order total"),
                new("Status", "enum:Draft,Submitted,Approved,Received,Cancelled", "Order status"),
            ],

            [SpreadsheetSheetType.PurchaseOrderLineItems] =
            [
                new("PO ID", "string", "Purchase order identifier", Required: true),
                new("Product ID", "string", "Product identifier", Required: true),
                new("Quantity", "int", "Ordered quantity"),
                new("Unit Cost", "decimal", "Cost per unit"),
                new("Quantity Received", "int", "Quantity received so far"),
            ],

            [SpreadsheetSheetType.Returns] =
            [
                new("ID", "string", "Unique identifier (e.g., RET-001)", Required: true),
                new("Original Transaction ID", "string", "ID of the original transaction"),
                new("Return Type", "enum:Customer,Supplier", "Type of return"),
                new("Customer ID", "string", "Customer identifier (for customer returns)"),
                new("Supplier ID", "string", "Supplier identifier (for supplier returns)"),
                new("Return Date", "datetime", "Date of return"),
                new("Product ID", "string", "Returned product ID"),
                new("Product", "string", "Returned product name (alternative to ID)"),
                new("Quantity", "int", "Quantity returned"),
                new("Reason", "string", "Reason for return"),
                new("Refund Amount", "decimal", "Refund amount"),
                new("Restocking Fee", "decimal", "Restocking fee charged"),
                new("Status", "enum:Pending,Approved,Rejected,Completed", "Return status"),
                new("Notes", "string", "Additional notes"),
                new("Processed By", "string", "Employee who processed the return"),
            ],

            [SpreadsheetSheetType.LostDamaged] =
            [
                new("ID", "string", "Unique identifier (e.g., LOST-001)", Required: true),
                new("Product ID", "string", "Product identifier"),
                new("Product", "string", "Product name (alternative to ID)"),
                new("Inventory Item ID", "string", "Inventory item identifier"),
                new("Quantity", "int", "Quantity lost or damaged"),
                new("Reason", "enum:Lost,Damaged,Stolen,Expired,Other", "Reason for loss"),
                new("Date Discovered", "datetime", "Date loss was discovered"),
                new("Date", "datetime", "Date (alternative to Date Discovered)"),
                new("Value Lost", "decimal", "Monetary value of the loss"),
                new("Notes", "string", "Additional notes"),
                new("Insurance Claim", "enum:Yes,No", "Whether an insurance claim was filed"),
            ],
        };
    }
}
