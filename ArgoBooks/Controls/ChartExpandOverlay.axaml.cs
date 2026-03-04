using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using ArgoBooks.Core.Enums;
using ArgoBooks.ViewModels;
using ArgoBooks.Views;
using LiveChartsCore.SkiaSharpView.Avalonia;
using LiveChartsCore.SkiaSharpView.VisualElements;

namespace ArgoBooks.Controls;

/// <summary>
/// A modal overlay that allows charts to be expanded to fill most of the window.
/// Automatically discovers chart panels in the current page and adds expand buttons.
/// </summary>
public partial class ChartExpandOverlay : UserControl
{
    // LiveChartsCore's GeoMap stores its core chart engine in a private readonly
    // field named "_core". Its DetachedFromVisualTree handler calls _core.Unload(),
    // which throws NullReferenceException after the first unload because there is no
    // AttachedToVisualTree handler to reinitialize state. We temporarily null this
    // field during reparenting so the handler short-circuits (it checks for null).
    private static readonly FieldInfo? s_geoMapCoreField =
        typeof(GeoMap).GetField("_core", BindingFlags.NonPublic | BindingFlags.Instance);

    private Panel? _sourcePanel;
    private Button? _expandButton;
    private readonly List<Control> _movedChildren = new();
    private ContentControl? _pageContentControl;
    private readonly List<(object element, double originalSize)> _originalTitleSizes = new();
    private readonly List<(PieChartLegend legend, double origFontSize, double origIndicatorSize, double origMaxHeight, double origWidth, Thickness origMargin)> _originalLegendSizes = new();
    private readonly List<(PieChart chart, Thickness origMargin)> _originalPieChartMargins = new();

    public ChartExpandOverlay()
    {
        InitializeComponent();

        // Intercept right-clicks in tunnel phase to prevent LiveCharts selection box
        // and show the chart context menu
        AddHandler(PointerPressedEvent, OnOverlayPointerPressedTunnel,
            RoutingStrategies.Tunnel, handledEventsToo: true);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Dispatcher.UIThread.Post(Initialize, DispatcherPriority.Loaded);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (_pageContentControl != null)
            _pageContentControl.PropertyChanged -= OnPageContentPropertyChanged;
        base.OnDetachedFromVisualTree(e);
    }

    /// <summary>
    /// Finds the page host ContentControl and subscribes to navigation changes.
    /// </summary>
    private void Initialize()
    {
        _pageContentControl = FindPageContentControl();
        if (_pageContentControl != null)
        {
            _pageContentControl.PropertyChanged += OnPageContentPropertyChanged;
            DecorateCurrentPage();
        }
    }

