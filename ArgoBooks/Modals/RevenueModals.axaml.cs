using Avalonia.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class RevenueModals : UserControl
{
    public RevenueModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is RevenueModalsViewModel vm)
        {
            vm.ScrollToLineItemsRequested += OnScrollToLineItemsRequested;
        }
    }

    private void OnScrollToLineItemsRequested(object? sender, EventArgs e)
    {
        // Scroll to bring the line items section into view
        LineItemsSection?.BringIntoView();
    }
}
