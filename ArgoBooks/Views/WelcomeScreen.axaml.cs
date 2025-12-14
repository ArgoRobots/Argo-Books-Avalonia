using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Views;

public partial class WelcomeScreen : UserControl
{
    public WelcomeScreen()
    {
        InitializeComponent();
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
