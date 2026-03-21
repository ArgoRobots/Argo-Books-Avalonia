using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal for designing invoice templates.
/// Invoice preview is handled by InvoicePreviewControl which uses WebView2 on Windows.
/// </summary>
public partial class InvoiceTemplateDesignerModal : UserControl
{
    public InvoiceTemplateDesignerModal()
    {
        InitializeComponent();
    }

    private void OnTemplateCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            return;

        if (sender is Border { DataContext: InvoiceTemplate template }
            && DataContext is InvoiceTemplateDesignerViewModel vm)
        {
            vm.EditTemplateCommand.Execute(template);
        }
    }
}
