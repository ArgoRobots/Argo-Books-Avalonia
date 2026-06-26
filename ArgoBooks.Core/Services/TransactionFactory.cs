using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;

namespace ArgoBooks.Core.Services;

/// <summary>
/// Builds Expense/Revenue transactions from a normalized draft. Pure: the caller is
/// responsible for adding the result to CompanyData and recording undo. Shared by the
/// bank-statement importer (and, optionally, the receipt scanner).
/// </summary>
public record TransactionDraft(
    DateTime Date,
    string Description,
    decimal Total,
    string? CounterpartyId,
    string? Notes,
    string OriginalCurrency = "USD",
    string? ProductId = null);

public static class TransactionFactory
{
    public static Expense CreateExpense(CompanyData data, TransactionDraft draft)
    {
        data.IdCounters.Expense++;
        return new Expense
        {
            Id = $"PUR-{draft.Date:yyyy}-{data.IdCounters.Expense:D5}",
            Date = draft.Date,
            SupplierId = draft.CounterpartyId,
            Description = draft.Description,
            LineItems = [BuildLine(draft)],
            Quantity = 1,
            UnitPrice = draft.Total,
            Amount = draft.Total,
            Total = draft.Total,
            Notes = draft.Notes ?? string.Empty,
            OriginalCurrency = draft.OriginalCurrency,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    public static Revenue CreateRevenue(CompanyData data, TransactionDraft draft)
    {
        data.IdCounters.Revenue++;
        return new Revenue
        {
            Id = $"REV-{draft.Date:yyyy}-{data.IdCounters.Revenue:D5}",
            Date = draft.Date,
            CustomerId = draft.CounterpartyId,
            Description = draft.Description,
            LineItems = [BuildLine(draft)],
            Quantity = 1,
            UnitPrice = draft.Total,
            Amount = draft.Total,
            Subtotal = draft.Total,
            Total = draft.Total,
            PaymentStatus = RevenuePaymentStatus.Paid,
            Notes = draft.Notes ?? string.Empty,
            OriginalCurrency = draft.OriginalCurrency,
            CreatedAt = DateTime.Now,
            UpdatedAt = DateTime.Now
        };
    }

    private static LineItem BuildLine(TransactionDraft draft) => new()
    {
        ProductId = draft.ProductId,
        Description = draft.Description,
        Quantity = 1,
        UnitPrice = draft.Total
    };
}
