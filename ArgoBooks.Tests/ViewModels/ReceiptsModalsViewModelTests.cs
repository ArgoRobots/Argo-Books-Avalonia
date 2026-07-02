using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArgoBooks.Core.Services;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the real ReceiptsModalsViewModel bulk-create flow: an approved scanned receipt becomes an
/// expense plus a receipt row, and undo/redo add and remove both together.
/// </summary>
public class ReceiptsModalsViewModelTests : ModalViewModelTestBase
{
    private static BulkScanItem ApprovedExpenseItem(decimal total) => new()
    {
        IsApproved = true,
        IsRevenueOverride = false,
        LineItemProductIds = new List<string?> { null },
        ScanResult = new ReceiptScanResult
        {
            TotalAmount = total,
            Subtotal = total,
            TaxAmount = 0m,
            SupplierName = "Acme",
            TransactionDate = new DateTime(2026, 3, 1),
            PaymentMethod = "Cash",
            LineItems = new List<ScannedLineItem>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = total }
            }
        }
    };

    [Fact]
    public async Task CreateApprovedReceipts_CreatesExpenseAndReceipt()
    {
        Company.Settings.Localization.Currency = "USD";
        var vm = new ReceiptsModalsViewModel();
        vm.BulkItems.Add(ApprovedExpenseItem(50m));

        await vm.CreateAllApprovedTransactionsCommand.ExecuteAsync(null);

        Assert.Equal(50m, Company.Expenses.Single().Total);
        Assert.Single(Company.Receipts);
    }

    [Fact]
    public async Task CreateApprovedReceipts_UndoThenRedo_RestoresExpenseAndReceipt()
    {
        Company.Settings.Localization.Currency = "USD";
        var vm = new ReceiptsModalsViewModel();
        vm.BulkItems.Add(ApprovedExpenseItem(50m));
        await vm.CreateAllApprovedTransactionsCommand.ExecuteAsync(null);

        Undo();
        Assert.Empty(Company.Expenses);
        Assert.Empty(Company.Receipts);

        Redo();
        Assert.Equal(50m, Company.Expenses.Single().Total);
        Assert.Single(Company.Receipts);
    }
}
