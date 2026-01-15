using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing supplier records.
/// </summary>
public partial class SupplierModals : UserControl
{
    private bool _eventsSubscribed;

    public SupplierModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SupplierModalsViewModel vm)
        {
            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(SupplierModalsViewModel.IsAddModalOpen))
                    {
                        if (vm.IsAddModalOpen)
                            ModalAnimationHelper.AnimateIn(AddModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddModalBorder);
                    }
                    else if (args.PropertyName == nameof(SupplierModalsViewModel.IsEditModalOpen))
                    {
                        if (vm.IsEditModalOpen)
                            ModalAnimationHelper.AnimateIn(EditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(EditModalBorder);
                    }
                };
            }
        }
    }
}
