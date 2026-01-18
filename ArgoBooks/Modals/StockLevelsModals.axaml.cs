using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for viewing and managing stock level records.
/// ESC key handling and animations are managed by ModalOverlay control.
/// </summary>
public partial class StockLevelsModals : UserControl
{
    public StockLevelsModals()
    {
        InitializeComponent();
    }
}
