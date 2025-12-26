using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

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
                // Get position relative to the chart container
                var position = e.GetPosition(sender as Control);
                viewModel.ShowChartContextMenu(position.X, position.Y);
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
        if (DataContext is DashboardPageViewModel viewModel && viewModel.IsChartContextMenuOpen)
        {
            // Check if click is outside the context menu
            var contextMenu = this.FindControl<Border>("ChartContextMenu");
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
