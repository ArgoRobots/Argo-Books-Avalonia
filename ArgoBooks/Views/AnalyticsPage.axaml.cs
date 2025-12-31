using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;
using ArgoBooks.Controls;
using ArgoBooks.ViewModels;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.SKCharts;
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
    /// Attempts to find the chart title by looking at parent containers.
    /// </summary>
    private static string? GetChartTitle(Control? chart)
    {
        if (chart == null) return null;

        // Walk up the visual tree to find a Grid with a header Border containing a TextBlock
        var parent = chart.Parent;
        while (parent != null)
        {
            if (parent is Grid grid)
            {
                // Look for a TextBlock in the first child (header area)
                foreach (var child in grid.Children)
                {
                    if (child is Border border)
                    {
                        var textBlock = FindTextBlock(border);
                        if (textBlock != null && !string.IsNullOrWhiteSpace(textBlock.Text))
                        {
                            return textBlock.Text;
                        }
                    }
                }
            }
            parent = parent.Parent;
        }
        return null;
    }

    /// <summary>
    /// Recursively finds a TextBlock within a control.
    /// </summary>
    private static TextBlock? FindTextBlock(Control control)
    {
        if (control is TextBlock tb)
            return tb;

        if (control is Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is Control c)
                {
                    var result = FindTextBlock(c);
                    if (result != null)
                        return result;
                }
            }
        }
        else if (control is Decorator decorator && decorator.Child is Control decoratorChild)
        {
            return FindTextBlock(decoratorChild);
        }
        else if (control is ContentControl cc && cc.Content is Control content)
        {
            return FindTextBlock(content);
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
