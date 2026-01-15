using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialog for saving report layouts as reusable templates.
/// </summary>
public partial class ReportSaveTemplateModal : UserControl
{
    public ReportSaveTemplateModal()
    {
        InitializeComponent();
    }

    private void Modal_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is ReportsPageViewModel vm)
        {
            vm.CloseSaveTemplateCommand.Execute(null);
            e.Handled = true;
        }
    }
}
