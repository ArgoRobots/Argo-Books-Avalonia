using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using ArgoBooks.ViewModels;
using Xunit;

namespace ArgoBooks.Tests.ViewModels;

/// <summary>
/// Drives the recurring schedule editor's save against an in-memory company. Saving generates any
/// occurrence already due, so what it does to the schedule's dates, status and the conversion
/// queue decides which entries reach the books.
/// </summary>
public class RecurringScheduleEditorViewModelTests : ModalViewModelTestBase
{
    public RecurringScheduleEditorViewModelTests()
    {
        Company.Settings.Localization.Currency = "USD";
        Company.Suppliers.Add(new Supplier { Id = "SUP-001", Name = "Landlord" });
    }

    // No rate source in the test run serves JPY, so an entry in it is saved pending
    // (Calculations.md Rule 3a).
    private void UseCurrencyWithNoRate() => Company.Settings.Localization.Currency = "JPY";

    /// <summary>
    /// The conversion service's own queue, which a successful pass copies back over the company
    /// file's. Created with a platform that never writes the real queue file.
    /// </summary>
    private static PendingConversionService ConversionService() =>
        PendingConversionService.Instance ?? new PendingConversionService(new NoDiskPlatform());

    /// <summary>
    /// Reads the service's queue without a test hook: reconciling into an empty company copies the
    /// whole queue into it, since none of those rows exist there to have been converted.
    /// </summary>
    private static async Task<bool> ServiceHasQueued(string transactionId)
    {
        var probe = new CompanyData();
        await ConversionService().ReconcileWithCompanyDataAsync(probe);
        return probe.PendingConversions.Any(p => p.TransactionId == transactionId);
    }

    private RecurringScheduleEditorViewModel NewRentSchedule(DateTime start)
    {
        var vm = new RecurringScheduleEditorViewModel();
        vm.ShowNew(CategoryType.Expense);
        vm.Amount = "2000";
        vm.StartDate = new DateTimeOffset(start);
        vm.SelectedCounterparty = vm.CounterpartyOptions.Single();
        return vm;
    }

    [Fact]
    public async Task Save_EntryDueNowWithNoRate_IsQueuedWithTheConversionService()
    {
        UseCurrencyWithNoRate();
        ConversionService();
        var vm = NewRentSchedule(DateTime.Today);

        await vm.SaveCommand.ExecuteAsync(null);

        var entry = Assert.Single(Company.Expenses);
        Assert.True(entry.IsPendingConversion);
        Assert.True(await ServiceHasQueued(entry.Id), "Only the company file queued it, so the next conversion pass drops it");
    }

    [Fact]
    public async Task Save_UndoForgetsTheQueuedEntry_AndRedoQueuesItAgain()
    {
        UseCurrencyWithNoRate();
        ConversionService();
        var vm = NewRentSchedule(DateTime.Today);
        await vm.SaveCommand.ExecuteAsync(null);
        var entry = Assert.Single(Company.Expenses);

        Undo();
        var afterUndo = (Company.PendingConversions.Any(p => p.TransactionId == entry.Id), await ServiceHasQueued(entry.Id));
        Redo();
        var afterRedo = (Company.PendingConversions.Any(p => p.TransactionId == entry.Id), await ServiceHasQueued(entry.Id));

        Assert.Equal((false, false), afterUndo);
        Assert.Equal((true, true), afterRedo);
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
