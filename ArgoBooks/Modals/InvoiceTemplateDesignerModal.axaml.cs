using ArgoBooks.Controls;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

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

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is InvoiceTemplateDesignerViewModel vm)
        {
            var previewControl = this.FindControl<InvoicePreviewControl>("PreviewControl");
            if (previewControl != null)
            {
                vm.CapturePreviewFunc = previewControl.CaptureScreenshotBase64Async;
            }
        }
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
