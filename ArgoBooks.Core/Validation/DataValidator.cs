using System.Text.RegularExpressions;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Validation;

/// <summary>
/// Validates data models before saving.
/// </summary>
public partial class DataValidator(CompanyData companyData)
{
    #region Entity Validation

    /// <summary>
    /// Validates a customer.
    /// </summary>
    public ValidationResult ValidateCustomer(Customer customer)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(customer.Name))
            result.AddError(nameof(customer.Name), "Customer name is required.");

        if (!string.IsNullOrWhiteSpace(customer.Email) && !IsValidEmail(customer.Email))
            result.AddError(nameof(customer.Email), "Invalid email address format.");

        // Check for duplicate name (excluding self)
        if (companyData.Customers.Any(c => c.Id != customer.Id &&
            c.Name.Equals(customer.Name, StringComparison.OrdinalIgnoreCase)))
            result.AddError(nameof(customer.Name), "A customer with this name already exists.");

        return result;
    }

    /// <summary>
    /// Validates a product.
    /// </summary>
    public ValidationResult ValidateProduct(Product product)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(product.Name))
            result.AddError(nameof(product.Name), "Product name is required.");

        if (product.UnitPrice < 0)
            result.AddError(nameof(product.UnitPrice), "Unit price cannot be negative.");

        if (product.CostPrice < 0)
            result.AddError(nameof(product.CostPrice), "Cost price cannot be negative.");

        if (product.TaxRate < 0 || product.TaxRate > 1)
            result.AddError(nameof(product.TaxRate), "Tax rate must be between 0 and 1 (0% to 100%).");

        // Check for duplicate SKU (excluding self)
        if (!string.IsNullOrWhiteSpace(product.Sku) &&
            companyData.Products.Any(p => p.Id != product.Id &&
            p.Sku.Equals(product.Sku, StringComparison.OrdinalIgnoreCase)))
            result.AddError(nameof(product.Sku), "A product with this SKU already exists.");

        // Validate supplier exists
        if (!string.IsNullOrWhiteSpace(product.SupplierId) &&
            companyData.GetSupplier(product.SupplierId) == null)
            result.AddError(nameof(product.SupplierId), "Supplier not found.");

        // Validate category exists
        if (!string.IsNullOrWhiteSpace(product.CategoryId) &&
            companyData.GetCategory(product.CategoryId) == null)
            result.AddError(nameof(product.CategoryId), "Category not found.");

        return result;
    }

    /// <summary>
    /// Validates a supplier.
    /// </summary>
    public ValidationResult ValidateSupplier(Supplier supplier)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(supplier.Name))
            result.AddError(nameof(supplier.Name), "Supplier name is required.");

        if (!string.IsNullOrWhiteSpace(supplier.Email) && !IsValidEmail(supplier.Email))
            result.AddError(nameof(supplier.Email), "Invalid email address format.");

        // Check for duplicate name (excluding self)
        if (companyData.Suppliers.Any(s => s.Id != supplier.Id &&
            s.Name.Equals(supplier.Name, StringComparison.OrdinalIgnoreCase)))
            result.AddError(nameof(supplier.Name), "A supplier with this name already exists.");

        return result;
    }

    /// <summary>
    /// Validates an employee.
    /// </summary>
    public ValidationResult ValidateEmployee(Employee employee)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(employee.FirstName))
            result.AddError(nameof(employee.FirstName), "First name is required.");

        if (string.IsNullOrWhiteSpace(employee.LastName))
            result.AddError(nameof(employee.LastName), "Last name is required.");

        if (!string.IsNullOrWhiteSpace(employee.Email) && !IsValidEmail(employee.Email))
            result.AddError(nameof(employee.Email), "Invalid email address format.");

        if (employee.SalaryAmount < 0)
            result.AddError(nameof(employee.SalaryAmount), "Salary cannot be negative.");

        // Validate department exists
        if (!string.IsNullOrWhiteSpace(employee.DepartmentId) &&
            companyData.GetDepartment(employee.DepartmentId) == null)
            result.AddError(nameof(employee.DepartmentId), "Department not found.");

        return result;
    }

    /// <summary>
    /// Validates a department.
    /// </summary>
    public ValidationResult ValidateDepartment(Department department)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(department.Name))
            result.AddError(nameof(department.Name), "Department name is required.");

        if (department.Budget < 0)
            result.AddError(nameof(department.Budget), "Budget cannot be negative.");

        // Check for duplicate name (excluding self)
        if (companyData.Departments.Any(d => d.Id != department.Id &&
            d.Name.Equals(department.Name, StringComparison.OrdinalIgnoreCase)))
            result.AddError(nameof(department.Name), "A department with this name already exists.");

        return result;
    }

    /// <summary>
    /// Validates a category.
    /// </summary>
    public ValidationResult ValidateCategory(Category category)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(category.Name))
            result.AddError(nameof(category.Name), "Category name is required.");

        if (category.DefaultTaxRate < 0 || category.DefaultTaxRate > 1)
            result.AddError(nameof(category.DefaultTaxRate), "Tax rate must be between 0 and 1.");

        // Check for duplicate name within same type (excluding self)
        if (companyData.Categories.Any(c => c.Id != category.Id &&
            c.Type == category.Type &&
            c.Name.Equals(category.Name, StringComparison.OrdinalIgnoreCase)))
            result.AddError(nameof(category.Name), "A category with this name already exists.");

        // Validate parent category exists and is same type
        if (!string.IsNullOrWhiteSpace(category.ParentId))
        {
            var parent = companyData.GetCategory(category.ParentId);
            if (parent == null)
                result.AddError(nameof(category.ParentId), "Parent category not found.");
            else if (parent.Type != category.Type)
                result.AddError(nameof(category.ParentId), "Parent category must be the same type.");
            else if (parent.Id == category.Id)
                result.AddError(nameof(category.ParentId), "Category cannot be its own parent.");
        }

        return result;
    }

    /// <summary>
    /// Validates a location.
    /// </summary>
    public ValidationResult ValidateLocation(Location location)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(location.Name))
            result.AddError(nameof(location.Name), "Location name is required.");

        if (location.Capacity < 0)
            result.AddError(nameof(location.Capacity), "Capacity cannot be negative.");

        // Check for duplicate name (excluding self)
        if (companyData.Locations.Any(l => l.Id != location.Id &&
            l.Name.Equals(location.Name, StringComparison.OrdinalIgnoreCase)))
            result.AddError(nameof(location.Name), "A location with this name already exists.");

        return result;
    }

    #endregion

    #region Transaction Validation

    /// <summary>
    /// Validates an invoice.
    /// </summary>
    public ValidationResult ValidateInvoice(Invoice invoice)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(invoice.CustomerId))
            result.AddError(nameof(invoice.CustomerId), "Customer is required.");
        else if (companyData.GetCustomer(invoice.CustomerId) == null)
            result.AddError(nameof(invoice.CustomerId), "Customer not found.");

        if (invoice.DueDate < invoice.IssueDate)
            result.AddError(nameof(invoice.DueDate), "Due date cannot be before issue date.");

        if (invoice.LineItems.Count == 0)
            result.AddError(nameof(invoice.LineItems), "Invoice must have at least one line item.");

        foreach (var item in invoice.LineItems)
        {
            result.Merge(ValidateLineItem(item));
        }

        if (invoice.Total < 0)
            result.AddError(nameof(invoice.Total), "Total cannot be negative.");

        return result;
    }

    /// <summary>
    /// Validates a sale.
    /// </summary>
    public ValidationResult ValidateSale(Revenue sale)
    {
        var result = new ValidationResult();

        if (sale.LineItems.Count == 0)
            result.AddError(nameof(sale.LineItems), "Sale must have at least one line item.");

        foreach (var item in sale.LineItems)
        {
            result.Merge(ValidateLineItem(item));
        }

        if (sale.Total < 0)
            result.AddError(nameof(sale.Total), "Total cannot be negative.");

        // Validate customer exists if specified
        if (!string.IsNullOrWhiteSpace(sale.CustomerId) &&
            companyData.GetCustomer(sale.CustomerId) == null)
            result.AddError(nameof(sale.CustomerId), "Customer not found.");

        return result;
    }

    /// <summary>
    /// Validates a purchase.
    /// </summary>
    public ValidationResult ValidatePurchase(Expense purchase)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(purchase.Description))
            result.AddError(nameof(purchase.Description), "Description is required.");

        if (purchase.Amount < 0)
            result.AddError(nameof(purchase.Amount), "Amount cannot be negative.");

        // Validate supplier exists if specified
        if (!string.IsNullOrWhiteSpace(purchase.SupplierId) &&
            companyData.GetSupplier(purchase.SupplierId) == null)
            result.AddError(nameof(purchase.SupplierId), "Supplier not found.");

        return result;
    }

    /// <summary>
    /// Validates a line item.
    /// </summary>
    public ValidationResult ValidateLineItem(LineItem lineItem)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(lineItem.Description) && string.IsNullOrWhiteSpace(lineItem.ProductId))
            result.AddError(nameof(lineItem.Description), "Description or product is required.");

        if (lineItem.Quantity <= 0)
            result.AddError(nameof(lineItem.Quantity), "Quantity must be greater than zero.");

        if (lineItem.UnitPrice < 0)
            result.AddError(nameof(lineItem.UnitPrice), "Unit price cannot be negative.");

        if (lineItem.TaxRate < 0 || lineItem.TaxRate > 1)
            result.AddError(nameof(lineItem.TaxRate), "Tax rate must be between 0 and 1.");

        return result;
    }

    #endregion

    #region Inventory Validation

    /// <summary>
    /// Validates an inventory item.
    /// </summary>
    public ValidationResult ValidateInventoryItem(InventoryItem item)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(item.ProductId))
            result.AddError(nameof(item.ProductId), "Product is required.");
        else if (companyData.GetProduct(item.ProductId) == null)
            result.AddError(nameof(item.ProductId), "Product not found.");

        if (string.IsNullOrWhiteSpace(item.LocationId))
            result.AddError(nameof(item.LocationId), "Location is required.");
        else if (companyData.GetLocation(item.LocationId) == null)
            result.AddError(nameof(item.LocationId), "Location not found.");

        if (item.InStock < 0)
            result.AddError(nameof(item.InStock), "Stock quantity cannot be negative.");

        if (item.Reserved < 0)
            result.AddError(nameof(item.Reserved), "Reserved quantity cannot be negative.");

        if (item.Reserved > item.InStock)
            result.AddError(nameof(item.Reserved), "Reserved quantity cannot exceed stock quantity.");

        if (item.ReorderPoint < 0)
            result.AddError(nameof(item.ReorderPoint), "Reorder point cannot be negative.");

        if (item.UnitCost < 0)
            result.AddError(nameof(item.UnitCost), "Unit cost cannot be negative.");

        return result;
    }

    /// <summary>
    /// Validates a stock transfer.
    /// </summary>
    public ValidationResult ValidateStockTransfer(StockTransfer transfer)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(transfer.SourceLocationId))
            result.AddError(nameof(transfer.SourceLocationId), "Source location is required.");
        else if (companyData.GetLocation(transfer.SourceLocationId) == null)
            result.AddError(nameof(transfer.SourceLocationId), "Source location not found.");

        if (string.IsNullOrWhiteSpace(transfer.DestinationLocationId))
            result.AddError(nameof(transfer.DestinationLocationId), "Destination location is required.");
        else if (companyData.GetLocation(transfer.DestinationLocationId) == null)
            result.AddError(nameof(transfer.DestinationLocationId), "Destination location not found.");

        if (transfer.SourceLocationId == transfer.DestinationLocationId)
            result.AddError(nameof(transfer.DestinationLocationId), "Source and destination cannot be the same.");

        if (transfer.Quantity <= 0)
            result.AddError(nameof(transfer.Quantity), "Quantity must be greater than zero.");

        return result;
    }

    #endregion

    #region Rental Validation

    /// <summary>
    /// Validates a rental item.
    /// </summary>
    public ValidationResult ValidateRentalItem(RentalItem item)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(item.Name))
            result.AddError(nameof(item.Name), "Name is required.");

        if (item.TotalQuantity < 0)
            result.AddError(nameof(item.TotalQuantity), "Total quantity cannot be negative.");

        if (item.DailyRate < 0)
            result.AddError(nameof(item.DailyRate), "Daily rate cannot be negative.");

        if (item.WeeklyRate < 0)
            result.AddError(nameof(item.WeeklyRate), "Weekly rate cannot be negative.");

        if (item.MonthlyRate < 0)
            result.AddError(nameof(item.MonthlyRate), "Monthly rate cannot be negative.");

        if (item.SecurityDeposit < 0)
            result.AddError(nameof(item.SecurityDeposit), "Security deposit cannot be negative.");

        return result;
    }

    /// <summary>
    /// Validates a rental record.
    /// </summary>
    public ValidationResult ValidateRentalRecord(RentalRecord rental)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(rental.RentalItemId))
            result.AddError(nameof(rental.RentalItemId), "Rental item is required.");

        if (string.IsNullOrWhiteSpace(rental.CustomerId))
            result.AddError(nameof(rental.CustomerId), "Customer is required.");
        else if (companyData.GetCustomer(rental.CustomerId) == null)
            result.AddError(nameof(rental.CustomerId), "Customer not found.");

        if (rental.Quantity <= 0)
            result.AddError(nameof(rental.Quantity), "Quantity must be greater than zero.");

        if (rental.DueDate < rental.StartDate)
            result.AddError(nameof(rental.DueDate), "Due date cannot be before start date.");

        if (rental.RateAmount < 0)
            result.AddError(nameof(rental.RateAmount), "Rate amount cannot be negative.");

        return result;
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Validates an email address format.
    /// </summary>
    private static bool IsValidEmail(string email)
    {
        return EmailRegex().IsMatch(email);
    }

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();

    #endregion
}
