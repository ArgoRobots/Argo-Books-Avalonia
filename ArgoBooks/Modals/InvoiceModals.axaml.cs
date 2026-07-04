using Avalonia.Controls;
using ArgoBooks.Controls;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating, editing, and filtering invoices.
/// Invoice preview and editing are handled by InvoicePreviewControl using NativeWebView.
/// </summary>
public partial class InvoiceModals : UserControl
{
    public InvoiceModals()
    {
        InitializeComponent();

        var editorPreview = this.FindControl<InvoicePreviewControl>("EditorPreview");
        if (editorPreview != null)
            editorPreview.InvoiceEdited += OnInvoiceEdited;
    }

    // Route an edit made directly on the invoice paper back into the view-model.
    private void OnInvoiceEdited(object? sender, InvoiceEditEventArgs e)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.ApplyPaperEdit(e.Field, e.Index, e.Value);
    }
}
