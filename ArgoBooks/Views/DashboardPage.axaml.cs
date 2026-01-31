using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ArgoBooks.Controls;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.ViewModels;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.VisualElements;

namespace ArgoBooks.Views;

/// <summary>
/// Dashboard page providing an overview of key business metrics,
/// recent transactions, and quick actions.
/// </summary>
public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();

        // Close context menu when clicking outside
        PointerPressed += OnPagePointerPressed;

        // Subscribe to ViewModel events when DataContext changes
        DataContextChanged += OnDataContextChanged;

        // Wire up chart scroll handler after control is loaded
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        // Attach handler at page level with handledEventsToo to intercept events handled by LiveCharts
        AddHandler(
            PointerWheelChangedEvent,
            OnChartPointerWheelChanged,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DashboardPageViewModel viewModel)
        {
            viewModel.SaveChartImageRequested += OnSaveChartImageRequested;
            viewModel.ExcelExportRequested += OnExcelExportRequested;
        }
    }

    /// <summary>
    /// Handles the save chart as image request from the ViewModel.
    /// </summary>
    private async void OnSaveChartImageRequested(object? sender, SaveChartImageEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Find the chart and determine the file name
        Control? chart = null;
        string chartName = "Chart";

        switch (e.ChartId)
        {
            case "ExpensesChart":
                chart = this.FindControl<CartesianChart>("ExpensesChart");
                chartName = "Total_Expenses";
                break;

            case "ExpenseDistributionChart":
                chart = this.FindControl<PieChart>("ExpenseDistributionChart");
                chartName = "Distribution_of_Expenses";
                break;
        }

        if (chart == null) return;

        await ChartImageExportService.SaveChartAsImageAsync(
            topLevel,
            chart,
            ChartImageExportService.CreateSafeFileName(chartName));
    }

    /// <summary>
    /// Handles the Excel export request from the ViewModel.
    /// </summary>
    private async void OnExcelExportRequested(object? sender, ExcelExportEventArgs e)
    {
        // Get the top-level window for the file picker
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Create safe filename from chart title
        var safeName = string.Join("_", e.ChartTitle.Split(Path.GetInvalidFileNameChars()));
        safeName = safeName.Replace(" ", "_");
        var suggestedFileName = $"{safeName}_{DateTime.Now:yyyy-MM-dd}";

        // Show save file dialog
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Chart to Excel",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "xlsx",
            FileTypeChoices =
            [
                new FilePickerFileType("Excel Workbook") { Patterns = ["*.xlsx"] }
            ]
        });

        if (file == null) return;

        try
        {
            var filePath = file.Path.LocalPath;

            // Export based on chart type
            if (e.IsMultiSeries)
            {
                // Multi-series chart (e.g., Revenue vs Expenses)
                var seriesData = new Dictionary<string, double[]>
                {
                    { e.SeriesName, e.Values }
                };
                foreach (var (name, values) in e.AdditionalSeries)
                {
                    seriesData[name] = values;
                }

                await ChartExcelExportService.ExportMultiSeriesChartAsync(
                    filePath,
                    e.ChartTitle,
                    e.Labels,
                    seriesData,
                    labelHeader: "Date",
                    isCurrency: true);
            }
            else if (e.IsDistribution)
            {
                // Distribution/Pie chart
                await ChartExcelExportService.ExportDistributionChartAsync(
                    filePath,
                    e.ChartTitle,
                    e.Labels,
                    e.Values,
                    categoryHeader: "Category",
                    valueHeader: e.SeriesName,
                    isCurrency: true);
            }
            else
            {
                // Single-series time chart
                var isCurrency = e.ChartType != ChartType.Comparison ||
                                 !e.SeriesName.Contains("Count", StringComparison.OrdinalIgnoreCase);

                await ChartExcelExportService.ExportChartAsync(
                    filePath,
                    e.ChartTitle,
                    e.Labels,
                    e.Values,
                    column1Header: "Date",
                    column2Header: e.SeriesName,
                    isCurrency: isCurrency);
            }

            System.Diagnostics.Debug.WriteLine($"Chart exported to Excel: {filePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to export chart to Excel: {ex.Message}");
            var dialog = App.ConfirmationDialog;
            if (dialog != null)
            {
                await dialog.ShowAsync(new ConfirmationDialogOptions
                {
                    Title = "Export Failed".Translate(),
                    Message = "Failed to export the chart to Excel: {0}".TranslateFormat(ex.Message),
                    PrimaryButtonText = "OK".Translate(),
                    SecondaryButtonText = null,
                    CancelButtonText = null
                });
            }
        }
    }

    /// <summary>
    /// Handles right-click on the chart to show the context menu.
    /// </summary>
    private void OnChartPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender as Control).Properties;

        if (properties.IsRightButtonPressed)
        {
            if (DataContext is DashboardPageViewModel viewModel)
            {
                // Get position relative to this page (the Panel container) for proper menu placement
                var position = e.GetPosition(this);
                var isPieChart = sender is PieChart;

                // Get the chart title from LabelVisual, fall back to chart Name for data lookup
                var chartId = GetChartTitle(sender as Control) ?? (sender switch
                {
                    CartesianChart cc => cc.Name ?? "ExpensesChart",
                    PieChart pc => pc.Name ?? "ExpenseDistributionChart",
                    _ => string.Empty
                });

                viewModel.ShowChartContextMenu(position.X, position.Y, chartId: chartId, isPieChart: isPieChart,
                    parentWidth: Bounds.Width, parentHeight: Bounds.Height);
                e.Handled = true;
            }
        }
        else if (properties.IsLeftButtonPressed)
        {
            // Close context menu on left click
            if (DataContext is DashboardPageViewModel viewModel)
            {
                viewModel.HideChartContextMenuCommand.Execute(null);
            }
        }
    }

    /// <summary>
    /// Attempts to find the chart title from the chart's Title property.
    /// </summary>
    private static string? GetChartTitle(Control? chart)
    {
        if (chart == null) return null;

        // Get the title directly from LiveCharts chart controls
        if (chart is CartesianChart cartesianChart &&
            cartesianChart.Title is LabelVisual cartesianLabel &&
            !string.IsNullOrWhiteSpace(cartesianLabel.Text))
        {
            return cartesianLabel.Text;
        }

        if (chart is PieChart pieChart &&
            pieChart.Title is LabelVisual pieLabel &&
            !string.IsNullOrWhiteSpace(pieLabel.Text))
        {
            return pieLabel.Text;
        }

        return null;
    }

    /// <summary>
    /// Handles clicks on the page to close the context menu.
    /// </summary>
    private void OnPagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is DashboardPageViewModel { IsChartContextMenuOpen: true } viewModel)
        {
            // Check if click is outside the context menu
            var contextMenu = this.FindControl<ChartContextMenu>("ChartContextMenu");
            if (contextMenu != null)
            {
                var position = e.GetPosition(contextMenu);
                var bounds = contextMenu.Bounds;

                // If click is outside the context menu bounds (considering the transform)
                if (position.X < 0 || position.Y < 0 ||
                    position.X > bounds.Width || position.Y > bounds.Height)
                {
                    viewModel.HideChartContextMenuCommand.Execute(null);
                }
            }
        }
    }

    /// <summary>
    /// Handles pointer wheel events on charts to allow scroll passthrough to parent ScrollViewer.
    /// LiveCharts captures wheel events for zooming, so we intercept them and forward to the ScrollViewer.
    /// When CTRL or Shift is held, allow LiveCharts to handle zooming instead.
    /// </summary>
    private void OnChartPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Check if the event originated from a CartesianChart
        var source = e.Source as Control;
        var chart = source?.FindAncestorOfType<CartesianChart>() ?? source as CartesianChart;

        // If CTRL or Shift is held, allow LiveCharts to handle zooming
        if ((e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift)) && chart != null)
        {
            return; // Don't intercept - let LiveCharts zoom
        }

        // Find the ScrollViewer and manually scroll it
        var scrollViewer = chart?.FindAncestorOfType<ScrollViewer>();
        if (scrollViewer != null)
        {
            // Use ScrollViewer's built-in line scroll methods for natural scroll feel
            var linesToScroll = (int)Math.Round(e.Delta.Y * 3);
            for (int i = 0; i < Math.Abs(linesToScroll); i++)
            {
                if (linesToScroll > 0)
                    scrollViewer.LineUp();
                else
                    scrollViewer.LineDown();
            }
        }
    }
}
