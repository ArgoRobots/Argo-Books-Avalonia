using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

public partial class FileMenuPanel : UserControl
{
    public FileMenuPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Closes the panel when clicking on the backdrop.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is FileMenuPanelViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }
}
