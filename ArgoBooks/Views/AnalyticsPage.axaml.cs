using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Analytics page.
/// </summary>
public partial class AnalyticsPage : UserControl
{
    public AnalyticsPage()
    {
        InitializeComponent();
    }

    private AnalyticsPageViewModel? ViewModel => DataContext as AnalyticsPageViewModel;
}
