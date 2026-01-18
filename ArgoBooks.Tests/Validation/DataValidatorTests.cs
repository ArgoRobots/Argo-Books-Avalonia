using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Rentals;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Validation;
using Xunit;

namespace ArgoBooks.Tests.Validation;

/// <summary>
/// Tests for the DataValidator class.
/// </summary>
public class DataValidatorTests
{
    private readonly CompanyData _companyData;
    private readonly DataValidator _validator;

    public DataValidatorTests()
    {
        _companyData = new CompanyData();
        _validator = new DataValidator(_companyData);
    }

    #region Customer Validation Tests

    [Fact]
    public void ValidateCustomer_ValidCustomer_ReturnsSuccess()
    {
        var customer = new Customer { Id = "CUS-001", Name = "John Doe" };

        var result = _validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateCustomer_EmptyName_ReturnsError()
    {
        var customer = new Customer { Id = "CUS-001", Name = "" };

        var result = _validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateCustomer_WhitespaceName_ReturnsError()
    {
        var customer = new Customer { Id = "CUS-001", Name = "   " };

        var result = _validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.org")]
    [InlineData("user+tag@company.co.uk")]
    public void ValidateCustomer_ValidEmail_ReturnsSuccess(string email)
    {
        var customer = new Customer { Id = "CUS-001", Name = "John Doe", Email = email };

        var result = _validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    [InlineData("user@domain")]
    [InlineData("user domain.com")]
    public void ValidateCustomer_InvalidEmail_ReturnsError(string email)
    {
        var customer = new Customer { Id = "CUS-001", Name = "John Doe", Email = email };

        var result = _validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void ValidateCustomer_EmptyEmail_IsAllowed()
    {
        var customer = new Customer { Id = "CUS-001", Name = "John Doe", Email = "" };

        var result = _validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCustomer_DuplicateName_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var customer = new Customer { Id = "CUS-002", Name = "John Doe" };

        var result = _validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateCustomer_DuplicateNameCaseInsensitive_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var customer = new Customer { Id = "CUS-002", Name = "JOHN DOE" };

        var result = _validator.ValidateCustomer(customer);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateCustomer_SameCustomerSameName_IsAllowed()
    {
        var customer = new Customer { Id = "CUS-001", Name = "John Doe" };
        _companyData.Customers.Add(customer);

        var result = _validator.ValidateCustomer(customer);

        Assert.True(result.IsValid);
    }

    #endregion

    #region Product Validation Tests

    [Fact]
    public void ValidateProduct_ValidProduct_ReturnsSuccess()
    {
        var product = new Product
        {
            Id = "PRD-001",
            Name = "Widget",
            UnitPrice = 10.00m,
            CostPrice = 5.00m,
            TaxRate = 0.08m
        };

        var result = _validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_EmptyName_ReturnsError()
    {
        var product = new Product { Id = "PRD-001", Name = "" };

        var result = _validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateProduct_NegativeUnitPrice_ReturnsError()
    {
        var product = new Product { Id = "PRD-001", Name = "Widget", UnitPrice = -10.00m };

        var result = _validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UnitPrice");
    }

    [Fact]
    public void ValidateProduct_NegativeCostPrice_ReturnsError()
    {
        var product = new Product { Id = "PRD-001", Name = "Widget", CostPrice = -5.00m };

        var result = _validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CostPrice");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    [InlineData(2.0)]
    public void ValidateProduct_InvalidTaxRate_ReturnsError(decimal taxRate)
    {
        var product = new Product { Id = "PRD-001", Name = "Widget", TaxRate = taxRate };

        var result = _validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TaxRate");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(0.08)]
    [InlineData(0.5)]
    [InlineData(1.0)]
    public void ValidateProduct_ValidTaxRate_ReturnsSuccess(decimal taxRate)
    {
        var product = new Product { Id = "PRD-001", Name = "Widget", TaxRate = taxRate };

        var result = _validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_DuplicateSku_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget A", Sku = "SKU-001" });
        var product = new Product { Id = "PRD-002", Name = "Widget B", Sku = "SKU-001" };

        var result = _validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Sku");
    }

    [Fact]
    public void ValidateProduct_DuplicateSkuCaseInsensitive_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget A", Sku = "sku-001" });
        var product = new Product { Id = "PRD-002", Name = "Widget B", Sku = "SKU-001" };

        var result = _validator.ValidateProduct(product);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_EmptySku_IsAllowed()
    {
        var product = new Product { Id = "PRD-001", Name = "Widget", Sku = "" };

        var result = _validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_NonExistentSupplier_ReturnsError()
    {
        var product = new Product { Id = "PRD-001", Name = "Widget", SupplierId = "SUP-999" };

        var result = _validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SupplierId");
    }

    [Fact]
    public void ValidateProduct_ValidSupplier_ReturnsSuccess()
    {
        _companyData.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Supplier Inc." });
        var product = new Product { Id = "PRD-001", Name = "Widget", SupplierId = "SUP-001" };

        var result = _validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateProduct_NonExistentCategory_ReturnsError()
    {
        var product = new Product { Id = "PRD-001", Name = "Widget", CategoryId = "CAT-999" };

        var result = _validator.ValidateProduct(product);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CategoryId");
    }

    [Fact]
    public void ValidateProduct_ValidCategory_ReturnsSuccess()
    {
        _companyData.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var product = new Product { Id = "PRD-001", Name = "Widget", CategoryId = "CAT-001" };

        var result = _validator.ValidateProduct(product);

        Assert.True(result.IsValid);
    }

    #endregion

    #region Supplier Validation Tests

    [Fact]
    public void ValidateSupplier_ValidSupplier_ReturnsSuccess()
    {
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp" };

        var result = _validator.ValidateSupplier(supplier);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateSupplier_EmptyName_ReturnsError()
    {
        var supplier = new Supplier { Id = "SUP-001", Name = "" };

        var result = _validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateSupplier_InvalidEmail_ReturnsError()
    {
        var supplier = new Supplier { Id = "SUP-001", Name = "Acme Corp", Email = "invalid" };

        var result = _validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void ValidateSupplier_DuplicateName_ReturnsError()
    {
        _companyData.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Acme Corp" });
        var supplier = new Supplier { Id = "SUP-002", Name = "Acme Corp" };

        var result = _validator.ValidateSupplier(supplier);

        Assert.False(result.IsValid);
    }

    #endregion

    #region Employee Validation Tests

    [Fact]
    public void ValidateEmployee_ValidEmployee_ReturnsSuccess()
    {
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            SalaryAmount = 50000
        };

        var result = _validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmployee_EmptyFirstName_ReturnsError()
    {
        var employee = new Employee { Id = "EMP-001", FirstName = "", LastName = "Doe" };

        var result = _validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "FirstName");
    }

    [Fact]
    public void ValidateEmployee_EmptyLastName_ReturnsError()
    {
        var employee = new Employee { Id = "EMP-001", FirstName = "John", LastName = "" };

        var result = _validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LastName");
    }

    [Fact]
    public void ValidateEmployee_InvalidEmail_ReturnsError()
    {
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            Email = "invalid"
        };

        var result = _validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Email");
    }

    [Fact]
    public void ValidateEmployee_NegativeSalary_ReturnsError()
    {
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            SalaryAmount = -1000
        };

        var result = _validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SalaryAmount");
    }

    [Fact]
    public void ValidateEmployee_NonExistentDepartment_ReturnsError()
    {
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = "DEP-999"
        };

        var result = _validator.ValidateEmployee(employee);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DepartmentId");
    }

    [Fact]
    public void ValidateEmployee_ValidDepartment_ReturnsSuccess()
    {
        _companyData.Departments.Add(new Department { Id = "DEP-001", Name = "Engineering" });
        var employee = new Employee
        {
            Id = "EMP-001",
            FirstName = "John",
            LastName = "Doe",
            DepartmentId = "DEP-001"
        };

        var result = _validator.ValidateEmployee(employee);

        Assert.True(result.IsValid);
    }

    #endregion

    #region Department Validation Tests

    [Fact]
    public void ValidateDepartment_ValidDepartment_ReturnsSuccess()
    {
        var department = new Department { Id = "DEP-001", Name = "Engineering", Budget = 100000 };

        var result = _validator.ValidateDepartment(department);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateDepartment_EmptyName_ReturnsError()
    {
        var department = new Department { Id = "DEP-001", Name = "" };

        var result = _validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateDepartment_NegativeBudget_ReturnsError()
    {
        var department = new Department { Id = "DEP-001", Name = "Engineering", Budget = -5000 };

        var result = _validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Budget");
    }

    [Fact]
    public void ValidateDepartment_DuplicateName_ReturnsError()
    {
        _companyData.Departments.Add(new Department { Id = "DEP-001", Name = "Engineering" });
        var department = new Department { Id = "DEP-002", Name = "Engineering" };

        var result = _validator.ValidateDepartment(department);

        Assert.False(result.IsValid);
    }

    #endregion

    #region Category Validation Tests

    [Fact]
    public void ValidateCategory_ValidCategory_ReturnsSuccess()
    {
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Electronics",
            Type = CategoryType.Revenue,
            DefaultTaxRate = 0.08m
        };

        var result = _validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_EmptyName_ReturnsError()
    {
        var category = new Category { Id = "CAT-001", Name = "", Type = CategoryType.Revenue };

        var result = _validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void ValidateCategory_InvalidTaxRate_ReturnsError(decimal taxRate)
    {
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Electronics",
            Type = CategoryType.Revenue,
            DefaultTaxRate = taxRate
        };

        var result = _validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DefaultTaxRate");
    }

    [Fact]
    public void ValidateCategory_DuplicateNameSameType_ReturnsError()
    {
        _companyData.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category { Id = "CAT-002", Name = "Electronics", Type = CategoryType.Revenue };

        var result = _validator.ValidateCategory(category);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_DuplicateNameDifferentType_IsAllowed()
    {
        _companyData.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category { Id = "CAT-002", Name = "Electronics", Type = CategoryType.Expense };

        var result = _validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateCategory_NonExistentParent_ReturnsError()
    {
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Smartphones",
            Type = CategoryType.Revenue,
            ParentId = "CAT-999"
        };

        var result = _validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentId");
    }

    [Fact]
    public void ValidateCategory_ParentDifferentType_ReturnsError()
    {
        _companyData.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category
        {
            Id = "CAT-002",
            Name = "Office Supplies",
            Type = CategoryType.Expense,
            ParentId = "CAT-001"
        };

        var result = _validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentId");
    }

    [Fact]
    public void ValidateCategory_SelfParent_ReturnsError()
    {
        var category = new Category
        {
            Id = "CAT-001",
            Name = "Electronics",
            Type = CategoryType.Revenue,
            ParentId = "CAT-001"
        };
        _companyData.Categories.Add(category);

        var result = _validator.ValidateCategory(category);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ParentId" && e.Message.Contains("own parent"));
    }

    [Fact]
    public void ValidateCategory_ValidParent_ReturnsSuccess()
    {
        _companyData.Categories.Add(new Category { Id = "CAT-001", Name = "Electronics", Type = CategoryType.Revenue });
        var category = new Category
        {
            Id = "CAT-002",
            Name = "Smartphones",
            Type = CategoryType.Revenue,
            ParentId = "CAT-001"
        };

        var result = _validator.ValidateCategory(category);

        Assert.True(result.IsValid);
    }

    #endregion

    #region Location Validation Tests

    [Fact]
    public void ValidateLocation_ValidLocation_ReturnsSuccess()
    {
        var location = new Location { Id = "LOC-001", Name = "Warehouse A", Capacity = 1000 };

        var result = _validator.ValidateLocation(location);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateLocation_EmptyName_ReturnsError()
    {
        var location = new Location { Id = "LOC-001", Name = "" };

        var result = _validator.ValidateLocation(location);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateLocation_NegativeCapacity_ReturnsError()
    {
        var location = new Location { Id = "LOC-001", Name = "Warehouse A", Capacity = -100 };

        var result = _validator.ValidateLocation(location);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Capacity");
    }

    [Fact]
    public void ValidateLocation_DuplicateName_ReturnsError()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var location = new Location { Id = "LOC-002", Name = "Warehouse A" };

        var result = _validator.ValidateLocation(location);

        Assert.False(result.IsValid);
    }

    #endregion

    #region Invoice Validation Tests

    [Fact]
    public void ValidateInvoice_ValidInvoice_ReturnsSuccess()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var invoice = new Invoice
        {
            Id = "INV-001",
            CustomerId = "CUS-001",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(30),
            LineItems = [new LineItem { Description = "Service", Quantity = 1, UnitPrice = 100 }],
            Total = 100
        };

        var result = _validator.ValidateInvoice(invoice);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateInvoice_EmptyCustomerId_ReturnsError()
    {
        var invoice = new Invoice
        {
            Id = "INV-001",
            CustomerId = "",
            LineItems = [new LineItem { Description = "Service", Quantity = 1, UnitPrice = 100 }]
        };

        var result = _validator.ValidateInvoice(invoice);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void ValidateInvoice_NonExistentCustomer_ReturnsError()
    {
        var invoice = new Invoice
        {
            Id = "INV-001",
            CustomerId = "CUS-999",
            LineItems = [new LineItem { Description = "Service", Quantity = 1, UnitPrice = 100 }]
        };

        var result = _validator.ValidateInvoice(invoice);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void ValidateInvoice_DueDateBeforeIssueDate_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var invoice = new Invoice
        {
            Id = "INV-001",
            CustomerId = "CUS-001",
            IssueDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(-1),
            LineItems = [new LineItem { Description = "Service", Quantity = 1, UnitPrice = 100 }]
        };

        var result = _validator.ValidateInvoice(invoice);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DueDate");
    }

    [Fact]
    public void ValidateInvoice_EmptyLineItems_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var invoice = new Invoice
        {
            Id = "INV-001",
            CustomerId = "CUS-001",
            LineItems = []
        };

        var result = _validator.ValidateInvoice(invoice);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LineItems");
    }

    [Fact]
    public void ValidateInvoice_NegativeTotal_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var invoice = new Invoice
        {
            Id = "INV-001",
            CustomerId = "CUS-001",
            LineItems = [new LineItem { Description = "Service", Quantity = 1, UnitPrice = 100 }],
            Total = -50
        };

        var result = _validator.ValidateInvoice(invoice);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Total");
    }

    [Fact]
    public void ValidateInvoice_InvalidLineItem_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var invoice = new Invoice
        {
            Id = "INV-001",
            CustomerId = "CUS-001",
            LineItems = [new LineItem { Description = "", Quantity = 0, UnitPrice = -10 }]
        };

        var result = _validator.ValidateInvoice(invoice);

        Assert.False(result.IsValid);
    }

    #endregion

    #region Revenue Validation Tests

    [Fact]
    public void ValidateRevenue_ValidRevenue_ReturnsSuccess()
    {
        var revenue = new Revenue
        {
            Id = "REV-001",
            LineItems = [new LineItem { Description = "Product", Quantity = 2, UnitPrice = 50 }],
            Total = 100
        };

        var result = _validator.ValidateRevenue(revenue);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateRevenue_EmptyLineItems_ReturnsError()
    {
        var revenue = new Revenue { Id = "REV-001", LineItems = [] };

        var result = _validator.ValidateRevenue(revenue);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LineItems");
    }

    [Fact]
    public void ValidateRevenue_NegativeTotal_ReturnsError()
    {
        var revenue = new Revenue
        {
            Id = "REV-001",
            LineItems = [new LineItem { Description = "Product", Quantity = 1, UnitPrice = 50 }],
            Total = -10
        };

        var result = _validator.ValidateRevenue(revenue);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Total");
    }

    [Fact]
    public void ValidateRevenue_NonExistentCustomer_ReturnsError()
    {
        var revenue = new Revenue
        {
            Id = "REV-001",
            CustomerId = "CUS-999",
            LineItems = [new LineItem { Description = "Product", Quantity = 1, UnitPrice = 50 }]
        };

        var result = _validator.ValidateRevenue(revenue);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void ValidateRevenue_ValidCustomer_ReturnsSuccess()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var revenue = new Revenue
        {
            Id = "REV-001",
            CustomerId = "CUS-001",
            LineItems = [new LineItem { Description = "Product", Quantity = 1, UnitPrice = 50 }],
            Total = 50
        };

        var result = _validator.ValidateRevenue(revenue);

        Assert.True(result.IsValid);
    }

    #endregion

    #region Expense Validation Tests

    [Fact]
    public void ValidateExpense_ValidExpense_ReturnsSuccess()
    {
        var expense = new Expense
        {
            Id = "EXP-001",
            Description = "Office Supplies",
            Amount = 200
        };

        var result = _validator.ValidateExpense(expense);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateExpense_EmptyDescription_ReturnsError()
    {
        var expense = new Expense { Id = "EXP-001", Description = "" };

        var result = _validator.ValidateExpense(expense);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    [Fact]
    public void ValidateExpense_NegativeAmount_ReturnsError()
    {
        var expense = new Expense { Id = "EXP-001", Description = "Office Supplies", Amount = -50 };

        var result = _validator.ValidateExpense(expense);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Amount");
    }

    [Fact]
    public void ValidateExpense_NonExistentSupplier_ReturnsError()
    {
        var expense = new Expense
        {
            Id = "EXP-001",
            Description = "Office Supplies",
            SupplierId = "SUP-999"
        };

        var result = _validator.ValidateExpense(expense);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SupplierId");
    }

    [Fact]
    public void ValidateExpense_ValidSupplier_ReturnsSuccess()
    {
        _companyData.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Acme Corp" });
        var expense = new Expense
        {
            Id = "EXP-001",
            Description = "Office Supplies",
            SupplierId = "SUP-001",
            Amount = 200
        };

        var result = _validator.ValidateExpense(expense);

        Assert.True(result.IsValid);
    }

    #endregion

    #region LineItem Validation Tests

    [Fact]
    public void ValidateLineItem_ValidLineItem_ReturnsSuccess()
    {
        var lineItem = new LineItem
        {
            Description = "Service",
            Quantity = 5,
            UnitPrice = 20,
            TaxRate = 0.1m
        };

        var result = _validator.ValidateLineItem(lineItem);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateLineItem_WithProductId_ReturnsSuccess()
    {
        var lineItem = new LineItem
        {
            ProductId = "PRD-001",
            Quantity = 5,
            UnitPrice = 20
        };

        var result = _validator.ValidateLineItem(lineItem);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateLineItem_NoDescriptionOrProduct_ReturnsError()
    {
        var lineItem = new LineItem { Quantity = 5, UnitPrice = 20 };

        var result = _validator.ValidateLineItem(lineItem);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Description");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidateLineItem_InvalidQuantity_ReturnsError(decimal quantity)
    {
        var lineItem = new LineItem { Description = "Service", Quantity = quantity, UnitPrice = 20 };

        var result = _validator.ValidateLineItem(lineItem);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void ValidateLineItem_NegativeUnitPrice_ReturnsError()
    {
        var lineItem = new LineItem { Description = "Service", Quantity = 5, UnitPrice = -10 };

        var result = _validator.ValidateLineItem(lineItem);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UnitPrice");
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(1.01)]
    public void ValidateLineItem_InvalidTaxRate_ReturnsError(decimal taxRate)
    {
        var lineItem = new LineItem { Description = "Service", Quantity = 5, UnitPrice = 20, TaxRate = taxRate };

        var result = _validator.ValidateLineItem(lineItem);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TaxRate");
    }

    #endregion

    #region InventoryItem Validation Tests

    [Fact]
    public void ValidateInventoryItem_ValidItem_ReturnsSuccess()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });

        var item = new InventoryItem
        {
            Id = "INV-ITM-001",
            ProductId = "PRD-001",
            LocationId = "LOC-001",
            InStock = 100,
            Reserved = 10,
            ReorderPoint = 20,
            UnitCost = 5.00m
        };

        var result = _validator.ValidateInventoryItem(item);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateInventoryItem_EmptyProductId_ReturnsError()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var item = new InventoryItem { ProductId = "", LocationId = "LOC-001" };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void ValidateInventoryItem_NonExistentProduct_ReturnsError()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var item = new InventoryItem { ProductId = "PRD-999", LocationId = "LOC-001" };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ProductId");
    }

    [Fact]
    public void ValidateInventoryItem_EmptyLocationId_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });
        var item = new InventoryItem { ProductId = "PRD-001", LocationId = "" };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LocationId");
    }

    [Fact]
    public void ValidateInventoryItem_NonExistentLocation_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });
        var item = new InventoryItem { ProductId = "PRD-001", LocationId = "LOC-999" };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "LocationId");
    }

    [Fact]
    public void ValidateInventoryItem_NegativeInStock_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var item = new InventoryItem { ProductId = "PRD-001", LocationId = "LOC-001", InStock = -10 };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "InStock");
    }

    [Fact]
    public void ValidateInventoryItem_NegativeReserved_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var item = new InventoryItem { ProductId = "PRD-001", LocationId = "LOC-001", InStock = 100, Reserved = -5 };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Reserved");
    }

    [Fact]
    public void ValidateInventoryItem_ReservedExceedsInStock_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var item = new InventoryItem { ProductId = "PRD-001", LocationId = "LOC-001", InStock = 10, Reserved = 20 };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Reserved");
    }

    [Fact]
    public void ValidateInventoryItem_NegativeReorderPoint_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var item = new InventoryItem { ProductId = "PRD-001", LocationId = "LOC-001", ReorderPoint = -10 };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "ReorderPoint");
    }

    [Fact]
    public void ValidateInventoryItem_NegativeUnitCost_ReturnsError()
    {
        _companyData.Products.Add(new Product { Id = "PRD-001", Name = "Widget" });
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var item = new InventoryItem { ProductId = "PRD-001", LocationId = "LOC-001", UnitCost = -5.00m };

        var result = _validator.ValidateInventoryItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "UnitCost");
    }

    #endregion

    #region StockTransfer Validation Tests

    [Fact]
    public void ValidateStockTransfer_ValidTransfer_ReturnsSuccess()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        _companyData.Locations.Add(new Location { Id = "LOC-002", Name = "Warehouse B" });

        var transfer = new StockTransfer
        {
            Id = "TRF-001",
            SourceLocationId = "LOC-001",
            DestinationLocationId = "LOC-002",
            Quantity = 50
        };

        var result = _validator.ValidateStockTransfer(transfer);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateStockTransfer_EmptySourceLocation_ReturnsError()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-002", Name = "Warehouse B" });
        var transfer = new StockTransfer { SourceLocationId = "", DestinationLocationId = "LOC-002", Quantity = 50 };

        var result = _validator.ValidateStockTransfer(transfer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SourceLocationId");
    }

    [Fact]
    public void ValidateStockTransfer_NonExistentSourceLocation_ReturnsError()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-002", Name = "Warehouse B" });
        var transfer = new StockTransfer
        {
            SourceLocationId = "LOC-999",
            DestinationLocationId = "LOC-002",
            Quantity = 50
        };

        var result = _validator.ValidateStockTransfer(transfer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SourceLocationId");
    }

    [Fact]
    public void ValidateStockTransfer_EmptyDestinationLocation_ReturnsError()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var transfer = new StockTransfer { SourceLocationId = "LOC-001", DestinationLocationId = "", Quantity = 50 };

        var result = _validator.ValidateStockTransfer(transfer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DestinationLocationId");
    }

    [Fact]
    public void ValidateStockTransfer_NonExistentDestinationLocation_ReturnsError()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var transfer = new StockTransfer
        {
            SourceLocationId = "LOC-001",
            DestinationLocationId = "LOC-999",
            Quantity = 50
        };

        var result = _validator.ValidateStockTransfer(transfer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DestinationLocationId");
    }

    [Fact]
    public void ValidateStockTransfer_SameSourceAndDestination_ReturnsError()
    {
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        var transfer = new StockTransfer
        {
            SourceLocationId = "LOC-001",
            DestinationLocationId = "LOC-001",
            Quantity = 50
        };

        var result = _validator.ValidateStockTransfer(transfer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DestinationLocationId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void ValidateStockTransfer_InvalidQuantity_ReturnsError(int quantity)
    {
        _companyData.Locations.Add(new Location { Id = "LOC-001", Name = "Warehouse A" });
        _companyData.Locations.Add(new Location { Id = "LOC-002", Name = "Warehouse B" });
        var transfer = new StockTransfer
        {
            SourceLocationId = "LOC-001",
            DestinationLocationId = "LOC-002",
            Quantity = quantity
        };

        var result = _validator.ValidateStockTransfer(transfer);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }

    #endregion

    #region RentalItem Validation Tests

    [Fact]
    public void ValidateRentalItem_ValidItem_ReturnsSuccess()
    {
        var item = new RentalItem
        {
            Id = "RNT-ITM-001",
            Name = "Projector",
            TotalQuantity = 10,
            DailyRate = 50,
            WeeklyRate = 200,
            MonthlyRate = 500,
            SecurityDeposit = 100
        };

        var result = _validator.ValidateRentalItem(item);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateRentalItem_EmptyName_ReturnsError()
    {
        var item = new RentalItem { Name = "" };

        var result = _validator.ValidateRentalItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Name");
    }

    [Fact]
    public void ValidateRentalItem_NegativeTotalQuantity_ReturnsError()
    {
        var item = new RentalItem { Name = "Projector", TotalQuantity = -5 };

        var result = _validator.ValidateRentalItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "TotalQuantity");
    }

    [Fact]
    public void ValidateRentalItem_NegativeDailyRate_ReturnsError()
    {
        var item = new RentalItem { Name = "Projector", DailyRate = -10 };

        var result = _validator.ValidateRentalItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DailyRate");
    }

    [Fact]
    public void ValidateRentalItem_NegativeWeeklyRate_ReturnsError()
    {
        var item = new RentalItem { Name = "Projector", WeeklyRate = -50 };

        var result = _validator.ValidateRentalItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "WeeklyRate");
    }

    [Fact]
    public void ValidateRentalItem_NegativeMonthlyRate_ReturnsError()
    {
        var item = new RentalItem { Name = "Projector", MonthlyRate = -100 };

        var result = _validator.ValidateRentalItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "MonthlyRate");
    }

    [Fact]
    public void ValidateRentalItem_NegativeSecurityDeposit_ReturnsError()
    {
        var item = new RentalItem { Name = "Projector", SecurityDeposit = -25 };

        var result = _validator.ValidateRentalItem(item);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "SecurityDeposit");
    }

    #endregion

    #region RentalRecord Validation Tests

    [Fact]
    public void ValidateRentalRecord_ValidRecord_ReturnsSuccess()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var record = new RentalRecord
        {
            Id = "RNT-001",
            RentalItemId = "RNT-ITM-001",
            CustomerId = "CUS-001",
            Quantity = 2,
            StartDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(7),
            RateAmount = 50
        };

        var result = _validator.ValidateRentalRecord(record);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateRentalRecord_EmptyRentalItemId_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var record = new RentalRecord { RentalItemId = "", CustomerId = "CUS-001", Quantity = 1 };

        var result = _validator.ValidateRentalRecord(record);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "RentalItemId");
    }

