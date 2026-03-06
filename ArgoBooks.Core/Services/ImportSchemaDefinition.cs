using ArgoBooks.Core.Enums;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Defines a single column in the import schema.
/// </summary>
/// <param name="Name">Column name as expected by the importer.</param>
/// <param name="Type">Data type description (string, decimal, int, datetime, enum:Value1,Value2).</param>
/// <param name="Description">Human-readable description for the LLM.</param>
/// <param name="Required">Whether this column is required for import.</param>
/// <param name="JsonName">JSON property name for Tier 2 LLM output (must match C# model's JsonPropertyName).</param>
public record SchemaColumn(string Name, string Type, string Description, bool Required = false, string? JsonName = null);

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
                new("ID", "string", "Unique identifier (e.g., CUS-001)", Required: true, JsonName: "id"),
                new("Name", "string", "Customer name", Required: true, JsonName: "name"),
                new("Company", "string", "Company name", JsonName: "companyName"),
                new("Email", "string", "Email address", JsonName: "email"),
                new("Phone", "string", "Phone number", JsonName: "phone"),
                new("Street", "string", "Street address", JsonName: "address.street"),
                new("City", "string", "City", JsonName: "address.city"),
                new("State", "string", "State or province", JsonName: "address.state"),
                new("Zip Code", "string", "ZIP or postal code", JsonName: "address.zipCode"),
                new("Country", "string", "Country", JsonName: "address.country"),
                new("Notes", "string", "Additional notes", JsonName: "notes"),
                new("Status", "enum:Active,Inactive", "Customer status", JsonName: "status"),
                new("Total Purchases", "decimal", "Total purchase amount", JsonName: "totalPurchases"),
            ],

            [SpreadsheetSheetType.Suppliers] =
            [
                new("ID", "string", "Unique identifier (e.g., SUP-001)", Required: true, JsonName: "id"),
                new("Name", "string", "Supplier name", Required: true, JsonName: "name"),
                new("Email", "string", "Email address", JsonName: "email"),
                new("Phone", "string", "Phone number", JsonName: "phone"),
                new("Website", "string", "Website URL", JsonName: "website"),
                new("Street", "string", "Street address", JsonName: "address.street"),
                new("City", "string", "City", JsonName: "address.city"),
                new("State", "string", "State or province", JsonName: "address.state"),
                new("Zip Code", "string", "ZIP or postal code", JsonName: "address.zipCode"),
                new("Country", "string", "Country", JsonName: "address.country"),
                new("Notes", "string", "Additional notes", JsonName: "notes"),
            ],

            [SpreadsheetSheetType.Products] =
            [
                new("ID", "string", "Unique identifier (e.g., PRD-001)", Required: true, JsonName: "id"),
                new("Name", "string", "Product name", Required: true, JsonName: "name"),
                new("Type", "enum:Revenue,Expenses,Rental", "Product category type", JsonName: "type"),
                new("Item Type", "enum:Product,Service", "Whether this is a product or service", JsonName: "itemType"),
                new("SKU", "string", "Stock keeping unit code", JsonName: "sku"),
                new("Description", "string", "Product description", JsonName: "description"),
                new("Category ID", "string", "Category identifier", JsonName: "categoryId"),
                new("Category Name", "string", "Category name (alternative to ID)"),
                new("Supplier ID", "string", "Supplier identifier", JsonName: "supplierId"),
                new("Supplier Name", "string", "Supplier name (alternative to ID)"),
                new("Reorder Point", "int", "Stock level that triggers reorder", JsonName: "reorderPoint"),
                new("Overstock Threshold", "int", "Stock level considered overstock", JsonName: "overstockThreshold"),
            ],

            [SpreadsheetSheetType.Categories] =
            [
                new("ID", "string", "Unique identifier (e.g., CAT-001)", Required: true, JsonName: "id"),
                new("Name", "string", "Category name", Required: true, JsonName: "name"),
                new("Type", "enum:Revenue,Expenses,Rental", "Category type", JsonName: "type"),
                new("Parent ID", "string", "Parent category ID for subcategories", JsonName: "parentId"),
                new("Description", "string", "Category description", JsonName: "description"),
                new("Item Type", "enum:Product,Service", "Default item type", JsonName: "itemType"),
                new("Icon", "string", "Emoji icon for the category", JsonName: "icon"),
            ],

            [SpreadsheetSheetType.Invoices] =
            [
                new("Invoice #", "string", "Invoice number (e.g., INV-2024-001)", Required: true, JsonName: "id"),
                new("Customer ID", "string", "Customer identifier", Required: true, JsonName: "customerId"),
                new("Issue Date", "datetime", "Date invoice was issued", JsonName: "issueDate"),
                new("Due Date", "datetime", "Payment due date", JsonName: "dueDate"),
                new("Subtotal", "decimal", "Amount before tax", JsonName: "subtotal"),
                new("Tax", "decimal", "Tax amount", JsonName: "taxAmount"),
                new("Total", "decimal", "Total amount due", JsonName: "total"),
                new("Paid", "decimal", "Amount already paid", JsonName: "amountPaid"),
                new("Balance", "decimal", "Remaining balance", JsonName: "balance"),
                new("Status", "enum:Draft,Sent,Paid,Overdue,Cancelled", "Invoice status", JsonName: "status"),
            ],

            [SpreadsheetSheetType.Expenses] =
            [
                new("ID", "string", "Unique identifier (e.g., PUR-001)", Required: true, JsonName: "id"),
                new("Date", "datetime", "Transaction date", Required: true, JsonName: "date"),
                new("Supplier ID", "string", "Supplier identifier", JsonName: "supplierId"),
                new("Product", "string", "Product or description of expense", JsonName: "description"),
                new("Description", "string", "Description (alternative to Product)", JsonName: "description"),
                new("Unit Price", "decimal", "Amount before tax", JsonName: "unitPrice"),
                new("Tax", "decimal", "Tax amount", JsonName: "taxAmount"),
                new("Total", "decimal", "Total amount including tax", JsonName: "total"),
                new("Reference", "string", "External reference number", JsonName: "referenceNumber"),
                new("Payment Method", "enum:Cash,CreditCard,DebitCard,BankTransfer,Check,PayPal,Other", "How payment was made", JsonName: "paymentMethod"),
                new("Shipping", "decimal", "Shipping cost", JsonName: "shippingCost"),
            ],

            [SpreadsheetSheetType.Revenue] =
            [
                new("ID", "string", "Unique identifier (e.g., SAL-001)", Required: true, JsonName: "id"),
                new("Date", "datetime", "Transaction date", Required: true, JsonName: "date"),
                new("Customer ID", "string", "Customer identifier", JsonName: "customerId"),
                new("Product", "string", "Product or description of sale", JsonName: "description"),
                new("Description", "string", "Description (alternative to Product)", JsonName: "description"),
                new("Unit Price", "decimal", "Amount before tax", JsonName: "unitPrice"),
                new("Tax", "decimal", "Tax amount", JsonName: "taxAmount"),
                new("Total", "decimal", "Total amount including tax", JsonName: "total"),
                new("Reference", "string", "External reference number", JsonName: "referenceNumber"),
                new("Payment Status", "string", "Payment status (e.g., Paid, Pending)", JsonName: "paymentStatus"),
                new("Shipping", "decimal", "Shipping cost", JsonName: "shippingCost"),
            ],

            [SpreadsheetSheetType.Inventory] =
            [
                new("ID", "string", "Unique identifier (e.g., INV-ITM-001)", Required: true, JsonName: "id"),
                new("Product ID", "string", "Associated product identifier", Required: true, JsonName: "productId"),
                new("Location ID", "string", "Storage location identifier", JsonName: "locationId"),
                new("In Stock", "int", "Current stock quantity", JsonName: "inStock"),
                new("Reserved", "int", "Reserved/allocated quantity", JsonName: "reserved"),
                new("Reorder Point", "int", "Stock level that triggers reorder", JsonName: "reorderPoint"),
                new("Unit Cost", "decimal", "Cost per unit", JsonName: "unitCost"),
                new("Last Updated", "datetime", "When stock was last counted", JsonName: "lastUpdated"),
            ],

            [SpreadsheetSheetType.Payments] =
            [
                new("ID", "string", "Unique identifier (e.g., PAY-001)", Required: true, JsonName: "id"),
                new("Invoice ID", "string", "Associated invoice identifier", JsonName: "invoiceId"),
                new("Customer ID", "string", "Customer identifier", JsonName: "customerId"),
                new("Date", "datetime", "Payment date", JsonName: "date"),
                new("Amount", "decimal", "Payment amount", JsonName: "amount"),
                new("Payment Method", "enum:Cash,CreditCard,DebitCard,BankTransfer,Check,PayPal,Other", "How payment was made", JsonName: "paymentMethod"),
                new("Reference", "string", "Payment reference number", JsonName: "referenceNumber"),
                new("Notes", "string", "Additional notes", JsonName: "notes"),
            ],

            [SpreadsheetSheetType.Locations] =
            [
                new("ID", "string", "Unique identifier (e.g., LOC-001)", Required: true, JsonName: "id"),
                new("Name", "string", "Location name", Required: true, JsonName: "name"),
                new("Contact Person", "string", "Contact person at location", JsonName: "contactPerson"),
                new("Phone", "string", "Phone number", JsonName: "phone"),
                new("Street", "string", "Street address", JsonName: "address.street"),
                new("City", "string", "City", JsonName: "address.city"),
                new("State", "string", "State or province", JsonName: "address.state"),
                new("Zip Code", "string", "ZIP or postal code", JsonName: "address.zipCode"),
                new("Country", "string", "Country", JsonName: "address.country"),
                new("Capacity", "int", "Storage capacity", JsonName: "capacity"),
                new("Utilization", "int", "Current utilization", JsonName: "currentUtilization"),
            ],

            [SpreadsheetSheetType.Departments] =
            [
                new("ID", "string", "Unique identifier (e.g., DEP-001)", Required: true, JsonName: "id"),
                new("Name", "string", "Department name", Required: true, JsonName: "name"),
                new("Description", "string", "Department description", JsonName: "description"),
            ],

            [SpreadsheetSheetType.Employees] =
            [
                new("ID", "string", "Unique identifier (e.g., EMP-001)", Required: true, JsonName: "id"),
                new("First Name", "string", "Employee first name", Required: true, JsonName: "firstName"),
                new("Last Name", "string", "Employee last name", Required: true, JsonName: "lastName"),
                new("Email", "string", "Email address", JsonName: "email"),
                new("Phone", "string", "Phone number", JsonName: "phone"),
                new("Date of Birth", "datetime", "Date of birth", JsonName: "dateOfBirth"),
                new("Department ID", "string", "Department identifier", JsonName: "departmentId"),
                new("Position", "string", "Job title or position", JsonName: "position"),
                new("Hire Date", "datetime", "Date of hire", JsonName: "hireDate"),
                new("Employment Type", "enum:Full-time,Part-time,Contract,Intern", "Type of employment", JsonName: "employmentType"),
                new("Salary Type", "enum:Annual,Hourly", "Salary calculation basis", JsonName: "salaryType"),
                new("Salary Amount", "decimal", "Salary amount", JsonName: "salaryAmount"),
                new("Pay Frequency", "enum:Weekly,Bi-weekly,Monthly", "How often employee is paid", JsonName: "payFrequency"),
                new("Status", "enum:Active,Inactive,OnLeave,Terminated", "Employment status", JsonName: "status"),
            ],

            [SpreadsheetSheetType.RentalInventory] =
            [
                new("ID", "string", "Unique identifier (e.g., RNT-ITM-001)", Required: true, JsonName: "id"),
                new("Name", "string", "Rental item name", Required: true, JsonName: "name"),
                new("Total Qty", "int", "Total quantity owned", JsonName: "totalQuantity"),
                new("Available", "int", "Currently available quantity", JsonName: "availableQuantity"),
                new("Rented", "int", "Currently rented quantity", JsonName: "rentedQuantity"),
                new("Daily Rate", "decimal", "Daily rental rate", JsonName: "dailyRate"),
                new("Weekly Rate", "decimal", "Weekly rental rate", JsonName: "weeklyRate"),
                new("Monthly Rate", "decimal", "Monthly rental rate", JsonName: "monthlyRate"),
                new("Deposit", "decimal", "Security deposit required", JsonName: "securityDeposit"),
                new("Product ID", "string", "Associated product identifier", JsonName: "productId"),
                new("Status", "enum:Active,Inactive", "Item status", JsonName: "status"),
            ],

            [SpreadsheetSheetType.RentalRecords] =
            [
                new("ID", "string", "Unique identifier (e.g., RNT-001)", Required: true, JsonName: "id"),
                new("Customer ID", "string", "Customer identifier", Required: true, JsonName: "customerId"),
                new("Rental Item ID", "string", "Rental inventory item ID", JsonName: "rentalItemId"),
                new("Start Date", "datetime", "Rental start date", JsonName: "startDate"),
                new("Due Date", "datetime", "Expected return date", JsonName: "dueDate"),
                new("Return Date", "datetime", "Actual return date (if returned)", JsonName: "returnDate"),
                new("Quantity", "int", "Quantity rented", JsonName: "quantity"),
                new("Rate Type", "enum:Daily,Weekly,Monthly", "Rental rate type", JsonName: "rateType"),
                new("Rate Amount", "decimal", "Rate amount per period", JsonName: "rateAmount"),
                new("Security Deposit", "decimal", "Security deposit amount", JsonName: "securityDeposit"),
                new("Total Cost", "decimal", "Total rental cost", JsonName: "totalCost"),
                new("Status", "enum:Active,Returned,Overdue,Cancelled", "Rental status", JsonName: "status"),
                new("Paid", "enum:Yes,No", "Whether the rental has been paid", JsonName: "paid"),
            ],

            [SpreadsheetSheetType.RecurringInvoices] =
            [
                new("ID", "string", "Unique identifier (e.g., REC-INV-001)", Required: true, JsonName: "id"),
                new("Customer ID", "string", "Customer identifier", Required: true, JsonName: "customerId"),
                new("Amount", "decimal", "Invoice amount", JsonName: "amount"),
                new("Description", "string", "Invoice description", JsonName: "description"),
                new("Frequency", "enum:Weekly,BiWeekly,Monthly,Quarterly,Annually", "Billing frequency", JsonName: "frequency"),
                new("Next Date", "datetime", "Next invoice date", JsonName: "nextInvoiceDate"),
                new("Status", "string", "Status (Active, Paused, etc.)", JsonName: "status"),
            ],

            [SpreadsheetSheetType.StockAdjustments] =
            [
                new("ID", "string", "Unique identifier (e.g., ADJ-001)", Required: true, JsonName: "id"),
                new("Inventory Item ID", "string", "Inventory item identifier", Required: true, JsonName: "inventoryItemId"),
                new("Type", "enum:Set,Add,Remove", "Adjustment type", JsonName: "adjustmentType"),
                new("Quantity", "int", "Adjustment quantity", JsonName: "quantity"),
                new("Previous Stock", "int", "Stock before adjustment", JsonName: "previousStock"),
                new("New Stock", "int", "Stock after adjustment", JsonName: "newStock"),
                new("Reason", "string", "Reason for adjustment", JsonName: "reason"),
                new("Timestamp", "datetime", "When adjustment was made", JsonName: "timestamp"),
            ],

            [SpreadsheetSheetType.PurchaseOrders] =
            [
                new("ID", "string", "Unique identifier (e.g., PO-001)", Required: true, JsonName: "id"),
                new("Supplier ID", "string", "Supplier identifier", Required: true, JsonName: "supplierId"),
                new("Order Date", "datetime", "Date order was placed", JsonName: "orderDate"),
                new("Expected Date", "datetime", "Expected delivery date", JsonName: "expectedDeliveryDate"),
                new("Total", "decimal", "Order total", JsonName: "total"),
                new("Status", "enum:Draft,Submitted,Approved,Received,Cancelled", "Order status", JsonName: "status"),
            ],

            [SpreadsheetSheetType.PurchaseOrderLineItems] =
            [
                new("PO ID", "string", "Purchase order identifier", Required: true),
                new("Product ID", "string", "Product identifier", Required: true, JsonName: "productId"),
                new("Quantity", "int", "Ordered quantity", JsonName: "quantity"),
                new("Unit Cost", "decimal", "Cost per unit", JsonName: "unitCost"),
                new("Quantity Received", "int", "Quantity received so far", JsonName: "quantityReceived"),
            ],

            [SpreadsheetSheetType.Returns] =
            [
                new("ID", "string", "Unique identifier (e.g., RET-001)", Required: true, JsonName: "id"),
                new("Original Transaction ID", "string", "ID of the original transaction", JsonName: "originalTransactionId"),
                new("Return Type", "enum:Customer,Supplier", "Type of return", JsonName: "returnType"),
                new("Customer ID", "string", "Customer identifier (for customer returns)", JsonName: "customerId"),
                new("Supplier ID", "string", "Supplier identifier (for supplier returns)", JsonName: "supplierId"),
                new("Return Date", "datetime", "Date of return", JsonName: "returnDate"),
                new("Product ID", "string", "Returned product ID"),
                new("Product", "string", "Returned product name (alternative to ID)"),
                new("Quantity", "int", "Quantity returned"),
                new("Reason", "string", "Reason for return"),
                new("Refund Amount", "decimal", "Refund amount", JsonName: "refundAmount"),
                new("Restocking Fee", "decimal", "Restocking fee charged", JsonName: "restockingFee"),
                new("Status", "enum:Pending,Approved,Rejected,Completed", "Return status", JsonName: "status"),
                new("Notes", "string", "Additional notes", JsonName: "notes"),
                new("Processed By", "string", "Employee who processed the return", JsonName: "processedBy"),
            ],

            [SpreadsheetSheetType.LostDamaged] =
            [
                new("ID", "string", "Unique identifier (e.g., LOST-001)", Required: true, JsonName: "id"),
                new("Product ID", "string", "Product identifier", JsonName: "productId"),
                new("Product", "string", "Product name (alternative to ID)"),
                new("Inventory Item ID", "string", "Inventory item identifier", JsonName: "inventoryItemId"),
                new("Quantity", "int", "Quantity lost or damaged", JsonName: "quantity"),
                new("Reason", "enum:Lost,Damaged,Stolen,Expired,Other", "Reason for loss", JsonName: "reason"),
                new("Date Discovered", "datetime", "Date loss was discovered", JsonName: "dateDiscovered"),
                new("Date", "datetime", "Date (alternative to Date Discovered)", JsonName: "dateDiscovered"),
                new("Value Lost", "decimal", "Monetary value of the loss", JsonName: "valueLost"),
                new("Notes", "string", "Additional notes", JsonName: "notes"),
                new("Insurance Claim", "enum:Yes,No", "Whether an insurance claim was filed", JsonName: "insuranceClaim"),
            ],
        };
    }
}
