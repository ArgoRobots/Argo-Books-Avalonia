using ArgoBooks.Controls;
using ArgoBooks.Core.Models.Invoices;
using ArgoBooks.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal for designing invoice templates.
/// Invoice preview is handled by InvoicePreviewControl using NativeWebView.
/// </summary>
public partial class InvoiceTemplateDesignerModal : UserControl
{
    private InvoiceTemplateDesignerViewModel? _previousViewModel;

    public InvoiceTemplateDesignerModal()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (_previousViewModel != null)
        {
            _previousViewModel.CapturePreviewFunc = null;
        }

        if (DataContext is InvoiceTemplateDesignerViewModel vm)
        {
            var previewControl = this.FindControl<InvoicePreviewControl>("PreviewControl");
            if (previewControl != null)
            {
                vm.CapturePreviewFunc = previewControl.CaptureScreenshotBase64Async;
            }
            _previousViewModel = vm;
        }
        else
        {
            _previousViewModel = null;
        }
    }

    private void OnTemplateCardPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
            return;

        if (sender is Border { DataContext: InvoiceTemplate template }
            && DataContext is InvoiceTemplateDesignerViewModel vm)
        {
            if (vm.EditTemplateCommand.CanExecute(template))
                vm.EditTemplateCommand.Execute(template);
        }
    }

    private void OnTemplateCardKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter && e.Key != Key.Space)
            return;

        if (sender is Border { DataContext: InvoiceTemplate template }
            && DataContext is InvoiceTemplateDesignerViewModel vm)
        {
            if (vm.EditTemplateCommand.CanExecute(template))
                vm.EditTemplateCommand.Execute(template);
            e.Handled = true;
        }
    }
}
