using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for report template management.
/// Animation is handled automatically by ModalAnimationBehavior in XAML.
/// </summary>
public partial class ReportModals : UserControl
{
    public ReportModals()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles Enter key press in the rename template TextBox.
    /// </summary>
    private void OnRenameTemplateKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ReportModalsViewModel vm)
        {
            vm.ReportsPageViewModel?.ConfirmRenameTemplateCommand.Execute(null);
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && DataContext is ReportModalsViewModel escVm)
        {
            escVm.ReportsPageViewModel?.CloseRenameTemplateCommand.Execute(null);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Handles Escape key press in the delete template modal.
    /// </summary>
    private void OnDeleteTemplateKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && DataContext is ReportModalsViewModel vm)
        {
            vm.ReportsPageViewModel?.CloseDeleteTemplateCommand.Execute(null);
            e.Handled = true;
        }
    }
}