    [Fact]
    public void ValidateRentalRecord_EmptyCustomerId_ReturnsError()
    {
        var record = new RentalRecord { RentalItemId = "RNT-ITM-001", CustomerId = "", Quantity = 1 };

        var result = _validator.ValidateRentalRecord(record);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void ValidateRentalRecord_NonExistentCustomer_ReturnsError()
    {
        var record = new RentalRecord { RentalItemId = "RNT-ITM-001", CustomerId = "CUS-999", Quantity = 1 };

        var result = _validator.ValidateRentalRecord(record);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ValidateRentalRecord_InvalidQuantity_ReturnsError(int quantity)
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var record = new RentalRecord
        {
            RentalItemId = "RNT-ITM-001",
            CustomerId = "CUS-001",
            Quantity = quantity
        };

        var result = _validator.ValidateRentalRecord(record);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Quantity");
    }

    [Fact]
    public void ValidateRentalRecord_DueDateBeforeStartDate_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var record = new RentalRecord
        {
            RentalItemId = "RNT-ITM-001",
            CustomerId = "CUS-001",
            Quantity = 1,
            StartDate = DateTime.UtcNow,
            DueDate = DateTime.UtcNow.AddDays(-1)
        };

        var result = _validator.ValidateRentalRecord(record);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "DueDate");
    }

    [Fact]
    public void ValidateRentalRecord_NegativeRateAmount_ReturnsError()
    {
        _companyData.Customers.Add(new Customer { Id = "CUS-001", Name = "John Doe" });
        var record = new RentalRecord
        {
            RentalItemId = "RNT-ITM-001",
            CustomerId = "CUS-001",
            Quantity = 1,
            RateAmount = -50
        };

        var result = _validator.ValidateRentalRecord(record);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "RateAmount");
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
    public void ValidationResult_Merge_CombinesErrors()
    {
        var result1 = ValidationResult.Failure("Prop1", "Error 1");
        var result2 = ValidationResult.Failure("Prop2", "Error 2");

        result1.Merge(result2);

        Assert.Equal(2, result1.Errors.Count);
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

    #endregion
}
