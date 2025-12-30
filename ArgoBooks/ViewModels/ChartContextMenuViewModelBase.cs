using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ArgoBooks.ViewModels;

/// <summary>
/// Base class for ViewModels that support chart context menu functionality.
/// Provides common properties and commands for showing/hiding the context menu
/// and handling chart actions like save, export, and zoom reset.
/// </summary>
public abstract partial class ChartContextMenuViewModelBase : ViewModelBase
{
    #region Chart Context Menu Properties

    [ObservableProperty]
    private bool _isChartContextMenuOpen;

    [ObservableProperty]
    private double _chartContextMenuX;

    [ObservableProperty]
    private double _chartContextMenuY;

    [ObservableProperty]
    private bool _showChartResetZoom = true;

    [ObservableProperty]
    private bool _showChartExportOptions = true;

    /// <summary>
    /// Gets or sets the identifier of the currently selected chart for context menu operations.
    /// </summary>
    [ObservableProperty]
    private string _selectedChartId = string.Empty;

    #endregion

    #region Chart Context Menu Methods

    /// <summary>
    /// Shows the chart context menu at the specified position.
    /// </summary>
    /// <param name="x">The X coordinate.</param>
    /// <param name="y">The Y coordinate.</param>
    /// <param name="chartId">The identifier of the chart that was clicked.</param>
    /// <param name="isPieChart">True if the chart is a pie chart (hides reset zoom).</param>
    /// <param name="isGeoMap">True if the chart is a geo map (only shows save as image).</param>
    public void ShowChartContextMenu(double x, double y, string chartId = "", bool isPieChart = false, bool isGeoMap = false)
    {
        ChartContextMenuX = x;
        ChartContextMenuY = y;
        SelectedChartId = chartId;
        ShowChartResetZoom = !isPieChart && !isGeoMap;
        ShowChartExportOptions = !isGeoMap;
        IsChartContextMenuOpen = true;
    }

    /// <summary>
    /// Hides the chart context menu.
    /// </summary>
    [RelayCommand]
    private void HideChartContextMenu()
    {
        IsChartContextMenuOpen = false;
    }

    #endregion

    #region Chart Context Menu Commands

    /// <summary>
    /// Saves the chart as an image file.
    /// </summary>
    [RelayCommand]
    private void SaveChartAsImage()
    {
        IsChartContextMenuOpen = false;
        OnSaveChartAsImage();
    }

    /// <summary>
    /// Exports the chart data to Google Sheets.
    /// </summary>
    [RelayCommand]
    private void ExportToGoogleSheets()
    {
        IsChartContextMenuOpen = false;
        OnExportToGoogleSheets();
    }

    /// <summary>
    /// Exports the chart data to Microsoft Excel.
    /// </summary>
    [RelayCommand]
    private void ExportToExcel()
    {
        IsChartContextMenuOpen = false;
        OnExportToExcel();
    }

    /// <summary>
    /// Resets the zoom on the chart.
    /// </summary>
    [RelayCommand]
    private void ResetChartZoom()
    {
        IsChartContextMenuOpen = false;
        OnResetChartZoom();
    }

    #endregion

    #region Virtual Methods for Derived Classes

    /// <summary>
    /// Called when the user requests to save the chart as an image.
    /// Override in derived classes to implement specific behavior.
    /// </summary>
    protected virtual void OnSaveChartAsImage() { }

    /// <summary>
    /// Called when the user requests to export to Google Sheets.
    /// Override in derived classes to implement specific behavior.
    /// </summary>
    protected virtual void OnExportToGoogleSheets() { }

    /// <summary>
    /// Called when the user requests to export to Excel.
    /// Override in derived classes to implement specific behavior.
    /// </summary>
    protected virtual void OnExportToExcel() { }

    /// <summary>
    /// Called when the user requests to reset the chart zoom.
    /// Override in derived classes to implement specific behavior.
    /// </summary>
    protected virtual void OnResetChartZoom() { }

    #endregion
}
