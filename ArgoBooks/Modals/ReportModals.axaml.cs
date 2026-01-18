using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for report template management.
/// ESC key handling is now managed by ModalOverlay.
/// </summary>
public partial class ReportModals : UserControl
{
    public ReportModals()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handles Enter key press in the rename template TextBox to confirm the rename.
    /// </summary>
    private void OnRenameTemplateKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && DataContext is ReportModalsViewModel vm)
        {
            vm.ReportsPageViewModel?.ConfirmRenameTemplateCommand.Execute(null);
            e.Handled = true;
        }
    }
}