    private void OnPageContentPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property != ContentControl.ContentProperty) return;

        // Close any open overlay when navigating away
        if (IsVisible) CloseOverlay();

        // Re-discover charts after the new page is loaded
        Dispatcher.UIThread.Post(DecorateCurrentPage, DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Finds the PageContentControl by searching the visual tree from the top level.
    /// </summary>
    private ContentControl? FindPageContentControl()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return null;

        return FindVisualDescendant<ContentControl>(topLevel, c => c.Name == "PageContentControl");
    }

    private static T? FindVisualDescendant<T>(Visual root, Func<T, bool> predicate) where T : Visual
    {
        foreach (var child in root.GetVisualChildren())
        {
            if (child is T match && predicate(match))
                return match;
            if (child is Visual visual)
            {
                var result = FindVisualDescendant(visual, predicate);
                if (result != null) return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Discovers chart panels in the current page and adds expand buttons.
    /// </summary>
    private void DecorateCurrentPage()
    {
        if (_pageContentControl?.Content is not Control pageContent) return;

        var chartPanels = new List<Panel>();
        FindChartPanels(pageContent, chartPanels);

        foreach (var panel in chartPanels)
        {
            AddExpandButton(panel);
        }
    }

    /// <summary>
    /// Recursively finds panels that directly contain chart controls
    /// (CartesianChart, PieChart, or GeoMap).
    /// </summary>
    private void FindChartPanels(Control control, List<Panel> result)
    {
        if (control == this) return;

        if (control is Panel panel)
        {
            bool hasChart = false;
            foreach (var child in panel.Children)
            {
                if (child is CartesianChart or PieChart or GeoMap)
                {
                    hasChart = true;
                    break;
                }

                // Check for pie charts inside a Grid (pattern: Grid > Grid > PieChart)
                if (child is Grid grid && !hasChart)
                {
                    hasChart = GridContainsChart(grid);
                }

                if (hasChart) break;
            }

            if (hasChart)
            {
                result.Add(panel);
                return; // Don't recurse into chart panels
            }

            foreach (var child in panel.Children)
            {
                FindChartPanels(child, result);
            }
        }
        else if (control is ContentControl cc && cc.Content is Control content)
        {
            FindChartPanels(content, result);
        }
        else if (control is Decorator decorator && decorator.Child != null)
        {
            FindChartPanels(decorator.Child, result);
        }
    }

    /// <summary>
    /// Checks if a Grid (or its child Grids) contains a chart control.
    /// </summary>
    private static bool GridContainsChart(Grid grid)
    {
        foreach (var child in grid.Children)
        {
            if (child is PieChart or PieChartLegend or CartesianChart or GeoMap)
                return true;
            if (child is Grid innerGrid && GridContainsChart(innerGrid))
                return true;
        }

        return false;
    }

    private void AddExpandButton(Panel panel)
    {
        // Guard against duplicate buttons
        foreach (var child in panel.Children)
        {
            if (child is Button btn && btn.Classes.Contains("chart-expand-btn"))
                return;
        }

        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(4),
            Content = new PathIcon
            {
                Data = Geometry.Parse(Icons.Fullscreen),
                Width = 12,
                Height = 12
            }
        };
        button.Classes.Add("chart-expand-btn");
        button.Click += OnExpandButtonClick;
        panel.Children.Add(button);
    }

    private void OnExpandButtonClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;
        if (button.Parent is not Panel sourcePanel) return;

        ShowChart(sourcePanel, button);
        e.Handled = true;
    }

    /// <summary>
    /// Shows the chart in the expanded overlay by reparenting its content.
    /// </summary>
    private void ShowChart(Panel sourcePanel, Button expandButton)
    {
        if (IsVisible) return;

        _sourcePanel = sourcePanel;
        _expandButton = expandButton;
        _movedChildren.Clear();

        var contentPanel = this.FindControl<Panel>("ContentPanel");
        if (contentPanel == null) return;

        // Preserve the page's DataContext so chart bindings (Series, Axes, etc.) keep working
        // after reparenting from the page into the AppShell-level overlay.
        // Set on ChartArea so the ChartContextMenu also inherits it.
        var chartArea = this.FindControl<Panel>("ChartArea");
        if (chartArea != null)
            chartArea.DataContext = sourcePanel.DataContext;

        // Move all children except the expand button to the overlay
        foreach (var child in sourcePanel.Children.ToList())
        {
            if (child != expandButton)
                _movedChildren.Add(child);
        }

        foreach (var child in _movedChildren)
        {
            SuppressGeoMapCoreDuringReparent(child, () =>
            {
                sourcePanel.Children.Remove(child);
                contentPanel.Children.Add(child);
            });
        }

        // Hide the expand button while overlay is open
        expandButton.IsVisible = false;

        // Enlarge chart titles and legends for the fullscreen view
        EnlargeChartElements(contentPanel);

        IsVisible = true;
        Focus();
    }

    /// <summary>
    /// Closes the overlay and returns chart content to its original panel.
    /// </summary>
    public void CloseOverlay()
    {
        if (!IsVisible || _sourcePanel == null) return;

        var contentPanel = this.FindControl<Panel>("ContentPanel");
        if (contentPanel == null) return;

        // Close context menu before clearing DataContext
        var chartArea = this.FindControl<Panel>("ChartArea");
        if (chartArea?.DataContext is ChartContextMenuViewModelBase vm && vm.IsChartContextMenuOpen)
            vm.HideChartContextMenuCommand.Execute(null);

        // Restore original sizes before reparenting back
        RestoreChartElements();

        var insertIndex = 0;
        foreach (var child in _movedChildren)
        {
            SuppressGeoMapCoreDuringReparent(child, () =>
            {
                contentPanel.Children.Remove(child);
                _sourcePanel!.Children.Insert(insertIndex, child);
            });
            insertIndex++;
        }

        if (_expandButton != null)
            _expandButton.IsVisible = true;

        // Clear the borrowed DataContext
        if (chartArea != null)
            chartArea.DataContext = null;

        _movedChildren.Clear();
        _sourcePanel = null;
        _expandButton = null;
        IsVisible = false;
    }

    /// <summary>
    /// Intercepts right-clicks on chart controls in the overlay to prevent LiveCharts'
    /// selection box and to show the chart context menu.
    /// </summary>
    private void OnOverlayPointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        if (!IsVisible) return;

        var source = e.Source as Control;
        var chart = source?.FindAncestorOfType<CartesianChart>() ?? source as CartesianChart;
        var pieChart = source?.FindAncestorOfType<PieChart>() ?? source as PieChart;
        var geoMap = source?.FindAncestorOfType<GeoMap>() ?? source as GeoMap;

        // Handle PieChartLegend: find the associated PieChart sibling
        if (chart == null && pieChart == null && geoMap == null)
        {
            var legend = source?.FindAncestorOfType<PieChartLegend>() ?? source as PieChartLegend;
            if (legend?.Parent is Grid parentGrid)
            {
                foreach (var child in parentGrid.Children)
                {
                    if (child is PieChart siblingChart)
                    {
                        pieChart = siblingChart;
                        break;
                    }
                }
            }
        }

        if (chart == null && pieChart == null && geoMap == null) return;
        if (!e.GetCurrentPoint(this).Properties.IsRightButtonPressed) return;

        // Prevent LiveCharts selection box
        e.Handled = true;

        var clickedControl = (Control?)chart ?? (Control?)pieChart ?? geoMap;
        var title = GetClickedChartTitle(clickedControl);

        // Set clicked chart on the source page for export operations
        SetPageClickedChart(clickedControl, title);

        // Show context menu via ViewModel
        var chartArea = this.FindControl<Panel>("ChartArea");
        if (chartArea?.DataContext is ChartContextMenuViewModelBase vm)
        {
            var position = e.GetPosition(chartArea);
            var chartDataType = clickedControl?.Tag as ChartDataType?;
            vm.ShowChartContextMenu(position.X, position.Y,
                chartDataType: chartDataType,
                isPieChart: pieChart != null,
                isGeoMap: geoMap != null,
                parentWidth: chartArea.Bounds.Width,
                parentHeight: chartArea.Bounds.Height);
        }
    }

    /// <summary>
    /// Sets the clicked chart reference on the source page so export operations work correctly.
    /// </summary>
    private void SetPageClickedChart(Control? chart, string name)
    {
        if (_pageContentControl?.Content is DashboardPage dashPage)
            dashPage.SetClickedChart(chart, name);
        else if (_pageContentControl?.Content is AnalyticsPage analyticsPage)
            analyticsPage.SetClickedChart(chart, name);
    }

    /// <summary>
    /// Gets the chart title from a specific clicked chart control.
    /// Mirrors the page-level GetChartTitle approach so that the chart title
    /// matches what ChartLoaderService expects for exports.
    /// </summary>
    private static string GetClickedChartTitle(Control? control)
    {
        if (control is CartesianChart cc &&
            cc.Title is LabelVisual cartLabel &&
            !string.IsNullOrWhiteSpace(cartLabel.Text))
        {
            return cartLabel.Text;
        }

        if (control is PieChart pieChart &&
            pieChart.Title is LabelVisual pieLabel &&
            !string.IsNullOrWhiteSpace(pieLabel.Text))
        {
            return pieLabel.Text;
        }

        // For PieChart/GeoMap without a Title property, navigate parent Grids to
        // find the TextBlock title: Grid(Row) > Grid(Column) > PieChart/GeoMap
        if (control is PieChart or PieChartLegend or GeoMap)
        {
            var columnGrid = control?.Parent as Grid;
            var rowGrid = columnGrid?.Parent as Grid;
            if (rowGrid != null)
            {
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

        return "Chart";
    }

    /// <summary>
    /// Temporarily nulls the GeoMap's internal <c>_core</c> field so that its
    /// <c>DetachedFromVisualTree</c> handler skips <c>GeoMapChart.Unload()</c>
    /// during a reparent operation. The field is restored after the action.
    /// For non-GeoMap controls the action runs directly.
    /// </summary>
    private static void SuppressGeoMapCoreDuringReparent(Control child, Action reparent)
    {
        if (child is not GeoMap || s_geoMapCoreField == null)
        {
            reparent();
            return;
        }

        object? savedCore = null;
        try
        {
            savedCore = s_geoMapCoreField.GetValue(child);
            s_geoMapCoreField.SetValue(child, null);
        }
        catch
        {
            // Reflection may fail on some runtimes; fall through to direct call.
        }

        try
        {
            reparent();
        }
        finally
        {
            if (savedCore != null)
            {
                try { s_geoMapCoreField.SetValue(child, savedCore); }
                catch { /* best effort */ }
            }
        }
    }

    /// <summary>
    /// Enlarges chart titles and pie chart legends for the fullscreen modal view.
    /// </summary>
    private void EnlargeChartElements(Panel contentPanel)
    {
        _originalTitleSizes.Clear();
        _originalLegendSizes.Clear();

        EnlargeChartElementsRecursive(contentPanel);
    }

    private void EnlargeChartElementsRecursive(Control control)
    {
        // Enlarge CartesianChart titles
        if (control is CartesianChart cc && cc.Title is LabelVisual cartLabel)
        {
            _originalTitleSizes.Add((cartLabel, cartLabel.TextSize));
            cartLabel.TextSize = 26;
        }

        // Enlarge PieChart titles and add margin to shrink pie slightly
        if (control is PieChart pc)
        {
            if (pc.Title is LabelVisual pieLabel)
            {
                _originalTitleSizes.Add((pieLabel, pieLabel.TextSize));
                pieLabel.TextSize = 26;
            }

            _originalPieChartMargins.Add((pc, pc.Margin));
            pc.Margin = new Thickness(40, 20, 0, 20);
        }

        // Enlarge TextBlock titles (used by pie charts and other charts without LabelVisual)
        if (control is TextBlock tb && tb.FontWeight == FontWeight.SemiBold && tb.FontSize < 20)
        {
            _originalTitleSizes.Add((tb, tb.FontSize));
            tb.FontSize = 24;
        }

        // Enlarge PieChartLegend
        if (control is PieChartLegend legend)
        {
            _originalLegendSizes.Add((legend, legend.LegendFontSize, legend.IndicatorSize, legend.MaxHeightOverride, legend.Width, legend.Margin));
            legend.LegendFontSize = 20;
            legend.IndicatorSize = 18;
            legend.MaxHeightOverride = 600;
            legend.Width = 340;
            legend.Margin = new Thickness(8, 0, 24, 0);
        }

        // Recurse into children
        if (control is Panel panel)
        {
            foreach (var child in panel.Children)
                EnlargeChartElementsRecursive(child);
        }
        else if (control is ContentControl contentControl && contentControl.Content is Control content)
        {
            EnlargeChartElementsRecursive(content);
        }
        else if (control is Decorator decorator && decorator.Child != null)
        {
            EnlargeChartElementsRecursive(decorator.Child);
        }
    }

    /// <summary>
    /// Restores chart titles and legends to their original sizes.
    /// </summary>
    private void RestoreChartElements()
    {
        foreach (var (element, originalSize) in _originalTitleSizes)
        {
            if (element is LabelVisual label)
                label.TextSize = originalSize;
            else if (element is TextBlock tb)
                tb.FontSize = originalSize;
        }

        foreach (var (legend, origFontSize, origIndicatorSize, origMaxHeight, origWidth, origMargin) in _originalLegendSizes)
        {
            legend.LegendFontSize = origFontSize;
            legend.IndicatorSize = origIndicatorSize;
            legend.MaxHeightOverride = origMaxHeight;
            legend.Width = origWidth;
            legend.Margin = origMargin;
        }

        foreach (var (chart, origMargin) in _originalPieChartMargins)
        {
            chart.Margin = origMargin;
        }

        _originalTitleSizes.Clear();
        _originalLegendSizes.Clear();
        _originalPieChartMargins.Clear();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        CloseOverlay();
    }

    private void OnBackdropClick(object? sender, PointerPressedEventArgs e)
    {
        CloseOverlay();
    }

    private void OnModalContentClick(object? sender, PointerPressedEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && IsVisible)
        {
            CloseOverlay();
            e.Handled = true;
        }

        base.OnKeyDown(e);
    }
}
