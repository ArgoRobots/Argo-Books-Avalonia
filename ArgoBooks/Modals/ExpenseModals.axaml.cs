using Avalonia.Controls;
using ArgoBooks.Helpers;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Modals;

/// <summary>
/// Modal dialogs for creating and editing expense records.
/// </summary>
public partial class ExpenseModals : UserControl
{
    private bool _eventsSubscribed;

    public ExpenseModals()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ExpenseModalsViewModel vm)
        {
            vm.ScrollToLineItemsRequested += OnScrollToLineItemsRequested;

            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                vm.PropertyChanged += (_, args) =>
                {
                    if (args.PropertyName == nameof(ExpenseModalsViewModel.IsAddEditModalOpen))
                    {
                        if (vm.IsAddEditModalOpen)
                            ModalAnimationHelper.AnimateIn(AddEditModalBorder);
                        else
                            ModalAnimationHelper.AnimateOut(AddEditModalBorder);
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
