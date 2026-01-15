using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing category records.
/// </summary>
public partial class CategoryModals : UserControl
{
    private bool _eventsSubscribed;

    public CategoryModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is CategoryModalsViewModel vm)
        {
            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(CategoryModalsViewModel.IsAddModalOpen))
                    {
                        if (vm.IsAddModalOpen)
                            ModalAnimationHelper.AnimateIn(AddModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddModalBorder);
                    }
                    else if (args.PropertyName == nameof(CategoryModalsViewModel.IsEditModalOpen))
                    {
                        if (vm.IsEditModalOpen)
                            ModalAnimationHelper.AnimateIn(EditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(EditModalBorder);
                    }
                    else if (args.PropertyName == nameof(CategoryModalsViewModel.IsMoveModalOpen))
                    {
                        if (vm.IsMoveModalOpen)
                            ModalAnimationHelper.AnimateIn(MoveModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(MoveModalBorder);
                    }
                };
            }
        }
    }
}
