using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ArgoBooks.Controls;
using ArgoBooks.Core.Services;
using ArgoBooks.Services;
using ArgoBooks.ViewModels;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.SKCharts;
using LiveChartsCore.SkiaSharpView.VisualElements;
using SkiaSharp;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Analytics page.
/// </summary>
public partial class AnalyticsPage : UserControl
{
    /// <summary>
    /// Stores a reference to the last clicked chart control for export operations.
    /// </summary>
    private Control? _clickedChart;

    public AnalyticsPage()
    {
        InitializeComponent();

        // Subscribe to ViewModel events when DataContext changes
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is AnalyticsPageViewModel viewModel)
        {
            viewModel.SaveChartImageRequested += OnSaveChartImageRequested;
            viewModel.ExcelExportRequested += OnExcelExportRequested;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Wire up page-level click handler to close context menu
        PointerPressed += OnPagePointerPressed;

        // Attach handler at page level with handledEventsToo to intercept events handled by LiveCharts
        this.AddHandler(
            PointerWheelChangedEvent,
            OnChartPointerWheelChanged,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        PointerPressed -= OnPagePointerPressed;
    }

    private AnalyticsPageViewModel? ViewModel => DataContext as AnalyticsPageViewModel;

    private void CustomerActivityInfoBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseCustomerActivityInfoCommand.Execute(null);
    }

    private void CustomerActivityInfoModal_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Prevent click from bubbling to backdrop
        e.Handled = true;
    }

    /// <summary>
    /// The name of the last clicked chart for file naming.
    /// </summary>
    private string _clickedChartName = "Chart";

    /// <summary>
    /// Handles right-click on charts to show the context menu.
    /// </summary>
    private void OnChartPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var properties = e.GetCurrentPoint(sender as Control).Properties;

        if (properties.IsRightButtonPressed)
        {
            if (DataContext is AnalyticsPageViewModel viewModel)
            {
                // Store reference to the clicked chart for later use in save/export operations
                _clickedChart = sender as Control;

                // Try to find the chart title from the parent container
                _clickedChartName = GetChartTitle(sender as Control) ?? "Chart";

                // Get position relative to this page (the Panel container) for proper menu placement
                var position = e.GetPosition(this);
                var isPieChart = sender is PieChart;
                var isGeoMap = sender is GeoMap;

                // Use the chart title as the chart ID for export operations
                viewModel.ShowChartContextMenu(position.X, position.Y, chartId: _clickedChartName, isPieChart: isPieChart, isGeoMap: isGeoMap,
                    parentWidth: Bounds.Width, parentHeight: Bounds.Height);
                e.Handled = true;
            }
        }
        else if (properties.IsLeftButtonPressed)
        {
            // Close context menu on left click
            if (DataContext is AnalyticsPageViewModel viewModel)
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
        // The Title property is a LabelVisual which has a Text property
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
    /// Handles the save chart as image request from the ViewModel.
    /// </summary>
    private async void OnSaveChartImageRequested(object? sender, SaveChartImageEventArgs e)
    {
        if (_clickedChart == null) return;

        // Get the top-level window for the file picker
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Create file-safe name from chart title
        var safeName = string.Join("_", _clickedChartName.Split(Path.GetInvalidFileNameChars()));
        safeName = safeName.Replace(" ", "_");
        var suggestedFileName = $"{safeName}_{DateTime.Now:yyyy-MM-dd}";

        // Show save file dialog
        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Chart as Image",
            SuggestedFileName = suggestedFileName,
            DefaultExtension = "png",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("PNG Image") { Patterns = new[] { "*.png" } },
                new FilePickerFileType("JPEG Image") { Patterns = new[] { "*.jpg", "*.jpeg" } }
            }
        });

        if (file == null) return;

        try
        {
            var filePath = file.Path.LocalPath;

            // Determine format based on file extension
            var extension = Path.GetExtension(filePath).ToLowerInvariant();
            var format = extension switch
            {
                ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
                _ => SKEncodedImageFormat.Png
            };

            // Save the chart based on its type
            switch (_clickedChart)
            {
                case CartesianChart cartesianChart:
                    var skCartesianChart = new SKCartesianChart(cartesianChart)
                    {
                        Width = (int)cartesianChart.Bounds.Width,
                        Height = (int)cartesianChart.Bounds.Height,
                        Background = SKColors.Transparent
                    };
                    skCartesianChart.SaveImage(filePath, format, 100);
                    break;

                case PieChart pieChart:
                    var skPieChart = new SKPieChart(pieChart)
                    {
                        Width = (int)pieChart.Bounds.Width,
                        Height = (int)pieChart.Bounds.Height,
                        Background = SKColors.Transparent
                    };
                    skPieChart.SaveImage(filePath, format, 100);
                    break;

                case GeoMap geoMap:
                    var skGeoMap = new SKGeoMap(geoMap)
                    {
                        Width = (int)geoMap.Bounds.Width,
                        Height = (int)geoMap.Bounds.Height,
                        Background = SKColors.Transparent
                    };
                    skGeoMap.SaveImage(filePath, format, 100);
                    break;
            }

            System.Diagnostics.Debug.WriteLine($"Chart saved to: {filePath}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save chart: {ex.Message}");
        }
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
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Excel Workbook") { Patterns = new[] { "*.xlsx" } }
            }
        });

        if (file == null) return;

        try
        {
            var filePath = file.Path.LocalPath;

            // Export based on chart type
            if (e.IsMultiSeries)
            {
                // Multi-series chart (e.g., Sales vs Expenses)
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
        }
    }

    /// <summary>
    /// Handles clicks on the page to close the context menu when clicking outside.
    /// </summary>
    private void OnPagePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is AnalyticsPageViewModel { IsChartContextMenuOpen: true } viewModel)
        {
            var contextMenu = this.FindControl<ChartContextMenu>("ChartContextMenu");
            if (contextMenu != null)
            {
                var position = e.GetPosition(contextMenu);
                var bounds = contextMenu.Bounds;

                // If clicked outside the context menu, close it
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
    /// </summary>
    private void OnChartPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        // Check if the event originated from a CartesianChart
        var source = e.Source as Control;
        var chart = source?.FindAncestorOfType<CartesianChart>() ?? source as CartesianChart;

        if (chart == null)
            return; // Not over a chart, let normal scrolling happen

        // Find the ScrollViewer and manually scroll it
        var scrollViewer = chart.FindAncestorOfType<ScrollViewer>();
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
