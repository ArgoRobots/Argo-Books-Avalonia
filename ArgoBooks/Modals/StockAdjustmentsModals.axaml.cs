using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Stock Adjustments modals.
/// </summary>
public partial class StockAdjustmentsModals : UserControl
{
    private bool _eventsSubscribed;

    public StockAdjustmentsModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is StockAdjustmentsModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(StockAdjustmentsModalsViewModel.IsAddModalOpen):
                        if (vm.IsAddModalOpen)
                            ModalAnimationHelper.AnimateIn(AddModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddModalBorder);
                        break;
                    case nameof(StockAdjustmentsModalsViewModel.IsViewModalOpen):
                        if (vm.IsViewModalOpen)
                            ModalAnimationHelper.AnimateIn(ViewModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(ViewModalBorder);
                        break;
                    case nameof(StockAdjustmentsModalsViewModel.IsFilterModalOpen):
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
