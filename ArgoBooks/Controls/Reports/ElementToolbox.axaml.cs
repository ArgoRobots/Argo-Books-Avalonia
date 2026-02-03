using Avalonia.Controls;
using Avalonia.Interactivity;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Reports;

namespace ArgoBooks.Controls.Reports;

/// <summary>
/// Toolbox control for adding new elements to the report canvas.
/// </summary>
public partial class ElementToolbox : UserControl
{
    /// <summary>
    /// Raised when a new element should be added.
    /// </summary>
    public event EventHandler<AddElementRequestedEventArgs>? AddElementRequested;

    public ElementToolbox()
    {
        InitializeComponent();
    }

    #region Add Element Handlers

    private void OnAddChartClick(object? sender, RoutedEventArgs e)
    {
        var element = new ChartReportElement
        {
            ChartType = ChartDataType.TotalRevenue,
            Width = 400,
            Height = 300
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddTableClick(object? sender, RoutedEventArgs e)
    {
        var element = new TableReportElement
        {
            Width = 500,
            Height = 250,
            MaxRows = 10
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddSummaryClick(object? sender, RoutedEventArgs e)
    {
        var element = new SummaryReportElement
        {
            Width = 300,
            Height = 200,
            ShowTotalRevenue = true,
            ShowTotalTransactions = true,
            ShowAverageValue = true,
            ShowGrowthRate = true
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddLabelClick(object? sender, RoutedEventArgs e)
    {
        var element = new LabelReportElement
        {
            Text = "Enter text here...",
            Width = 200,
            Height = 40,
            FontSize = 14
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddDateRangeClick(object? sender, RoutedEventArgs e)
    {
        var element = new DateRangeReportElement
        {
            Width = 250,
            Height = 30,
            DateFormat = "MMM dd, yyyy"
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddImageClick(object? sender, RoutedEventArgs e)
    {
        var element = new ImageReportElement
        {
            Width = 200,
            Height = 150,
            ScaleMode = ImageScaleMode.Fit
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    #endregion

    #region Quick Add Chart Handlers

    private void OnAddRevenueChartClick(object? sender, RoutedEventArgs e)
    {
        var element = new ChartReportElement
        {
            ChartType = ChartDataType.TotalRevenue,
            Width = 400,
            Height = 280,
            ShowLegend = true,
            ShowTitle = true
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddExpensesChartClick(object? sender, RoutedEventArgs e)
    {
        var element = new ChartReportElement
        {
            ChartType = ChartDataType.TotalExpenses,
            Width = 400,
            Height = 280,
            ShowLegend = true,
            ShowTitle = true
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddProfitChartClick(object? sender, RoutedEventArgs e)
    {
        var element = new ChartReportElement
        {
            ChartType = ChartDataType.TotalProfits,
            Width = 400,
            Height = 280,
            ShowLegend = true,
            ShowTitle = true
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddDistributionChartClick(object? sender, RoutedEventArgs e)
    {
        var element = new ChartReportElement
        {
            ChartType = ChartDataType.RevenueDistribution,
            Width = 350,
            Height = 300,
            ShowLegend = true,
            ShowTitle = true
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddWorldMapClick(object? sender, RoutedEventArgs e)
    {
        var element = new ChartReportElement
        {
            ChartType = ChartDataType.WorldMap,
            Width = 500,
            Height = 300,
            ShowLegend = true,
            ShowTitle = true
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    private void OnAddGrowthChartClick(object? sender, RoutedEventArgs e)
    {
        var element = new ChartReportElement
        {
            ChartType = ChartDataType.CustomerGrowth,
            Width = 400,
            Height = 280,
            ShowLegend = true,
            ShowTitle = true
        };
        AddElementRequested?.Invoke(this, new AddElementRequestedEventArgs(element));
    }

    #endregion
}

/// <summary>
/// Event args for add element requests.
/// </summary>
public class AddElementRequestedEventArgs(ReportElementBase element) : EventArgs
{
    /// <summary>
    /// The element to be added.
    /// </summary>
    public ReportElementBase Element { get; } = element;
}
