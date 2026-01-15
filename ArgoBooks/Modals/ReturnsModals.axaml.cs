using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Returns modals.
/// </summary>
public partial class ReturnsModals : UserControl
{
    private bool _eventsSubscribed;

    public ReturnsModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ReturnsModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(ReturnsModalsViewModel.IsFilterModalOpen))
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
