using Avalonia.Controls;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating, editing, and filtering invoices.
/// Invoice preview is handled by InvoicePreviewControl which uses WebView2 on Windows.
/// </summary>
public partial class InvoiceModals : UserControl
{
    public InvoiceModals()
    {
        InitializeComponent();
    }
}
