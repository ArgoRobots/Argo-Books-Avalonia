using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls;

/// <summary>
/// A reusable context menu for chart right-click actions.
/// </summary>
public partial class ChartContextMenu : UserControl
{
    #region Styled Properties

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ChartContextMenu, bool>(nameof(IsOpen));

    public static readonly StyledProperty<double> MenuXProperty =
        AvaloniaProperty.Register<ChartContextMenu, double>(nameof(MenuX));

    public static readonly StyledProperty<double> MenuYProperty =
        AvaloniaProperty.Register<ChartContextMenu, double>(nameof(MenuY));

    public static readonly StyledProperty<ICommand?> SaveChartAsImageCommandProperty =
        AvaloniaProperty.Register<ChartContextMenu, ICommand?>(nameof(SaveChartAsImageCommand));

    public static readonly StyledProperty<ICommand?> ExportToGoogleSheetsCommandProperty =
        AvaloniaProperty.Register<ChartContextMenu, ICommand?>(nameof(ExportToGoogleSheetsCommand));

    public static readonly StyledProperty<ICommand?> ExportToExcelCommandProperty =
        AvaloniaProperty.Register<ChartContextMenu, ICommand?>(nameof(ExportToExcelCommand));

    public static readonly StyledProperty<ICommand?> ResetChartZoomCommandProperty =
        AvaloniaProperty.Register<ChartContextMenu, ICommand?>(nameof(ResetChartZoomCommand));

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets whether the context menu is visible.
    /// </summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <summary>
    /// Gets or sets the X position of the context menu.
    /// </summary>
    public double MenuX
    {
        get => GetValue(MenuXProperty);
        set => SetValue(MenuXProperty, value);
    }

    /// <summary>
    /// Gets or sets the Y position of the context menu.
    /// </summary>
    public double MenuY
    {
        get => GetValue(MenuYProperty);
        set => SetValue(MenuYProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to save the chart as an image.
    /// </summary>
    public ICommand? SaveChartAsImageCommand
    {
        get => GetValue(SaveChartAsImageCommandProperty);
        set => SetValue(SaveChartAsImageCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to export to Google Sheets.
    /// </summary>
    public ICommand? ExportToGoogleSheetsCommand
    {
        get => GetValue(ExportToGoogleSheetsCommandProperty);
        set => SetValue(ExportToGoogleSheetsCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to export to Excel.
    /// </summary>
    public ICommand? ExportToExcelCommand
    {
        get => GetValue(ExportToExcelCommandProperty);
        set => SetValue(ExportToExcelCommandProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to reset chart zoom.
    /// </summary>
    public ICommand? ResetChartZoomCommand
    {
        get => GetValue(ResetChartZoomCommandProperty);
        set => SetValue(ResetChartZoomCommandProperty, value);
    }

    #endregion

    public ChartContextMenu()
    {
        InitializeComponent();
    }
}
