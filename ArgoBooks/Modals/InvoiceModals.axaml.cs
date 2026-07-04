using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
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
        {
            editorPreview.InvoiceEdited += OnInvoiceEdited;
            editorPreview.ProductPicked += OnProductPicked;
            editorPreview.CreateProductRequested += OnCreateProductRequested;
            editorPreview.AddLineRequested += OnAddLineRequested;
            editorPreview.RemoveLineRequested += OnRemoveLineRequested;
            editorPreview.CustomerPicked += OnCustomerPicked;
            editorPreview.CreateCustomerRequested += OnCreateCustomerRequested;
            editorPreview.DateEdited += OnDateEdited;
            editorPreview.PickLogoRequested += OnPickLogoRequested;
        }
    }

    private void OnCustomerPicked(object? sender, string customerId)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.SelectCustomerFromPaper(customerId);
    }

    private void OnCreateCustomerRequested(object? sender, EventArgs e)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.CreateCustomerFromPaper();
    }

    private void OnDateEdited(object? sender, (string Field, string Value) e)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.SetDateFromPaper(e.Field, e.Value);
    }

    // Let the user pick a logo image from the invoice paper; embed it as base64 on the template.
    private async void OnPickLogoRequested(object? sender, EventArgs e)
    {
        if (DataContext is not InvoiceModalsViewModel vm) return;
        var top = TopLevel.GetTopLevel(this);
        if (top == null) return;

        var files = await top.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Choose a logo",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Images")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.gif", "*.webp", "*.bmp" }
                }
            }
        });

        if (files.Count == 0) return;

        try
        {
            await using var stream = await files[0].OpenReadAsync();
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            vm.SetLogoFromPaper(Convert.ToBase64String(ms.ToArray()));
        }
        catch
        {
            // Ignore unreadable/oversized images; the user can pick another.
        }
    }

    private void OnAddLineRequested(object? sender, EventArgs e)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.AddLineFromPaper();
    }

    private void OnRemoveLineRequested(object? sender, int index)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.RemoveLineFromPaper(index);
    }

    // Route an edit made directly on the invoice paper back into the view-model.
    private void OnInvoiceEdited(object? sender, InvoiceEditEventArgs e)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.ApplyPaperEdit(e.Field, e.Index, e.Value);
    }

    private void OnProductPicked(object? sender, ProductPickEventArgs e)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.SelectProductForLine(e.Index, e.ProductId);
    }

    private void OnCreateProductRequested(object? sender, int index)
    {
        if (DataContext is InvoiceModalsViewModel vm)
            vm.CreateProductForLine(index);
    }
}
