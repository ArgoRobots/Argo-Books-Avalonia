#pragma warning disable CS0618 // LabelVisual is obsolete — DrawnLabelVisual is not API-compatible
using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ArgoBooks.Controls;
using ArgoBooks.Controls.Dashboard;
using ArgoBooks.Core.Enums;
using ArgoBooks.Core.Models.Dashboard;
using ArgoBooks.Core.Services;
using ArgoBooks.Localization;
using ArgoBooks.Services;
using ArgoBooks.ViewModels;
using ArgoBooks.ViewModels.Dashboard;
using LiveChartsCore.SkiaSharpView.Avalonia;
using OfficeOpenXml.Drawing.Chart;
using LiveChartsCore.SkiaSharpView.VisualElements;

namespace ArgoBooks.Views;

/// <summary>
/// Dashboard page providing an overview of key business metrics via customizable widgets.
/// </summary>
public partial class DashboardPage : UserControl
{
    private Control? _clickedChart;
    private string _clickedChartName = "Chart";
    private DashboardDragDropManager? _dragDropManager;
    private DashboardPageViewModel? _previousViewModel;

    /// <summary>
    /// Sets the clicked chart reference from an external source (e.g., ChartExpandOverlay).
    /// </summary>
    public void SetClickedChart(Control? chart, string name)
    {
        _clickedChart = chart;
        _clickedChartName = name;
    }

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

        // Intercept right-click in tunneling phase to prevent LiveCharts selection box
        AddHandler(
            PointerPressedEvent,
            OnChartPointerPressedTunnel,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_previousViewModel != null)
        {
            _previousViewModel.SaveChartImageRequested -= OnSaveChartImageRequested;
            _previousViewModel.ExcelExportRequested -= OnExcelExportRequested;
            _previousViewModel.LayoutViewModel.Rows.CollectionChanged -= OnRowsCollectionChanged;
            _previousViewModel = null;
        }

