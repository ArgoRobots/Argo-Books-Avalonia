using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing rental transaction records.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class RentalRecordsModals : UserControl
{
    public RentalRecordsModals()
    {
        InitializeComponent();
    }
}
