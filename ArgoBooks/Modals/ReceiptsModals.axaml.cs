using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Code-behind for the Receipts modals.
/// </summary>
public partial class ReceiptsModals : UserControl
{
    private bool _eventsSubscribed;

    public ReceiptsModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ReceiptsModalsViewModel vm)
        {
            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ReceiptsModalsViewModel.IsFilterModalOpen))
                    {
                        if (vm.IsFilterModalOpen)
                            ModalAnimationHelper.AnimateIn(FilterModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(FilterModalBorder);
                    }
                    else if (args.PropertyName == nameof(ReceiptsModalsViewModel.IsScanReviewModalOpen))
                    {
                        if (vm.IsScanReviewModalOpen)
                            ModalAnimationHelper.AnimateIn(ScanReviewModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(ScanReviewModalBorder);
                    }
                };
            }
        }
    }
}
