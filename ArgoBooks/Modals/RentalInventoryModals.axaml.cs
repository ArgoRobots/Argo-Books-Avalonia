using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for managing rental inventory items.
/// </summary>
public partial class RentalInventoryModals : UserControl
{
    private bool _eventsSubscribed;

    public RentalInventoryModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is RentalInventoryModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(RentalInventoryModalsViewModel.IsAddModalOpen):
                        if (vm.IsAddModalOpen)
                            ModalAnimationHelper.AnimateIn(AddModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddModalBorder);
                        break;
                    case nameof(RentalInventoryModalsViewModel.IsEditModalOpen):
                        if (vm.IsEditModalOpen)
                            ModalAnimationHelper.AnimateIn(EditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(EditModalBorder);
                        break;
                    case nameof(RentalInventoryModalsViewModel.IsFilterModalOpen):
                        if (vm.IsFilterModalOpen)
                            ModalAnimationHelper.AnimateIn(FilterModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(FilterModalBorder);
                        break;
                    case nameof(RentalInventoryModalsViewModel.IsRentOutModalOpen):
                        if (vm.IsRentOutModalOpen)
                            ModalAnimationHelper.AnimateIn(RentOutModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(RentOutModalBorder);
                        break;
                }
            };
        }
    }
}
