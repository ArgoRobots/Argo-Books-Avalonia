using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing payment records.
/// </summary>
public partial class PaymentModals : UserControl
{
    private bool _eventsSubscribed;

    public PaymentModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is PaymentModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(PaymentModalsViewModel.IsAddModalOpen))
                {
                    if (vm.IsAddModalOpen)
                        ModalAnimationHelper.AnimateIn(AddModalBorder);
                    else
                        ModalAnimationHelper.AnimateOut(AddModalBorder);
                }
                else if (args.PropertyName == nameof(PaymentModalsViewModel.IsEditModalOpen))
                {
                    if (vm.IsEditModalOpen)
                        ModalAnimationHelper.AnimateIn(EditModalBorder);
                    else
                        ModalAnimationHelper.AnimateOut(EditModalBorder);
                }
                else if (args.PropertyName == nameof(PaymentModalsViewModel.IsFilterModalOpen))
                {
                    if (vm.IsFilterModalOpen)
                        ModalAnimationHelper.AnimateIn(FilterModalBorder);
                    else
                        ModalAnimationHelper.AnimateOut(FilterModalBorder);
                }
            };
        }
    }
}
