using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for configuring report page settings like margins and orientation.
/// </summary>
public partial class ReportPageSettingsModal : UserControl
{
    public ReportPageSettingsModal()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is ReportsPageViewModel vm)
        {
            vm.ClosePageSettingsCommand.Execute(null);
            e.Handled = true;
        }
    }
}
