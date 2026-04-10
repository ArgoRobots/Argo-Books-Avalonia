using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Controls.Dashboard;

public partial class WidgetCatalogPanel : UserControl
{
    public WidgetCatalogPanel()
    {
        InitializeComponent();
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ViewModels.Dashboard.WidgetCatalogViewModel vm)
        {
            vm.CloseCommand.Execute(null);
        }
        e.Handled = true;
    }
}
