using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and viewing stock adjustment records.
/// ESC key handling and animations are managed by ModalOverlay control.
/// </summary>
public partial class StockAdjustmentsModals : UserControl
{
    public StockAdjustmentsModals()
    {
        InitializeComponent();
    }
}
