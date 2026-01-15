using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for viewing and managing stock level records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class StockLevelsModals : UserControl
{
    public StockLevelsModals()
    {
        InitializeComponent();
    }
}
