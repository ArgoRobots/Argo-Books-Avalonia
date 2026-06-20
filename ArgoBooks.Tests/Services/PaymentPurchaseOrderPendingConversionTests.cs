using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Inventory;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Payments and purchase orders follow the same exact-date pending-conversion rule as Revenue/Expense
/// (Calculations.md Rule 3a): a row whose exact-date rate is unavailable imports pending and is healed
/// later by <see cref="PendingConversionService"/>, never converted at a wrong-date rate. While pending
/// their Effective*USD report 0 (Calculations.md §3) so they don't contaminate USD aggregates.
/// </summary>
public class PaymentPurchaseOrderPendingConversionTests
{
    // USD->EUR = 0.90, so EUR->USD = 1/0.90 and 100 EUR -> 111.11 USD.
    private const decimal UsdToEur = 0.90m;
    private static decimal EurToUsd(decimal eur) => Math.Round(eur * (1m / UsdToEur), 2);

    [Fact]
    public async Task Process_PendingPayment_ExactRateAvailable_ConvertsAndClears()
    {
        var past = DateTime.Today.AddMonths(-2);
        var ex = new ExchangeRateService(new MockPlatform(), new HttpClient(new AlwaysEurHandler(UsdToEur)));
        await ex.GetExchangeRateAsync("USD", "EUR", past); // cache the exact-date rate

        var data = new CompanyData();
        var payment = new Payment
        {
            Id = "PAY-1",
            Amount = 100m,
            OriginalCurrency = "EUR",
            Date = past,
            IsPendingConversion = true,
            AmountUSD = 0m
        };
        data.Payments.Add(payment);

        var svc = new PendingConversionService(new MockPlatform(), exchangeRateService: ex);
        await svc.AddPendingConversionAsync(new PendingConversion
        {
            TransactionId = "PAY-1",
            TransactionType = "Payment",
            Total = 100m,
            OriginalCurrency = "EUR",
            TransactionDate = past
        });

        await svc.ProcessPendingConversionsAsync(data);

        Assert.False(payment.IsPendingConversion);
        Assert.Equal(EurToUsd(100m), payment.AmountUSD);
        Assert.Equal(EurToUsd(100m), payment.EffectiveAmountUSD); // now visible to aggregations
        Assert.Equal(0, svc.PendingCount); // entry left the queue
    }

    [Fact]
    public async Task Process_PendingPurchaseOrder_ExactRateAvailable_ConvertsAndClears()
    {
        var past = DateTime.Today.AddMonths(-2);
        var ex = new ExchangeRateService(new MockPlatform(), new HttpClient(new AlwaysEurHandler(UsdToEur)));
        await ex.GetExchangeRateAsync("USD", "EUR", past);

        var data = new CompanyData();
        var po = new PurchaseOrder
        {
            Id = "PO-1",
            Total = 250m,
            OriginalCurrency = "EUR",
            OrderDate = past,
            IsPendingConversion = true,
            TotalUSD = 0m
        };
        data.PurchaseOrders.Add(po);

        var svc = new PendingConversionService(new MockPlatform(), exchangeRateService: ex);
        await svc.AddPendingConversionAsync(new PendingConversion
        {
            TransactionId = "PO-1",
            TransactionType = "PurchaseOrder",
            Total = 250m,
            OriginalCurrency = "EUR",
            TransactionDate = past
        });

        await svc.ProcessPendingConversionsAsync(data);

        Assert.False(po.IsPendingConversion);
        Assert.Equal(EurToUsd(250m), po.TotalUSD);
        Assert.Equal(EurToUsd(250m), po.EffectiveTotalUSD);
        Assert.Equal(0, svc.PendingCount);
    }

    [Fact]
    public async Task Process_PendingPayment_ExactRateUnavailable_StaysPending()
    {
        var past = DateTime.Today.AddMonths(-3);
        // Serves today's rate but fails any historical (date=...) request.
        var ex = new ExchangeRateService(new MockPlatform(), new HttpClient(new TodayOnlyEurHandler(UsdToEur)));
        await ex.GetExchangeRateAsync("USD", "EUR", DateTime.Today); // cache today only

        var data = new CompanyData();
        var payment = new Payment
        {
            Id = "PAY-2",
            Amount = 100m,
            OriginalCurrency = "EUR",
            Date = past,
            IsPendingConversion = true,
            AmountUSD = 0m
        };
        data.Payments.Add(payment);

        var svc = new PendingConversionService(new MockPlatform(), exchangeRateService: ex);
        await svc.AddPendingConversionAsync(new PendingConversion
        {
            TransactionId = "PAY-2",
            TransactionType = "Payment",
            Total = 100m,
            OriginalCurrency = "EUR",
            TransactionDate = past
        });

        await svc.ProcessPendingConversionsAsync(data);

        Assert.True(payment.IsPendingConversion); // never converted at today's wrong-date rate
        Assert.Equal(0m, payment.AmountUSD);
        Assert.Equal(0m, payment.EffectiveAmountUSD);
        Assert.Equal(1, svc.PendingCount); // stays queued for a later attempt
    }

    [Fact]
    public async Task Reconcile_DropsEntryForHealedPayment()
    {
        var past = DateTime.Today.AddMonths(-1);
        var data = new CompanyData();
        // Payment already converted (no longer pending).
        data.Payments.Add(new Payment
        {
            Id = "PAY-9",
            Amount = 50m,
            OriginalCurrency = "EUR",
            Date = past,
            AmountUSD = 55m,
            IsPendingConversion = false
        });
        data.PendingConversions.Add(new PendingConversion
        {
            TransactionId = "PAY-9",
            TransactionType = "Payment",
            Total = 50m,
            OriginalCurrency = "EUR",
            TransactionDate = past
        });

        var svc = new PendingConversionService(new MockPlatform());
        await svc.ReconcileWithCompanyDataAsync(data);

        Assert.Empty(data.PendingConversions);
        Assert.Equal(0, svc.PendingCount);
    }

    [Fact]
    public void Payment_EffectiveAmountUSD_IsZeroWhilePending()
    {
        var p = new Payment { Amount = 100m, OriginalCurrency = "EUR", AmountUSD = 0m, IsPendingConversion = true };
        Assert.Equal(0m, p.EffectiveAmountUSD);

        p.AmountUSD = 111.11m;
        p.IsPendingConversion = false;
        Assert.Equal(111.11m, p.EffectiveAmountUSD);
    }

    [Fact]
    public void PurchaseOrder_EffectiveTotalUSD_IsZeroWhilePending()
    {
        var po = new PurchaseOrder { Total = 100m, OriginalCurrency = "EUR", TotalUSD = 0m, IsPendingConversion = true };
        Assert.Equal(0m, po.EffectiveTotalUSD);

        po.TotalUSD = 111.11m;
        po.IsPendingConversion = false;
        Assert.Equal(111.11m, po.EffectiveTotalUSD);
    }

    #region Stubs

    private sealed class AlwaysEurHandler(decimal usdToEur) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var payload = $$"""{ "success": true, "base": "USD", "rates": { "EUR": {{usdToEur}} } }""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class TodayOnlyEurHandler(decimal usdToEur) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Historical requests carry a ?date=... query; fail those, succeed for "latest" (today).
            if (request.RequestUri?.Query.Contains("date=", StringComparison.OrdinalIgnoreCase) == true)
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

            var payload = $$"""{ "success": true, "base": "USD", "rates": { "EUR": {{usdToEur}} } }""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class MockPlatform : IPlatformService
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

    #endregion
}
