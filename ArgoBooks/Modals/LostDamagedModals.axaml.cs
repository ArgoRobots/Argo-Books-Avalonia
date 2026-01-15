using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Lost/Damaged modals.
/// </summary>
public partial class LostDamagedModals : UserControl
{
    private bool _eventsSubscribed;

    public LostDamagedModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is LostDamagedModalsViewModel vm)
        {
            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(LostDamagedModalsViewModel.IsFilterModalOpen))
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
