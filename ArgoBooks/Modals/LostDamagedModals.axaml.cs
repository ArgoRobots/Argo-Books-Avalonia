using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for managing lost/damaged item records.
/// Animation and ESC key handling are provided by ModalOverlay control.
/// </summary>
public partial class LostDamagedModals : UserControl
{
    public LostDamagedModals()
    {
        InitializeComponent();
    }
}
