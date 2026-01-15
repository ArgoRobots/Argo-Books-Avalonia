using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating, editing, and filtering invoices.
/// </summary>
public partial class InvoiceModals : UserControl
{
    private bool _eventsSubscribed;

    public InvoiceModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is InvoiceModalsViewModel vm)
        {
            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(InvoiceModalsViewModel.IsCreateEditModalOpen))
                    {
                        if (vm.IsCreateEditModalOpen)
                            ModalAnimationHelper.AnimateIn(CreateEditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(CreateEditModalBorder);
                    }
                    else if (args.PropertyName == nameof(InvoiceModalsViewModel.IsFilterModalOpen))
                    {
                        if (vm.IsFilterModalOpen)
                            ModalAnimationHelper.AnimateIn(FilterModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(FilterModalBorder);
                    }
                    else if (args.PropertyName == nameof(InvoiceModalsViewModel.IsHistoryModalOpen))
                    {
                        if (vm.IsHistoryModalOpen)
                            ModalAnimationHelper.AnimateIn(HistoryModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(HistoryModalBorder);
                    }
                    else if (args.PropertyName == nameof(InvoiceModalsViewModel.IsPreviewModalOpen))
                    {
                        if (vm.IsPreviewModalOpen)
                            ModalAnimationHelper.AnimateIn(PreviewModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(PreviewModalBorder);
                    }
                };
            }
        }
    }

    private void OnOverlayPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Close modals when clicking the overlay
        if (DataContext is InvoiceModalsViewModel vm)
        {
            if (vm.IsCreateEditModalOpen)
                vm.CloseCreateEditModalCommand.Execute(null);
            else if (vm.IsFilterModalOpen)
                vm.CloseFilterModalCommand.Execute(null);
            else if (vm.IsHistoryModalOpen)
                vm.CloseHistoryModalCommand.Execute(null);
        }
    }
}
