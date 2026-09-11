using System.Net;
using System.Reflection;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Editing an expense or revenue entered in another currency, or still waiting for its rate.
/// </summary>
public class TransactionEditCurrencyTests : ModalViewModelTestBase
{
    private static readonly PropertyInfo RatesInstance =
        typeof(ExchangeRateService).GetProperty(nameof(ExchangeRateService.Instance), BindingFlags.Public | BindingFlags.Static)!;

    /// <summary>
    /// A row keeps one USD unit price, the average of its lines. Loading every line at that average
    /// turned a 1,000 laptop and ten 10 mice into eleven lines of 505, five times the real total.
    /// </summary>
    [Fact]
    public async Task EditingARowEnteredInAnotherCurrency_KeepsEachLinesOwnPrice()
    {
        UseNoExchangeRates();
        Company.Settings.Localization.Currency = "USD";
        Company.Products.Add(new Product { Id = "P1", Name = "Laptop" });
        Company.Products.Add(new Product { Id = "P2", Name = "Mouse" });
        // Entered in CAD at 0.5 USD per CAD, before the company moved to USD.
        Company.Expenses.Add(new Expense
        {
            Id = "PUR-2026-00001",
            Date = new DateTime(2026, 3, 1),
            OriginalCurrency = "CAD",
            LineItems =
            [
                new LineItem { ProductId = "P1", Description = "Laptop", Quantity = 1, UnitPrice = 1000m },
                new LineItem { ProductId = "P2", Description = "Mouse", Quantity = 10, UnitPrice = 10m }
            ],
            Quantity = 11,
            UnitPrice = 505m,
            Amount = 1100m,
            Total = 1100m,
            TotalUSD = 550m,
            UnitPriceUSD = 252.5m
        });

        var vm = new ExpenseModalsViewModel();
        vm.OpenEditModal(new ExpenseDisplayItem { Id = "PUR-2026-00001" });
        await vm.SaveExpenseCommand.ExecuteAsync(null);

        var expense = Company.Expenses.Single();
        Assert.Equal(550m, expense.Total);
        Assert.Equal([500m, 5m], expense.LineItems.Select(li => li.UnitPrice));
    }

    /// <summary>
    /// Undo put the row back but left the edited amount queued, and the queued amount is what the
    /// conversion uses, so the restored row later converted at the edited figure.
    /// </summary>
    [Fact]
    public async Task UndoingAnEditToAPendingExpense_QueuesItsOriginalAmountAgain()
    {
        var service = UseCurrencyWithNoRate();
        var vm = new ExpenseModalsViewModel();
        vm.OpenAddModal();
        FillLine(vm.LineItems.First(), vm.ProductOptions, 100m);
        await vm.SaveExpenseCommand.ExecuteAsync(null);
        var id = Company.Expenses.Single().Id;

        vm.OpenEditModal(new ExpenseDisplayItem { Id = id });
        vm.LineItems.First().UnitPrice = 250m;
        await vm.SaveExpenseCommand.ExecuteAsync(null);

        Undo();
        var afterUndo = (QueuedTotal(id), await QueuedTotal(service, id));
        Redo();
        var afterRedo = (QueuedTotal(id), await QueuedTotal(service, id));

        Assert.Equal((100m, 100m), afterUndo);
        Assert.Equal((250m, 250m), afterRedo);
    }

    [Fact]
    public async Task UndoingAnEditToAPendingRevenue_QueuesItsOriginalAmountAgain()
    {
        var service = UseCurrencyWithNoRate();
        var vm = new RevenueModalsViewModel();
        vm.OpenAddModal();
        FillLine(vm.LineItems.First(), vm.ProductOptions, 100m);
        await vm.SaveRevenueCommand.ExecuteAsync(null);
        var id = Company.Revenues.Single().Id;

        vm.OpenEditModal(new RevenueDisplayItem { Id = id });
        vm.LineItems.First().UnitPrice = 250m;
        await vm.SaveRevenueCommand.ExecuteAsync(null);

        Undo();
        var afterUndo = (QueuedTotal(id), await QueuedTotal(service, id));
        Redo();
        var afterRedo = (QueuedTotal(id), await QueuedTotal(service, id));

        Assert.Equal((100m, 100m), afterUndo);
        Assert.Equal((250m, 250m), afterRedo);
    }

    /// <summary>
    /// A EUR company whose rate lookups all come back empty, so every save waits for its rate.
    /// Returns the conversion service the view models queue with.
    /// </summary>
    private PendingConversionService UseCurrencyWithNoRate()
    {
        Company.Settings.Localization.Currency = "EUR";
        Company.Products.Add(new Product { Id = "P1", Name = "Widget" });
        UseNoExchangeRates(); // puts the shared rate service back after the test
        RatesInstance.SetValue(null, new ExchangeRateService(new NoDiskPlatform(), new HttpClient(new NoRatesHandler())));
        return PendingConversionService.Instance ?? new PendingConversionService(new NoDiskPlatform());
    }

    private static void FillLine(TransactionLineItemBase line, IEnumerable<ProductOption> options, decimal unitPrice)
    {
        line.SelectedProduct = options.First(p => p.Id == "P1");
        line.Quantity = 1;
        line.UnitPrice = unitPrice;
    }

    private decimal? QueuedTotal(string id) =>
        Company.PendingConversions.SingleOrDefault(p => p.TransactionId == id)?.Total;

    /// <summary>
    /// The service's own copy of the queue: reconciling into an empty company copies all of it
    /// across, since none of those rows exist there to have been converted.
    /// </summary>
    private static async Task<decimal?> QueuedTotal(PendingConversionService service, string id)
    {
        var probe = new CompanyData();
        await service.ReconcileWithCompanyDataAsync(probe);
        return probe.PendingConversions.SingleOrDefault(p => p.TransactionId == id)?.Total;
    }

    private sealed class NoRatesHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{ "success": true, "base": "USD", "rates": {} }""", Encoding.UTF8, "application/json")
            });
    }

    private sealed class NoDiskPlatform : IPlatformService
    {
        public PlatformType Platform => PlatformType.Linux;
        public string GetAppDataPath() => Path.GetTempPath();
        public string GetTempPath() => Path.GetTempPath();
        public string GetDefaultDocumentsPath() => Path.GetTempPath();
        public string GetLogsPath() => Path.GetTempPath();
        public string GetCachePath() => Path.GetTempPath();
        public void EnsureDirectoryExists(string path) { }
        public bool SupportsFileSystem => false;
        public bool SupportsNativeDialogs => false;
        public bool SupportsBiometrics => false;
        public Task<bool> IsBiometricAvailableAsync() => Task.FromResult(false);
        public Task<string> GetBiometricAvailabilityDetailsAsync() => Task.FromResult("");
        public Task<bool> AuthenticateWithBiometricAsync(string reason) => Task.FromResult(false);
        public void StorePasswordForBiometric(string fileId, string password) { }
        public string? GetPasswordForBiometric(string fileId) => null;
        public void ClearPasswordForBiometric(string fileId) { }
        public bool SupportsAutoUpdate => false;
        public int MaxRecentCompanies => 10;
        public string NormalizePath(string path) => path;
        public string CombinePaths(params string[] paths) => Path.Combine(paths);
        public string GetMachineId() => "test-machine-id";
        public void RegisterFileTypeAssociations(string iconPath) { }
        public StringComparer PathComparer => StringComparer.Ordinal;
    }
}
