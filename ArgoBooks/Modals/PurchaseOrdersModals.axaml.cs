using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Purchase Orders modals.
/// </summary>
public partial class PurchaseOrdersModals : UserControl
{
    private bool _eventsSubscribed;

    public PurchaseOrdersModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PurchaseOrdersModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(PurchaseOrdersModalsViewModel.IsAddModalOpen):
                        if (vm.IsAddModalOpen)
                            ModalAnimationHelper.AnimateIn(AddEditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddEditModalBorder);
                        break;

                    case nameof(PurchaseOrdersModalsViewModel.IsViewModalOpen):
                        if (vm.IsViewModalOpen)
                            ModalAnimationHelper.AnimateIn(ViewModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(ViewModalBorder);
                        break;

                    case nameof(PurchaseOrdersModalsViewModel.IsReceiveModalOpen):
                        if (vm.IsReceiveModalOpen)
                            ModalAnimationHelper.AnimateIn(ReceiveModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(ReceiveModalBorder);
                        break;

                    case nameof(PurchaseOrdersModalsViewModel.IsFilterModalOpen):
                        if (vm.IsFilterModalOpen)
                            ModalAnimationHelper.AnimateIn(FilterModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(FilterModalBorder);
                        break;
                }
            };
        }
    }
}
