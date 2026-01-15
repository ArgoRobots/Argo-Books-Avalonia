using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing revenue/sales records.
/// </summary>
public partial class RevenueModals : UserControl
{
    private bool _eventsSubscribed;

    public RevenueModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is RevenueModalsViewModel vm)
        {
            vm.ScrollToLineItemsRequested += OnScrollToLineItemsRequested;

            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(RevenueModalsViewModel.IsAddEditModalOpen))
                    {
                        if (vm.IsAddEditModalOpen)
                            ModalAnimationHelper.AnimateIn(AddEditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddEditModalBorder);
                    }
                    else if (args.PropertyName == nameof(RevenueModalsViewModel.IsFilterModalOpen))
                    {
                        if (vm.IsFilterModalOpen)
                            ModalAnimationHelper.AnimateIn(FilterModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(FilterModalBorder);
                    }
                    else if (args.PropertyName == nameof(RevenueModalsViewModel.IsItemStatusModalOpen))
                    {
                        if (vm.IsItemStatusModalOpen)
                            ModalAnimationHelper.AnimateIn(ItemStatusModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(ItemStatusModalBorder);
                    }
                };
            }
        }
    }

    private void OnScrollToLineItemsRequested(object? sender, EventArgs e)
    {
        // Scroll to bring the line items section into view
        LineItemsSection?.BringIntoView();
    }
}
