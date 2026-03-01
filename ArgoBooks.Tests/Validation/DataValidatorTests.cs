using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Validation;
using Xunit;

namespace ArgoBooks.Tests.Validation;

/// <summary>
/// Comprehensive tests for the DataValidator class covering all entity validation methods.
/// </summary>
public class DataValidatorTests
{
    #region Test Helpers

    /// <summary>
    /// Creates a fresh CompanyData instance pre-populated with seed data for reference validation tests.
    /// Each test gets its own isolated copy to prevent cross-test contamination.
    /// </summary>
    private static (CompanyData data, DataValidator validator) CreateValidatorWithSeedData()
    {
        var companyData = new CompanyData();

        companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "Existing Customer" });
        companyData.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Existing Supplier" });
        companyData.Departments.Add(new Department { Id = "DEP-001", Name = "Engineering", Budget = 100000 });
        companyData.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        companyData.Categories.Add(new Category { Id = "CAT-002", Name = "Office Supplies", Type = CategoryType.Expense });
        companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Main Warehouse", Capacity = 1000 });
        companyData.Products.Add(new Product { Id = "PRD-001", Name = "Existing Widget", Sku = "SKU-EXISTING" });

        var validator = new DataValidator(companyData);
        return (companyData, validator);
    }

    /// <summary>
    /// Creates a fresh CompanyData and DataValidator with no seed data.
    /// </summary>
    private static (CompanyData data, DataValidator validator) CreateEmptyValidator()
    {
        var companyData = new CompanyData();
        var validator = new DataValidator(companyData);
        return (companyData, validator);
    }

    #endregion

    #region Customer Validation Tests

    [Fact]
    public void ValidateCustomer_ValidCustomer_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = "John Doe" };

        var result = validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateCustomer_ValidCustomerWithEmail_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = "John Doe", Email = "john@example.com" };

        var result = validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateCustomer_EmptyName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = "" };

        var result = validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateCustomer_WhitespaceName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = "   " };

        var result = validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateCustomer_NullName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = null! };

        var result = validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@company.co.uk")]
    [InlineData("firstname.lastname@sub.domain.com")]
    public void ValidateCustomer_ValidEmail_ReturnsSuccess(string email)
    {
        var (_, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = "John Doe", Email = email };

        var result = validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    [InlineData("user domain.com")]
    [InlineData("user@@domain.com")]
    public void ValidateCustomer_InvalidEmail_ReturnsError(string email)
    {
        var (_, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = "John Doe", Email = email };

        var result = validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateCustomer_EmptyOrNullEmail_IsAllowed(string? email)
    {
        var (_, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = "John Doe", Email = email ?? string.Empty };

        var result = validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCustomer_DuplicateName_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var customer = new Customer { Id = "CUS-002", Name = "John Doe" };

        var result = validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.Message.Contains("already exists"));
    }

    [Fact]
    public void ValidateCustomer_DuplicateNameCaseInsensitive_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var customer = new Customer { Id = "CUS-002", Name = "JOHN DOE" };

        var result = validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateCustomer_SameCustomerSameName_IsAllowed()
    {
        var (data, validator) = CreateEmptyValidator();
        var customer = new Customer { Id = "CUS-001", Name = "John Doe" };
        data.Customers.Add(customer);

        var result = validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCustomer_DifferentNameNoConflict_ReturnsSuccess()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var customer = new Customer { Id = "CUS-002", Name = "Jane Smith" };

        var result = validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCustomer_MultipleErrors_ReturnsAllErrors()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Customers.Add(new Customer { Id = "CUS-001", Name = "" });
        var customer = new Customer { Id = "CUS-002", Name = "", Email = "invalid" };

        var result = validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    #endregion

    #region Product Validation Tests

    [Fact]
    public void ValidateProduct_ValidProduct_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product
        {
            Id = "PRD-001",
            Name = "Widget",
            UnitPrice = 10.00m,
            CostPrice = 5.00m,
            TaxRate = 0.08m
        };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateProduct_ValidProductWithAllReferences_ReturnsSuccess()
    {
        var (_, validator) = CreateValidatorWithSeedData();
        var product = new Product
        {
            Id = "PRD-NEW",
            Name = "New Widget",
            UnitPrice = 25.00m,
            CostPrice = 12.00m,
            TaxRate = 0.10m,
            Sku = "SKU-NEW",
            SupplierId = "SUP-001",
            CategoryId = "CAT-001"
        };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_EmptyName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "" };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateProduct_WhitespaceName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "   " };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateProduct_NegativeUnitPrice_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", UnitPrice = -10.00m };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UnitPrice");
    }

    [Fact]
    public void ValidateProduct_ZeroUnitPrice_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", UnitPrice = 0m };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_NegativeCostPrice_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", CostPrice = -5.00m };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CostPrice");
    }

    [Fact]
    public void ValidateProduct_ZeroCostPrice_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", CostPrice = 0m };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1.0)]
    [InlineData(1.01)]
    [InlineData(2.0)]
    [InlineData(100.0)]
    public void ValidateProduct_InvalidTaxRate_ReturnsError(double taxRate)
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", TaxRate = (decimal)taxRate };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TaxRate");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.01)]
    [InlineData(0.08)]
    [InlineData(0.5)]
    [InlineData(0.99)]
    [InlineData(1.0)]
    public void ValidateProduct_ValidTaxRate_ReturnsSuccess(double taxRate)
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", TaxRate = (decimal)taxRate };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_TaxRateExactlyZero_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", TaxRate = 0m };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_TaxRateExactlyOne_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", TaxRate = 1.0m };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_DuplicateSku_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Products.Add(new Product { Id = "PRD-001", Name = "Widget A", Sku = "SKU-001" });
        var product = new Product { Id = "PRD-002", Name = "Widget B", Sku = "SKU-001" };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Sku");
    }

    [Fact]
    public void ValidateProduct_DuplicateSkuCaseInsensitive_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Products.Add(new Product { Id = "PRD-001", Name = "Widget A", Sku = "sku-001" });
        var product = new Product { Id = "PRD-002", Name = "Widget B", Sku = "SKU-001" };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Sku");
    }

    [Fact]
    public void ValidateProduct_SameProductSameSku_IsAllowed()
    {
        var (data, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget A", Sku = "SKU-001" };
        data.Products.Add(product);

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateProduct_EmptyOrNullSku_IsAllowed(string? sku)
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", Sku = sku ?? string.Empty };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_NonExistentSupplier_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", SupplierId = "SUP-999" };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SupplierId" && e.Message.Contains("not found"));
    }

    [Fact]
    public void ValidateProduct_ValidSupplier_ReturnsSuccess()
    {
        var (_, validator) = CreateValidatorWithSeedData();
        var product = new Product { Id = "PRD-NEW", Name = "Widget", SupplierId = "SUP-001" };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_EmptySupplierId_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", SupplierId = "" };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_NullSupplierId_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", SupplierId = null };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_NonExistentCategory_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", CategoryId = "CAT-999" };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CategoryId" && e.Message.Contains("not found"));
    }

    [Fact]
    public void ValidateProduct_ValidCategory_ReturnsSuccess()
    {
        var (_, validator) = CreateValidatorWithSeedData();
        var product = new Product { Id = "PRD-NEW", Name = "Widget", CategoryId = "CAT-001" };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_EmptyCategoryId_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", CategoryId = "" };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_NullCategoryId_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product { Id = "PRD-001", Name = "Widget", CategoryId = null };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_MultipleErrors_ReturnsAllErrors()
    {
        var (_, validator) = CreateEmptyValidator();
        var product = new Product
        {
            Id = "PRD-001",
            Name = "",
            UnitPrice = -10m,
            CostPrice = -5m,
            TaxRate = 2.0m,
            SupplierId = "SUP-NONEXISTENT",
            CategoryId = "CAT-NONEXISTENT"
        };

        var result = validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "UnitPrice");
        Assert.Contains(result.Errors, e => e.PropertyName == "CostPrice");
        Assert.Contains(result.Errors, e => e.PropertyName == "TaxRate");
        Assert.Contains(result.Errors, e => e.PropertyName == "SupplierId");
        Assert.Contains(result.Errors, e => e.PropertyName == "CategoryId");
    }

    #endregion

    #region Supplier Validation Tests

    [Fact]
    public void ValidateSupplier_ValidSupplier_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp" };

        var result = validator.ValidateSupplier(supplier);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateSupplier_ValidSupplierWithEmail_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp", Email = "contact@acme.com" };

        var result = validator.ValidateSupplier(supplier);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSupplier_EmptyName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var supplier = new Supplier { Id = "SUP-001", Name = "" };

        var result = validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateSupplier_WhitespaceName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var supplier = new Supplier { Id = "SUP-001", Name = "   " };

        var result = validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    [InlineData("no spaces@domain.com")]
    public void ValidateSupplier_InvalidEmail_ReturnsError(string email)
    {
        var (_, validator) = CreateEmptyValidator();
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp", Email = email };

        var result = validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("contact@acme.com")]
    [InlineData("info@supplier.co.uk")]
    public void ValidateSupplier_ValidEmail_ReturnsSuccess(string email)
    {
        var (_, validator) = CreateEmptyValidator();
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp", Email = email };

        var result = validator.ValidateSupplier(supplier);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSupplier_EmptyEmail_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp", Email = "" };

        var result = validator.ValidateSupplier(supplier);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSupplier_DuplicateName_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Acme Corp" });
        var supplier = new Supplier { Id = "SUP-002", Name = "Acme Corp" };

        var result = validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.Message.Contains("already exists"));
    }

    [Fact]
    public void ValidateSupplier_DuplicateNameCaseInsensitive_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Acme Corp" });
        var supplier = new Supplier { Id = "SUP-002", Name = "ACME CORP" };

        var result = validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateSupplier_SameSupplierSameName_IsAllowed()
    {
        var (data, validator) = CreateEmptyValidator();
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp" };
        data.Suppliers.Add(supplier);

        var result = validator.ValidateSupplier(supplier);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSupplier_DifferentNameNoConflict_ReturnsSuccess()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Acme Corp" });
        var supplier = new Supplier { Id = "SUP-002", Name = "Global Parts" };

        var result = validator.ValidateSupplier(supplier);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSupplier_MultipleErrors_ReturnsAllErrors()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "" });
        var supplier = new Supplier { Id = "SUP-002", Name = "", Email = "bad-email" };

        var result = validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    #endregion

    #region Employee Validation Tests

    [Fact]
    public void ValidateEmployee_ValidEmployee_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            SalaryAmount = 50000
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateEmployee_ValidEmployeeWithAllFields_ReturnsSuccess()
    {
        var (_, validator) = CreateValidatorWithSeedData();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "Jane",
            LastName = "Smith",
            Email = "jane@company.com",
            SalaryAmount = 75000,
            DepartmentId = "DEP-001"
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmployee_EmptyFirstName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee { Id = "EMP-001", FirstName = "", LastName = "Doe" };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void ValidateEmployee_WhitespaceFirstName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee { Id = "EMP-001", FirstName = "   ", LastName = "Doe" };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void ValidateEmployee_EmptyLastName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee { Id = "EMP-001", FirstName = "John", LastName = "" };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LastName");
    }

    [Fact]
    public void ValidateEmployee_WhitespaceLastName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee { Id = "EMP-001", FirstName = "John", LastName = "   " };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LastName");
    }

    [Fact]
    public void ValidateEmployee_BothNamesMissing_ReturnsBothErrors()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee { Id = "EMP-001", FirstName = "", LastName = "" };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FirstName");
        Assert.Contains(result.Errors, e => e.PropertyName == "LastName");
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    public void ValidateEmployee_InvalidEmail_ReturnsError(string email)
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = email
        };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("john@company.com")]
    [InlineData("jane.doe@example.org")]
    public void ValidateEmployee_ValidEmail_ReturnsSuccess(string email)
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = email
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmployee_EmptyEmail_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = ""
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmployee_NegativeSalary_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            SalaryAmount = -1000
        };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SalaryAmount");
    }

    [Fact]
    public void ValidateEmployee_ZeroSalary_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            SalaryAmount = 0
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmployee_NonExistentDepartment_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = "DEP-999"
        };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DepartmentId" && e.Message.Contains("not found"));
    }

    [Fact]
    public void ValidateEmployee_ValidDepartment_ReturnsSuccess()
    {
        var (_, validator) = CreateValidatorWithSeedData();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = "DEP-001"
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmployee_EmptyDepartmentId_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = ""
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmployee_NullDepartmentId_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = null
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmployee_MultipleErrors_ReturnsAllErrors()
    {
        var (_, validator) = CreateEmptyValidator();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "",
            LastName = "",
            Email = "invalid",
            SalaryAmount = -5000,
            DepartmentId = "DEP-NONEXISTENT"
        };

        var result = validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FirstName");
        Assert.Contains(result.Errors, e => e.PropertyName == "LastName");
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
        Assert.Contains(result.Errors, e => e.PropertyName == "SalaryAmount");
        Assert.Contains(result.Errors, e => e.PropertyName == "DepartmentId");
    }

    #endregion

    #region Department Validation Tests

    [Fact]
    public void ValidateDepartment_ValidDepartment_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var department = new Department { Id = "DEP-001", Name = "Engineering", Budget = 100000 };

        var result = validator.ValidateDepartment(department);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateDepartment_EmptyName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var department = new Department { Id = "DEP-001", Name = "" };

        var result = validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateDepartment_WhitespaceName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var department = new Department { Id = "DEP-001", Name = "   " };

        var result = validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateDepartment_NegativeBudget_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var department = new Department { Id = "DEP-001", Name = "Engineering", Budget = -5000 };

        var result = validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Budget");
    }

    [Fact]
    public void ValidateDepartment_ZeroBudget_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var department = new Department { Id = "DEP-001", Name = "Engineering", Budget = 0 };

        var result = validator.ValidateDepartment(department);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDepartment_DuplicateName_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Departments.Add(new Department { Id = "DEP-001", Name = "Engineering" });
        var department = new Department { Id = "DEP-002", Name = "Engineering" };

        var result = validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.Message.Contains("already exists"));
    }

    [Fact]
    public void ValidateDepartment_DuplicateNameCaseInsensitive_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Departments.Add(new Department { Id = "DEP-001", Name = "Engineering" });
        var department = new Department { Id = "DEP-002", Name = "ENGINEERING" };

        var result = validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateDepartment_SameDepartmentSameName_IsAllowed()
    {
        var (data, validator) = CreateEmptyValidator();
        var department = new Department { Id = "DEP-001", Name = "Engineering" };
        data.Departments.Add(department);

        var result = validator.ValidateDepartment(department);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDepartment_DifferentNameNoConflict_ReturnsSuccess()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Departments.Add(new Department { Id = "DEP-001", Name = "Engineering" });
        var department = new Department { Id = "DEP-002", Name = "Marketing" };

        var result = validator.ValidateDepartment(department);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDepartment_MultipleErrors_ReturnsAllErrors()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Departments.Add(new Department { Id = "DEP-001", Name = "" });
        var department = new Department { Id = "DEP-002", Name = "", Budget = -1000 };

        var result = validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Budget");
    }

    #endregion

    #region Category Validation Tests

    [Fact]
    public void ValidateCategory_ValidCategory_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Electronics",
            Type = CategoryType.Revenue
        };

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Theory]
    [InlineData(CategoryType.Revenue)]
    [InlineData(CategoryType.Expense)]
    [InlineData(CategoryType.Rental)]
    public void ValidateCategory_ValidCategoryAllTypes_ReturnsSuccess(CategoryType type)
    {
        var (_, validator) = CreateEmptyValidator();
        var category = new Category { Id = "CAT-001", Name = "Test Category", Type = type };

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_EmptyName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var category = new Category { Id = "CAT-001", Name = "", Type = CategoryType.Revenue };

        var result = validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateCategory_WhitespaceName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var category = new Category { Id = "CAT-001", Name = "   ", Type = CategoryType.Revenue };

        var result = validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateCategory_DuplicateNameSameType_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category { Id = "CAT-002", Name = "Electronics", Type = CategoryType.Revenue };

        var result = validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.Message.Contains("already exists"));
    }

    [Fact]
    public void ValidateCategory_DuplicateNameSameTypeCaseInsensitive_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category { Id = "CAT-002", Name = "ELECTRONICS", Type = CategoryType.Revenue };

        var result = validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateCategory_DuplicateNameDifferentType_IsAllowed()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category { Id = "CAT-002", Name = "Electronics", Type = CategoryType.Expense };

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_SameCategorySameName_IsAllowed()
    {
        var (data, validator) = CreateEmptyValidator();
        var category = new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue };
        data.Categories.Add(category);

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_NonExistentParent_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Smartphones",
            Type = CategoryType.Revenue,
            ParentId = "CAT-999"
        };

        var result = validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentId" && e.Message.Contains("not found"));
    }

    [Fact]
    public void ValidateCategory_ParentDifferentType_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category
        {
            Id = "CAT-002",
            Name = "Office Supplies",
            Type = CategoryType.Expense,
            ParentId = "CAT-001"
        };

        var result = validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentId" && e.Message.Contains("same type"));
    }

    [Fact]
    public void ValidateCategory_SelfParent_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Electronics",
            Type = CategoryType.Revenue,
            ParentId = "CAT-001"
        };
        data.Categories.Add(category);

        var result = validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentId" && e.Message.Contains("own parent"));
    }

    [Fact]
    public void ValidateCategory_ValidParentSameType_ReturnsSuccess()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category
        {
            Id = "CAT-002",
            Name = "Smartphones",
            Type = CategoryType.Revenue,
            ParentId = "CAT-001"
        };

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_NullParentId_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Electronics",
            Type = CategoryType.Revenue,
            ParentId = null
        };

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_EmptyParentId_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Electronics",
            Type = CategoryType.Revenue,
            ParentId = ""
        };

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_ParentExpenseChildExpense_ReturnsSuccess()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Categories.Add(new Category { Id = "CAT-001", Name = "Operating Costs", Type = CategoryType.Expense });
        var category = new Category
        {
            Id = "CAT-002",
            Name = "Utilities",
            Type = CategoryType.Expense,
            ParentId = "CAT-001"
        };

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_ParentRentalChildRental_ReturnsSuccess()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Categories.Add(new Category { Id = "CAT-001", Name = "Equipment", Type = CategoryType.Rental });
        var category = new Category
        {
            Id = "CAT-002",
            Name = "Heavy Equipment",
            Type = CategoryType.Rental,
            ParentId = "CAT-001"
        };

        var result = validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_DuplicateNameAcrossAllThreeTypes_OnlyMatchingTypeConflicts()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Categories.Add(new Category { Id = "CAT-001", Name = "Misc", Type = CategoryType.Revenue });
        data.Categories.Add(new Category { Id = "CAT-002", Name = "Misc", Type = CategoryType.Expense });

        // Adding a third "Misc" with Rental type should succeed since no Rental "Misc" exists
        var category = new Category { Id = "CAT-003", Name = "Misc", Type = CategoryType.Rental };
        var result = validator.ValidateCategory(category);
        Assert.True(result.IsValid);

        // Adding a fourth "Misc" with Revenue type should fail since Revenue "Misc" already exists
        var conflicting = new Category { Id = "CAT-004", Name = "Misc", Type = CategoryType.Revenue };
        var conflictResult = validator.ValidateCategory(conflicting);
        Assert.False(conflictResult.IsValid);
    }

    #endregion

    #region Location Validation Tests

    [Fact]
    public void ValidateLocation_ValidLocation_ReturnsSuccess()
    {
        var (_, validator) = CreateEmptyValidator();
        var location = new Location { Id = "LOC-001", Name = "Warehouse A", Capacity = 1000 };

        var result = validator.ValidateLocation(location);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateLocation_EmptyName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var location = new Location { Id = "LOC-001", Name = "" };

        var result = validator.ValidateLocation(location);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateLocation_WhitespaceName_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var location = new Location { Id = "LOC-001", Name = "   " };

        var result = validator.ValidateLocation(location);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateLocation_NegativeCapacity_ReturnsError()
    {
        var (_, validator) = CreateEmptyValidator();
        var location = new Location { Id = "LOC-001", Name = "Warehouse A", Capacity = -100 };

        var result = validator.ValidateLocation(location);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Capacity");
    }

    [Fact]
    public void ValidateLocation_ZeroCapacity_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var location = new Location { Id = "LOC-001", Name = "Warehouse A", Capacity = 0 };

        var result = validator.ValidateLocation(location);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateLocation_DuplicateName_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var location = new Location { Id = "LOC-002", Name = "Warehouse A" };

        var result = validator.ValidateLocation(location);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name" && e.Message.Contains("already exists"));
    }

    [Fact]
    public void ValidateLocation_DuplicateNameCaseInsensitive_ReturnsError()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var location = new Location { Id = "LOC-002", Name = "WAREHOUSE A" };

        var result = validator.ValidateLocation(location);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateLocation_SameLocationSameName_IsAllowed()
    {
        var (data, validator) = CreateEmptyValidator();
        var location = new Location { Id = "LOC-001", Name = "Warehouse A" };
        data.Locations.Add(location);

        var result = validator.ValidateLocation(location);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateLocation_DifferentNameNoConflict_ReturnsSuccess()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var location = new Location { Id = "LOC-002", Name = "Warehouse B" };

        var result = validator.ValidateLocation(location);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateLocation_MultipleErrors_ReturnsAllErrors()
    {
        var (data, validator) = CreateEmptyValidator();
        data.Locations.Add(new Location { Id = "LOC-001", Name = "" });
        var location = new Location { Id = "LOC-002", Name = "", Capacity = -50 };

        var result = validator.ValidateLocation(location);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
        Assert.Contains(result.Errors, e => e.PropertyName == "Capacity");
    }

    [Fact]
    public void ValidateLocation_LargeCapacity_IsAllowed()
    {
        var (_, validator) = CreateEmptyValidator();
        var location = new Location { Id = "LOC-001", Name = "Mega Warehouse", Capacity = int.MaxValue };

        var result = validator.ValidateLocation(location);

        Assert.True(result.IsValid);
    }

    #endregion

    #region ValidationResult Tests

    [Fact]
    public void ValidationResult_Success_ReturnsValidResult()
    {
        var result = ValidationResult.Success();

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidationResult_Failure_ReturnsInvalidResult()
    {
        var result = ValidationResult.Failure("PropertyName", "Error message");

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
        Assert.Equal("PropertyName", result.Errors[0].PropertyName);
        Assert.Equal("Error message", result.Errors[0].Message);
    }

    [Fact]
    public void ValidationResult_AddError_MakesResultInvalid()
    {
        var result = new ValidationResult();
        Assert.True(result.IsValid);

        result.AddError("Field", "An error occurred.");

        Assert.False(result.IsValid);
        Assert.Single(result.Errors);
    }

    [Fact]
    public void ValidationResult_Merge_CombinesErrors()
    {
        var result1 = ValidationResult.Failure("Prop1", "Error 1");
        var result2 = ValidationResult.Failure("Prop2", "Error 2");

        result1.Merge(result2);

        Assert.Equal(2, result1.Errors.Count);
    }

    [Fact]
    public void ValidationResult_Merge_SuccessWithSuccess_StaysValid()
    {
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Success();

        result1.Merge(result2);

        Assert.True(result1.IsValid);
        Assert.Empty(result1.Errors);
    }

    [Fact]
    public void ValidationResult_Merge_SuccessWithFailure_BecomesInvalid()
    {
        var result1 = ValidationResult.Success();
        var result2 = ValidationResult.Failure("Prop", "Error");

        result1.Merge(result2);

        Assert.False(result1.IsValid);
        Assert.Single(result1.Errors);
    }

    [Fact]
    public void ValidationResult_GetErrorMessage_ReturnsAllErrors()
    {
        var result = new ValidationResult();
        result.AddError("Prop1", "Error 1");
        result.AddError("Prop2", "Error 2");

        var message = result.GetErrorMessage();

        Assert.Contains("Error 1", message);
        Assert.Contains("Error 2", message);
    }

    [Fact]
    public void ValidationResult_GetErrorMessage_WithCustomSeparator_UsesIt()
    {
        var result = new ValidationResult();
        result.AddError("Prop1", "Error 1");
        result.AddError("Prop2", "Error 2");

        var message = result.GetErrorMessage("; ");

        Assert.Contains("; ", message);
    }

    [Fact]
    public void ValidationResult_GetErrorsForProperty_FiltersCorrectly()
    {
        var result = new ValidationResult();
        result.AddError("Name", "Name error 1");
        result.AddError("Email", "Email error");
        result.AddError("Name", "Name error 2");

        var nameErrors = result.GetErrorsForProperty("Name").ToList();

        Assert.Equal(2, nameErrors.Count);
        Assert.All(nameErrors, e => Assert.Equal("Name", e.PropertyName));
    }

    [Fact]
    public void ValidationResult_GetErrorsForProperty_NoMatch_ReturnsEmpty()
    {
        var result = new ValidationResult();
        result.AddError("Name", "Name error");

        var emailErrors = result.GetErrorsForProperty("Email").ToList();

        Assert.Empty(emailErrors);
    }

    #endregion

    #region Cross-Validator Integration Tests

    [Fact]
    public void Validator_SameCompanyData_SharedState()
    {
        var (data, validator) = CreateEmptyValidator();

        // Add a customer
        data.Customers.Add(new Customer { Id = "CUS-001", Name = "Shared Customer" });

        // The validator should see the shared customer when validating a duplicate
        var duplicate = new Customer { Id = "CUS-002", Name = "Shared Customer" };
        var result = validator.ValidateCustomer(duplicate);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_ProductReferencingExistingSupplierAndCategory_ReturnsSuccess()
    {
        var (_, validator) = CreateValidatorWithSeedData();
        var product = new Product
        {
            Id = "PRD-NEW",
            Name = "Fully Referenced Product",
            UnitPrice = 99.99m,
            CostPrice = 49.99m,
            TaxRate = 0.07m,
            Sku = "SKU-FULLREF",
            SupplierId = "SUP-001",
            CategoryId = "CAT-001"
        };

        var result = validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_EmployeeReferencingExistingDepartment_ReturnsSuccess()
    {
        var (_, validator) = CreateValidatorWithSeedData();
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "Test",
            LastName = "Employee",
            DepartmentId = "DEP-001"
        };

        var result = validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validator_MultipleDuplicateChecksIndependent()
    {
        var (data, validator) = CreateEmptyValidator();

        // Customer and supplier can share the same name without conflict
        data.Customers.Add(new Customer { Id = "CUS-001", Name = "Acme Corp" });
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp" };

        var result = validator.ValidateSupplier(supplier);

        Assert.True(result.IsValid);
    }

    #endregion
}
