using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Views;

/// <summary>
/// Welcome screen displayed when no company is loaded.
/// </summary>
public partial class WelcomeScreen : UserControl
{
    public WelcomeScreen()
    {
        InitializeComponent();

        // Set up window drag behavior for the title bar region
        var dragRegion = this.FindControl<Border>("TitleBarDragRegion");
        if (dragRegion != null)
        {
            dragRegion.PointerPressed += OnTitleBarPointerPressed;
        }
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            // Find the parent window
            var window = TopLevel.GetTopLevel(this) as Window;
            if (window == null) return;

            // Double-click to maximize/restore
            if (e.ClickCount == 2)
            {
                window.WindowState = window.WindowState == WindowState.Maximized
                    ? WindowState.Normal
                    : WindowState.Maximized;
            }
            else
            {
                // Single click to start drag
                window.BeginMoveDrag(e);
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        // Auto-scroll to top when the welcome screen becomes visible
        if (change.Property == IsVisibleProperty && change.NewValue is true)
        {
            MainScrollViewer?.ScrollToHome();
        }
    }
}
