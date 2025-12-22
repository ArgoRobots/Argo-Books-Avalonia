using ArgoBooks.Controls.Reports;
using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ArgoBooks.Views;

public partial class ReportsPage : UserControl
{
    public ReportsPage()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Wire up CTRL+scroll zoom for the design canvas
        if (this.FindControl<ReportDesignCanvas>("DesignCanvas") is { } designCanvas)
        {
            designCanvas.PointerWheelChanged += OnCanvasPointerWheelChanged;
        }

        // Wire up CTRL+scroll zoom for the preview canvas
        if (this.FindControl<ScrollViewer>("PreviewScrollViewer") is { } previewScroller)
        {
            previewScroller.PointerWheelChanged += OnPreviewPointerWheelChanged;
        }
    }

    private void OnCanvasPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (DataContext is ReportsPageViewModel vm)
            {
                if (e.Delta.Y > 0)
                    vm.ZoomInCommand.Execute(null);
                else if (e.Delta.Y < 0)
                    vm.ZoomOutCommand.Execute(null);

                e.Handled = true;
            }
        }
    }

    private void OnPreviewPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
        {
            if (DataContext is ReportsPageViewModel vm)
            {
                if (e.Delta.Y > 0)
                    vm.PreviewZoomInCommand.Execute(null);
                else if (e.Delta.Y < 0)
                    vm.PreviewZoomOutCommand.Execute(null);

                e.Handled = true;
            }
        }
    }
}
