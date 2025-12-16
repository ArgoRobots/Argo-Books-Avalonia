using Avalonia.Controls;
using Avalonia.Input;
using ArgoBooks.ViewModels;

namespace ArgoBooks.Views;

/// <summary>
/// Code-behind for the Suppliers page.
/// </summary>
public partial class SuppliersPage : UserControl
{
    public SuppliersPage()
    {
        InitializeComponent();
    }

    private SuppliersPageViewModel? ViewModel => DataContext as SuppliersPageViewModel;

    /// <summary>
    /// Closes the Add modal when backdrop is clicked.
    /// </summary>
    private void Backdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseAddModalCommand.Execute(null);
    }

    /// <summary>
    /// Closes the Edit modal when backdrop is clicked.
    /// </summary>
    private void EditBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseEditModalCommand.Execute(null);
    }

    /// <summary>
    /// Closes the Delete confirmation when backdrop is clicked.
    /// </summary>
    private void DeleteBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseDeleteConfirmCommand.Execute(null);
    }

    /// <summary>
    /// Closes the Filter modal when backdrop is clicked.
    /// </summary>
    private void FilterBackdrop_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        ViewModel?.CloseFilterModalCommand.Execute(null);
    }
}
