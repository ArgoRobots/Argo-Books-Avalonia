using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing rental transaction records.
/// </summary>
public partial class RentalRecordsModals : UserControl
{
    private bool _eventsSubscribed;

    public RentalRecordsModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is RentalRecordsModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(RentalRecordsModalsViewModel.IsAddModalOpen):
                        if (vm.IsAddModalOpen)
                            ModalAnimationHelper.AnimateIn(AddModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddModalBorder);
                        break;
                    case nameof(RentalRecordsModalsViewModel.IsEditModalOpen):
                        if (vm.IsEditModalOpen)
                            ModalAnimationHelper.AnimateIn(EditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(EditModalBorder);
                        break;
                    case nameof(RentalRecordsModalsViewModel.IsFilterModalOpen):
                        if (vm.IsFilterModalOpen)
                            ModalAnimationHelper.AnimateIn(FilterModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(FilterModalBorder);
                        break;
                    case nameof(RentalRecordsModalsViewModel.IsReturnModalOpen):
                        if (vm.IsReturnModalOpen)
                            ModalAnimationHelper.AnimateIn(ReturnModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(ReturnModalBorder);
                        break;
                    case nameof(RentalRecordsModalsViewModel.IsViewModalOpen):
                        if (vm.IsViewModalOpen)
                            ModalAnimationHelper.AnimateIn(ViewModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(ViewModalBorder);
                        break;
                }
            };
        }
    }
}
