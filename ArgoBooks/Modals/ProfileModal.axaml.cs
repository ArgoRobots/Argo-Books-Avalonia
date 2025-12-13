using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

public partial class ProfileModal : UserControl
{
    public ProfileModal()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Closes the modal when clicking on the backdrop.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ProfileModalViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }
}
