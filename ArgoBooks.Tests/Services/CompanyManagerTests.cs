using ArgoBooks.Core.Data;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Entities;
using ArgoBooks.Core.Models.Tracking;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the CompanyManager class.
/// </summary>
public class CompanyManagerTests : IDisposable
{
    private readonly CompanyManager _manager;

    public CompanyManagerTests()
    {
        var platformService = new MockPlatformService();
        var footerService = new FooterService();
        var compressionService = new CompressionService();
        var fileService = new FileService(compressionService, footerService);
        var settingsService = new GlobalSettingsService(platformService);
        _manager = new CompanyManager(fileService, settingsService, footerService);
    }

    public void Dispose()
    {
        _manager.Dispose();
    }

    #region VerifyCurrentPassword Tests

    [Fact]
    public void VerifyCurrentPassword_WhenNoCompanyOpen_ReturnsFalse()
    {
        Assert.False(_manager.VerifyCurrentPassword("test"));
    }

    [Fact]
    public void VerifyCurrentPassword_WhenNoCompanyOpen_WithNull_ReturnsFalse()
    {
        Assert.False(_manager.VerifyCurrentPassword(null));
    }

    #endregion

    #region PendingRename Tests

    [Fact]
    public void ClearPendingRename_ClearsPath()
    {
        _manager.SetPendingRename("/new/path.argo");
        _manager.ClearPendingRename();

        Assert.Null(_manager.PendingRenamePath);
    }

    [Fact]
    public void SetPendingRename_OverwritesPrevious()
    {
        _manager.SetPendingRename("/first/path.argo");
        _manager.SetPendingRename("/second/path.argo");

        Assert.Equal("/second/path.argo", _manager.PendingRenamePath);
    }

    #endregion

    #region Constructor Validation Tests

    [Fact]
    public void Constructor_NullFileService_Throws()
    {
        var platformService = new MockPlatformService();
        var footerService = new FooterService();
        var settingsService = new GlobalSettingsService(platformService);

        Assert.Throws<ArgumentNullException>(() =>
            new CompanyManager(null!, settingsService, footerService));
    }

    [Fact]
    public void Constructor_NullSettingsService_Throws()
    {
        var footerService = new FooterService();
        var compressionService = new CompressionService();
        var fileService = new FileService(compressionService, footerService);

        Assert.Throws<ArgumentNullException>(() =>
            new CompanyManager(fileService, null!, footerService));
    }

    [Fact]
    public void Constructor_NullFooterService_Throws()
    {
        var platformService = new MockPlatformService();
        var compressionService = new CompressionService();
        var footerService = new FooterService();
        var fileService = new FileService(compressionService, footerService);
        var settingsService = new GlobalSettingsService(platformService);

        Assert.Throws<ArgumentNullException>(() =>
            new CompanyManager(fileService, settingsService, null!));
    }

    #endregion

    #region HealInvoiceTotalsIfNeeded Tests

    private static CompanyData BuildCompanyWithDriftedInvoice(string? healedVersion)
    {
        return new CompanyData
        {
            Settings = new CompanySettings { InvoiceTotalsHealedVersion = healedVersion },
            Invoices =
            {
                new Invoice { Id = "INV-1", Total = 100m, OriginalCurrency = "USD" }
            },
            Payments =
            {
                new Payment { InvoiceId = "INV-1", Amount = 40m, OriginalCurrency = "USD" }
            }
        };
    }

    [Fact]
    public void HealInvoiceTotalsIfNeeded_NotYetHealed_RecalculatesAndStampsVersion()
    {
        var data = BuildCompanyWithDriftedInvoice(healedVersion: null);

        CompanyManager.HealInvoiceTotalsIfNeeded(data);

        Assert.Equal(40m, data.Invoices[0].AmountPaid);
        Assert.Equal(CompanyManager.InvoiceTotalsHealVersion, data.Settings.InvoiceTotalsHealedVersion);
    }

    [Fact]
    public void HealInvoiceTotalsIfNeeded_AlreadyHealed_SkipsRecalculation()
    {
        var data = BuildCompanyWithDriftedInvoice(healedVersion: CompanyManager.InvoiceTotalsHealVersion);

        CompanyManager.HealInvoiceTotalsIfNeeded(data);

        // Heal was skipped, so the (default) totals are left untouched.
        Assert.Equal(0m, data.Invoices[0].AmountPaid);
    }

    #endregion

    #region Deferred Receipts Round-Trip Tests