        if (DataContext is DashboardPageViewModel viewModel)
        {
            _previousViewModel = viewModel;
            viewModel.SaveChartImageRequested += OnSaveChartImageRequested;
            viewModel.ExcelExportRequested += OnExcelExportRequested;
            viewModel.LayoutViewModel.Rows.CollectionChanged += OnRowsCollectionChanged;
            RebuildRows(viewModel.LayoutViewModel);
        }
    }

    #region Row Panel Management

    private void OnRowsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (DataContext is not DashboardPageViewModel viewModel) return;
        RebuildRows(viewModel.LayoutViewModel);
    }

    private void RebuildRows(DashboardLayoutViewModel layoutVm)
    {
        // Unsubscribe from old widget VMs
        foreach (var child in RowsContainer.Children)
        {
            if (child is DashboardRowHost rowHost)
            {
                foreach (var widgetChild in rowHost.Panel.Children)
                {
                    if (widgetChild is WidgetHost host && host.DataContext is WidgetHostViewModel oldVm)
                        oldVm.PropertyChanged -= OnWidgetHostPropertyChanged;
                }
            }
        }

        RowsContainer.Children.Clear();
        _dragDropManager = null;

        for (int rowIdx = 0; rowIdx < layoutVm.Rows.Count; rowIdx++)
        {
            var rowVm = layoutVm.Rows[rowIdx];
            var rowHost = new DashboardRowHost { DataContext = rowVm };

            // Wire row-level buttons
            var capturedRowVm = rowVm; // capture for lambda
            rowHost.AddButton.Click += (_, _) => layoutVm.OpenCatalogForRow(capturedRowVm);
            rowHost.DeleteButton.Click += (_, _) => layoutVm.RemoveRow(capturedRowVm);

            // Populate widget panel
            foreach (var hostVm in rowVm.Widgets)
            {
                var widgetHost = CreateWidgetHost(hostVm, layoutVm);
                rowHost.Panel.Children.Add(widgetHost);
            }

            // Listen for widget collection changes in this row
            var capturedRow = rowVm;
            rowVm.Widgets.CollectionChanged += (_, _) => RebuildRows(layoutVm);

            RowsContainer.Children.Add(rowHost);
        }

        SetupDragDrop(layoutVm);
    }

    private WidgetHost CreateWidgetHost(WidgetHostViewModel hostVm, DashboardLayoutViewModel layoutVm)
    {
        var widgetHost = new WidgetHost { DataContext = hostVm };
        widgetHost.SetWidgetContent(hostVm);
        DashboardRowPanel.SetWidgetFraction(widgetHost, hostVm.Size.ToFraction());
        hostVm.PropertyChanged += OnWidgetHostPropertyChanged;

        var removeButton = widgetHost.FindControl<Button>("RemoveButton");
        if (removeButton != null)
        {
            removeButton.Command = layoutVm.RemoveWidgetCommand;
            removeButton.CommandParameter = hostVm;
        }

        return widgetHost;
    }

    private void OnWidgetHostPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WidgetHostViewModel.Size) && sender is WidgetHostViewModel hostVm)
        {
            foreach (var child in RowsContainer.Children)
            {
                if (child is not DashboardRowHost rowHost) continue;
                foreach (var widgetChild in rowHost.Panel.Children)
                {
                    if (widgetChild is WidgetHost wh && wh.DataContext == hostVm)
                    {
                        DashboardRowPanel.SetWidgetFraction(wh, hostVm.Size.ToFraction());
                        rowHost.Panel.InvalidateMeasure();
                        rowHost.Panel.InvalidateArrange();
                        return;
                    }
                }
            }
        }
    }

    private void SetupDragDrop(DashboardLayoutViewModel layoutVm)
    {
        var rowPanels = new List<DashboardRowPanel>();
        foreach (var child in RowsContainer.Children)
        {
            if (child is DashboardRowHost rowHost)
                rowPanels.Add(rowHost.Panel);
        }

        if (rowPanels.Count == 0) return;

        _dragDropManager = new DashboardDragDropManager(
            rowPanels,
            RowsContainer,
            MainScrollViewer,
            layoutVm);

        foreach (var child in RowsContainer.Children)
        {
            if (child is not DashboardRowHost rowHost) continue;
            for (int i = 0; i < rowHost.Panel.Children.Count; i++)
            {
                if (rowHost.Panel.Children[i] is WidgetHost widgetHost)
                {
                    var dragHandle = widgetHost.FindControl<Border>("DragHandle");
                    if (dragHandle != null)
                        _dragDropManager.AttachDragHandle(dragHandle);
                }
            }
        }
    }

    #endregion

    #region Chart Context Menu & Export

    /// <summary>
    /// Intercepts right-click in tunneling phase to prevent LiveCharts from starting selection box.
    /// </summary>
    private void OnChartPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        var source = e.Source as Control;
        var chart = source?.FindAncestorOfType<CartesianChart>() ?? source as CartesianChart;
        var pieChart = source?.FindAncestorOfType<PieChart>() ?? source as PieChart;

        if ((chart != null || pieChart != null) && e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            // Show context menu and mark as handled to prevent LiveCharts selection box
            if (DataContext is DashboardPageViewModel viewModel)
            {
                var position = e.GetPosition(this);
                var isPieChart = pieChart != null;
                var targetChart = (Control?)chart ?? pieChart;

                _clickedChart = targetChart;
                _clickedChartName = GetChartTitle(targetChart) ?? "Chart";
                var chartDataType = _clickedChart?.Tag as ChartDataType?;

                viewModel.ShowChartContextMenu(position.X, position.Y, chartDataType: chartDataType, isPieChart: isPieChart,
                    parentWidth: Bounds.Width, parentHeight: Bounds.Height);
            }
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles the save chart as image request from the ViewModel.
    /// </summary>
    private async void OnSaveChartImageRequested(object? sender, SaveChartImageEventArgs e)
    {
        try
        {
            if (_clickedChart == null) return;

            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel == null) return;

            await ChartImageExportService.SaveChartAsImageAsync(
                topLevel,
                _clickedChart,
                ChartImageExportService.CreateSafeFileName(_clickedChartName));
        }
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Export, "OnSaveChartImageRequested");
        }
    }

    /// <summary>
    /// Handles the Excel export request from the ViewModel.
    /// </summary>
    private async void OnExcelExportRequested(object? sender, ExcelExportEventArgs e)
    {
        try
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

                // Map chart style to Excel chart type
                var excelChartType = e.ChartStyle switch
                {
                    ChartStyle.Column => eChartType.ColumnClustered,
                    ChartStyle.Area => eChartType.Area,
                    ChartStyle.Scatter => eChartType.XYScatter,
                    _ => eChartType.Line
                };

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
                        isCurrency: true,
                        excelChartType: excelChartType);
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
                        isCurrency: isCurrency,
                        excelChartType: excelChartType);
                }
            }
            catch (Exception ex)
            {
                App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Export, "Failed to export chart to Excel");
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
        catch (Exception ex)
        {
            App.ErrorLogger?.LogError(ex, Core.Models.Telemetry.ErrorCategory.Export, "OnExcelExportRequested");
        }
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

        // Only intercept events that originate from a chart
        if (chart == null)
            return;

        // If CTRL or Shift is held, allow LiveCharts to handle zooming
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift))
        {
            return; // Don't intercept - let LiveCharts zoom
        }

        // Mark as handled to prevent LiveCharts from zooming when no modifier is held
        e.Handled = true;

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

    #endregion
}
