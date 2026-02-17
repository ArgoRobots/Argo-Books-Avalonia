using System.Text.Json;
using ArgoBooks.Core.Data;

namespace ArgoBooks.Services;

/// <summary>
/// Helper for looking up, adding, and removing entities in CompanyData by entity type string.
/// Used by EventLogService to reconstruct undoable actions from persisted audit events.
/// </summary>
internal static class EntityCollectionHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Finds an entity by type and ID in CompanyData and returns its JSON snapshot.
    /// Returns null if not found.
    /// </summary>
    public static string? FindAndSerializeEntity(CompanyData data, string entityType, string entityId)
    {
        if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
            return null;

        object? entity = FindEntity(data, entityType, entityId);
        if (entity == null)
            return null;

        return JsonSerializer.Serialize(entity, entity.GetType(), JsonOptions);
    }

    /// <summary>
    /// Finds an entity by type and ID in CompanyData.
    /// </summary>
    public static object? FindEntity(CompanyData data, string entityType, string entityId)
    {
        return NormalizeEntityType(entityType) switch
        {
            "customer" => data.Customers.FirstOrDefault(e => e.Id == entityId),
            "product" => data.Products.FirstOrDefault(e => e.Id == entityId),
            "supplier" => data.Suppliers.FirstOrDefault(e => e.Id == entityId),
            "employee" => data.Employees.FirstOrDefault(e => e.Id == entityId),
            "department" => data.Departments.FirstOrDefault(e => e.Id == entityId),
            "category" => data.Categories.FirstOrDefault(e => e.Id == entityId),
            "accountant" => data.Accountants.FirstOrDefault(e => e.Id == entityId),
            "location" => data.Locations.FirstOrDefault(e => e.Id == entityId),
            "revenue" => data.Revenues.FirstOrDefault(e => e.Id == entityId),
            "expense" => data.Expenses.FirstOrDefault(e => e.Id == entityId),
            "invoice" => data.Invoices.FirstOrDefault(e => e.Id == entityId),
            "payment" => data.Payments.FirstOrDefault(e => e.Id == entityId),
            "recurringinvoice" => data.RecurringInvoices.FirstOrDefault(e => e.Id == entityId),
            "inventory" or "inventoryitem" => data.Inventory.FirstOrDefault(e => e.Id == entityId),
            "stockadjustment" => data.StockAdjustments.FirstOrDefault(e => e.Id == entityId),
            "stocktransfer" => data.StockTransfers.FirstOrDefault(e => e.Id == entityId),
            "purchaseorder" => data.PurchaseOrders.FirstOrDefault(e => e.Id == entityId),
            "rentalitem" => data.RentalInventory.FirstOrDefault(e => e.Id == entityId),
            "rental" or "rentalrecord" => data.Rentals.FirstOrDefault(e => e.Id == entityId),
            "return" => data.Returns.FirstOrDefault(e => e.Id == entityId),
            "lostdamaged" => data.LostDamaged.FirstOrDefault(e => e.Id == entityId),
            "receipt" => data.Receipts.FirstOrDefault(e => e.Id == entityId),
            "reporttemplate" => data.ReportTemplates.FirstOrDefault(e => e.Id == entityId),
            "invoicetemplate" => data.InvoiceTemplates.FirstOrDefault(e => e.Id == entityId),
            _ => null
        };
    }

    /// <summary>
    /// Finds an entity by type and name (display name) in CompanyData and returns its ID.
    /// Used when EntityId is not available but EntityName is.
    /// Returns null if not found.
    /// </summary>
    public static string? FindEntityIdByName(CompanyData data, string entityType, string entityName)
    {
        if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityName))
            return null;

        return NormalizeEntityType(entityType) switch
        {
            "customer" => data.Customers.FirstOrDefault(e => e.Name == entityName)?.Id,
            "product" => data.Products.FirstOrDefault(e => e.Name == entityName)?.Id,
            "supplier" => data.Suppliers.FirstOrDefault(e => e.Name == entityName)?.Id,
            "employee" => data.Employees.FirstOrDefault(e =>
                $"{e.FirstName} {e.LastName}" == entityName || e.Id == entityName)?.Id,
            "department" => data.Departments.FirstOrDefault(e => e.Name == entityName)?.Id,
            "category" => data.Categories.FirstOrDefault(e => e.Name == entityName)?.Id,
            "accountant" => data.Accountants.FirstOrDefault(e => e.Name == entityName)?.Id,
            "location" => data.Locations.FirstOrDefault(e => e.Name == entityName)?.Id,
            // Transactions: entityName is typically the ID itself (e.g., "PUR-2026-00042")
            "revenue" => data.Revenues.FirstOrDefault(e => e.Id == entityName)?.Id,
            "expense" => data.Expenses.FirstOrDefault(e => e.Id == entityName)?.Id,
            "invoice" => data.Invoices.FirstOrDefault(e => e.Id == entityName || e.InvoiceNumber == entityName)?.Id,
            "payment" => data.Payments.FirstOrDefault(e => e.Id == entityName)?.Id,
            "recurringinvoice" => data.RecurringInvoices.FirstOrDefault(e => e.Id == entityName)?.Id,
            "inventory" or "inventoryitem" => data.Inventory.FirstOrDefault(e => e.Id == entityName)?.Id,
            "stockadjustment" => data.StockAdjustments.FirstOrDefault(e => e.Id == entityName)?.Id,
            "stocktransfer" => data.StockTransfers.FirstOrDefault(e => e.Id == entityName)?.Id,
            "purchaseorder" => data.PurchaseOrders.FirstOrDefault(e => e.Id == entityName || e.PoNumber == entityName)?.Id,
            "rentalitem" => data.RentalInventory.FirstOrDefault(e => e.Name == entityName)?.Id,
            "rental" or "rentalrecord" => data.Rentals.FirstOrDefault(e => e.Id == entityName)?.Id,
            "return" => data.Returns.FirstOrDefault(e => e.Id == entityName)?.Id,
            "lostdamaged" => data.LostDamaged.FirstOrDefault(e => e.Id == entityName)?.Id,
            "receipt" => data.Receipts.FirstOrDefault(e => e.Id == entityName)?.Id,
            "reporttemplate" => data.ReportTemplates.FirstOrDefault(e => e.Name == entityName)?.Id,
            "invoicetemplate" => data.InvoiceTemplates.FirstOrDefault(e => e.Name == entityName)?.Id,
            _ => null
        };
    }

    /// <summary>
    /// Removes an entity by type and ID from CompanyData.
    /// Returns true if the entity was found and removed.
    /// </summary>
    public static bool RemoveEntity(CompanyData data, string entityType, string entityId)
    {
        if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(entityId))
            return false;

        return NormalizeEntityType(entityType) switch
        {
            "customer" => RemoveById(data.Customers, entityId),
            "product" => RemoveById(data.Products, entityId),
            "supplier" => RemoveById(data.Suppliers, entityId),
            "employee" => RemoveById(data.Employees, entityId),
            "department" => RemoveById(data.Departments, entityId),
            "category" => RemoveById(data.Categories, entityId),
            "accountant" => RemoveById(data.Accountants, entityId),
            "location" => RemoveById(data.Locations, entityId),
            "revenue" => RemoveById(data.Revenues, entityId),
            "expense" => RemoveById(data.Expenses, entityId),
            "invoice" => RemoveById(data.Invoices, entityId),
            "payment" => RemoveById(data.Payments, entityId),
            "recurringinvoice" => RemoveById(data.RecurringInvoices, entityId),
            "inventory" or "inventoryitem" => RemoveById(data.Inventory, entityId),
            "stockadjustment" => RemoveById(data.StockAdjustments, entityId),
            "stocktransfer" => RemoveById(data.StockTransfers, entityId),
            "purchaseorder" => RemoveById(data.PurchaseOrders, entityId),
            "rentalitem" => RemoveById(data.RentalInventory, entityId),
            "rental" or "rentalrecord" => RemoveById(data.Rentals, entityId),
            "return" => RemoveById(data.Returns, entityId),
            "lostdamaged" => RemoveById(data.LostDamaged, entityId),
            "receipt" => RemoveById(data.Receipts, entityId),
            "reporttemplate" => RemoveById(data.ReportTemplates, entityId),
            "invoicetemplate" => RemoveById(data.InvoiceTemplates, entityId),
            _ => false
        };
    }

    /// <summary>
    /// Adds an entity to CompanyData from a JSON snapshot.
    /// Returns true if the entity was successfully deserialized and added.
    /// </summary>
    public static bool AddEntityFromSnapshot(CompanyData data, string entityType, string snapshotJson)
    {
        if (string.IsNullOrEmpty(entityType) || string.IsNullOrEmpty(snapshotJson))
            return false;

        try
        {
            return NormalizeEntityType(entityType) switch
            {
                "customer" => TryDeserializeAndAdd(data.Customers, snapshotJson),
                "product" => TryDeserializeAndAdd(data.Products, snapshotJson),
                "supplier" => TryDeserializeAndAdd(data.Suppliers, snapshotJson),
                "employee" => TryDeserializeAndAdd(data.Employees, snapshotJson),
                "department" => TryDeserializeAndAdd(data.Departments, snapshotJson),
                "category" => TryDeserializeAndAdd(data.Categories, snapshotJson),
                "accountant" => TryDeserializeAndAdd(data.Accountants, snapshotJson),
                "location" => TryDeserializeAndAdd(data.Locations, snapshotJson),
                "revenue" => TryDeserializeAndAdd(data.Revenues, snapshotJson),
                "expense" => TryDeserializeAndAdd(data.Expenses, snapshotJson),
                "invoice" => TryDeserializeAndAdd(data.Invoices, snapshotJson),
                "payment" => TryDeserializeAndAdd(data.Payments, snapshotJson),
                "recurringinvoice" => TryDeserializeAndAdd(data.RecurringInvoices, snapshotJson),
                "inventory" or "inventoryitem" => TryDeserializeAndAdd(data.Inventory, snapshotJson),
                "stockadjustment" => TryDeserializeAndAdd(data.StockAdjustments, snapshotJson),
                "stocktransfer" => TryDeserializeAndAdd(data.StockTransfers, snapshotJson),
                "purchaseorder" => TryDeserializeAndAdd(data.PurchaseOrders, snapshotJson),
                "rentalitem" => TryDeserializeAndAdd(data.RentalInventory, snapshotJson),
                "rental" or "rentalrecord" => TryDeserializeAndAdd(data.Rentals, snapshotJson),
                "return" => TryDeserializeAndAdd(data.Returns, snapshotJson),
                "lostdamaged" => TryDeserializeAndAdd(data.LostDamaged, snapshotJson),
                "receipt" => TryDeserializeAndAdd(data.Receipts, snapshotJson),
                "reporttemplate" => TryDeserializeAndAdd(data.ReportTemplates, snapshotJson),
                "invoicetemplate" => TryDeserializeAndAdd(data.InvoiceTemplates, snapshotJson),
                _ => false
            };
        }
        catch
        {
            return false;
        }
    }

    private static string NormalizeEntityType(string entityType)
    {
        return entityType.Trim().ToLowerInvariant().Replace(" ", "");
    }

    private static bool RemoveById<T>(List<T> collection, string entityId) where T : class
    {
        var idProp = typeof(T).GetProperty("Id");
        if (idProp == null) return false;

        var entity = collection.FirstOrDefault(e => (string?)idProp.GetValue(e) == entityId);
        if (entity == null) return false;

        return collection.Remove(entity);
    }

    private static bool TryDeserializeAndAdd<T>(List<T> collection, string json) where T : class
    {
        var entity = JsonSerializer.Deserialize<T>(json, JsonOptions);
        if (entity == null) return false;

        collection.Add(entity);
        return true;
    }
}
