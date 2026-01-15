using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for viewing and managing stock level records.
/// </summary>
public partial class StockLevelsModals : UserControl
{
    private bool _eventsSubscribed;

    public StockLevelsModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is StockLevelsModalsViewModel vm && !_eventsSubscribed)
        {
            _eventsSubscribed = true;
            vm.PropertyChanged += (_, args) =>
            {
                switch (args.PropertyName)
                {
                    case nameof(StockLevelsModalsViewModel.IsAdjustStockModalOpen):
                        if (vm.IsAdjustStockModalOpen)
                            ModalAnimationHelper.AnimateIn(AdjustStockModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AdjustStockModalBorder);
                        break;
                    case nameof(StockLevelsModalsViewModel.IsAddItemModalOpen):
                        if (vm.IsAddItemModalOpen)
                            ModalAnimationHelper.AnimateIn(AddItemModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddItemModalBorder);
                        break;
                    case nameof(StockLevelsModalsViewModel.IsFilterModalOpen):
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
