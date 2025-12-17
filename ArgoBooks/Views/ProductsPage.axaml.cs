using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Products/Services page.
/// </summary>
public partial class ProductsPage : UserControl
{
    public ProductsPage()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle clicking on the Add modal backdrop to close it.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ProductsPageViewModel vm)
        {
            vm.CloseAddModalCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handle clicking on the Edit modal backdrop to close it.
    /// </summary>
    private void EditBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ProductsPageViewModel vm)
        {
            vm.CloseEditModalCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handle clicking on the Delete modal backdrop to close it.
    /// </summary>
    private void DeleteBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ProductsPageViewModel vm)
        {
            vm.CloseDeleteConfirmCommand.Execute(null);
        }
    }

    /// <summary>
    /// Handle clicking on the Filter modal backdrop to close it.
    /// </summary>
    private void FilterBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ProductsPageViewModel vm)
        {
            vm.CloseFilterModalCommand.Execute(null);
        }
    }
}
