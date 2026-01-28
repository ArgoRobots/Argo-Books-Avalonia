using Avalonia.Controls;

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
}
