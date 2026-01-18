using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing rental transaction records.
/// ESC key handling and animations are managed by ModalOverlay control.
/// </summary>
public partial class RentalRecordsModals : UserControl
{
    public RentalRecordsModals()
    {
        InitializeComponent();
    }
}
