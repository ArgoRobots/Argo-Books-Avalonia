using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing department records.
/// </summary>
public partial class DepartmentModals : UserControl
{
    private bool _eventsSubscribed;

    public DepartmentModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is DepartmentModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(DepartmentModalsViewModel.IsAddModalOpen))
                {
                    if (vm.IsAddModalOpen)
                        ModalAnimationHelper.AnimateIn(AddModalBorder);
                    else
                        ModalAnimationHelper.AnimateOut(AddModalBorder);
                }
                else if (args.PropertyName == nameof(DepartmentModalsViewModel.IsEditModalOpen))
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
