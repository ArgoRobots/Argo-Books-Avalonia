using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

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

    public static readonly StyledProperty<bool> ShowResetZoomProperty =
        AvaloniaProperty.Register<ChartContextMenu, bool>(nameof(ShowResetZoom), true);

    public static readonly StyledProperty<bool> ShowExportOptionsProperty =
        AvaloniaProperty.Register<ChartContextMenu, bool>(nameof(ShowExportOptions), true);

    public static readonly StyledProperty<ICommand?> CloseCommandProperty =
        AvaloniaProperty.Register<ChartContextMenu, ICommand?>(nameof(CloseCommand));

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

    /// <summary>
    /// Gets or sets whether to show the Reset Zoom option (hide for pie charts).
    /// </summary>
    public bool ShowResetZoom
    {
        get => GetValue(ShowResetZoomProperty);
        set => SetValue(ShowResetZoomProperty, value);
    }

    /// <summary>
    /// Gets or sets whether to show export options (hide for geomaps).
    /// </summary>
    public bool ShowExportOptions
    {
        get => GetValue(ShowExportOptionsProperty);
        set => SetValue(ShowExportOptionsProperty, value);
    }

    /// <summary>
    /// Gets or sets the command to close the context menu.
    /// </summary>
    public ICommand? CloseCommand
    {
        get => GetValue(CloseCommandProperty);
        set => SetValue(CloseCommandProperty, value);
    }

    #endregion

    public ChartContextMenu()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsOpenProperty)
        {
            var animationBorder = this.FindControl<Border>("AnimationBorder");
            if (animationBorder == null) return;

            if (IsOpen)
            {
                // Animate in
                Dispatcher.UIThread.Post(() =>
                {
                    animationBorder.Opacity = 1;
                    animationBorder.RenderTransform = new TranslateTransform(0, 0);
                }, DispatcherPriority.Render);
            }
            else
            {
                // Reset for next open
                Dispatcher.UIThread.Post(() =>
                {
                    animationBorder.Opacity = 0;
                    animationBorder.RenderTransform = new TranslateTransform(0, -8);
                }, DispatcherPriority.Background);
            }
        }
    }

    /// <summary>
    /// Handles pointer pressed on the overlay to close the context menu.
    /// </summary>
    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (CloseCommand?.CanExecute(null) == true)
        {
            CloseCommand.Execute(null);
        }
        else
        {
            // Fallback: directly set IsOpen to false
            IsOpen = false;
        }
        e.Handled = true;
    }
}
