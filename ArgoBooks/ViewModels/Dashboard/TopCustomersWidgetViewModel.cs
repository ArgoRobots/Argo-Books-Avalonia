using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ArgoBooks.ViewModels.Dashboard;

public record TopCustomerItem(int Rank, string Name, string TotalRevenue, int TransactionCount);

public partial class TopCustomersWidgetViewModel : WidgetViewModelBase
{
    public override WidgetType WidgetType => WidgetType.TopCustomers;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasCustomers))]
    [NotifyPropertyChangedFor(nameof(HasNoCustomers))]
    private ObservableCollection<TopCustomerItem> _customers = [];

    public bool HasCustomers => Customers.Count > 0;
    public bool HasNoCustomers => Customers.Count == 0;

    public override bool HasConfig => true;

    [ObservableProperty]
    private int _count = 5;

    [ObservableProperty]
    private string _sortBy = "revenue";

    public int[] CountOptions { get; } = [5, 10];

    public string[] SortByOptions { get; } = ["revenue", "count"];

    partial void OnCountChanged(int value) => LoadData();
    partial void OnSortByChanged(string value) => LoadData();

    public override void Initialize(Dictionary<string, string> config)
    {
        ApplyConfig(config);
    }

    public override void ApplyConfig(Dictionary<string, string> config)
    {
        if (config.TryGetValue("Count", out var countStr) && int.TryParse(countStr, out var count))
            Count = count;
        if (config.TryGetValue("SortBy", out var sortBy))
            SortBy = sortBy;
    }

    public override Dictionary<string, string> GetConfig()
    {
        return new Dictionary<string, string>
        {
            ["Count"] = Count.ToString(),
            ["SortBy"] = SortBy
        };
    }

    public override void LoadData()
    {
        var data = CompanyManager?.CompanyData;
        if (data == null) return;

        LoadTopCustomers(data);
    }

    private void LoadTopCustomers(CompanyData data)
    {
        // Refund totals per customer, so the leaderboard reflects what the customer actually
        // retained (gross − refunds). Each refund is converted to display currency at its OWN
        // date before summing (Calculations.md §3a Phase 2), so a non-USD display total isn't
        // re-priced at today's rate.
        var refundsByCustomer = data.Payments
            .Where(p => p.IsRefund && !string.IsNullOrEmpty(p.CustomerId))
            .GroupBy(p => p.CustomerId)
            .ToDictionary(g => g.Key, g => CurrencyService.SumDisplayFromUSD(
                g, p => Math.Abs(p.EffectiveAmountUSD), p => p.Date));

        var grouped = data.Revenues
            .Where(r => !string.IsNullOrEmpty(r.CustomerId))
            .Where(RevenueAggregator.IsCollected)
            .GroupBy(r => r.CustomerId!)
            .Select(g => new
            {
                CustomerId = g.Key,
                // Each revenue row converted at its OWN date, refunds already in display currency.
                TotalRevenue = CurrencyService.SumDisplayFromUSD(g, r => r.EffectiveTotalUSD, r => r.Date)
                                - (refundsByCustomer.TryGetValue(g.Key, out var rf) ? rf : 0m),
                Count = g.Count()
            });

        var sorted = SortBy == "count"
            ? grouped.OrderByDescending(g => g.Count)
            : grouped.OrderByDescending(g => g.TotalRevenue);

        var items = sorted
            .Take(Count)
            .Select((g, i) =>
            {
                var customer = data.GetCustomer(g.CustomerId);
                var name = customer?.Name ?? "Unknown";
                // TotalRevenue is already in display currency, so just format.
                var formatted = CurrencyService.Format(g.TotalRevenue);
                return new TopCustomerItem(i + 1, name, formatted, g.Count);
            })
            .ToList();

        Customers = new ObservableCollection<TopCustomerItem>(items);
    }
}
