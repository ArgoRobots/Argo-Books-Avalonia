using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using ArgoBooks.Controls;
using ArgoBooks.ViewModels;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.SKCharts;
using SkiaSharp;

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
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DashboardPageViewModel viewModel)
        {
            viewModel.SaveChartImageRequested += OnSaveChartImageRequested;
        }
    }

    /// <summary>
    /// Handles the save chart as image request from the ViewModel.
    /// </summary>
    private async void OnSaveChartImageRequested(object? sender, SaveChartImageEventArgs e)
    {
        // Get the top-level window for the file picker
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Determine the chart name for the file and get chart bounds
        string suggestedFileName;
        Size chartBounds;

        switch (e.ChartId)
        {
            case "ExpensesChart":
                var expensesChart = this.FindControl<CartesianChart>("ExpensesChart");
                if (expensesChart == null) return;
                suggestedFileName = $"Total_Expenses_{DateTime.Now:yyyy-MM-dd}";
                chartBounds = new Size(expensesChart.Bounds.Width, expensesChart.Bounds.Height);
                break;

            case "ExpenseDistributionChart":
                var pieChart = this.FindControl<PieChart>("ExpenseDistributionChart");
                if (pieChart == null) return;
                suggestedFileName = $"Distribution_of_Expenses_{DateTime.Now:yyyy-MM-dd}";
                chartBounds = new Size(pieChart.Bounds.Width, pieChart.Bounds.Height);
                break;

            default:
                return;
        }

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
            switch (e.ChartId)
            {
                case "ExpensesChart":
                    var expensesChart = this.FindControl<CartesianChart>("ExpensesChart")!;
                    var skCartesianChart = new SKCartesianChart(expensesChart)
                    {
                        Width = (int)chartBounds.Width,
                        Height = (int)chartBounds.Height,
                        Background = SKColors.Transparent
                    };
                    skCartesianChart.SaveImage(filePath, format, 100);
                    break;

                case "ExpenseDistributionChart":
                    var pieChart = this.FindControl<PieChart>("ExpenseDistributionChart")!;
                    var skPieChart = new SKPieChart(pieChart)
                    {
                        Width = (int)chartBounds.Width,
                        Height = (int)chartBounds.Height,
                        Background = SKColors.Transparent
                    };
                    skPieChart.SaveImage(filePath, format, 100);
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

                // Determine the chart identifier based on the sender
                var chartId = sender switch
                {
                    CartesianChart cc when cc.Name == "ExpensesChart" => "ExpensesChart",
                    PieChart pc when pc.Name == "ExpenseDistributionChart" => "ExpenseDistributionChart",
                    _ => string.Empty
                };

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
}
