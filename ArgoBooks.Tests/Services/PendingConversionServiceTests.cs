using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Common;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// A pending row converts ONLY at its exact transaction-date rate. When that date's rate cannot be
/// fetched (but today's can), it must stay pending rather than fall back to today's rate.
/// </summary>
public class PendingConversionServiceTests
{
    [Fact]
    public async Task Process_PastRow_ExactRateUnavailable_StaysPending_NotTodaysRate()
    {
        var today = DateTime.Today;
        var past = today.AddMonths(-3);

        // Handler serves today's "latest" rate but fails any historical (date=...) request.
        var ex = new ExchangeRateService(new MockPlatform(), new HttpClient(new TodayOnlyEurHandler(0.9m)));
        await ex.GetExchangeRateAsync("USD", "EUR", today); // cache today only

        var data = new CompanyData();
        var expense = new Expense
        {
            Id = "E1",
            Total = 100m,
            OriginalCurrency = "EUR",
            Date = past,
            IsPendingConversion = true,
            TotalUSD = 0m
        };
        data.Expenses.Add(expense);

        var svc = new PendingConversionService(new MockPlatform(), exchangeRateService: ex);
        await svc.AddPendingConversionAsync(new PendingConversion
        {
            TransactionId = "E1",
            TransactionType = "Expense",
            Total = 100m,
            OriginalCurrency = "EUR",
            TransactionDate = past
        });

        await svc.ProcessPendingConversionsAsync(data);

        Assert.True(expense.IsPendingConversion); // exact-date rate missing -> not converted at today's rate
        Assert.Equal(0m, expense.TotalUSD);
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
}
