using Avalonia;
using Avalonia.Controls;

namespace ArgoBooks.Controls.Dashboard.Widgets;

public partial class QuickActionsWidget : UserControl
{
    private const double CompactQuickActionsThreshold = 1050;

    public QuickActionsWidget()
    {
        InitializeComponent();
        SizeChanged += OnQuickActionsSizeChanged;
    }

    private void OnQuickActionsSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var width = e.NewSize.Width;
        if (width < CompactQuickActionsThreshold)
        {
            foreach (var child in QuickActionsWrapPanel.Children)
            {
                if (child is Button btn)
                {
                    btn.Padding = new Thickness(8, 6);
                    btn.Margin = new Thickness(3);
                }
            }
        }
        else
        {
            foreach (var child in QuickActionsWrapPanel.Children)
            {
                if (child is Button btn)
                {
                    btn.Padding = new Thickness(12, 10);
                    btn.Margin = new Thickness(6);
                }
            }
        }
    }
}
