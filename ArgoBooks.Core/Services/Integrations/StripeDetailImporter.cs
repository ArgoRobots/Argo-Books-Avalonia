using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services.Integrations;

public record StripeDetailResult(int RevenuesCreated, int ExpensesCreated, int Returns);

/// <summary>
/// Imports Stripe charges as detailed Revenue records (product, customer, tax,
/// discount), auto-creating the customer, product, and a "Stripe" category, and a
/// processing-fee expense. Constructs records directly because the flat
/// TransactionFactory can't carry tax/discount/line items.
/// </summary>
public class StripeDetailImporter
{
    private readonly Dictionary<string, string> _customerCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _productCache = new(StringComparer.OrdinalIgnoreCase);
    private string? _stripeCategoryId;

    public StripeDetailResult ImportCharges(CompanyData data, IReadOnlyList<StripeChargeDetail> charges)
    {
        int revs = 0, exps = 0;
        foreach (var ch in charges)
        {
            var customerId = ResolveCustomer(data, ch);
            var productId = ResolveProduct(data, ch.ProductName);

            var gross = ch.GrossCents / 100m;
            var tax = ch.TaxCents / 100m;
            var discount = ch.DiscountCents / 100m;
            var subtotal = gross - tax;
            var taxRate = subtotal > 0 ? tax / subtotal : 0m;
            var date = DateTimeOffset.FromUnixTimeSeconds(ch.CreatedUnix).LocalDateTime;
            var currency = string.IsNullOrWhiteSpace(ch.Currency) ? "USD" : ch.Currency.ToUpperInvariant();

            data.IdCounters.Revenue++;
            var rev = new Revenue
            {
                Id = $"REV-{date:yyyy}-{data.IdCounters.Revenue:D5}",
                Date = date,
                Description = ch.ProductName,
                CustomerId = customerId ?? string.Empty,
                Quantity = 1,
                UnitPrice = subtotal,
                Amount = subtotal,
                Subtotal = subtotal,
                TaxRate = taxRate,
                TaxAmount = tax,
                Discount = discount,
                Total = gross,
                ReferenceNumber = ch.ChargeId,
                Notes = "Imported from Stripe",
                OriginalCurrency = currency,
                PaymentStatus = RevenuePaymentStatus.Paid,
                LineItems =
                [
                    new LineItem
                    {
                        ProductId = productId,
                        Description = ch.ProductName,
                        Quantity = 1,
                        UnitPrice = subtotal,
                        TaxRate = taxRate,
                        Discount = discount
                    }
                ]
            };
            rev.TotalUSD = rev.Total;
            rev.UnitPriceUSD = rev.UnitPrice;
            rev.TaxAmountUSD = rev.TaxAmount;
            rev.DiscountUSD = rev.Discount;
            data.Revenues.Add(rev);
            revs++;

            if (ch.FeeCents > 0)
            {
                data.IdCounters.Expense++;
                var feeAmount = ch.FeeCents / 100m;
                var fee = new Expense
                {
                    Id = $"PUR-{date:yyyy}-{data.IdCounters.Expense:D5}",
                    Date = date,
                    Description = "Stripe fees",
                    Quantity = 1,
                    UnitPrice = feeAmount,
                    Amount = feeAmount,
                    Total = feeAmount,
                    ReferenceNumber = ch.ChargeId,
                    Notes = "Imported from Stripe",
                    OriginalCurrency = currency
                };
                fee.TotalUSD = fee.Total;
                fee.UnitPriceUSD = fee.UnitPrice;
                data.Expenses.Add(fee);
                exps++;
            }
        }

        if (revs > 0 || exps > 0) data.MarkAsModified();
        return new StripeDetailResult(revs, exps, 0);
    }

    private string? ResolveCustomer(CompanyData data, StripeChargeDetail ch)
    {
        var key = ch.CustomerEmail ?? ch.CustomerName;
        if (string.IsNullOrWhiteSpace(key)) return null;
        if (_customerCache.TryGetValue(key, out var cached)) return cached;

        var existing = data.Customers.FirstOrDefault(c =>
            (!string.IsNullOrEmpty(ch.CustomerEmail) && string.Equals(c.Email, ch.CustomerEmail, StringComparison.OrdinalIgnoreCase))
            || string.Equals(c.Name, ch.CustomerName, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { _customerCache[key] = existing.Id; return existing.Id; }

        data.IdCounters.Customer++;
        var customer = new Customer
        {
            Id = $"CUS-{data.IdCounters.Customer:D3}",
            Name = string.IsNullOrWhiteSpace(ch.CustomerName) ? (ch.CustomerEmail ?? "Stripe customer") : ch.CustomerName!,
            Email = ch.CustomerEmail ?? string.Empty
        };
        data.Customers.Add(customer);
        _customerCache[key] = customer.Id;
        return customer.Id;
    }

    private string ResolveProduct(CompanyData data, string name)
    {
        if (_productCache.TryGetValue(name, out var cached)) return cached;
        var existing = data.Products.FirstOrDefault(p =>
            p.Type == CategoryType.Revenue && string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null) { _productCache[name] = existing.Id; return existing.Id; }

        var categoryId = ResolveStripeCategory(data);
        data.IdCounters.Product++;
        var product = new Product
        {
            Id = $"PRD-{data.IdCounters.Product:D3}",
            Name = name,
            CategoryId = categoryId,
            Type = CategoryType.Revenue,
            ItemType = "Service"
        };
        data.Products.Add(product);
        _productCache[name] = product.Id;
        return product.Id;
    }

    private string ResolveStripeCategory(CompanyData data)
    {
        if (_stripeCategoryId != null) return _stripeCategoryId;
        var existing = data.Categories.FirstOrDefault(c =>
            c.Type == CategoryType.Revenue && string.Equals(c.Name, "Stripe", StringComparison.OrdinalIgnoreCase));
        if (existing != null) { _stripeCategoryId = existing.Id; return existing.Id; }

        data.IdCounters.Category++;
        var cat = new Category { Id = $"CAT-SAL-{data.IdCounters.Category:D3}", Name = "Stripe", Type = CategoryType.Revenue };
        data.Categories.Add(cat);
        _stripeCategoryId = cat.Id;
        return cat.Id;
    }
}
