using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Dashboard;
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
        var grouped = data.Revenues
            .Where(r => !string.IsNullOrEmpty(r.CustomerId))
            .GroupBy(r => r.CustomerId!)
            .Select(g => new
            {
                CustomerId = g.Key,
                TotalRevenue = g.Sum(r => r.EffectiveSubtotalUSD),
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
                var formatted = CurrencyService.FormatFromUSD(g.TotalRevenue, DateTime.Now);
                return new TopCustomerItem(i + 1, name, formatted, g.Count);
            })
            .ToList();

        Customers = new ObservableCollection<TopCustomerItem>(items);
    }
}
