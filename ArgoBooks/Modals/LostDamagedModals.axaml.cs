using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for managing lost/damaged item records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class LostDamagedModals : UserControl
{
    public LostDamagedModals()
    {
        InitializeComponent();
    }
}
