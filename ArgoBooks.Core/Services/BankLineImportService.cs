using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.BankMatching;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

public class BankImportCreation
{
    public List<Transaction> CreatedTransactions { get; } = [];
    public List<object> CreatedEntities { get; } = [];
}

/// <summary>
/// Turns resolved unmatched bank lines into Expense/Revenue transactions, auto-creating any
/// new supplier/customer/category, and marks each line matched to its new transaction.
/// </summary>
public class BankLineImportService
{
    public BankImportCreation CreateFromLines(CompanyData data, IReadOnlyList<BankLineResolution> resolutions, bool linkToBankLine = true)
    {
        var creation = new BankImportCreation();

        // Bank statements carry no currency, so amounts are in the company's currency. (Previously
        // every imported transaction was hardcoded to USD, which mislabeled amounts for non-USD
        // companies.)
        var companyCurrency = string.IsNullOrWhiteSpace(data.Settings.Localization.Currency)
            ? "USD"
            : data.Settings.Localization.Currency;

        // Dedup caches so repeated rows reuse one created entity instead of making duplicates.
        var supplierCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var customerCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var categoryCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var productCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var r in resolutions)
        {
            var isExpense = r.Type == BookRecordType.Expense;

            // Resolve / create counterparty.
            string? counterpartyId = r.CounterpartyId;
            if (counterpartyId == null && !string.IsNullOrWhiteSpace(r.NewCounterpartyName))
            {
                counterpartyId = isExpense
                    ? ResolveSupplier(data, creation, supplierCache, r.NewCounterpartyName!)
                    : ResolveCustomer(data, creation, customerCache, r.NewCounterpartyName!);
            }

            // Resolve / create the product that carries the category for this transaction.
            string? productId = r.ProductId;
            if (productId == null && !string.IsNullOrWhiteSpace(r.NewProductName))
            {
                var type = isExpense ? CategoryType.Expense : CategoryType.Revenue;
                var categoryId = r.ProductCategoryId;
                // Don't propagate a category id that doesn't resolve to a real category (a deleted one, or
                // a bad value the caller/AI supplied - the model sometimes echoes a hallucinated id or a
                // name in this field). Fall back to resolving by name, or leave it unset, so the product
                // and its learned rule never carry a dangling category reference (which the Bank import
                // rules screen then shows with an empty category).
                if (!string.IsNullOrEmpty(categoryId) && data.GetCategory(categoryId) == null)
                    categoryId = null;
                if (categoryId == null && !string.IsNullOrWhiteSpace(r.NewProductCategoryName))
                    categoryId = ResolveCategory(data, creation, categoryCache, r.NewProductCategoryName!, type);

                productId = ResolveProduct(data, creation, productCache, r.NewProductName!, categoryId, type);
            }

            var draft = new TransactionDraft(
                Date: r.Line.Date,
                Description: r.Line.Description,
                Total: Math.Abs(r.Line.Amount),
                CounterpartyId: counterpartyId,
                Notes: "Imported from bank statement",
                OriginalCurrency: companyCurrency,
                ProductId: productId);

            Transaction tx = isExpense
                ? TransactionFactory.CreateExpense(data, draft)
                : TransactionFactory.CreateRevenue(data, draft);

            // Pass the amount straight through to the USD base (no FX conversion, no "Pending"):
            // a bank statement has no exchange-rate data, and for a non-USD company leaving these
            // unset would make USD-based analytics read 0. Mirrors the spreadsheet importer's
            // passthrough for rows that have no currency column.
            tx.TotalUSD = tx.Total;
            tx.UnitPriceUSD = tx.UnitPrice;

            if (linkToBankLine)
            {
                tx.BankMatched = true;
                tx.BankMatchedLineId = r.Line.Id;
                tx.BankMatchedDate = DateTime.UtcNow;
            }

            if (isExpense) data.Expenses.Add((Expense)tx);
            else data.Revenues.Add((Revenue)tx);

            if (linkToBankLine)
            {
                r.Line.MatchStatus = BankLineMatchStatus.Matched;
                r.Line.MatchedRecordType = r.Type;
                r.Line.MatchedRecordId = tx.Id;
                r.Line.MatchedDate = DateTime.UtcNow;
            }

            creation.CreatedTransactions.Add(tx);
        }

        data.MarkAsModified();
        return creation;
    }

    private static string ResolveSupplier(CompanyData data, BankImportCreation creation,
        Dictionary<string, string> cache, string name)
    {
        var key = name.Trim();
        if (cache.TryGetValue(key, out var cached)) return cached;

        var existing = data.Suppliers.FirstOrDefault(s => string.Equals(s.Name, key, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { cache[key] = existing.Id; return existing.Id; }

        data.IdCounters.Supplier++;
        var supplier = new Supplier { Id = $"SUP-{data.IdCounters.Supplier:D3}", Name = key };
        data.Suppliers.Add(supplier);
        creation.CreatedEntities.Add(supplier);
        cache[key] = supplier.Id;
        return supplier.Id;
    }

    private static string ResolveCustomer(CompanyData data, BankImportCreation creation,
        Dictionary<string, string> cache, string name)
    {
        var key = name.Trim();
        if (cache.TryGetValue(key, out var cached)) return cached;

        var existing = data.Customers.FirstOrDefault(c => string.Equals(c.Name, key, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { cache[key] = existing.Id; return existing.Id; }

        data.IdCounters.Customer++;
        var customer = new Customer { Id = $"CUS-{data.IdCounters.Customer:D3}", Name = key };
        data.Customers.Add(customer);
        creation.CreatedEntities.Add(customer);
        cache[key] = customer.Id;
        return customer.Id;
    }

    private static string ResolveCategory(CompanyData data, BankImportCreation creation,
        Dictionary<string, string> cache, string name, CategoryType type)
    {
        var key = $"{type}|{name.Trim()}";
        if (cache.TryGetValue(key, out var cached)) return cached;

        var existing = data.Categories.FirstOrDefault(c =>
            c.Type == type && string.Equals(c.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing != null) { cache[key] = existing.Id; return existing.Id; }

        data.IdCounters.Category++;
        var prefix = type == CategoryType.Expense ? "CAT-PUR" : "CAT-SAL";
        var category = new Category
        {
            Id = $"{prefix}-{data.IdCounters.Category:D3}",
            Name = name.Trim(),
            Type = type
        };
        data.Categories.Add(category);
        creation.CreatedEntities.Add(category);
        cache[key] = category.Id;
        return category.Id;
    }

    private static string ResolveProduct(CompanyData data, BankImportCreation creation,
        Dictionary<string, string> cache, string name, string? categoryId, CategoryType type)
    {
        var key = $"{type}|{name.Trim()}";
        if (cache.TryGetValue(key, out var cached)) return cached;

        var existing = data.Products.FirstOrDefault(p =>
            p.Type == type && string.Equals(p.Name, name.Trim(), StringComparison.OrdinalIgnoreCase));
        if (existing != null) { cache[key] = existing.Id; return existing.Id; }

        data.IdCounters.Product++;
        var product = new Product
        {
            Id = $"PRD-{data.IdCounters.Product:D3}",
            Name = name.Trim(),
            CategoryId = categoryId,
            Type = type,
            ItemType = "Product"
        };
        data.Products.Add(product);
        creation.CreatedEntities.Add(product);
        cache[key] = product.Id;
        return product.Id;
    }
}
