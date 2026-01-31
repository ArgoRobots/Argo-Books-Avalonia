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
        AddHandler(
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
                var clickedControl = sender as Control;

                // For PieChartLegend, find the associated PieChart sibling
                if (clickedControl is PieChartLegend legend)
                {
                    // Navigate: PieChartLegend → Parent Grid → find PieChart sibling
                    if (legend.Parent is Grid parentGrid)
                    {
                        foreach (var child in parentGrid.Children)
                        {
                            if (child is PieChart siblingChart)
                            {
                                _clickedChart = siblingChart;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // Store reference to the clicked chart for later use in save/export operations
                    _clickedChart = clickedControl;
                }

                // Try to find the chart title from the parent container
                _clickedChartName = GetChartTitle(clickedControl) ?? "Chart";

                // Get position relative to this page (the Panel container) for proper menu placement
                var position = e.GetPosition(this);
                var isPieChart = sender is PieChart || sender is PieChartLegend;
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
    /// Attempts to find the chart title from the chart's Title property or from a sibling TextBlock.
    /// </summary>
    private static string? GetChartTitle(Control? control)
    {
        if (control == null) return null;

        // Get the title directly from LiveCharts chart controls
        // The Title property is a LabelVisual which has a Text property
        if (control is CartesianChart cartesianChart &&
            cartesianChart.Title is LabelVisual cartesianLabel &&
            !string.IsNullOrWhiteSpace(cartesianLabel.Text))
        {
            return cartesianLabel.Text;
        }

        if (control is PieChart pieChart &&
            pieChart.Title is LabelVisual pieLabel &&
            !string.IsNullOrWhiteSpace(pieLabel.Text))
        {
            return pieLabel.Text;
        }

        // For pie charts and legends without a Title property, look for a TextBlock title
        // in the parent container structure:
        // Grid (RowDefinitions="Auto,*")
        //   ├─ TextBlock Row="0" (the title)
        //   └─ Grid Row="1"
        //       ├─ PieChart/GeoMap Column="0"
        //       └─ PieChartLegend Column="1"
        if (control is PieChart or PieChartLegend or GeoMap)
        {
            // Navigate up: Control → Grid (Column container) → Grid (Row container)
            var columnGrid = control.Parent as Grid;
            var rowGrid = columnGrid?.Parent as Grid;

            if (rowGrid != null)
            {
                // Find the TextBlock in Row 0 (the title)
                foreach (var child in rowGrid.Children)
                {
                    if (child is TextBlock textBlock &&
                        Grid.GetRow(textBlock) == 0 &&
                        !string.IsNullOrWhiteSpace(textBlock.Text))
                    {
                        return textBlock.Text;
                    }
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Handles the save chart as image request from the ViewModel.
    /// </summary>
    private async void OnSaveChartImageRequested(object? sender, SaveChartImageEventArgs e)
    {
        if (_clickedChart == null) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        await ChartImageExportService.SaveChartAsImageAsync(
            topLevel,
            _clickedChart,
            ChartImageExportService.CreateSafeFileName(_clickedChartName));
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
