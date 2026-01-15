using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and viewing stock adjustment records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class StockAdjustmentsModals : UserControl
{
    public StockAdjustmentsModals()
    {
        InitializeComponent();
    }
}
