using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Full-page invoice editor host. Reuses <see cref="InvoiceModalsViewModel"/> for all invoice logic;
/// the page is prepared via <c>PrepareForEditor</c> in the navigation factory.
/// </summary>
public partial class InvoiceEditorPage : UserControl
{
    private InvoiceModalsViewModel? _vm;

    public InvoiceEditorPage()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _vm = DataContext as InvoiceModalsViewModel;
        if (_vm != null)
            _vm.InvoiceSaved += OnInvoiceSaved;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        if (_vm != null)
        {
            _vm.InvoiceSaved -= OnInvoiceSaved;
            _vm = null;
        }
    }

    // Saved or sent: return to the invoices list.
    private void OnInvoiceSaved(object? sender, EventArgs e)
        => Dispatcher.UIThread.Post(() => App.NavigationService?.GoBack());

    private void OnBackClick(object? sender, RoutedEventArgs e)
        => App.NavigationService?.GoBack();
}
