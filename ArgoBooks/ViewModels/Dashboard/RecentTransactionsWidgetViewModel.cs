using System.Collections.ObjectModel;
using ArgoBooks.Core.Data;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels.Dashboard;

public partial class RecentTransactionsWidgetViewModel : WidgetViewModelBase
{
    public override WidgetType WidgetType => WidgetType.RecentTransactions;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasRecentTransactions))]
    [NotifyPropertyChangedFor(nameof(HasNoRecentTransactions))]
    private ObservableCollection<RecentTransactionItem> _recentTransactions = [];

    public bool HasRecentTransactions => RecentTransactions.Count > 0;
    public bool HasNoRecentTransactions => RecentTransactions.Count == 0;

    public override bool HasConfig => true;

    [ObservableProperty]
    private int _rowCount = 10;

    public int[] RowCountOptions { get; } = [5, 10, 20];

    partial void OnRowCountChanged(int value) => LoadData();

    public override void Initialize(Dictionary<string, string> config)
    {
        ApplyConfig(config);
    }

    public override void ApplyConfig(Dictionary<string, string> config)
    {
        if (config.TryGetValue("RowCount", out var rowCountStr) && int.TryParse(rowCountStr, out var rowCount))
            RowCount = rowCount;
    }

    public override Dictionary<string, string> GetConfig()
    {
        return new Dictionary<string, string>
        {
            ["RowCount"] = RowCount.ToString()
        };
    }

    public override void LoadData()
    {
        var data = CompanyManager?.CompanyData;
        if (data == null) return;

        LoadRecentTransactions(data);
    }

    private void LoadRecentTransactions(CompanyData data)
    {
        var recentItems = new List<RecentTransactionItem>();

        var recentSales = data.Revenues
            .OrderByDescending(s => s.Date)
            .Take(RowCount)
            .Select(s => new RecentTransactionItem
            {
                Id = s.Id,
                Type = "Revenue",
                Description = string.IsNullOrEmpty(s.Description) ? "Revenue Transaction" : s.Description,
                Amount = CurrencyService.FormatFromUSD(s.EffectiveSubtotalUSD, s.Date),
                AmountValue = CurrencyService.GetDisplayAmount(s.EffectiveSubtotalUSD, s.Date),
                Date = s.Date,
                DateFormatted = FormatDate(s.Date),
                Status = string.Empty,
                StatusVariant = string.Empty,
                IsIncome = true,
                CustomerName = GetCustomerName(data, s.CustomerId)
            });

        recentItems.AddRange(recentSales);

        var recentPurchases = data.Expenses
            .OrderByDescending(p => p.Date)
            .Take(RowCount)
            .Select(p => new RecentTransactionItem
            {
                Id = p.Id,
                Type = "Expense",
                Description = string.IsNullOrEmpty(p.Description) ? "Purchase Transaction" : p.Description,
                Amount = CurrencyService.FormatFromUSD(p.EffectiveSubtotalUSD, p.Date),
                AmountValue = CurrencyService.GetDisplayAmount(p.EffectiveSubtotalUSD, p.Date),
                Date = p.Date,
                DateFormatted = FormatDate(p.Date),
                Status = string.Empty,
                StatusVariant = string.Empty,
                IsIncome = false,
                CustomerName = GetSupplierName(data, p.SupplierId)
            });

        recentItems.AddRange(recentPurchases);

        var sortedItems = recentItems
            .OrderByDescending(t => t.Date)
            .Take(RowCount)
            .ToList();

        RecentTransactions = new ObservableCollection<RecentTransactionItem>(sortedItems);
    }

    [RelayCommand]
    private void NavigateToTransaction(RecentTransactionItem? transaction)
    {
        if (transaction == null) return;

        var pageName = transaction.IsIncome ? "Revenue" : "Expenses";
        App.NavigationService?.NavigateTo(pageName, new TransactionNavigationParameter(transaction.Id));
    }

    #region Helper Methods

    private static string FormatDate(DateTime date)
    {
        var now = DateTime.Now;
        if (date.Date == now.Date)
            return "Today";
        if (date.Date == now.Date.AddDays(-1))
            return "Yesterday";
        if (date.Date > now.Date.AddDays(-7))
            return date.ToString("dddd");
        return DateFormatService.Format(date);
    }

    private static string GetCustomerName(CompanyData data, string? customerId)
    {
        if (string.IsNullOrEmpty(customerId)) return "Unknown";
        var customer = data.GetCustomer(customerId);
        return customer?.Name ?? "Unknown";
    }

    private static string GetSupplierName(CompanyData data, string? supplierId)
    {
        if (string.IsNullOrEmpty(supplierId)) return "Unknown";
        var supplier = data.GetSupplier(supplierId);
        return supplier?.Name ?? "Unknown";
    }

    #endregion
}
