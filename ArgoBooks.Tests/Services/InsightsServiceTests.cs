using System.Globalization;
using System.Net;
using System.Reflection;
using System.Text;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Insights;
using ArgoBooks.Core.Models.Transactions;
using ArgoBooks.Core.Platform;
using ArgoBooks.Core.Services;
using Xunit;

namespace ArgoBooks.Tests.Services;

/// <summary>
/// Tests for the InsightsService class. The display-currency tests install a freshly seeded
/// <see cref="ExchangeRateService.Instance"/> (and restore the prior one), sharing the
/// ExchangeRateSingleton collection so they don't race other singleton-mutating tests.
/// </summary>
[Collection("ExchangeRateSingleton")]
public class InsightsServiceTests
{
    private static readonly DateTime Date1 = new(2024, 3, 10);
    private static readonly DateTime Date2 = new(2024, 9, 20);

    #region GenerateInsightsAsync Tests

    [Fact]
    public async Task GenerateInsightsAsync_EmptyCompanyData_ReturnsInsufficientData()
    {
        var service = new InsightsService();
        var companyData = new CompanyData();
        var dateRange = new AnalysisDateRange
        {
            StartDate = DateTime.Now.AddMonths(-12),
            EndDate = DateTime.Now
        };

        var result = await service.GenerateInsightsAsync(companyData, dateRange);

        Assert.NotNull(result);
        Assert.False(result.HasSufficientData);
        Assert.NotNull(result.InsufficientDataMessage);
    }

    #endregion

    #region FormatCurrency / display currency

    [Fact]
    public void FormatCurrency_UsdRun_IsCultureIndependent()
    {
        // A fresh service defaults to a USD run, so amounts stay in USD. The format (symbol + N0,
        // InvariantCulture) must not depend on the machine locale.
        var service = new InsightsService();
        var method = FormatCurrencyMethod();

        string Run(string culture)
        {
            string result = null!;
            var thread = new Thread(() =>
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                result = (string)method.Invoke(service, [1234m])!;
            });
            thread.Start();
            thread.Join();
            return result;
        }

        Assert.Equal("$1,234", Run("en-US"));
        Assert.Equal(Run("en-US"), Run("de-DE"));
    }

    [Fact]
    public async Task DisplayCurrency_NonUsdCompanyWithRates_ConvertsAtTransactionDateAndUsesSymbol()
    {
        // A EUR company with the exact-date USD->EUR rate cached for every conversion date resolves to
        // EUR: amounts convert at each transaction's own date (100 USD * 0.90 = 90 EUR on Date1) and
        // format with the euro symbol, not a hardcoded "$".
        var service = await SeededServiceAsync(Date1);
        var prior = SetInstance(service);
        try
        {
            var data = new CompanyData();
            data.Settings.Localization.Currency = "EUR";
            data.Revenues.Add(UsdRevenue("R1", Date1, 100m));

            var insights = new InsightsService();
            ResolveDisplayCode(insights, data);

            var converted = ToDisplay(insights, 100m, Date1);
            Assert.Equal(90m, converted);

            var formatted = (string)FormatCurrencyMethod().Invoke(insights, [converted])!;
            Assert.Equal("€90", formatted);
        }
        finally
        {
            SetInstance(prior);
        }
    }

    [Fact]
    public async Task DisplayCurrency_MissingRate_FallsBackToUsd()
    {
        // Same EUR company, but one transaction date (Date2) has no cached exact-date rate (only Date1
        // is seeded). Because a needed date can't be converted, the whole run falls back to USD:
        // amounts stay unconverted and format with "$" (the all-or-nothing rule reports use).
        var service = await SeededServiceAsync(Date1);
        var prior = SetInstance(service);
        try
        {
            var data = new CompanyData();
            data.Settings.Localization.Currency = "EUR";
            data.Revenues.Add(UsdRevenue("R1", Date1, 100m));
            data.Revenues.Add(UsdRevenue("R2", Date2, 200m)); // Date2 rate not cached

            var insights = new InsightsService();
            ResolveDisplayCode(insights, data);

            Assert.Equal(100m, ToDisplay(insights, 100m, Date1));
            Assert.Equal("$100", (string)FormatCurrencyMethod().Invoke(insights, [100m])!);
        }
        finally
        {
            SetInstance(prior);
        }
    }

    #endregion

    #region Helpers

    private static MethodInfo FormatCurrencyMethod() =>
        typeof(InsightsService).GetMethod("FormatCurrency", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static void ResolveDisplayCode(InsightsService service, CompanyData data) =>
        typeof(InsightsService).GetMethod("ResolveDisplayCode", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, [data]);

    private static decimal ToDisplay(InsightsService service, decimal amountUSD, DateTime date) =>
        (decimal)typeof(InsightsService).GetMethod("ToDisplay", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(service, [amountUSD, date])!;

    private static Revenue UsdRevenue(string id, DateTime date, decimal total) => new()
    {
        Id = id,
        Date = date,
        Description = id,
        OriginalCurrency = "USD",
        Total = total,
        TotalUSD = total,
        TaxAmount = 0,
        TaxAmountUSD = 0
    };

    /// <summary>
    /// Builds an ExchangeRateService whose cache holds a USD->EUR rate (0.90) for each seeded date,
    /// via the real async fetch path against a stub handler. A date omitted here is never cached,
    /// exercising the missing-rate fallback.
    /// </summary>
    private static async Task<ExchangeRateService> SeededServiceAsync(params DateTime[] seedDates)
    {
        var service = new ExchangeRateService(new MockPlatformService(), new HttpClient(new FixedEurHandler()));
        foreach (var d in seedDates)
            await service.GetExchangeRateAsync("USD", "EUR", d);
        return service;
    }

    private static ExchangeRateService? SetInstance(ExchangeRateService? service)
    {
        var prop = typeof(ExchangeRateService)
            .GetProperty(nameof(ExchangeRateService.Instance), BindingFlags.Public | BindingFlags.Static)!;
        var prior = (ExchangeRateService?)prop.GetValue(null);
        prop.SetValue(null, service);
        return prior;
    }

    #endregion

    #region Stubs

    /// <summary>Returns a fixed USD->EUR rate of 0.90 for any date.</summary>
    private sealed class FixedEurHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            const string payload = """{ "success": true, "base": "USD", "rates": { "EUR": 0.9 } }""";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class MockPlatformService : IPlatformService
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
