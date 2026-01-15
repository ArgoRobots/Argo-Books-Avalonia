using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing storage location records.
/// </summary>
public partial class LocationsModals : UserControl
{
    private bool _eventsSubscribed;

    public LocationsModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LocationsModalsViewModel vm)
        {
            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(LocationsModalsViewModel.IsAddModalOpen))
                    {
                        if (vm.IsAddModalOpen)
                            ModalAnimationHelper.AnimateIn(AddModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddModalBorder);
                    }
                    else if (args.PropertyName == nameof(LocationsModalsViewModel.IsEditModalOpen))
                    {
                        if (vm.IsEditModalOpen)
                            ModalAnimationHelper.AnimateIn(EditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(EditModalBorder);
                    }
                    else if (args.PropertyName == nameof(LocationsModalsViewModel.IsFilterModalOpen))
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
}
