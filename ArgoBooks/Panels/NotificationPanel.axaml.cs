using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Panels;

public partial class NotificationPanel : UserControl
{
    public NotificationPanel()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Closes the panel when clicking on the backdrop.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is NotificationPanelViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
    }
}
