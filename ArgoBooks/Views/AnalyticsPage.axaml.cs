using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
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

                // Get position relative to this page (the Panel container) for proper menu placement
                var position = e.GetPosition(this);
                var isPieChart = sender is PieChart;
                var isGeoMap = sender is GeoMap;

                // Determine a chart identifier based on the chart type
                var chartId = sender switch
                {
                    CartesianChart => "CartesianChart",
                    PieChart => "PieChart",
                    GeoMap => "GeoMap",
                    _ => string.Empty
                };

                viewModel.ShowChartContextMenu(position.X, position.Y, chartId: chartId, isPieChart: isPieChart, isGeoMap: isGeoMap);
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
    /// Handles the save chart as image request from the ViewModel.
    /// </summary>
    private async void OnSaveChartImageRequested(object? sender, SaveChartImageEventArgs e)
    {
        if (_clickedChart == null) return;

        // Get the top-level window for the file picker
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Determine the suggested file name based on chart type
        var suggestedFileName = _clickedChart switch
        {
            CartesianChart => $"Chart_{DateTime.Now:yyyy-MM-dd}",
            PieChart => $"PieChart_{DateTime.Now:yyyy-MM-dd}",
            GeoMap => $"GeoMap_{DateTime.Now:yyyy-MM-dd}",
            _ => $"Chart_{DateTime.Now:yyyy-MM-dd}"
        };

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
}
