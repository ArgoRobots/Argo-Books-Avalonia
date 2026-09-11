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
    private static BulkScanItem ApprovedExpenseItem(decimal total, DateTime? date = null) => new()
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
            TransactionDate = date ?? new DateTime(2026, 3, 1),
            PaymentMethod = "Cash",
            LineItems = new List<ScannedLineItem>
            {
                new() { Description = "Item", Quantity = 1, UnitPrice = total }
            }
        }
    };

    // No rate source in the test run serves JPY, so a JPY receipt has no exact-date rate and is
    // saved pending (Calculations.md Rule 3a). Today avoids the historical-date retries.
    private void UseCurrencyWithNoRate() => Company.Settings.Localization.Currency = "JPY";

    private static void CreateSuggestedSupplier(ReceiptsModalsViewModel vm, string name)
    {
        vm.SuggestedSupplierName = name;
        vm.CreateSuggestedSupplierCommand.Execute(null);
    }

    private static void CreateSuggestedProduct(ReceiptsModalsViewModel vm, string name)
    {
        var line = new ScannedLineItemViewModel
        {
            Description = name, Quantity = "1", UnitPrice = "5", TotalPrice = "5",
            ShowCreateProductSuggestion = true, SuggestedProductName = name
        };
        vm.LineItems.Add(line);
        vm.CreateSuggestedProductCommand.Execute(line);
    }

    private static void FillSingleScan(ReceiptsModalsViewModel vm, bool isRevenue)
    {
        vm.IsRevenue = isRevenue;
        vm.ExtractedDate = new DateTimeOffset(DateTime.Today);
        vm.ExtractedSubtotal = "50";
        vm.ExtractedTotal = "50";
        if (!isRevenue)
            vm.SelectedSupplier = new SupplierOption { Id = "SUP-001", Name = "Acme" };
        vm.LineItems.Add(new ScannedLineItemViewModel
        {
            Description = "Item", Quantity = "1", UnitPrice = "50", TotalPrice = "50",
            SelectedProduct = new ProductOption { Id = "PRD-001", Name = "Item" }
        });
    }

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

    [Fact]
    public void CloseBulkReview_WithoutSaving_RemovesEverythingTheReviewCreated()
    {
        Company.Settings.Localization.Currency = "USD";
        var vm = new ReceiptsModalsViewModel();
        vm.BulkItems.Add(ApprovedExpenseItem(50m));
        CreateSuggestedSupplier(vm, "Acme");
        CreateSuggestedSupplier(vm, "Globex");
        CreateSuggestedProduct(vm, "Onions");

        vm.CloseBulkReviewCommand.Execute(null);

        Assert.Empty(Company.Suppliers);
        Assert.Empty(Company.Products);
        Assert.Empty(Company.Categories);
    }

    [Fact]
    public async Task CreateApprovedReceipts_UndoRemovesCreatedEntities_RedoRestoresThem()
    {
        Company.Settings.Localization.Currency = "USD";
        var vm = new ReceiptsModalsViewModel();
        var item = ApprovedExpenseItem(50m);
        vm.BulkItems.Add(item);
        CreateSuggestedSupplier(vm, "Acme");
        CreateSuggestedProduct(vm, "Onions");
        item.SelectedSupplierId = Company.Suppliers.Single().Id;
        item.LineItemProductIds = [Company.Products!.Single().Id];

        await vm.CreateAllApprovedTransactionsCommand.ExecuteAsync(null);
        Assert.Single(Company.Suppliers);
        Assert.Single(Company.Products);
        Assert.Single(Company.Categories);

        Undo();
        Assert.Empty(Company.Suppliers);
        Assert.Empty(Company.Products);
        Assert.Empty(Company.Categories);

        Redo();
        Assert.Single(Company.Suppliers);
        Assert.Single(Company.Products);
        Assert.Single(Company.Categories);
    }

    // Suggestions accepted while looking at a receipt that was then skipped aren't used by anything
    // saved, so saving leaves them out the same way closing without saving does.
    [Fact]
    public async Task CreateApprovedReceipts_LeavesOutWhatWasCreatedForSkippedReceipts()
    {
        Company.Settings.Localization.Currency = "USD";
        var vm = new ReceiptsModalsViewModel();
        var saved = ApprovedExpenseItem(50m);
        var skipped = ApprovedExpenseItem(20m);
        skipped.IsApproved = false;
        vm.BulkItems.Add(saved);
        vm.BulkItems.Add(skipped);

        CreateSuggestedSupplier(vm, "Acme");
        saved.SelectedSupplierId = Company.Suppliers.Single().Id;
        CreateSuggestedSupplier(vm, "Globex");
        CreateSuggestedProduct(vm, "Onions");

        await vm.CreateAllApprovedTransactionsCommand.ExecuteAsync(null);

        Assert.Equal("Acme", Company.Suppliers.Single().Name);
        Assert.Empty(Company.Products);
        Assert.Empty(Company.Categories);

        Undo();
        Assert.Empty(Company.Suppliers);

        Redo();
        Assert.Equal("Acme", Company.Suppliers.Single().Name);
        Assert.Empty(Company.Products);
    }

    // Scanned files often share a name (Scan.jpg, IMG_0001.jpg). Keyed by name, the second file's
    // preview overwrote the first one's.
    [Fact]
    public async Task ScanPreview_TwoFilesWithTheSameName_EachKeepsItsOwnPreview()
    {
        var sharedName = $"Scan_{Guid.NewGuid():N}.jpg";

        var first = Assert.Single(await ReceiptsModalsViewModel.RenderPreviewPagesAsync([1, 1, 1], sharedName, isPdf: false, "ScanPreview"));
        var second = Assert.Single(await ReceiptsModalsViewModel.RenderPreviewPagesAsync([2, 2, 2], sharedName, isPdf: false, "ScanPreview"));

        Assert.Equal(new byte[] { 1, 1, 1 }, await File.ReadAllBytesAsync(first));
        Assert.Equal(new byte[] { 2, 2, 2 }, await File.ReadAllBytesAsync(second));
    }

    [Fact]
    public async Task CreateApprovedReceipts_Pending_UndoDropsQueueEntry_RedoRestoresIt()
    {
        UseCurrencyWithNoRate();
        var vm = new ReceiptsModalsViewModel();
        vm.BulkItems.Add(ApprovedExpenseItem(50m, DateTime.Today));
        await vm.CreateAllApprovedTransactionsCommand.ExecuteAsync(null);
        var expense = Company.Expenses.Single();
        Assert.True(expense.IsPendingConversion);
        Assert.Equal(expense.Id, Assert.Single(Company.PendingConversions).TransactionId);

        Undo();
        Assert.Empty(Company.PendingConversions);

        Redo();
        Assert.Equal(expense.Id, Assert.Single(Company.PendingConversions).TransactionId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SingleScan_Pending_UndoDropsQueueEntry_RedoRestoresIt(bool isRevenue)
    {
        UseCurrencyWithNoRate();
        var vm = new ReceiptsModalsViewModel();
        FillSingleScan(vm, isRevenue);

        await vm.CreateTransactionCommand.ExecuteAsync(null);
        var id = isRevenue ? Company.Revenues.Single().Id : Company.Expenses.Single().Id;
        Assert.Equal(id, Assert.Single(Company.PendingConversions).TransactionId);

        Undo();
        Assert.Empty(Company.PendingConversions);

        Redo();
        Assert.Equal(id, Assert.Single(Company.PendingConversions).TransactionId);
    }
}