    [Fact]
    public async Task DeferredReceipts_SurviveSaveAndReopen()
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        try
        {
            await _manager.CreateCompanyAsync(filePath, "Receipts Co");
            _manager.CompanyData!.Receipts.Add(new Receipt { Id = "RCP-1", FileName = "r.jpg", FileData = "QUJD" });
            await _manager.SaveCompanyAsync();
            await _manager.CloseCompanyAsync();

            Assert.True(await _manager.OpenCompanyAsync(filePath));

            // Receipts load in the background; after ensuring, the persisted one is present.
            await _manager.EnsureReceiptsLoadedAsync();
            Assert.Single(_manager.CompanyData!.Receipts);
            Assert.Equal("RCP-1", _manager.CompanyData.Receipts[0].Id);
            Assert.Equal("QUJD", _manager.CompanyData.Receipts[0].FileData);
        }
        finally
        {
            await _manager.CloseCompanyAsync();
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public async Task SaveCompanyAs_ClearsPendingRename()
    {
        // A rename queued (deferred) before a Save As must not survive it: the rename target applied
        // to the OLD file path, so leaving it set makes a later normal Save move the just-saved-as
        // file to the rename target (or fail "file exists").
        var originalPath = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        var renameTarget = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        var saveAsPath = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        try
        {
            await _manager.CreateCompanyAsync(originalPath, "Acme");
            _manager.SetPendingRename(renameTarget); // deferred rename, no save yet
            Assert.Equal(renameTarget, _manager.PendingRenamePath);

            await _manager.SaveCompanyAsAsync(saveAsPath);

            Assert.Null(_manager.PendingRenamePath);
        }
        finally
        {
            await _manager.CloseCompanyAsync();
            foreach (var p in new[] { originalPath, renameTarget, saveAsPath })
                if (File.Exists(p)) File.Delete(p);
        }
    }

    [Fact]
    public async Task Save_ImmediatelyAfterOpen_DoesNotDropDeferredReceipts()
    {
        // The critical data-safety case: a save that runs before receipts finish loading
        // (e.g. the auto portal-sync save) must merge them first, not overwrite with empties.
        var filePath = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        try
        {
            await _manager.CreateCompanyAsync(filePath, "Receipts Co");
            _manager.CompanyData!.Receipts.Add(new Receipt { Id = "RCP-1", FileName = "r.jpg", FileData = "QUJD" });
            await _manager.SaveCompanyAsync();
            await _manager.CloseCompanyAsync();

            await _manager.OpenCompanyAsync(filePath);
            // Save right away, WITHOUT an explicit EnsureReceiptsLoadedAsync first.
            await _manager.SaveCompanyAsync();
            await _manager.CloseCompanyAsync();

            // Reopen: the receipt must still be there.
            await _manager.OpenCompanyAsync(filePath);
            await _manager.EnsureReceiptsLoadedAsync();
            Assert.Single(_manager.CompanyData!.Receipts);
            Assert.Equal("RCP-1", _manager.CompanyData.Receipts[0].Id);
        }
        finally
        {
            await _manager.CloseCompanyAsync();
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    #endregion

    #region ChangeCustomerId Cascade Tests

    [Fact]
    public async Task ChangeCustomerId_CascadesToRecurringScheduleTemplate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        try
        {
            await _manager.CreateCompanyAsync(path, "Acme");
            var data = _manager.CompanyData!;
            var customer = new Customer { Id = "CUST-1", Name = "Acme Corp" };
            data.Customers.Add(customer);
            data.RecurringInvoices.Add(new RecurringInvoice
            {
                Id = "REC-INV-00001",
                CustomerId = "CUST-1",
                // The generator clones this template per occurrence, so its CustomerId must follow a rename.
                Template = new Invoice { CustomerId = "CUST-1" }
            });

            _manager.ChangeCustomerId(customer, "CUST-2");

            var schedule = data.RecurringInvoices[0];
            Assert.Equal("CUST-2", schedule.CustomerId);
            Assert.Equal("CUST-2", schedule.Template!.CustomerId);
        }
        finally
        {
            await _manager.CloseCompanyAsync();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>
    /// A recurring transaction keeps the same kind of template as a recurring invoice, and every
    /// occurrence is cloned from it, so a counterparty left on the old id would be inherited by
    /// every entry generated from then on.
    /// </summary>
    [Fact]
    public async Task ChangeCustomerId_CascadesToRecurringTransactionTemplate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        try
        {
            await _manager.CreateCompanyAsync(path, "Acme");
            var data = _manager.CompanyData!;
            var customer = new Customer { Id = "CUST-1", Name = "Acme Corp" };
            data.Customers.Add(customer);
            data.RecurringTransactions.Add(new RecurringTransaction
            {
                Id = "REC-TXN-00001",
                Type = CategoryType.Revenue,
                RevenueTemplate = new Revenue { CustomerId = "CUST-1" }
            });

            _manager.ChangeCustomerId(customer, "CUST-2");

            Assert.Equal("CUST-2", data.RecurringTransactions[0].RevenueTemplate!.CustomerId);
        }
        finally
        {
            await _manager.CloseCompanyAsync();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task ChangeSupplierId_CascadesToRecurringTransactionTemplate()
    {
        var path = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        try
        {
            await _manager.CreateCompanyAsync(path, "Acme");
            var data = _manager.CompanyData!;
            var supplier = new Supplier { Id = "SUP-1", Name = "Landlord" };
            data.Suppliers.Add(supplier);
            data.RecurringTransactions.Add(new RecurringTransaction
            {
                Id = "REC-TXN-00002",
                Type = CategoryType.Expense,
                ExpenseTemplate = new Expense { SupplierId = "SUP-1" }
            });

            _manager.ChangeSupplierId(supplier, "SUP-2");

            Assert.Equal("SUP-2", data.RecurringTransactions[0].ExpenseTemplate!.SupplierId);
        }
        finally
        {
            await _manager.CloseCompanyAsync();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    /// <summary>Line items inside either kind of schedule's template follow a product rename too.</summary>
    [Fact]
    public async Task ChangeProductId_CascadesIntoBothScheduleTemplates()
    {
        var path = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        try
        {
            await _manager.CreateCompanyAsync(path, "Acme");
            var data = _manager.CompanyData!;
            var product = new Product { Id = "PRD-1", Name = "Rent" };
            data.Products.Add(product);
            data.RecurringInvoices.Add(new RecurringInvoice
            {
                Id = "REC-INV-00002",
                Template = new Invoice { LineItems = [new LineItem { ProductId = "PRD-1" }] }
            });
            data.RecurringTransactions.Add(new RecurringTransaction
            {
                Id = "REC-TXN-00003",
                Type = CategoryType.Expense,
                ExpenseTemplate = new Expense { LineItems = [new LineItem { ProductId = "PRD-1" }] }
            });

            _manager.ChangeProductId(product, "PRD-2");

            Assert.Equal("PRD-2", data.RecurringInvoices[0].Template!.LineItems[0].ProductId);
            Assert.Equal("PRD-2", data.RecurringTransactions[0].ExpenseTemplate!.LineItems[0].ProductId);
        }
        finally
        {
            await _manager.CloseCompanyAsync();
            if (File.Exists(path)) File.Delete(path);
        }
    }

    #endregion

    #region Cross-Instance Lock Tests

    private static CompanyManager NewManager()
    {
        var platformService = new MockPlatformService();
        var footerService = new FooterService();
        var compressionService = new CompressionService();
        var fileService = new FileService(compressionService, footerService);
        var settingsService = new GlobalSettingsService(platformService);
        return new CompanyManager(fileService, settingsService, footerService);
    }

    [Fact]
    public async Task CreateCompany_TargetOpenInAnotherInstance_Throws()
    {
        var sharedPath = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        var other = NewManager();
        try
        {
            // A second running instance holds the company open (and its cross-instance lock).
            await other.CreateCompanyAsync(sharedPath, "Held");

            // Creating (which overwrites) onto that same held path must be refused, not silently clobber it.
            await Assert.ThrowsAsync<CompanyAlreadyOpenException>(
                () => _manager.CreateCompanyAsync(sharedPath, "Intruder"));
        }
        finally
        {
            await other.CloseCompanyAsync();
            other.Dispose();
            await _manager.CloseCompanyAsync();
            if (File.Exists(sharedPath)) File.Delete(sharedPath);
        }
    }

    [Fact]
    public async Task SaveCompanyAs_TargetOpenInAnotherInstance_Throws()
    {
        var heldPath = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        var minePath = Path.Combine(Path.GetTempPath(), $"argo-cm-{Guid.NewGuid():N}.argo");
        var other = NewManager();
        try
        {
            await other.CreateCompanyAsync(heldPath, "Held");
            await _manager.CreateCompanyAsync(minePath, "Mine");

            // Save As onto a path another instance holds open must be refused.
            await Assert.ThrowsAsync<CompanyAlreadyOpenException>(
                () => _manager.SaveCompanyAsAsync(heldPath));

            // And our own company must be left pointing at its original file, untouched.
            Assert.Equal(minePath, _manager.CurrentFilePath);
        }
        finally
        {
            await other.CloseCompanyAsync();
            other.Dispose();
            await _manager.CloseCompanyAsync();
            foreach (var p in new[] { heldPath, minePath })
                if (File.Exists(p)) File.Delete(p);
        }
    }

    #endregion

    #region Mock Classes

    private class MockPlatformService : IPlatformService
    {
        public PlatformType Platform => PlatformType.Linux;
        public string GetAppDataPath() => Path.Combine(Path.GetTempPath(), "ArgoBooks_Test_" + Guid.NewGuid().ToString("N")[..8]);
        public string GetTempPath() => Path.GetTempPath();
        public string GetDefaultDocumentsPath() => Path.GetTempPath();
        public string GetLogsPath() => Path.GetTempPath();
        public string GetCachePath() => Path.GetTempPath();
        public void EnsureDirectoryExists(string path) => Directory.CreateDirectory(path);
        public bool SupportsFileSystem => true;
        public bool SupportsNativeDialogs => false;
        public bool SupportsBiometrics => false;
        public Task<bool> IsBiometricAvailableAsync() => Task.FromResult(false);
        public Task<string> GetBiometricAvailabilityDetailsAsync() => Task.FromResult("Not supported");
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

    #endregion
}
