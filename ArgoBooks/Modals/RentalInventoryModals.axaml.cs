using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for managing rental inventory items.
/// ESC key handling and animations are managed by ModalOverlay control.
/// </summary>
public partial class RentalInventoryModals : UserControl
{
    public RentalInventoryModals()
    {
        InitializeComponent();
    }
}
