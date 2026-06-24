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

        foreach (var r in resolutions)
        {
            var isExpense = r.Type == BookRecordType.Expense;

            // Resolve / create counterparty.
            string? counterpartyId = r.CounterpartyId;
            if (counterpartyId == null && !string.IsNullOrWhiteSpace(r.NewCounterpartyName))
            {
                if (isExpense)
                {
                    data.IdCounters.Supplier++;
                    var supplier = new Supplier { Id = $"SUP-{data.IdCounters.Supplier:D3}", Name = r.NewCounterpartyName!.Trim() };
                    data.Suppliers.Add(supplier);
                    creation.CreatedEntities.Add(supplier);
                    counterpartyId = supplier.Id;
                }
                else
                {
                    data.IdCounters.Customer++;
                    var customer = new Customer { Id = $"CUS-{data.IdCounters.Customer:D3}", Name = r.NewCounterpartyName!.Trim() };
                    data.Customers.Add(customer);
                    creation.CreatedEntities.Add(customer);
                    counterpartyId = customer.Id;
                }
            }

            // Resolve / create category (kept for the learned rule; transactions categorize via line item/category linkage elsewhere).
            if (r.CategoryId == null && !string.IsNullOrWhiteSpace(r.NewCategoryName))
            {
                data.IdCounters.Category++;
                var prefix = isExpense ? "CAT-PUR" : "CAT-SAL";
                var category = new Category
                {
                    Id = $"{prefix}-{data.IdCounters.Category:D3}",
                    Name = r.NewCategoryName!.Trim(),
                    Type = isExpense ? CategoryType.Expense : CategoryType.Revenue
                };
                data.Categories.Add(category);
                creation.CreatedEntities.Add(category);
            }

            var draft = new TransactionDraft(
                Date: r.Line.Date,
                Description: r.Line.Description,
                Total: Math.Abs(r.Line.Amount),
                CounterpartyId: counterpartyId,
                Notes: "Imported from bank statement");

            Transaction tx = isExpense
                ? TransactionFactory.CreateExpense(data, draft)
                : TransactionFactory.CreateRevenue(data, draft);

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
}
